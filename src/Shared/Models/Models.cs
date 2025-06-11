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