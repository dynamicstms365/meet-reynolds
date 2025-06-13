using Shared.Models;

namespace CopilotAgent.Services;

public interface IEnvironmentManager
{
    Task<EnvironmentResult> CreateEnvironmentAsync(EnvironmentSpec spec);
    Task<List<Environment>> ListEnvironmentsAsync();
    Task<bool> ValidateEnvironmentAsync(string environmentName);
    Task<EnvironmentResult> GetEnvironmentDetailsAsync(string environmentName);
}

public interface ICliExecutor
{
    Task<CliResult> ExecuteAsync(string command, CliOptions options);
}

public interface ICodeGenerator
{
    Task<CodeGenerationResult> GenerateCodeAsync(CodeSpec spec);
}

public interface IKnowledgeRetriever
{
    Task<KnowledgeResult> RetrieveKnowledgeAsync(string query);
}

public interface IPacCliValidator
{
    bool IsCommandSafe(string command);
    Task<ValidationResult> ValidateCommandAsync(string command);
}

public interface IM365CliValidator
{
    bool IsCommandSafe(string command);
    Task<ValidationResult> ValidateCommandAsync(string command);
}

public interface IPacCliService
{
    Task<CliResult> ExecuteAsync(string command);
}

public interface IM365CliService
{
    Task<CliResult> ExecuteAsync(string command);
}

// Supporting classes
public class Environment
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}

public class CodeGenerationResult
{
    public bool Success { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Error { get; set; }
    public string[] Files { get; set; } = Array.Empty<string>();
}

public class KnowledgeResult
{
    public string Answer { get; set; } = string.Empty;
    public string[] Sources { get; set; } = Array.Empty<string>();
    public double Confidence { get; set; }
}

public class CodeSpec
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
}

public class CliOptions
{
    public int TimeoutSeconds { get; set; } = 30;
    public string WorkingDirectory { get; set; } = string.Empty;
    public Dictionary<string, string> Environment { get; set; } = new();
}

// Stakeholder Visibility Interfaces
public interface IStakeholderVisibilityService
{
    Task<StakeholderConfiguration> CreateStakeholderAsync(StakeholderConfiguration stakeholder);
    Task<StakeholderConfiguration> UpdateStakeholderAsync(string stakeholderId, StakeholderConfiguration stakeholder);
    Task<bool> DeleteStakeholderAsync(string stakeholderId);
    Task<StakeholderConfiguration?> GetStakeholderAsync(string stakeholderId);
    Task<IEnumerable<StakeholderConfiguration>> GetAllStakeholdersAsync();
    Task<IEnumerable<StakeholderConfiguration>> GetStakeholdersByRepositoryAsync(string repository);
}

public interface IDashboardService
{
    Task<ProjectProgressSummary> GenerateProjectSummaryAsync(string repository, string? stakeholderId = null);
    Task<ProjectProgressSummary> GenerateOrganizationSummaryAsync(string organization, string? stakeholderId = null);
    Task<DashboardConfiguration> GetDashboardConfigurationAsync(string stakeholderId);
    Task<DashboardConfiguration> UpdateDashboardConfigurationAsync(string stakeholderId, DashboardConfiguration config);
    Task<Dictionary<string, object>> GetWidgetDataAsync(string stakeholderId, string widgetId);
}

public interface INotificationService
{
    Task<StakeholderNotification> CreateNotificationAsync(StakeholderNotification notification);
    Task<bool> SendNotificationAsync(string notificationId);
    Task<IEnumerable<StakeholderNotification>> GetPendingNotificationsAsync();
    Task<IEnumerable<StakeholderNotification>> GetNotificationsByStakeholderAsync(string stakeholderId, int limit = 50);
    Task ProcessScheduledNotificationsAsync();
    Task<bool> NotifyStakeholdersAsync(string repository, NotificationType type, object data);
}