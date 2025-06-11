using Shared.Models;
using System.Diagnostics;

namespace CopilotAgent.Services;

public class PacCliValidator : IPacCliValidator
{
    private readonly HashSet<string> _allowedCommands = new()
    {
        "pac auth list",
        "pac auth create",
        "pac auth select",
        "pac auth clear",
        "pac env list",
        "pac env create",
        "pac env who",
        "pac solution list",
        "pac solution export",
        "pac solution import",
        "pac solution clone",
        "pac application list",
        "pac help"
    };

    private readonly ILogger<PacCliValidator> _logger;
    private readonly ISecurityAuditService _auditService;

    public PacCliValidator(ILogger<PacCliValidator> logger, ISecurityAuditService auditService)
    {
        _logger = logger;
        _auditService = auditService;
    }

    public bool IsCommandSafe(string command)
    {
        var baseCommand = ExtractBaseCommand(command);
        return _allowedCommands.Contains(baseCommand);
    }

    public async Task<ValidationResult> ValidateCommandAsync(string command)
    {
        var userId = GetCurrentUserId(); // This would be injected from context in a real implementation

        try
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                await _auditService.LogCliValidationAsync("pac", command, userId, false, "Command cannot be empty");
                return ValidationResult.CreateFailure("Command cannot be empty");
            }

            if (!command.TrimStart().StartsWith("pac "))
            {
                await _auditService.LogCliValidationAsync("pac", command, userId, false, "Only pac commands are allowed");
                return ValidationResult.CreateFailure("Only pac commands are allowed");
            }

            var baseCommand = ExtractBaseCommand(command);
            if (!IsCommandSafe(command))
            {
                _logger.LogWarning("Unsafe command attempted: {Command}", baseCommand);
                await _auditService.LogCliValidationAsync("pac", command, userId, false, $"Command not allowed: {baseCommand}");
                return ValidationResult.CreateFailure($"Command not allowed: {baseCommand}");
            }

            // Check for dangerous parameters
            if (ContainsDangerousParameters(command))
            {
                await _auditService.LogCliValidationAsync("pac", command, userId, false, "Command contains potentially dangerous parameters");
                return ValidationResult.CreateFailure("Command contains potentially dangerous parameters");
            }

            // Check user permissions (placeholder - would integrate with actual permission system)
            var hasPermission = await CheckUserPermissionsAsync(userId, baseCommand);
            if (!hasPermission)
            {
                await _auditService.LogCliValidationAsync("pac", command, userId, false, "Insufficient permissions");
                return ValidationResult.CreateFailure("Insufficient permissions for this operation");
            }

            await _auditService.LogCliValidationAsync("pac", command, userId, true);
            return ValidationResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PAC CLI command");
            await _auditService.LogCliValidationAsync("pac", command, userId, false, $"Validation error: {ex.Message}");
            return ValidationResult.CreateFailure($"Validation error: {ex.Message}");
        }
    }

    private string ExtractBaseCommand(string command)
    {
        var parts = command.Trim().Split(' ');
        if (parts.Length >= 3)
        {
            return $"{parts[0]} {parts[1]} {parts[2]}";
        }
        if (parts.Length >= 2)
        {
            return $"{parts[0]} {parts[1]}";
        }
        return command.Trim();
    }

    private bool ContainsDangerousParameters(string command)
    {
        var dangerousParams = new[] { "--force", "--delete", "--reset", "--remove" };
        var lowerCommand = command.ToLowerInvariant();
        
        return dangerousParams.Any(param => lowerCommand.Contains(param));
    }

    private async Task<bool> CheckUserPermissionsAsync(string? userId, string command)
    {
        // Placeholder for actual permission checking logic
        // In a real implementation, this would check against user roles, permissions, etc.
        await Task.CompletedTask;
        
        // For now, allow all authenticated users for non-destructive operations
        if (string.IsNullOrEmpty(userId))
        {
            return false; // No user context
        }

        // Check for high-privilege operations that require admin access
        var highPrivilegeCommands = new[] { "pac env create", "pac solution import", "pac application install" };
        if (highPrivilegeCommands.Any(cmd => command.StartsWith(cmd)))
        {
            // In a real implementation, check if user has admin privileges
            // For now, log the attempt and allow (since this is a foundation implementation)
            _logger.LogInformation("High-privilege command attempted by user {UserId}: {Command}", userId, command);
        }

        return true; // Allow for foundation implementation
    }

    private static string? GetCurrentUserId()
    {
        // In a real implementation, this would be injected from the current context
        // For now, return a placeholder
        return "system";
    }
}

public class M365CliValidator : IM365CliValidator
{
    private readonly HashSet<string> _allowedCommands = new()
    {
        "m365 login",
        "m365 logout",
        "m365 status",
        "m365 app list",
        "m365 app get",
        "m365 app add",
        "m365 app set",
        "m365 app permission",
        "m365 spo site",
        "m365 spo list",
        "m365 teams list",
        "m365 teams get",
        "m365 graph get",
        "m365 help"
    };

    private readonly ILogger<M365CliValidator> _logger;
    private readonly ISecurityAuditService _auditService;

    public M365CliValidator(ILogger<M365CliValidator> logger, ISecurityAuditService auditService)
    {
        _logger = logger;
        _auditService = auditService;
    }

    public bool IsCommandSafe(string command)
    {
        var baseCommand = ExtractBaseCommand(command);
        return _allowedCommands.Any(allowed => baseCommand.StartsWith(allowed));
    }

    public async Task<ValidationResult> ValidateCommandAsync(string command)
    {
        var userId = GetCurrentUserId(); // This would be injected from context in a real implementation

        try
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                await _auditService.LogCliValidationAsync("m365", command, userId, false, "Command cannot be empty");
                return ValidationResult.CreateFailure("Command cannot be empty");
            }

            if (!command.TrimStart().StartsWith("m365 "))
            {
                await _auditService.LogCliValidationAsync("m365", command, userId, false, "Only m365 commands are allowed");
                return ValidationResult.CreateFailure("Only m365 commands are allowed");
            }

            if (!IsCommandSafe(command))
            {
                var baseCommand = ExtractBaseCommand(command);
                _logger.LogWarning("Unsafe command attempted: {Command}", baseCommand);
                await _auditService.LogCliValidationAsync("m365", command, userId, false, $"Command not allowed: {baseCommand}");
                return ValidationResult.CreateFailure($"Command not allowed: {baseCommand}");
            }

            // Check for dangerous operations
            if (ContainsDangerousOperations(command))
            {
                await _auditService.LogCliValidationAsync("m365", command, userId, false, "Command contains potentially dangerous operations");
                return ValidationResult.CreateFailure("Command contains potentially dangerous operations");
            }

            // Check user permissions (placeholder - would integrate with actual permission system)
            var hasPermission = await CheckUserPermissionsAsync(userId, ExtractBaseCommand(command));
            if (!hasPermission)
            {
                await _auditService.LogCliValidationAsync("m365", command, userId, false, "Insufficient permissions");
                return ValidationResult.CreateFailure("Insufficient permissions for this operation");
            }

            await _auditService.LogCliValidationAsync("m365", command, userId, true);
            return ValidationResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating M365 CLI command");
            await _auditService.LogCliValidationAsync("m365", command, userId, false, $"Validation error: {ex.Message}");
            return ValidationResult.CreateFailure($"Validation error: {ex.Message}");
        }
    }

    private string ExtractBaseCommand(string command)
    {
        var parts = command.Trim().Split(' ');
        if (parts.Length >= 3)
        {
            return $"{parts[0]} {parts[1]} {parts[2]}";
        }
        if (parts.Length >= 2)
        {
            return $"{parts[0]} {parts[1]}";
        }
        return command.Trim();
    }

    private bool ContainsDangerousOperations(string command)
    {
        var dangerousOps = new[] { "delete", "remove", "clear", "--force" };
        var lowerCommand = command.ToLowerInvariant();
        
        return dangerousOps.Any(op => lowerCommand.Contains(op));
    }

    private async Task<bool> CheckUserPermissionsAsync(string? userId, string command)
    {
        // Placeholder for actual permission checking logic
        // In a real implementation, this would check against user roles, permissions, etc.
        await Task.CompletedTask;
        
        // For now, allow all authenticated users for non-destructive operations
        if (string.IsNullOrEmpty(userId))
        {
            return false; // No user context
        }

        // Check for high-privilege operations that require admin access
        var highPrivilegeCommands = new[] { "m365 app add", "m365 app set", "m365 teams" };
        if (highPrivilegeCommands.Any(cmd => command.StartsWith(cmd)))
        {
            // In a real implementation, check if user has admin privileges
            // For now, log the attempt and allow (since this is a foundation implementation)
            _logger.LogInformation("High-privilege command attempted by user {UserId}: {Command}", userId, command);
        }

        return true; // Allow for foundation implementation
    }

    private static string? GetCurrentUserId()
    {
        // In a real implementation, this would be injected from the current context
        // For now, return a placeholder
        return "system";
    }
}

public class PacCliService : IPacCliService
{
    private readonly ILogger<PacCliService> _logger;
    private readonly ISecurityAuditService _auditService;
    private readonly ICliMonitoringService _monitoringService;
    private readonly RetryOptions _retryOptions;

    public PacCliService(
        ILogger<PacCliService> logger, 
        ISecurityAuditService auditService,
        ICliMonitoringService monitoringService)
    {
        _logger = logger;
        _auditService = auditService;
        _monitoringService = monitoringService;
        _retryOptions = new RetryOptions
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromSeconds(1),
            BackoffMultiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(30),
            ShouldRetry = ex => ex is not UnauthorizedAccessException && ex is not ArgumentException
        };
    }

    public async Task<CliResult> ExecuteAsync(string command)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var sanitizedCommand = SanitizeCommand(command);
        var userId = GetCurrentUserId(); // This would be injected from context in a real implementation
        var retryCount = 0;
        
        try
        {
            _logger.LogInformation("Executing PAC CLI command: {Command}", sanitizedCommand);

            // Execute with retry logic
            var result = await ExecuteWithRetryAsync(command, userId);
            retryCount = result.ExitCode == -1 ? _retryOptions.MaxRetries : 0; // Track retries
            
            stopwatch.Stop();
            
            // Record monitoring metrics
            await _monitoringService.RecordOperationAsync(new CliOperationMetrics
            {
                CliTool = "pac",
                Command = sanitizedCommand,
                ExecutionTime = stopwatch.Elapsed,
                Success = result.Success,
                Error = result.Error,
                RetryCount = retryCount
            });
            
            // Log operation metrics
            await _auditService.LogCliOperationAsync(
                "pac", 
                command, 
                userId, 
                result.Success ? "Success" : "Failed", 
                stopwatch.Elapsed,
                result.Error);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Exception executing PAC CLI command: {Command}", sanitizedCommand);
            
            // Record monitoring metrics for exception
            await _monitoringService.RecordOperationAsync(new CliOperationMetrics
            {
                CliTool = "pac",
                Command = sanitizedCommand,
                ExecutionTime = stopwatch.Elapsed,
                Success = false,
                Error = ex.Message,
                RetryCount = retryCount
            });
            
            await _auditService.LogCliOperationAsync(
                "pac", 
                command, 
                userId, 
                "Exception", 
                stopwatch.Elapsed,
                ex.Message);
                
            return CliResult.CreateFailure(ex.Message);
        }
    }

    private async Task<CliResult> ExecuteWithRetryAsync(string command, string? userId)
    {
        var attempt = 0;
        var delay = _retryOptions.InitialDelay;

        while (attempt <= _retryOptions.MaxRetries)
        {
            attempt++;

            try
            {
                var result = await ExecuteCommandAsync(command);
                
                if (result.Success || attempt > _retryOptions.MaxRetries)
                {
                    return result;
                }

                // Log retry attempt
                await _auditService.LogCliRetryAsync("pac", command, userId, attempt, result.Error);
                
                _logger.LogWarning("PAC CLI command failed (attempt {Attempt}/{MaxRetries}): {Error}", 
                    attempt, _retryOptions.MaxRetries, result.Error);

                if (attempt <= _retryOptions.MaxRetries)
                {
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(Math.Min(
                        delay.TotalMilliseconds * _retryOptions.BackoffMultiplier,
                        _retryOptions.MaxDelay.TotalMilliseconds));
                }
            }
            catch (Exception ex) when (_retryOptions.ShouldRetry?.Invoke(ex) == true && attempt <= _retryOptions.MaxRetries)
            {
                await _auditService.LogCliRetryAsync("pac", command, userId, attempt, ex.Message);
                
                _logger.LogWarning(ex, "PAC CLI command exception (attempt {Attempt}/{MaxRetries})", 
                    attempt, _retryOptions.MaxRetries);

                if (attempt <= _retryOptions.MaxRetries)
                {
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(Math.Min(
                        delay.TotalMilliseconds * _retryOptions.BackoffMultiplier,
                        _retryOptions.MaxDelay.TotalMilliseconds));
                }
            }
            catch (Exception)
            {
                // Non-retryable exception
                throw;
            }
        }

        return CliResult.CreateFailure("Maximum retry attempts exceeded");
    }

    private async Task<CliResult> ExecuteCommandAsync(string command)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "pac",
            Arguments = command.Substring(4), // Remove "pac " prefix
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };
        
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait for process to complete with timeout
        var completed = await WaitForExitAsync(process, TimeSpan.FromSeconds(30));
        
        if (!completed)
        {
            process.Kill();
            return CliResult.CreateFailure("Command timed out");
        }

        var output = outputBuilder.ToString();
        var error = errorBuilder.ToString();
        var exitCode = process.ExitCode;

        _logger.LogInformation("PAC CLI command completed with exit code: {ExitCode}", exitCode);

        if (exitCode == 0)
        {
            return CliResult.CreateSuccess(output);
        }
        else
        {
            return new CliResult
            {
                Success = false,
                Output = output,
                Error = error,
                ExitCode = exitCode
            };
        }
    }

    private async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static string SanitizeCommand(string command)
    {
        // Remove sensitive information from logs
        return command
            .Replace("--secret", "--secret ***")
            .Replace("--password", "--password ***")
            .Replace("--token", "--token ***")
            .Replace("--clientSecret", "--clientSecret ***");
    }

    private static string? GetCurrentUserId()
    {
        // In a real implementation, this would be injected from the current context
        // For now, return a placeholder
        return "system";
    }
}

public class M365CliService : IM365CliService
{
    private readonly ILogger<M365CliService> _logger;
    private readonly ISecurityAuditService _auditService;
    private readonly ICliMonitoringService _monitoringService;
    private readonly RetryOptions _retryOptions;

    public M365CliService(
        ILogger<M365CliService> logger, 
        ISecurityAuditService auditService,
        ICliMonitoringService monitoringService)
    {
        _logger = logger;
        _auditService = auditService;
        _monitoringService = monitoringService;
        _retryOptions = new RetryOptions
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromSeconds(1),
            BackoffMultiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(30),
            ShouldRetry = ex => ex is not UnauthorizedAccessException && ex is not ArgumentException
        };
    }

    public async Task<CliResult> ExecuteAsync(string command)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var sanitizedCommand = SanitizeCommand(command);
        var userId = GetCurrentUserId(); // This would be injected from context in a real implementation
        var retryCount = 0;
        
        try
        {
            _logger.LogInformation("Executing M365 CLI command: {Command}", sanitizedCommand);

            // Execute with retry logic
            var result = await ExecuteWithRetryAsync(command, userId);
            retryCount = result.ExitCode == -1 ? _retryOptions.MaxRetries : 0; // Track retries
            
            stopwatch.Stop();
            
            // Record monitoring metrics
            await _monitoringService.RecordOperationAsync(new CliOperationMetrics
            {
                CliTool = "m365",
                Command = sanitizedCommand,
                ExecutionTime = stopwatch.Elapsed,
                Success = result.Success,
                Error = result.Error,
                RetryCount = retryCount
            });
            
            // Log operation metrics
            await _auditService.LogCliOperationAsync(
                "m365", 
                command, 
                userId, 
                result.Success ? "Success" : "Failed", 
                stopwatch.Elapsed,
                result.Error);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Exception executing M365 CLI command: {Command}", sanitizedCommand);
            
            // Record monitoring metrics for exception
            await _monitoringService.RecordOperationAsync(new CliOperationMetrics
            {
                CliTool = "m365",
                Command = sanitizedCommand,
                ExecutionTime = stopwatch.Elapsed,
                Success = false,
                Error = ex.Message,
                RetryCount = retryCount
            });
            
            await _auditService.LogCliOperationAsync(
                "m365", 
                command, 
                userId, 
                "Exception", 
                stopwatch.Elapsed,
                ex.Message);
                
            return CliResult.CreateFailure(ex.Message);
        }
    }

    private async Task<CliResult> ExecuteWithRetryAsync(string command, string? userId)
    {
        var attempt = 0;
        var delay = _retryOptions.InitialDelay;

        while (attempt <= _retryOptions.MaxRetries)
        {
            attempt++;

            try
            {
                var result = await ExecuteCommandAsync(command);
                
                if (result.Success || attempt > _retryOptions.MaxRetries)
                {
                    return result;
                }

                // Log retry attempt
                await _auditService.LogCliRetryAsync("m365", command, userId, attempt, result.Error);
                
                _logger.LogWarning("M365 CLI command failed (attempt {Attempt}/{MaxRetries}): {Error}", 
                    attempt, _retryOptions.MaxRetries, result.Error);

                if (attempt <= _retryOptions.MaxRetries)
                {
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(Math.Min(
                        delay.TotalMilliseconds * _retryOptions.BackoffMultiplier,
                        _retryOptions.MaxDelay.TotalMilliseconds));
                }
            }
            catch (Exception ex) when (_retryOptions.ShouldRetry?.Invoke(ex) == true && attempt <= _retryOptions.MaxRetries)
            {
                await _auditService.LogCliRetryAsync("m365", command, userId, attempt, ex.Message);
                
                _logger.LogWarning(ex, "M365 CLI command exception (attempt {Attempt}/{MaxRetries})", 
                    attempt, _retryOptions.MaxRetries);

                if (attempt <= _retryOptions.MaxRetries)
                {
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(Math.Min(
                        delay.TotalMilliseconds * _retryOptions.BackoffMultiplier,
                        _retryOptions.MaxDelay.TotalMilliseconds));
                }
            }
            catch (Exception)
            {
                // Non-retryable exception
                throw;
            }
        }

        return CliResult.CreateFailure("Maximum retry attempts exceeded");
    }

    private async Task<CliResult> ExecuteCommandAsync(string command)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "m365",
            Arguments = command.Substring(5), // Remove "m365 " prefix
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };
        
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait for process to complete with timeout
        var completed = await WaitForExitAsync(process, TimeSpan.FromSeconds(30));
        
        if (!completed)
        {
            process.Kill();
            return CliResult.CreateFailure("Command timed out");
        }

        var output = outputBuilder.ToString();
        var error = errorBuilder.ToString();
        var exitCode = process.ExitCode;

        _logger.LogInformation("M365 CLI command completed with exit code: {ExitCode}", exitCode);

        if (exitCode == 0)
        {
            return CliResult.CreateSuccess(output);
        }
        else
        {
            return new CliResult
            {
                Success = false,
                Output = output,
                Error = error,
                ExitCode = exitCode
            };
        }
    }

    private async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static string SanitizeCommand(string command)
    {
        // Remove sensitive information from logs
        return command
            .Replace("--secret", "--secret ***")
            .Replace("--password", "--password ***")
            .Replace("--token", "--token ***")
            .Replace("--clientSecret", "--clientSecret ***");
    }

    private static string? GetCurrentUserId()
    {
        // In a real implementation, this would be injected from the current context
        // For now, return a placeholder
        return "system";
    }
}