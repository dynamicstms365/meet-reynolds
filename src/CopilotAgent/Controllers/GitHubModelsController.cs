using Microsoft.AspNetCore.Mvc;
using CopilotAgent.Services;
using System.Text.Json;

namespace CopilotAgent.Controllers;

/// <summary>
/// GitHub Models API Controller for Issue #72
/// Provides endpoints for parallel workload management with specialized models
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GitHubModelsController : ControllerBase
{
    private readonly ILogger<GitHubModelsController> _logger;
    private readonly IGitHubModelsService _modelsService;
    private readonly IGitHubModelsOrchestrator _orchestrator;
    private readonly ISecurityAuditService _auditService;

    public GitHubModelsController(
        ILogger<GitHubModelsController> logger,
        IGitHubModelsService modelsService,
        IGitHubModelsOrchestrator orchestrator,
        ISecurityAuditService auditService)
    {
        _logger = logger;
        _modelsService = modelsService;
        _orchestrator = orchestrator;
        _auditService = auditService;
    }

    /// <summary>
    /// Get available GitHub Models and their capabilities
    /// </summary>
    [HttpGet("models/available")]
    public async Task<IActionResult> GetAvailableModels()
    {
        try
        {
            _logger.LogInformation("📊 Retrieving available GitHub Models");
            
            var models = await _modelsService.GetAvailableModelsAsync();
            
            await _auditService.LogEventAsync(
                "GitHub_Models_Query",
                action: "GetAvailableModels",
                result: "SUCCESS",
                details: new { ModelsCount = models.Count });

            return Ok(new
            {
                success = true,
                models = models,
                total = models.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available models");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Route a single request to the optimal specialized model
    /// </summary>
    [HttpPost("models/route")]
    public async Task<IActionResult> RouteToSpecializedModel([FromBody] ModelRequest request)
    {
        try
        {
            _logger.LogInformation("🎯 Routing request to specialized model. Task: {TaskType}", request.TaskType);
            
            var response = await _modelsService.RouteToSpecializedModelAsync(request);
            
            return Ok(new
            {
                success = response.Success,
                response = response,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing to specialized model");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Orchestrate multiple concurrent tasks with parallel workload management
    /// </summary>
    [HttpPost("orchestrate/parallel")]
    public async Task<IActionResult> OrchestratePipeline([FromBody] OrchestrationRequest request)
    {
        try
        {
            _logger.LogInformation("🚀 Orchestrating parallel pipeline. Repository: {Repository}, Workloads: {Count}", 
                request.Repository, request.Workloads.Count);
            
            var result = await _orchestrator.OrchestratePipelineAsync(request);
            
            return Ok(new
            {
                success = result.Success,
                orchestration = result,
                efficiency = result.ParallelEfficiency,
                reynolds_comment = result.ReynoldsComment,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error orchestrating pipeline");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Analyze workload requirements for optimal model selection
    /// </summary>
    [HttpPost("analyze/workload")]
    public async Task<IActionResult> AnalyzeWorkload([FromBody] WorkloadAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation("🔍 Analyzing workload for repository: {Repository}", request.Repository);
            
            var analysis = await _orchestrator.AnalyzeWorkloadRequirementsAsync(request.Repository, "");
            var modelSelection = await _orchestrator.SelectOptimalModelsAsync(request);
            
            return Ok(new
            {
                success = true,
                analysis = analysis,
                model_selection = modelSelection,
                recommendations = new
                {
                    optimal_concurrency = analysis.OptimalConcurrencyLevel,
                    recommended_models = analysis.RecommendedModelTypes,
                    pilot_participation = modelSelection.PilotParticipation
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing workload");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get pilot program status and configuration
    /// </summary>
    [HttpGet("pilot/status")]
    public async Task<IActionResult> GetPilotStatus()
    {
        try
        {
            _logger.LogInformation("📋 Retrieving pilot program status");
            
            var status = await _orchestrator.GetPilotProgramStatusAsync();
            var config = await _modelsService.GetPilotConfigurationAsync();
            
            return Ok(new
            {
                success = true,
                status = status,
                configuration = config,
                is_enabled = status.Enabled,
                current_phase = status.CurrentPhase,
                participation_rate = status.ParticipationRate,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pilot status");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get model performance metrics
    /// </summary>
    [HttpGet("metrics/performance")]
    public async Task<IActionResult> GetPerformanceMetrics()
    {
        try
        {
            _logger.LogInformation("📈 Retrieving model performance metrics");
            
            var metrics = await _modelsService.GetModelPerformanceMetricsAsync();
            
            return Ok(new
            {
                success = true,
                metrics = metrics,
                summary = new
                {
                    success_rate = metrics.SuccessRate,
                    average_latency_ms = metrics.AverageLatency.TotalMilliseconds,
                    total_requests = metrics.TotalRequests,
                    pilot_participation = metrics.PilotParticipation,
                    models_in_use = metrics.ModelsUsed
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Execute batch workloads concurrently
    /// </summary>
    [HttpPost("execute/batch")]
    public async Task<IActionResult> ExecuteBatchWorkloads([FromBody] List<ModelWorkload> workloads)
    {
        try
        {
            _logger.LogInformation("⚡ Executing batch workloads. Count: {Count}", workloads.Count);
            
            var result = await _orchestrator.ExecuteParallelWorkloadsAsync(workloads);
            
            return Ok(new
            {
                success = result.Success,
                execution = result,
                performance = new
                {
                    total_time_ms = result.TotalExecutionTime.TotalMilliseconds,
                    average_latency_ms = result.AverageLatency.TotalMilliseconds,
                    concurrency_level = result.ConcurrencyLevel,
                    success_rate = result.WorkloadResults.Count(r => r.Success) / (double)result.WorkloadResults.Count
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing batch workloads");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint for GitHub Models integration
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            await Task.CompletedTask; // Satisfy async requirement
            
            var health = new
            {
                service = "GitHub Models Integration",
                status = "healthy",
                version = System.Environment.GetEnvironmentVariable("COPILOT_VERSION") ?? "dev-local",
                features = new[]
                {
                    "Parallel Workload Management",
                    "Specialized Model Routing",
                    "Pilot Program Support",
                    "Reynolds Integration",
                    "Performance Metrics"
                },
                timestamp = DateTime.UtcNow
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { status = "unhealthy", error = ex.Message });
        }
    }

    /// <summary>
    /// Get integration configuration and capabilities
    /// </summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetConfiguration()
    {
        try
        {
            var config = await _modelsService.GetPilotConfigurationAsync();
            var models = await _modelsService.GetAvailableModelsAsync();
            
            return Ok(new
            {
                success = true,
                configuration = new
                {
                    pilot_program = config,
                    available_models = models.Count,
                    supported_task_types = Enum.GetNames<TaskType>(),
                    complexity_levels = Enum.GetNames<ComplexityLevel>(),
                    workload_priorities = Enum.GetNames<WorkloadPriority>()
                },
                capabilities = new
                {
                    parallel_execution = true,
                    model_routing = true,
                    reynolds_integration = true,
                    performance_tracking = true,
                    pilot_program = config.Enabled
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Create a sample orchestration request for testing
    /// </summary>
    [HttpPost("test/sample-request")]
    public async Task<IActionResult> CreateSampleRequest([FromBody] SampleRequestConfig config)
    {
        try
        {
            await Task.CompletedTask; // Satisfy async requirement
            
            var sampleRequest = new OrchestrationRequest
            {
                Repository = config.Repository ?? "dynamicstms365/copilot-powerplatform",
                Context = config.Context ?? "Sample GitHub Models integration test",
                Workloads = new List<ModelWorkload>
                {
                    new ModelWorkload
                    {
                        Type = "code_generation",
                        Content = "Generate a C# class for user management",
                        ComplexityLevel = ComplexityLevel.Medium,
                        Priority = WorkloadPriority.High,
                        Repository = config.Repository ?? "dynamicstms365/copilot-powerplatform",
                        UserId = config.UserId ?? "test-user"
                    },
                    new ModelWorkload
                    {
                        Type = "documentation",
                        Content = "Create API documentation for the user management endpoints",
                        ComplexityLevel = ComplexityLevel.Low,
                        Priority = WorkloadPriority.Medium,
                        Repository = config.Repository ?? "dynamicstms365/copilot-powerplatform",
                        UserId = config.UserId ?? "test-user"
                    },
                    new ModelWorkload
                    {
                        Type = "code_review",
                        Content = "Review the generated user management code for security and best practices",
                        ComplexityLevel = ComplexityLevel.High,
                        Priority = WorkloadPriority.High,
                        Repository = config.Repository ?? "dynamicstms365/copilot-powerplatform",
                        UserId = config.UserId ?? "test-user"
                    }
                }
            };

            return Ok(new
            {
                success = true,
                sample_request = sampleRequest,
                instructions = new
                {
                    next_step = "Use this request with POST /api/GitHubModels/orchestrate/parallel",
                    expected_models = new[] { "gpt-4-code-specialist", "gpt-4-docs-specialist", "gpt-4-review-specialist" },
                    estimated_processing_time = "3-5 seconds"
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sample request");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

/// <summary>
/// Configuration for creating sample requests
/// </summary>
public class SampleRequestConfig
{
    public string? Repository { get; set; }
    public string? Context { get; set; }
    public string? UserId { get; set; }
}