using Shared.Models;

namespace CopilotAgent.Services;

public interface ICliRollbackService
{
    Task<bool> CanRollbackAsync(string operationType, string operationId);
    Task<CliResult> ExecuteRollbackAsync(string operationType, string operationId, Dictionary<string, object> operationContext);
    Task RegisterOperationAsync(string operationType, string operationId, Dictionary<string, object> rollbackContext);
}

public class CliRollbackService : ICliRollbackService
{
    private readonly ILogger<CliRollbackService> _logger;
    private readonly ISecurityAuditService _auditService;
    private readonly IPacCliService _pacCliService;
    private readonly IM365CliService _m365CliService;
    
    // In-memory store for rollback information (in production, this would be persistent storage)
    private readonly Dictionary<string, RollbackInfo> _rollbackRegistry = new();

    public CliRollbackService(
        ILogger<CliRollbackService> logger,
        ISecurityAuditService auditService,
        IPacCliService pacCliService,
        IM365CliService m365CliService)
    {
        _logger = logger;
        _auditService = auditService;
        _pacCliService = pacCliService;
        _m365CliService = m365CliService;
    }

    public async Task<bool> CanRollbackAsync(string operationType, string operationId)
    {
        await Task.CompletedTask;
        
        var key = $"{operationType}:{operationId}";
        if (!_rollbackRegistry.ContainsKey(key))
        {
            return false;
        }

        var rollbackInfo = _rollbackRegistry[key];
        
        // Check if rollback is still possible (time-based, state-based, etc.)
        var isWithinRollbackWindow = DateTime.UtcNow - rollbackInfo.CreatedAt < TimeSpan.FromHours(24);
        var isRollbackable = GetSupportedRollbackOperations().Contains(operationType.ToLowerInvariant());
        
        return isWithinRollbackWindow && isRollbackable && !rollbackInfo.HasBeenRolledBack;
    }

    public async Task<CliResult> ExecuteRollbackAsync(string operationType, string operationId, Dictionary<string, object> operationContext)
    {
        var key = $"{operationType}:{operationId}";
        
        try
        {
            _logger.LogInformation("Executing rollback for operation: {OperationType}:{OperationId}", operationType, operationId);

            if (!await CanRollbackAsync(operationType, operationId))
            {
                var error = "Rollback not available for this operation";
                await _auditService.LogEventAsync("CLI_Rollback", result: "Failed", details: new { Error = error, OperationType = operationType, OperationId = operationId });
                return CliResult.CreateFailure(error);
            }

            var rollbackInfo = _rollbackRegistry[key];
            var rollbackCommand = GenerateRollbackCommand(operationType, rollbackInfo.RollbackContext, operationContext);
            
            if (string.IsNullOrEmpty(rollbackCommand))
            {
                var error = "No rollback command available for this operation type";
                await _auditService.LogEventAsync("CLI_Rollback", result: "Failed", details: new { Error = error, OperationType = operationType, OperationId = operationId });
                return CliResult.CreateFailure(error);
            }

            // Execute the rollback command
            CliResult result;
            if (rollbackCommand.StartsWith("pac "))
            {
                result = await _pacCliService.ExecuteAsync(rollbackCommand);
            }
            else if (rollbackCommand.StartsWith("m365 "))
            {
                result = await _m365CliService.ExecuteAsync(rollbackCommand);
            }
            else
            {
                var error = "Unsupported rollback command format";
                await _auditService.LogEventAsync("CLI_Rollback", result: "Failed", details: new { Error = error, Command = rollbackCommand });
                return CliResult.CreateFailure(error);
            }

            if (result.Success)
            {
                rollbackInfo.HasBeenRolledBack = true;
                rollbackInfo.RolledBackAt = DateTime.UtcNow;
                
                _logger.LogInformation("Rollback completed successfully for operation: {OperationType}:{OperationId}", operationType, operationId);
                await _auditService.LogEventAsync("CLI_Rollback", result: "Success", details: new { OperationType = operationType, OperationId = operationId, Command = rollbackCommand });
            }
            else
            {
                _logger.LogWarning("Rollback failed for operation: {OperationType}:{OperationId} - {Error}", operationType, operationId, result.Error);
                await _auditService.LogEventAsync("CLI_Rollback", result: "Failed", details: new { OperationType = operationType, OperationId = operationId, Command = rollbackCommand, Error = result.Error });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during rollback execution: {OperationType}:{OperationId}", operationType, operationId);
            await _auditService.LogEventAsync("CLI_Rollback", result: "Exception", details: new { OperationType = operationType, OperationId = operationId, Error = ex.Message });
            return CliResult.CreateFailure(ex.Message);
        }
    }

    public async Task RegisterOperationAsync(string operationType, string operationId, Dictionary<string, object> rollbackContext)
    {
        await Task.CompletedTask;
        
        var key = $"{operationType}:{operationId}";
        var rollbackInfo = new RollbackInfo
        {
            OperationType = operationType,
            OperationId = operationId,
            RollbackContext = rollbackContext,
            CreatedAt = DateTime.UtcNow
        };

        _rollbackRegistry[key] = rollbackInfo;
        
        _logger.LogDebug("Registered rollback information for operation: {OperationType}:{OperationId}", operationType, operationId);
        await _auditService.LogEventAsync("CLI_Rollback_Register", result: "Success", details: new { OperationType = operationType, OperationId = operationId });
    }

    private string? GenerateRollbackCommand(string operationType, Dictionary<string, object> rollbackContext, Dictionary<string, object> operationContext)
    {
        return operationType.ToLowerInvariant() switch
        {
            "environment_create" => GenerateEnvironmentDeleteCommand(rollbackContext),
            "solution_import" => GenerateSolutionDeleteCommand(rollbackContext),
            "app_install" => GenerateAppUninstallCommand(rollbackContext),
            "m365_app_create" => GenerateM365AppDeleteCommand(rollbackContext),
            "sharepoint_site_create" => GenerateSharePointSiteDeleteCommand(rollbackContext),
            _ => null
        };
    }

    private string? GenerateEnvironmentDeleteCommand(Dictionary<string, object> context)
    {
        if (context.TryGetValue("environmentName", out var envName))
        {
            // Note: Environment deletion is typically not recommended for rollback
            // This is a placeholder - in practice, you might want to disable the environment instead
            return $"pac env select --name \"{envName}\" && pac env delete --confirm";
        }
        return null;
    }

    private string? GenerateSolutionDeleteCommand(Dictionary<string, object> context)
    {
        if (context.TryGetValue("solutionName", out var solutionName))
        {
            return $"pac solution delete --name \"{solutionName}\"";
        }
        return null;
    }

    private string? GenerateAppUninstallCommand(Dictionary<string, object> context)
    {
        if (context.TryGetValue("appId", out var appId))
        {
            return $"pac application uninstall --application-id \"{appId}\"";
        }
        return null;
    }

    private string? GenerateM365AppDeleteCommand(Dictionary<string, object> context)
    {
        if (context.TryGetValue("appId", out var appId))
        {
            return $"m365 app remove --appId \"{appId}\"";
        }
        return null;
    }

    private string? GenerateSharePointSiteDeleteCommand(Dictionary<string, object> context)
    {
        if (context.TryGetValue("siteUrl", out var siteUrl))
        {
            return $"m365 spo site remove --url \"{siteUrl}\"";
        }
        return null;
    }

    private static HashSet<string> GetSupportedRollbackOperations()
    {
        return new HashSet<string>
        {
            "environment_create",
            "solution_import", 
            "app_install",
            "m365_app_create",
            "sharepoint_site_create"
        };
    }
}

public class RollbackInfo
{
    public string OperationType { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public Dictionary<string, object> RollbackContext { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public bool HasBeenRolledBack { get; set; }
    public DateTime? RolledBackAt { get; set; }
}