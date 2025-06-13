namespace Shared.Models;

public class AgentRequest
{
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
}

public class AgentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public IntentType? IntentType { get; set; }
}

public class AgentCapabilities
{
    public string[] SupportedIntents { get; set; } = Array.Empty<string>();
    public string[] SupportedCliTools { get; set; } = Array.Empty<string>();
    public string[] SupportedOperations { get; set; } = Array.Empty<string>();
}

public enum IntentType
{
    EnvironmentManagement,
    CliExecution,
    CodeGeneration,
    KnowledgeQuery,
    General
}

public class EnvironmentSpec
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class EnvironmentResult
{
    public bool Success { get; set; }
    public string? EnvironmentId { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class CliResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string? Error { get; set; }
    public int ExitCode { get; set; }

    public static CliResult CreateFailure(string error) => new() { Success = false, Error = error };
    public static CliResult CreateSuccess(string output) => new() { Success = true, Output = output };
}

public class ValidationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }

    public static ValidationResult CreateFailure(string error) => new() { Success = false, Error = error };
    public static ValidationResult CreateSuccess() => new() { Success = true };
}

// GitHub Integration Models
public class GitHubWebhookPayload
{
    public string Action { get; set; } = string.Empty;
    public string? Event { get; set; }
    public GitHubRepository? Repository { get; set; }
    public GitHubInstallation? Installation { get; set; }
    public GitHubUser? Sender { get; set; }
    public Dictionary<string, object>? ClientPayload { get; set; }
    public GitHubDiscussionPayload? Discussion { get; set; }
    public GitHubIssuePayload? Issue { get; set; }
    public GitHubCommentPayload? Comment { get; set; }
    public GitHubPullRequestPayload? PullRequest { get; set; }
}

public class GitHubDiscussionPayload
{
    public long Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public GitHubUser? Author { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GitHubIssuePayload
{
    public long Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public GitHubUser? User { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GitHubCommentPayload
{
    public long Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public GitHubUser? User { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GitHubPullRequestPayload
{
    public long Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public GitHubUser? User { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GitHubRepository
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Private { get; set; }
}

public class GitHubInstallation
{
    public long Id { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
}

public class GitHubUser
{
    public long Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class GitHubAppAuthentication
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string[] Permissions { get; set; } = Array.Empty<string>();
}

public class GitHubConnectivityResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? InstallationId { get; set; }
    public string[] Repositories { get; set; } = Array.Empty<string>();
    public string[] Permissions { get; set; } = Array.Empty<string>();
    public DateTime? TokenExpiresAt { get; set; }
}

public class SecurityAuditLog
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Event { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? Repository { get; set; }
    public string? Action { get; set; }
    public string? Result { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

public class CliOperationMetrics
{
    public string CliTool { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class RetryOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
    public double BackoffMultiplier { get; set; } = 2.0;
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public Func<Exception, bool>? ShouldRetry { get; set; }
}

public class CliMonitoringOptions
{
    public bool EnablePerformanceTracking { get; set; } = true;
    public bool EnableSuccessRateTracking { get; set; } = true;
    public double SuccessRateThreshold { get; set; } = 0.98;
    public TimeSpan MonitoringWindow { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableAlerting { get; set; } = true;
}

// Scope Creep Monitoring Models
public class ProjectScopeParameters
{
    public string ProjectId { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public int ExpectedIssueCount { get; set; }
    public int ExpectedPullRequestCount { get; set; }
    public int ExpectedTaskCount { get; set; }
    public DateTime ProjectStartDate { get; set; }
    public DateTime? ProjectEndDate { get; set; }
    public double ScopeDeviationThreshold { get; set; } = 0.25; // 25% deviation threshold
    public List<string> ScopeKeywords { get; set; } = new();
    public Dictionary<string, object> CustomParameters { get; set; } = new();
}

public class ScopeDeviationMetrics
{
    public string ProjectId { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Current vs Expected Counts
    public int CurrentIssueCount { get; set; }
    public int CurrentPullRequestCount { get; set; }
    public int CurrentTaskCount { get; set; }
    public int ExpectedIssueCount { get; set; }
    public int ExpectedPullRequestCount { get; set; }
    public int ExpectedTaskCount { get; set; }
    
    // Deviation Calculations
    public double IssueDeviation { get; set; }
    public double PullRequestDeviation { get; set; }
    public double TaskDeviation { get; set; }
    public double OverallDeviation { get; set; }
    
    // Scope Creep Indicators
    public bool HasScopeCreep { get; set; }
    public List<string> CreepIndicators { get; set; } = new();
    public List<string> RecentAdditions { get; set; } = new();
}

public class ScopeCreepAlert
{
    public string ProjectId { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public ScopeCreepSeverity Severity { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string ReynoldsMessage { get; set; } = string.Empty; // Reynolds-style witty message
    public List<string> Changes { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public ScopeDeviationMetrics Metrics { get; set; } = new();
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public enum ScopeCreepSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public class ScopeMonitoringOptions
{
    public bool EnableScopeMonitoring { get; set; } = true;
    public double ScopeDeviationThreshold { get; set; } = 0.25; // 25% deviation threshold
    public TimeSpan MonitoringWindow { get; set; } = TimeSpan.FromHours(1);
    public bool EnableReynoldsMessages { get; set; } = true;
    public bool EnableRecommendations { get; set; } = true;
    public int MaxRecentAdditionsToTrack { get; set; } = 10;
}