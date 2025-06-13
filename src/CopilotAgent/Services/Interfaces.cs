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

// Codespace Management Service Interfaces
public interface ICodespaceManagementService
{
    Task<CodespaceResult> CreateCodespaceAsync(CodespaceSpec spec);
    Task<CodespaceResult> GetCodespaceStatusAsync(string codespaceId);
    Task<bool> DeleteCodespaceAsync(string codespaceId);
    Task<CodespaceResult[]> ListCodespacesAsync(string repository);
}

public interface IOnboardingService
{
    Task<OnboardingStep[]> GetOnboardingStepsAsync();
    Task<OnboardingProgress> StartOnboardingAsync(string userId, string codespaceId);
    Task<OnboardingProgress> UpdateProgressAsync(string userId, string stepId, Dictionary<string, object> stepData);
    Task<bool> CompleteOnboardingAsync(string userId);
    Task<string> GenerateWelcomeMessageAsync(string userId, string repositoryName);
}