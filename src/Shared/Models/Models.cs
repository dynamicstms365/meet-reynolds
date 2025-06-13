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

// Stakeholder Visibility Models
public class StakeholderConfiguration
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string[] Repositories { get; set; } = Array.Empty<string>();
    public StakeholderUpdatePreferences UpdatePreferences { get; set; } = new();
    public DashboardConfiguration DashboardConfig { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class StakeholderUpdatePreferences
{
    public bool IssueProgressUpdates { get; set; } = true;
    public bool PullRequestStatusUpdates { get; set; } = true;
    public bool DiscussionUpdates { get; set; } = false;
    public bool SecurityAlerts { get; set; } = true;
    public bool PerformanceMetrics { get; set; } = false;
    public NotificationFrequency Frequency { get; set; } = NotificationFrequency.Daily;
    public string[] ImportantLabels { get; set; } = Array.Empty<string>();
    public string[] ImportantAssignees { get; set; } = Array.Empty<string>();
    public NotificationChannel[] Channels { get; set; } = new[] { NotificationChannel.Email };
}

public class DashboardConfiguration
{
    public string Title { get; set; } = "Project Dashboard";
    public DashboardWidget[] Widgets { get; set; } = Array.Empty<DashboardWidget>();
    public string Theme { get; set; } = "default";
    public int RefreshIntervalMinutes { get; set; } = 30;
    public bool ShowMetrics { get; set; } = true;
    public bool ShowRecentActivity { get; set; } = true;
}

public class DashboardWidget
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public WidgetType Type { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
    public int Order { get; set; }
    public bool IsVisible { get; set; } = true;
}

public class ProjectProgressSummary
{
    public string Repository { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public IssuesSummary Issues { get; set; } = new();
    public PullRequestsSummary PullRequests { get; set; } = new();
    public DiscussionsSummary Discussions { get; set; } = new();
    public ProjectMetrics Metrics { get; set; } = new();
    public RecentActivity[] RecentActivities { get; set; } = Array.Empty<RecentActivity>();
}

public class IssuesSummary
{
    public int TotalOpen { get; set; }
    public int TotalClosed { get; set; }
    public int OpenCritical { get; set; }
    public int OpenHigh { get; set; }
    public int ClosedThisWeek { get; set; }
    public int NewThisWeek { get; set; }
    public double AverageClosureTimeHours { get; set; }
    public GitHubIssue[] RecentIssues { get; set; } = Array.Empty<GitHubIssue>();
}

public class PullRequestsSummary
{
    public int TotalOpen { get; set; }
    public int TotalMerged { get; set; }
    public int AwaitingReview { get; set; }
    public int MergedThisWeek { get; set; }
    public int NewThisWeek { get; set; }
    public double AverageMergeTimeHours { get; set; }
    public GitHubPullRequest[] RecentPullRequests { get; set; } = Array.Empty<GitHubPullRequest>();
}

public class DiscussionsSummary
{
    public int TotalActive { get; set; }
    public int NewThisWeek { get; set; }
    public int AnsweredThisWeek { get; set; }
    public GitHubDiscussion[] RecentDiscussions { get; set; } = Array.Empty<GitHubDiscussion>();
}

public class ProjectMetrics
{
    public double ProjectHealth { get; set; }
    public int TeamProductivityScore { get; set; }
    public double IssueResolutionTrend { get; set; }
    public double CodeQualityScore { get; set; }
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

public class RecentActivity
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class GitHubPullRequest
{
    public string NodeId { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? MergedAt { get; set; }
    public string[] Labels { get; set; } = Array.Empty<string>();
    public string[] Assignees { get; set; } = Array.Empty<string>();
    public string[] RequestedReviewers { get; set; } = Array.Empty<string>();
    public bool IsDraft { get; set; }
    public int CommentsCount { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class StakeholderNotification
{
    public string Id { get; set; } = string.Empty;
    public string StakeholderId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum NotificationFrequency
{
    Immediate,
    Hourly,
    Daily,
    Weekly
}

public enum NotificationChannel
{
    Email,
    Teams,
    Dashboard,
    Webhook
}

public enum WidgetType
{
    IssuesSummary,
    PullRequestsSummary,
    RecentActivity,
    ProjectMetrics,
    Chart,
    Table,
    Custom
}

public enum NotificationType
{
    IssueUpdate,
    PullRequestUpdate,
    DiscussionUpdate,
    SecurityAlert,
    ProjectSummary,
    PerformanceAlert
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed,
    Cancelled
}

// GitHub Models (moved from Services to shared)
public class GitHubIssue
{
    public string NodeId { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string[] Labels { get; set; } = Array.Empty<string>();
    public string[] Assignees { get; set; } = Array.Empty<string>();
    public int CommentCount { get; set; }
    public IEnumerable<GitHubComment> Comments { get; set; } = new List<GitHubComment>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class GitHubDiscussion
{
    public string NodeId { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CommentCount { get; set; }
    public IEnumerable<GitHubComment> Comments { get; set; } = new List<GitHubComment>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class GitHubComment
{
    public string NodeId { get; set; } = string.Empty;
    public long Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}