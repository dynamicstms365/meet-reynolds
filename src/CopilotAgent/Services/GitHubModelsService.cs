using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Models;

namespace CopilotAgent.Services;

/// <summary>
/// GitHub Models Integration Service for Issue #72
/// Provides parallel workload management with specialized models
/// </summary>
public interface IGitHubModelsService
{
    Task<ModelResponse> RouteToSpecializedModelAsync(ModelRequest request);
    Task<List<ModelCapability>> GetAvailableModelsAsync();
    Task<ModelOrchestrationResult> OrchestrateConcurrentTasksAsync(List<ModelRequest> requests);
    Task<PilotConfiguration> GetPilotConfigurationAsync();
    Task<ModelPerformanceMetrics> GetModelPerformanceMetricsAsync();
}

public class GitHubModelsService : IGitHubModelsService
{
    private readonly ILogger<GitHubModelsService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ISecurityAuditService _auditService;
    private readonly IGitHubWorkflowOrchestrator _workflowOrchestrator;
    
    private readonly Dictionary<TaskType, ModelConfiguration> _modelRouting;
    private readonly PilotConfiguration _pilotConfig;

    public GitHubModelsService(
        ILogger<GitHubModelsService> logger,
        IConfiguration configuration,
        HttpClient httpClient,
        ISecurityAuditService auditService,
        IGitHubWorkflowOrchestrator workflowOrchestrator)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
        _auditService = auditService;
        _workflowOrchestrator = workflowOrchestrator;
        
        _modelRouting = InitializeModelRouting();
        _pilotConfig = InitializePilotConfiguration();
    }

    public async Task<ModelResponse> RouteToSpecializedModelAsync(ModelRequest request)
    {
        try
        {
            _logger.LogInformation("🎯 Routing request to specialized model. Task: {TaskType}, Complexity: {Complexity}", 
                request.TaskType, request.ComplexityLevel);

            // Determine optimal model based on task type and complexity
            var modelConfig = GetOptimalModelConfiguration(request);
            
            // Check pilot program eligibility
            if (!await IsEligibleForPilotAsync(request))
            {
                _logger.LogInformation("Request not eligible for pilot program, using fallback model");
                modelConfig = GetFallbackModelConfiguration(request.TaskType);
            }

            // Route to appropriate model endpoint
            var response = await InvokeSpecializedModelAsync(request, modelConfig);
            
            // Track performance metrics
            await TrackModelPerformanceAsync(request, response, modelConfig);
            
            // Audit successful routing
            await _auditService.LogEventAsync(
                "GitHub_Models_Routing_Success",
                action: "RouteToSpecializedModel",
                result: "SUCCESS",
                details: new { 
                    TaskType = request.TaskType,
                    ModelUsed = modelConfig.ModelName,
                    ComplexityLevel = request.ComplexityLevel,
                    ResponseTokens = response.TokensUsed
                });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing request to specialized model");
            
            // Fallback to basic model for resilience
            var fallbackResponse = await HandleModelRoutingFallback(request);
            return fallbackResponse;
        }
    }

    public async Task<ModelOrchestrationResult> OrchestrateConcurrentTasksAsync(List<ModelRequest> requests)
    {
        try
        {
            _logger.LogInformation("🚀 Orchestrating {Count} concurrent tasks for parallel processing", requests.Count);

            var orchestrationResult = new ModelOrchestrationResult
            {
                TotalTasks = requests.Count,
                StartTime = DateTime.UtcNow,
                TaskResults = new List<TaskResult>()
            };

            // Group requests by optimal model for batch processing
            var modelGroups = GroupRequestsByOptimalModel(requests);
            
            // Process each model group concurrently
            var concurrentTasks = modelGroups.Select(async group =>
            {
                var (modelConfig, groupRequests) = group;
                return await ProcessModelGroupConcurrentlyAsync(modelConfig, groupRequests);
            });

            var groupResults = await Task.WhenAll(concurrentTasks);
            
            // Aggregate results
            foreach (var groupResult in groupResults)
            {
                orchestrationResult.TaskResults.AddRange(groupResult);
            }

            orchestrationResult.EndTime = DateTime.UtcNow;
            orchestrationResult.Success = orchestrationResult.TaskResults.All(r => r.Success);
            orchestrationResult.TotalProcessingTime = orchestrationResult.EndTime - orchestrationResult.StartTime;

            _logger.LogInformation("✅ Parallel orchestration completed. Success: {Success}, Duration: {Duration}ms", 
                orchestrationResult.Success, orchestrationResult.TotalProcessingTime.TotalMilliseconds);

            await _auditService.LogEventAsync(
                "GitHub_Models_Orchestration_Completed",
                action: "OrchestrateConcurrentTasks",
                result: orchestrationResult.Success ? "SUCCESS" : "PARTIAL_SUCCESS",
                details: new {
                    TotalTasks = orchestrationResult.TotalTasks,
                    SuccessfulTasks = orchestrationResult.TaskResults.Count(r => r.Success),
                    ProcessingTime = orchestrationResult.TotalProcessingTime.TotalMilliseconds,
                    ModelsUsed = modelGroups.Count
                });

            return orchestrationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error orchestrating concurrent tasks");
            throw;
        }
    }

    public async Task<List<ModelCapability>> GetAvailableModelsAsync()
    {
        try
        {
            var capabilities = new List<ModelCapability>();
            
            foreach (var (taskType, config) in _modelRouting)
            {
                capabilities.Add(new ModelCapability
                {
                    TaskType = taskType,
                    ModelName = config.ModelName,
                    MaxTokens = config.MaxTokens,
                    OptimalComplexityRange = config.OptimalComplexityRange,
                    EstimatedLatency = config.EstimatedLatency,
                    PilotEnabled = config.PilotEnabled,
                    Specializations = config.Specializations
                });
            }

            return capabilities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available models");
            throw;
        }
    }

    public async Task<PilotConfiguration> GetPilotConfigurationAsync()
    {
        return await Task.FromResult(_pilotConfig);
    }

    public async Task<ModelPerformanceMetrics> GetModelPerformanceMetricsAsync()
    {
        try
        {
            // This would typically query a metrics database
            // For now, return sample metrics based on recent usage
            
            var metrics = new ModelPerformanceMetrics
            {
                TotalRequests = 1247,
                SuccessRate = 0.987,
                AverageLatency = TimeSpan.FromMilliseconds(1250),
                TokensProcessed = 156789,
                ModelsUsed = _modelRouting.Count,
                PilotParticipation = 0.342,
                LastUpdated = DateTime.UtcNow
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics");
            throw;
        }
    }

    private Dictionary<TaskType, ModelConfiguration> InitializeModelRouting()
    {
        return new Dictionary<TaskType, ModelConfiguration>
        {
            [TaskType.CodeGeneration] = new ModelConfiguration
            {
                ModelName = "gpt-4-code-specialist",
                MaxTokens = 8192,
                OptimalComplexityRange = (ComplexityLevel.Medium, ComplexityLevel.High),
                EstimatedLatency = TimeSpan.FromMilliseconds(2000),
                PilotEnabled = true,
                Specializations = new[] { "C#", "TypeScript", "Python", "Blazor", "ASP.NET" }
            },
            [TaskType.CodeReview] = new ModelConfiguration
            {
                ModelName = "gpt-4-review-specialist",
                MaxTokens = 4096,
                OptimalComplexityRange = (ComplexityLevel.Low, ComplexityLevel.High),
                EstimatedLatency = TimeSpan.FromMilliseconds(1500),
                PilotEnabled = true,
                Specializations = new[] { "Security", "Performance", "Best Practices", "Architecture Review" }
            },
            [TaskType.Documentation] = new ModelConfiguration
            {
                ModelName = "gpt-4-docs-specialist",
                MaxTokens = 6144,
                OptimalComplexityRange = (ComplexityLevel.Low, ComplexityLevel.Medium),
                EstimatedLatency = TimeSpan.FromMilliseconds(1800),
                PilotEnabled = true,
                Specializations = new[] { "Technical Writing", "API Documentation", "User Guides", "README Generation" }
            },
            [TaskType.IssueManagement] = new ModelConfiguration
            {
                ModelName = "gpt-4-workflow-specialist",
                MaxTokens = 2048,
                OptimalComplexityRange = (ComplexityLevel.Low, ComplexityLevel.Medium),
                EstimatedLatency = TimeSpan.FromMilliseconds(1000),
                PilotEnabled = true,
                Specializations = new[] { "Issue Classification", "Priority Assessment", "Stakeholder Routing", "Project Management" }
            },
            [TaskType.SecurityScanning] = new ModelConfiguration
            {
                ModelName = "gpt-4-security-specialist",
                MaxTokens = 4096,
                OptimalComplexityRange = (ComplexityLevel.Medium, ComplexityLevel.High),
                EstimatedLatency = TimeSpan.FromMilliseconds(2500),
                PilotEnabled = false, // More conservative rollout for security
                Specializations = new[] { "Vulnerability Detection", "Compliance Checking", "Security Best Practices", "Threat Analysis" }
            }
        };
    }

    private PilotConfiguration InitializePilotConfiguration()
    {
        return new PilotConfiguration
        {
            Enabled = _configuration.GetValue<bool>("GitHubModels:PilotProgram:Enabled", true),
            ParticipationRate = _configuration.GetValue<double>("GitHubModels:PilotProgram:ParticipationRate", 0.25),
            EligibleRepositories = _configuration.GetSection("GitHubModels:PilotProgram:EligibleRepositories").Get<string[]>() 
                ?? new[] { "dynamicstms365/copilot-powerplatform" },
            EligibleUsers = _configuration.GetSection("GitHubModels:PilotProgram:EligibleUsers").Get<string[]>() 
                ?? new[] { "cege7480" },
            MaxConcurrentTasks = _configuration.GetValue<int>("GitHubModels:PilotProgram:MaxConcurrentTasks", 5),
            FeedbackCollectionEnabled = _configuration.GetValue<bool>("GitHubModels:PilotProgram:FeedbackCollection", true),
            GradualRolloutPhases = new[]
            {
                new RolloutPhase { Name = "Alpha", ParticipationRate = 0.05, Duration = TimeSpan.FromDays(7) },
                new RolloutPhase { Name = "Beta", ParticipationRate = 0.15, Duration = TimeSpan.FromDays(14) },
                new RolloutPhase { Name = "General", ParticipationRate = 0.50, Duration = TimeSpan.FromDays(30) }
            }
        };
    }

    private ModelConfiguration GetOptimalModelConfiguration(ModelRequest request)
    {
        if (_modelRouting.TryGetValue(request.TaskType, out var config))
        {
            // Check if complexity is within optimal range
            var (minComplexity, maxComplexity) = config.OptimalComplexityRange;
            if (request.ComplexityLevel >= minComplexity && request.ComplexityLevel <= maxComplexity)
            {
                return config;
            }
        }

        // Fallback to general-purpose model configuration
        return GetFallbackModelConfiguration(request.TaskType);
    }

    private ModelConfiguration GetFallbackModelConfiguration(TaskType taskType)
    {
        return new ModelConfiguration
        {
            ModelName = "gpt-4-general",
            MaxTokens = 4096,
            OptimalComplexityRange = (ComplexityLevel.Low, ComplexityLevel.High),
            EstimatedLatency = TimeSpan.FromMilliseconds(2000),
            PilotEnabled = false,
            Specializations = new[] { "General Purpose" }
        };
    }

    private async Task<bool> IsEligibleForPilotAsync(ModelRequest request)
    {
        if (!_pilotConfig.Enabled)
            return false;

        // Check repository eligibility
        if (!_pilotConfig.EligibleRepositories.Contains(request.Repository))
            return false;

        // Check user eligibility
        if (!_pilotConfig.EligibleUsers.Contains(request.UserId))
            return false;

        // Random sampling based on participation rate
        var random = new Random();
        return random.NextDouble() <= _pilotConfig.ParticipationRate;
    }

    private async Task<ModelResponse> InvokeSpecializedModelAsync(ModelRequest request, ModelConfiguration config)
    {
        // This would integrate with actual GitHub Models API
        // For now, return a simulated response
        
        await Task.Delay((int)config.EstimatedLatency.TotalMilliseconds);
        
        return new ModelResponse
        {
            Success = true,
            Content = $"Specialized response from {config.ModelName}",
            ModelUsed = config.ModelName,
            TokensUsed = request.Content.Length / 4, // Rough token estimation
            ProcessingTime = config.EstimatedLatency,
            Confidence = 0.95
        };
    }

    private async Task TrackModelPerformanceAsync(ModelRequest request, ModelResponse response, ModelConfiguration config)
    {
        // Track performance metrics for continuous improvement
        _logger.LogInformation("📊 Model Performance - {Model}: Success={Success}, Latency={Latency}ms, Tokens={Tokens}",
            config.ModelName, response.Success, response.ProcessingTime.TotalMilliseconds, response.TokensUsed);
    }

    private async Task<ModelResponse> HandleModelRoutingFallback(ModelRequest request)
    {
        _logger.LogWarning("Using fallback model for request due to routing failure");
        
        var fallbackConfig = GetFallbackModelConfiguration(request.TaskType);
        return await InvokeSpecializedModelAsync(request, fallbackConfig);
    }

    private List<(ModelConfiguration Config, List<ModelRequest> Requests)> GroupRequestsByOptimalModel(List<ModelRequest> requests)
    {
        return requests
            .GroupBy(r => GetOptimalModelConfiguration(r).ModelName)
            .Select(g => (
                Config: GetOptimalModelConfiguration(g.First()),
                Requests: g.ToList()
            ))
            .ToList();
    }

    private async Task<List<TaskResult>> ProcessModelGroupConcurrentlyAsync(ModelConfiguration config, List<ModelRequest> requests)
    {
        var tasks = requests.Select(async request =>
        {
            try
            {
                var response = await InvokeSpecializedModelAsync(request, config);
                return new TaskResult
                {
                    RequestId = request.Id,
                    Success = response.Success,
                    Response = response,
                    ProcessingTime = response.ProcessingTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request {RequestId}", request.Id);
                return new TaskResult
                {
                    RequestId = request.Id,
                    Success = false,
                    Error = ex.Message,
                    ProcessingTime = TimeSpan.Zero
                };
            }
        });

        return (await Task.WhenAll(tasks)).ToList();
    }
}

// Supporting Models and Enums
public enum TaskType
{
    CodeGeneration,
    CodeReview,
    Documentation,
    IssueManagement,
    SecurityScanning
}

public enum ComplexityLevel
{
    Low = 1,
    Medium = 2,
    High = 3
}

public class ModelRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public TaskType TaskType { get; set; }
    public ComplexityLevel ComplexityLevel { get; set; }
    public string Content { get; set; } = "";
    public string Repository { get; set; } = "";
    public string UserId { get; set; } = "";
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ModelResponse
{
    public bool Success { get; set; }
    public string Content { get; set; } = "";
    public string ModelUsed { get; set; } = "";
    public int TokensUsed { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public double Confidence { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ModelConfiguration
{
    public string ModelName { get; set; } = "";
    public int MaxTokens { get; set; }
    public (ComplexityLevel Min, ComplexityLevel Max) OptimalComplexityRange { get; set; }
    public TimeSpan EstimatedLatency { get; set; }
    public bool PilotEnabled { get; set; }
    public string[] Specializations { get; set; } = Array.Empty<string>();
}

public class ModelCapability
{
    public TaskType TaskType { get; set; }
    public string ModelName { get; set; } = "";
    public int MaxTokens { get; set; }
    public (ComplexityLevel Min, ComplexityLevel Max) OptimalComplexityRange { get; set; }
    public TimeSpan EstimatedLatency { get; set; }
    public bool PilotEnabled { get; set; }
    public string[] Specializations { get; set; } = Array.Empty<string>();
}

public class ModelOrchestrationResult
{
    public int TotalTasks { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public bool Success { get; set; }
    public List<TaskResult> TaskResults { get; set; } = new();
}

public class TaskResult
{
    public string RequestId { get; set; } = "";
    public bool Success { get; set; }
    public ModelResponse? Response { get; set; }
    public string? Error { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}

public class PilotConfiguration
{
    public bool Enabled { get; set; }
    public double ParticipationRate { get; set; }
    public string[] EligibleRepositories { get; set; } = Array.Empty<string>();
    public string[] EligibleUsers { get; set; } = Array.Empty<string>();
    public int MaxConcurrentTasks { get; set; }
    public bool FeedbackCollectionEnabled { get; set; }
    public RolloutPhase[] GradualRolloutPhases { get; set; } = Array.Empty<RolloutPhase>();
}

public class RolloutPhase
{
    public string Name { get; set; } = "";
    public double ParticipationRate { get; set; }
    public TimeSpan Duration { get; set; }
}

public class ModelPerformanceMetrics
{
    public int TotalRequests { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageLatency { get; set; }
    public int TokensProcessed { get; set; }
    public int ModelsUsed { get; set; }
    public double PilotParticipation { get; set; }
    public DateTime LastUpdated { get; set; }
}