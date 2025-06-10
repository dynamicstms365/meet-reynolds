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