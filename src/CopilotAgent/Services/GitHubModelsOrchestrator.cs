using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CopilotAgent.MCP;
using Shared.Models;

namespace CopilotAgent.Services;

/// <summary>
/// GitHub Models Orchestrator for Issue #72
/// Integrates GitHub Models with Reynolds orchestration system for parallel workload management
/// </summary>
public interface IGitHubModelsOrchestrator
{
    Task<OrchestrationResult> OrchestratePipelineAsync(OrchestrationRequest request);
    Task<ModelSelectionResult> SelectOptimalModelsAsync(WorkloadAnalysisRequest request);
    Task<ParallelExecutionResult> ExecuteParallelWorkloadsAsync(List<ModelWorkload> workloads);
    Task<PilotProgramStatus> GetPilotProgramStatusAsync();
    Task<WorkloadAnalysis> AnalyzeWorkloadRequirementsAsync(string repository, string context);
}

public class GitHubModelsOrchestrator : IGitHubModelsOrchestrator
{
    private readonly ILogger<GitHubModelsOrchestrator> _logger;
    private readonly IGitHubModelsService _modelsService;
    private readonly IGitHubWorkflowOrchestrator _workflowOrchestrator;
    private readonly ISecurityAuditService _auditService;
    private readonly IConfiguration _configuration;
    private readonly ReynoldsPersonaService _reynoldsPersona;
    
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly Dictionary<string, WorkloadProcessor> _workloadProcessors;

    public GitHubModelsOrchestrator(
        ILogger<GitHubModelsOrchestrator> logger,
        IGitHubModelsService modelsService,
        IGitHubWorkflowOrchestrator workflowOrchestrator,
        ISecurityAuditService auditService,
        IConfiguration configuration,
        ReynoldsPersonaService reynoldsPersona)
    {
        _logger = logger;
        _modelsService = modelsService;
        _workflowOrchestrator = workflowOrchestrator;
        _auditService = auditService;
        _configuration = configuration;
        _reynoldsPersona = reynoldsPersona;
        
        var maxConcurrency = _configuration.GetValue<int>("GitHubModels:MaxConcurrentWorkloads", 10);
        _concurrencyLimiter = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        
        _workloadProcessors = InitializeWorkloadProcessors();
    }

    public async Task<OrchestrationResult> OrchestratePipelineAsync(OrchestrationRequest request)
    {
        try
        {
            _logger.LogInformation("🎭 Reynolds GitHub Models Pipeline initiated. Repository: {Repository}, Workloads: {Count}", 
                request.Repository, request.Workloads.Count);

            var orchestrationResult = new OrchestrationResult
            {
                RequestId = request.Id,
                Repository = request.Repository,
                StartTime = DateTime.UtcNow,
                Workloads = new List<WorkloadResult>()
            };

            // Phase 1: Analyze workload requirements with Reynolds intelligence
            var workloadAnalysis = await AnalyzeWorkloadRequirementsAsync(request.Repository, request.Context);
            
            // Phase 2: Select optimal models for each workload type
            var modelSelection = await SelectOptimalModelsAsync(new WorkloadAnalysisRequest
            {
                Repository = request.Repository,
                Workloads = request.Workloads,
                Analysis = workloadAnalysis
            });

            // Phase 3: Create specialized workloads with Reynolds persona enhancement
            var enhancedWorkloads = await EnhanceWorkloadsWithReynoldsPersonaAsync(request.Workloads, modelSelection);

            // Phase 4: Execute parallel workloads with intelligent coordination
            var parallelResult = await ExecuteParallelWorkloadsAsync(enhancedWorkloads);
            
            // Phase 5: Aggregate and enhance results
            orchestrationResult.Workloads = parallelResult.WorkloadResults;
            orchestrationResult.EndTime = DateTime.UtcNow;
            orchestrationResult.Success = parallelResult.Success;
            orchestrationResult.TotalProcessingTime = orchestrationResult.EndTime - orchestrationResult.StartTime;
            orchestrationResult.ModelsUsed = modelSelection.SelectedModels.Count;
            orchestrationResult.ParallelEfficiency = CalculateParallelEfficiency(orchestrationResult);

            // Apply Reynolds-style result enhancement
            orchestrationResult = await _reynoldsPersona.EnhanceOrchestrationResultAsync(orchestrationResult);

            _logger.LogInformation("✅ Reynolds Pipeline completed. Success: {Success}, Models: {Models}, Efficiency: {Efficiency:P1}", 
                orchestrationResult.Success, orchestrationResult.ModelsUsed, orchestrationResult.ParallelEfficiency);

            await _auditService.LogEventAsync(
                "GitHub_Models_Pipeline_Completed",
                repository: request.Repository,
                action: "OrchestratePipeline",
                result: orchestrationResult.Success ? "SUCCESS" : "PARTIAL_SUCCESS",
                details: new {
                    WorkloadsProcessed = orchestrationResult.Workloads.Count,
                    ModelsUsed = orchestrationResult.ModelsUsed,
                    ProcessingTime = orchestrationResult.TotalProcessingTime.TotalMilliseconds,
                    ParallelEfficiency = orchestrationResult.ParallelEfficiency
                });

            return orchestrationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error orchestrating GitHub Models pipeline");
            throw;
        }
    }

    public async Task<ModelSelectionResult> SelectOptimalModelsAsync(WorkloadAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation("🧠 Selecting optimal models for {Count} workloads", request.Workloads.Count);

            var availableModels = await _modelsService.GetAvailableModelsAsync();
            var selectedModels = new List<ModelAssignment>();

            foreach (var workload in request.Workloads)
            {
                var optimalModel = SelectBestModelForWorkload(workload, availableModels, request.Analysis);
                
                selectedModels.Add(new ModelAssignment
                {
                    WorkloadId = workload.Id,
                    WorkloadType = workload.Type,
                    SelectedModel = optimalModel,
                    SelectionReason = GenerateSelectionReason(workload, optimalModel),
                    EstimatedLatency = optimalModel.EstimatedLatency,
                    ConfidenceScore = CalculateModelConfidence(workload, optimalModel)
                });
            }

            var result = new ModelSelectionResult
            {
                TotalWorkloads = request.Workloads.Count,
                SelectedModels = selectedModels,
                SelectionStrategy = "OptimalComplexityMatching",
                PilotParticipation = selectedModels.Count(m => m.SelectedModel.PilotEnabled) / (double)selectedModels.Count,
                EstimatedTotalLatency = selectedModels.Sum(m => m.EstimatedLatency.TotalMilliseconds)
            };

            _logger.LogInformation("🎯 Model selection completed. Pilot participation: {Pilot:P1}, Avg confidence: {Confidence:P1}",
                result.PilotParticipation, selectedModels.Average(m => m.ConfidenceScore));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting optimal models");
            throw;
        }
    }

    public async Task<ParallelExecutionResult> ExecuteParallelWorkloadsAsync(List<ModelWorkload> workloads)
    {
        try
        {
            _logger.LogInformation("🚀 Executing {Count} parallel workloads with Reynolds coordination", workloads.Count);

            var executionResult = new ParallelExecutionResult
            {
                StartTime = DateTime.UtcNow,
                WorkloadResults = new List<WorkloadResult>(),
                ConcurrencyLevel = Math.Min(workloads.Count, _concurrencyLimiter.CurrentCount)
            };

            // Group workloads by priority for optimal execution order
            var prioritizedGroups = GroupWorkloadsByPriority(workloads);
            
            // Execute high-priority workloads first, then medium and low in parallel
            foreach (var priorityGroup in prioritizedGroups)
            {
                var groupTasks = priorityGroup.Value.Select(async workload =>
                {
                    await _concurrencyLimiter.WaitAsync();
                    try
                    {
                        return await ExecuteSingleWorkloadAsync(workload);
                    }
                    finally
                    {
                        _concurrencyLimiter.Release();
                    }
                });

                var groupResults = await Task.WhenAll(groupTasks);
                executionResult.WorkloadResults.AddRange(groupResults);
            }

            executionResult.EndTime = DateTime.UtcNow;
            executionResult.TotalExecutionTime = executionResult.EndTime - executionResult.StartTime;
            executionResult.Success = executionResult.WorkloadResults.All(r => r.Success);
            executionResult.AverageLatency = TimeSpan.FromMilliseconds(
                executionResult.WorkloadResults.Average(r => r.ProcessingTime.TotalMilliseconds));

            _logger.LogInformation("✅ Parallel execution completed. Success rate: {Success:P1}, Avg latency: {Latency}ms",
                executionResult.WorkloadResults.Count(r => r.Success) / (double)executionResult.WorkloadResults.Count,
                executionResult.AverageLatency.TotalMilliseconds);

            return executionResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing parallel workloads");
            throw;
        }
    }

    public async Task<PilotProgramStatus> GetPilotProgramStatusAsync()
    {
        try
        {
            var pilotConfig = await _modelsService.GetPilotConfigurationAsync();
            var performanceMetrics = await _modelsService.GetModelPerformanceMetricsAsync();

            return new PilotProgramStatus
            {
                Enabled = pilotConfig.Enabled,
                CurrentPhase = DetermineCurrentRolloutPhase(pilotConfig),
                ParticipationRate = performanceMetrics.PilotParticipation,
                TotalParticipants = (int)(performanceMetrics.TotalRequests * performanceMetrics.PilotParticipation),
                SuccessRate = performanceMetrics.SuccessRate,
                FeedbackEnabled = pilotConfig.FeedbackCollectionEnabled,
                NextPhaseDate = CalculateNextPhaseDate(pilotConfig),
                EligibleRepositories = pilotConfig.EligibleRepositories.Length,
                MaxConcurrentTasks = pilotConfig.MaxConcurrentTasks
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pilot program status");
            throw;
        }
    }

    public async Task<WorkloadAnalysis> AnalyzeWorkloadRequirementsAsync(string repository, string context)
    {
        try
        {
            _logger.LogInformation("🔍 Analyzing workload requirements for repository: {Repository}", repository);

            // Analyze repository context and requirements
            var analysis = new WorkloadAnalysis
            {
                Repository = repository,
                AnalysisTimestamp = DateTime.UtcNow,
                ContextComplexity = AnalyzeContextComplexity(context),
                EstimatedWorkloadTypes = IdentifyLikelyWorkloadTypes(context),
                ResourceRequirements = EstimateResourceRequirements(context),
                OptimalConcurrencyLevel = DetermineOptimalConcurrency(context),
                RecommendedModelTypes = RecommendModelTypes(context)
            };

            // Apply Reynolds intelligence for enhanced analysis
            analysis = await _reynoldsPersona.EnhanceWorkloadAnalysisAsync(analysis, context) as WorkloadAnalysis ?? analysis;

            _logger.LogInformation("📊 Workload analysis completed. Complexity: {Complexity}, Recommended models: {Models}",
                analysis.ContextComplexity, analysis.RecommendedModelTypes.Count);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing workload requirements");
            throw;
        }
    }

    private Dictionary<string, WorkloadProcessor> InitializeWorkloadProcessors()
    {
        return new Dictionary<string, WorkloadProcessor>
        {
            ["code_generation"] = new CodeGenerationProcessor(_modelsService, _logger),
            ["code_review"] = new CodeReviewProcessor(_modelsService, _logger),
            ["documentation"] = new DocumentationProcessor(_modelsService, _logger),
            ["issue_management"] = new IssueManagementProcessor(_modelsService, _workflowOrchestrator, _logger),
            ["security_scanning"] = new SecurityScanningProcessor(_modelsService, _logger)
        };
    }

    private async Task<List<ModelWorkload>> EnhanceWorkloadsWithReynoldsPersonaAsync(
        List<ModelWorkload> workloads, ModelSelectionResult modelSelection)
    {
        var enhancedWorkloads = new List<ModelWorkload>();

        foreach (var workload in workloads)
        {
            var modelAssignment = modelSelection.SelectedModels.FirstOrDefault(m => m.WorkloadId == workload.Id);
            if (modelAssignment != null)
            {
                workload.AssignedModel = modelAssignment.SelectedModel;
                workload.EnhancedContext = await _reynoldsPersona.EnhanceWorkloadContextAsync(workload.Context, workload.Type);
                enhancedWorkloads.Add(workload);
            }
        }

        return enhancedWorkloads;
    }

    private async Task<WorkloadResult> ExecuteSingleWorkloadAsync(ModelWorkload workload)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            
            if (_workloadProcessors.TryGetValue(workload.Type.ToLowerInvariant(), out var processor))
            {
                var result = await processor.ProcessAsync(workload);
                result.ProcessingTime = DateTime.UtcNow - startTime;
                return result;
            }
            else
            {
                // Fallback to direct model service call
                var modelRequest = new ModelRequest
                {
                    Id = workload.Id,
                    TaskType = MapWorkloadTypeToTaskType(workload.Type),
                    ComplexityLevel = workload.ComplexityLevel,
                    Content = workload.Content,
                    Repository = workload.Repository,
                    UserId = workload.UserId,
                    Context = workload.Context
                };

                var response = await _modelsService.RouteToSpecializedModelAsync(modelRequest);
                
                return new WorkloadResult
                {
                    WorkloadId = workload.Id,
                    Success = response.Success,
                    Output = response.Content,
                    ModelUsed = response.ModelUsed,
                    TokensUsed = response.TokensUsed,
                    ProcessingTime = DateTime.UtcNow - startTime,
                    Confidence = response.Confidence
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workload {WorkloadId}", workload.Id);
            return new WorkloadResult
            {
                WorkloadId = workload.Id,
                Success = false,
                Error = ex.Message,
                ProcessingTime = TimeSpan.Zero
            };
        }
    }

    private ModelCapability SelectBestModelForWorkload(ModelWorkload workload, List<ModelCapability> availableModels, WorkloadAnalysis analysis)
    {
        var taskType = MapWorkloadTypeToTaskType(workload.Type);
        
        var suitableModels = availableModels
            .Where(m => m.TaskType == taskType)
            .Where(m => workload.ComplexityLevel >= m.OptimalComplexityRange.Min && 
                       workload.ComplexityLevel <= m.OptimalComplexityRange.Max)
            .OrderByDescending(m => CalculateModelScore(m, workload, analysis))
            .ToList();

        return suitableModels.FirstOrDefault() ?? availableModels.First(m => m.TaskType == taskType);
    }

    private double CalculateModelScore(ModelCapability model, ModelWorkload workload, WorkloadAnalysis analysis)
    {
        double score = 0;
        
        // Base score for complexity match
        var complexityMatch = 1.0 - Math.Abs((int)workload.ComplexityLevel - (int)model.OptimalComplexityRange.Min) / 3.0;
        score += complexityMatch * 0.4;
        
        // Pilot program bonus
        if (model.PilotEnabled && analysis.RecommendedModelTypes.Contains(model.TaskType))
            score += 0.3;
        
        // Latency penalty
        var latencyScore = 1.0 - (model.EstimatedLatency.TotalMilliseconds / 5000.0);
        score += Math.Max(0, latencyScore) * 0.3;
        
        return score;
    }

    private Dictionary<WorkloadPriority, List<ModelWorkload>> GroupWorkloadsByPriority(List<ModelWorkload> workloads)
    {
        return workloads
            .GroupBy(w => w.Priority)
            .OrderByDescending(g => g.Key)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private TaskType MapWorkloadTypeToTaskType(string workloadType)
    {
        return workloadType.ToLowerInvariant() switch
        {
            "code_generation" => TaskType.CodeGeneration,
            "code_review" => TaskType.CodeReview,
            "documentation" => TaskType.Documentation,
            "issue_management" => TaskType.IssueManagement,
            "security_scanning" => TaskType.SecurityScanning,
            _ => TaskType.CodeGeneration
        };
    }

    private double CalculateParallelEfficiency(OrchestrationResult result)
    {
        if (!result.Workloads.Any()) return 0;
        
        var totalSequentialTime = result.Workloads.Sum(w => w.ProcessingTime.TotalMilliseconds);
        var actualParallelTime = result.TotalProcessingTime.TotalMilliseconds;
        
        return totalSequentialTime / Math.Max(actualParallelTime, 1);
    }

    // Additional helper methods...
    private ComplexityLevel AnalyzeContextComplexity(string context) => ComplexityLevel.Medium;
    private List<TaskType> IdentifyLikelyWorkloadTypes(string context) => new() { TaskType.CodeGeneration, TaskType.Documentation };
    private ResourceRequirements EstimateResourceRequirements(string context) => new();
    private int DetermineOptimalConcurrency(string context) => 3;
    private List<TaskType> RecommendModelTypes(string context) => new() { TaskType.CodeGeneration };
    private string GenerateSelectionReason(ModelWorkload workload, ModelCapability model) => $"Optimal for {workload.Type}";
    private double CalculateModelConfidence(ModelWorkload workload, ModelCapability model) => 0.85;
    private string DetermineCurrentRolloutPhase(PilotConfiguration config) => "Beta";
    private DateTime CalculateNextPhaseDate(PilotConfiguration config) => DateTime.UtcNow.AddDays(7);
}

// Supporting classes and enums
public enum WorkloadPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public class OrchestrationRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Repository { get; set; } = "";
    public string Context { get; set; } = "";
    public List<ModelWorkload> Workloads { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OrchestrationResult
{
    public string RequestId { get; set; } = "";
    public string Repository { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public bool Success { get; set; }
    public List<WorkloadResult> Workloads { get; set; } = new();
    public int ModelsUsed { get; set; }
    public double ParallelEfficiency { get; set; }
    public string ReynoldsComment { get; set; } = "";
}

public class ModelWorkload
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "";
    public string Content { get; set; } = "";
    public ComplexityLevel ComplexityLevel { get; set; }
    public WorkloadPriority Priority { get; set; }
    public string Repository { get; set; } = "";
    public string UserId { get; set; } = "";
    public Dictionary<string, object> Context { get; set; } = new();
    public string EnhancedContext { get; set; } = "";
    public ModelCapability? AssignedModel { get; set; }
}

public class WorkloadResult
{
    public string WorkloadId { get; set; } = "";
    public bool Success { get; set; }
    public string Output { get; set; } = "";
    public string? Error { get; set; }
    public string ModelUsed { get; set; } = "";
    public int TokensUsed { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public double Confidence { get; set; }
}

public class ModelSelectionResult
{
    public int TotalWorkloads { get; set; }
    public List<ModelAssignment> SelectedModels { get; set; } = new();
    public string SelectionStrategy { get; set; } = "";
    public double PilotParticipation { get; set; }
    public double EstimatedTotalLatency { get; set; }
}

public class ModelAssignment
{
    public string WorkloadId { get; set; } = "";
    public string WorkloadType { get; set; } = "";
    public ModelCapability SelectedModel { get; set; } = new();
    public string SelectionReason { get; set; } = "";
    public TimeSpan EstimatedLatency { get; set; }
    public double ConfidenceScore { get; set; }
}

public class WorkloadAnalysisRequest
{
    public string Repository { get; set; } = "";
    public List<ModelWorkload> Workloads { get; set; } = new();
    public WorkloadAnalysis Analysis { get; set; } = new();
}

public class WorkloadAnalysis
{
    public string Repository { get; set; } = "";
    public DateTime AnalysisTimestamp { get; set; }
    public ComplexityLevel ContextComplexity { get; set; }
    public List<TaskType> EstimatedWorkloadTypes { get; set; } = new();
    public ResourceRequirements ResourceRequirements { get; set; } = new();
    public int OptimalConcurrencyLevel { get; set; }
    public List<TaskType> RecommendedModelTypes { get; set; } = new();
}

public class ResourceRequirements
{
    public int EstimatedTokens { get; set; }
    public TimeSpan EstimatedProcessingTime { get; set; }
    public int RecommendedConcurrency { get; set; }
}

public class ParallelExecutionResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalExecutionTime { get; set; }
    public bool Success { get; set; }
    public List<WorkloadResult> WorkloadResults { get; set; } = new();
    public int ConcurrencyLevel { get; set; }
    public TimeSpan AverageLatency { get; set; }
}

public class PilotProgramStatus
{
    public bool Enabled { get; set; }
    public string CurrentPhase { get; set; } = "";
    public double ParticipationRate { get; set; }
    public int TotalParticipants { get; set; }
    public double SuccessRate { get; set; }
    public bool FeedbackEnabled { get; set; }
    public DateTime NextPhaseDate { get; set; }
    public int EligibleRepositories { get; set; }
    public int MaxConcurrentTasks { get; set; }
}

// Workload Processors
public abstract class WorkloadProcessor
{
    protected readonly IGitHubModelsService _modelsService;
    protected readonly ILogger _logger;

    protected WorkloadProcessor(IGitHubModelsService modelsService, ILogger logger)
    {
        _modelsService = modelsService;
        _logger = logger;
    }

    public abstract Task<WorkloadResult> ProcessAsync(ModelWorkload workload);
}

public class CodeGenerationProcessor : WorkloadProcessor
{
    public CodeGenerationProcessor(IGitHubModelsService modelsService, ILogger logger) : base(modelsService, logger) { }
    
    public override async Task<WorkloadResult> ProcessAsync(ModelWorkload workload)
    {
        var request = new ModelRequest
        {
            TaskType = TaskType.CodeGeneration,
            Content = workload.Content,
            ComplexityLevel = workload.ComplexityLevel,
            Repository = workload.Repository,
            UserId = workload.UserId
        };
        
        var response = await _modelsService.RouteToSpecializedModelAsync(request);
        
        return new WorkloadResult
        {
            WorkloadId = workload.Id,
            Success = response.Success,
            Output = response.Content,
            ModelUsed = response.ModelUsed,
            TokensUsed = response.TokensUsed,
            Confidence = response.Confidence
        };
    }
}

public class CodeReviewProcessor : WorkloadProcessor
{
    public CodeReviewProcessor(IGitHubModelsService modelsService, ILogger logger) : base(modelsService, logger) { }
    
    public override async Task<WorkloadResult> ProcessAsync(ModelWorkload workload)
    {
        var request = new ModelRequest
        {
            TaskType = TaskType.CodeReview,
            Content = workload.Content,
            ComplexityLevel = workload.ComplexityLevel,
            Repository = workload.Repository,
            UserId = workload.UserId
        };
        
        var response = await _modelsService.RouteToSpecializedModelAsync(request);
        
        return new WorkloadResult
        {
            WorkloadId = workload.Id,
            Success = response.Success,
            Output = response.Content,
            ModelUsed = response.ModelUsed,
            TokensUsed = response.TokensUsed,
            Confidence = response.Confidence
        };
    }
}

public class DocumentationProcessor : WorkloadProcessor
{
    public DocumentationProcessor(IGitHubModelsService modelsService, ILogger logger) : base(modelsService, logger) { }
    
    public override async Task<WorkloadResult> ProcessAsync(ModelWorkload workload)
    {
        var request = new ModelRequest
        {
            TaskType = TaskType.Documentation,
            Content = workload.Content,
            ComplexityLevel = workload.ComplexityLevel,
            Repository = workload.Repository,
            UserId = workload.UserId
        };
        
        var response = await _modelsService.RouteToSpecializedModelAsync(request);
        
        return new WorkloadResult
        {
            WorkloadId = workload.Id,
            Success = response.Success,
            Output = response.Content,
            ModelUsed = response.ModelUsed,
            TokensUsed = response.TokensUsed,
            Confidence = response.Confidence
        };
    }
}

public class IssueManagementProcessor : WorkloadProcessor
{
    private readonly IGitHubWorkflowOrchestrator _workflowOrchestrator;
    
    public IssueManagementProcessor(IGitHubModelsService modelsService, IGitHubWorkflowOrchestrator workflowOrchestrator, ILogger logger) 
        : base(modelsService, logger)
    {
        _workflowOrchestrator = workflowOrchestrator;
    }
    
    public override async Task<WorkloadResult> ProcessAsync(ModelWorkload workload)
    {
        var request = new ModelRequest
        {
            TaskType = TaskType.IssueManagement,
            Content = workload.Content,
            ComplexityLevel = workload.ComplexityLevel,
            Repository = workload.Repository,
            UserId = workload.UserId
        };
        
        var response = await _modelsService.RouteToSpecializedModelAsync(request);
        
        return new WorkloadResult
        {
            WorkloadId = workload.Id,
            Success = response.Success,
            Output = response.Content,
            ModelUsed = response.ModelUsed,
            TokensUsed = response.TokensUsed,
            Confidence = response.Confidence
        };
    }
}

public class SecurityScanningProcessor : WorkloadProcessor
{
    public SecurityScanningProcessor(IGitHubModelsService modelsService, ILogger logger) : base(modelsService, logger) { }
    
    public override async Task<WorkloadResult> ProcessAsync(ModelWorkload workload)
    {
        var request = new ModelRequest
        {
            TaskType = TaskType.SecurityScanning,
            Content = workload.Content,
            ComplexityLevel = workload.ComplexityLevel,
            Repository = workload.Repository,
            UserId = workload.UserId
        };
        
        var response = await _modelsService.RouteToSpecializedModelAsync(request);
        
        return new WorkloadResult
        {
            WorkloadId = workload.Id,
            Success = response.Success,
            Output = response.Content,
            ModelUsed = response.ModelUsed,
            TokensUsed = response.TokensUsed,
            Confidence = response.Confidence
        };
    }
}