using Shared.Models;

namespace CopilotAgent.Services;

public interface ISecurityAuditService
{
    Task LogEventAsync(string eventName, string? userId = null, string? repository = null, string? action = null, string? result = null, object? details = null);
    Task LogWebhookEventAsync(GitHubWebhookPayload payload, string result);
    Task LogAuthenticationEventAsync(string result, string? error = null);
}

public class SecurityAuditService : ISecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(ILogger<SecurityAuditService> logger)
    {
        _logger = logger;
    }

    public async Task LogEventAsync(string eventName, string? userId = null, string? repository = null, string? action = null, string? result = null, object? details = null)
    {
        var auditLog = new SecurityAuditLog
        {
            Event = eventName,
            UserId = userId,
            Repository = repository,
            Action = action,
            Result = result,
            Details = details != null ? ConvertToStringDictionary(details) : new Dictionary<string, object>()
        };

        _logger.LogInformation("SECURITY_AUDIT: {Event} | User: {User} | Repo: {Repository} | Action: {Action} | Result: {Result}",
            auditLog.Event, auditLog.UserId ?? "system", auditLog.Repository ?? "none", auditLog.Action ?? "none", auditLog.Result ?? "none");

        // In a production environment, this would also write to a dedicated security audit log store
        // For now, we're using structured logging which can be collected by monitoring systems
        await Task.CompletedTask;
    }

    public async Task LogWebhookEventAsync(GitHubWebhookPayload payload, string result)
    {
        await LogEventAsync(
            eventName: "GitHub_Webhook_Received",
            userId: payload.Sender?.Login,
            repository: payload.Repository?.FullName,
            action: payload.Action,
            result: result,
            details: new
            {
                Event = payload.Event,
                InstallationId = payload.Installation?.Id,
                SenderType = payload.Sender?.Type,
                RepositoryPrivate = payload.Repository?.Private
            });
    }

    public async Task LogAuthenticationEventAsync(string result, string? error = null)
    {
        await LogEventAsync(
            eventName: "GitHub_App_Authentication",
            action: "GetToken",
            result: result,
            details: new
            {
                Error = error,
                Timestamp = DateTime.UtcNow
            });
    }

    private static Dictionary<string, object> ConvertToStringDictionary(object obj)
    {
        var dict = new Dictionary<string, object>();
        
        if (obj == null) return dict;

        var properties = obj.GetType().GetProperties();
        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj);
            if (value != null)
            {
                dict[prop.Name] = value;
            }
        }

        return dict;
    }
}