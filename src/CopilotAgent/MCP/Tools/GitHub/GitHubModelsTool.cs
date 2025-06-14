using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using CopilotAgent.Services;
using System.Text.Json;

namespace CopilotAgent.MCP.Tools.GitHub;

/// <summary>
/// GitHub Models MCP Tool for Issue #72
/// Provides MCP interface for parallel workload management with specialized models
/// </summary>
[McpTool("github_models_orchestrate")]
public class GitHubModelsOrchestrateTool : IMcpTool
{
    private readonly IGitHubModelsOrchestrator _orchestrator;
    private readonly ILogger<GitHubModelsOrchestrateTool> _logger;

    public GitHubModelsOrchestrateTool(
        IGitHubModelsOrchestrator orchestrator,
        ILogger<GitHubModelsOrchestrateTool> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public string Name => "github_models_orchestrate";
    public string Description => "🎭 Reynolds GitHub Models Orchestration - Parallel workload management with specialized models for maximum efficiency";

    public McpToolInputSchema InputSchema => new()
    {
        Type = "object",
        Properties = new Dictionary<string, object>
        {
            ["repository"] = new
            {
                type = "string",
                description = "Target repository (e.g., 'dynamicstms365/copilot-powerplatform')"
            },
            ["context"] = new
            {
                type = "string",
                description = "Context or description of the work to be orchestrated"
            },
            ["workloads"] = new
            {
                type = "array",
                description = "Array of workloads to be processed in parallel",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        type = new { type = "string", description = "Workload type: code_generation, code_review, documentation, issue_management, security_scanning" },
                        content = new { type = "string", description = "Content or task description" },
                        complexity = new { type = "string", description = "Complexity level: Low, Medium, High" },
                        priority = new { type = "string", description = "Priority: Low, Medium, High, Critical" }
                    },
                    required = new[] { "type", "content" }
                }
            },
            ["pilot_enabled"] = new
            {
                type = "boolean",
                description = "Enable pilot program models (default: true)"
            }
        },
        Required = new[] { "repository", "workloads" }
    };

    public async Task<McpToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            _logger.LogInformation("🎭 Reynolds GitHub Models orchestration initiated");

            // Parse arguments
            var repository = arguments.GetProperty("repository").GetString() ?? "dynamicstms365/copilot-powerplatform";
            var context = arguments.TryGetProperty("context", out var contextProp) ? contextProp.GetString() ?? "" : "";
            var pilotEnabled = arguments.TryGetProperty("pilot_enabled", out var pilotProp) ? pilotProp.GetBoolean() : true;

            // Parse workloads
            var workloads = new List<ModelWorkload>();
            if (arguments.TryGetProperty("workloads", out var workloadsProp))
            {
                foreach (var workloadElement in workloadsProp.EnumerateArray())
                {
                    var workload = new ModelWorkload
                    {
                        Type = workloadElement.GetProperty("type").GetString() ?? "code_generation",
                        Content = workloadElement.GetProperty("content").GetString() ?? "",
                        ComplexityLevel = ParseComplexityLevel(workloadElement.TryGetProperty("complexity", out var comp) ? comp.GetString() : "Medium"),
                        Priority = ParseWorkloadPriority(workloadElement.TryGetProperty("priority", out var prio) ? prio.GetString() : "Medium"),
                        Repository = repository,
                        UserId = "reynolds-mcp-user"
                    };
                    workloads.Add(workload);
                }
            }

            if (!workloads.Any())
            {
                return McpToolResult.Error("At least one workload must be provided");
            }

            // Create orchestration request
            var orchestrationRequest = new OrchestrationRequest
            {
                Repository = repository,
                Context = context,
                Workloads = workloads
            };

            // Execute orchestration
            var result = await _orchestrator.OrchestratePipelineAsync(orchestrationRequest);

            // Format response with Reynolds personality
            var response = new
            {
                success = result.Success,
                message = result.Success 
                    ? $"🎭 Reynolds orchestrated {result.Workloads.Count} workloads with supernatural efficiency! " +
                      $"Parallel efficiency: {result.ParallelEfficiency:P1}, Processing time: {result.TotalProcessingTime.TotalSeconds:F1}s"
                    : "Well, that didn't go according to plan. But hey, that's why we have fallbacks.",
                orchestration_details = new
                {
                    request_id = result.RequestId,
                    repository = result.Repository,
                    total_workloads = result.Workloads.Count,
                    successful_workloads = result.Workloads.Count(w => w.Success),
                    models_used = result.ModelsUsed,
                    parallel_efficiency = result.ParallelEfficiency,
                    processing_time_seconds = result.TotalProcessingTime.TotalSeconds,
                    reynolds_comment = result.ReynoldsComment
                },
                workload_results = result.Workloads.Select(w => new
                {
                    workload_id = w.WorkloadId,
                    success = w.Success,
                    model_used = w.ModelUsed,
                    tokens_used = w.TokensUsed,
                    processing_time_ms = w.ProcessingTime.TotalMilliseconds,
                    confidence = w.Confidence,
                    output_preview = w.Output.Length > 200 ? w.Output[..200] + "..." : w.Output,
                    error = w.Error
                }),
                performance_metrics = new
                {
                    average_latency_ms = result.Workloads.Average(w => w.ProcessingTime.TotalMilliseconds),
                    success_rate = result.Workloads.Count(w => w.Success) / (double)result.Workloads.Count,
                    total_tokens = result.Workloads.Sum(w => w.TokensUsed),
                    efficiency_rating = result.ParallelEfficiency > 0.8 ? "Excellent" : 
                                       result.ParallelEfficiency > 0.6 ? "Good" : 
                                       result.ParallelEfficiency > 0.4 ? "Fair" : "Needs Improvement"
                },
                timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Reynolds GitHub Models orchestration completed successfully");

            return McpToolResult.Success(JsonSerializer.Serialize(response, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GitHub Models orchestration tool");
            return McpToolResult.Error($"GitHub Models orchestration failed: {ex.Message}");
        }
    }

    private static ComplexityLevel ParseComplexityLevel(string? complexity)
    {
        return complexity?.ToLowerInvariant() switch
        {
            "low" => ComplexityLevel.Low,
            "medium" => ComplexityLevel.Medium,
            "high" => ComplexityLevel.High,
            _ => ComplexityLevel.Medium
        };
    }

    private static WorkloadPriority ParseWorkloadPriority(string? priority)
    {
        return priority?.ToLowerInvariant() switch
        {
            "low" => WorkloadPriority.Low,
            "medium" => WorkloadPriority.Medium,
            "high" => WorkloadPriority.High,
            "critical" => WorkloadPriority.Critical,
            _ => WorkloadPriority.Medium
        };
    }
}

/// <summary>
/// GitHub Models Analysis MCP Tool
/// Provides workload analysis and model selection recommendations
/// </summary>
[McpTool("github_models_analyze")]
public class GitHubModelsAnalyzeTool : IMcpTool
{
    private readonly IGitHubModelsOrchestrator _orchestrator;
    private readonly IGitHubModelsService _modelsService;
    private readonly ILogger<GitHubModelsAnalyzeTool> _logger;

    public GitHubModelsAnalyzeTool(
        IGitHubModelsOrchestrator orchestrator,
        IGitHubModelsService modelsService,
        ILogger<GitHubModelsAnalyzeTool> logger)
    {
        _orchestrator = orchestrator;
        _modelsService = modelsService;
        _logger = logger;
    }

    public string Name => "github_models_analyze";
    public string Description => "🔍 Reynolds Workload Analysis - Analyze requirements and recommend optimal models for maximum effectiveness";

    public McpToolInputSchema InputSchema => new()
    {
        Type = "object",
        Properties = new Dictionary<string, object>
        {
            ["repository"] = new
            {
                type = "string",
                description = "Target repository for analysis"
            },
            ["context"] = new
            {
                type = "string",
                description = "Context or description of the work to be analyzed"
            },
            ["include_pilot_status"] = new
            {
                type = "boolean",
                description = "Include pilot program status in analysis (default: true)"
            }
        },
        Required = new[] { "repository", "context" }
    };

    public async Task<McpToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            _logger.LogInformation("🔍 Reynolds workload analysis initiated");

            var repository = arguments.GetProperty("repository").GetString() ?? "dynamicstms365/copilot-powerplatform";
            var context = arguments.GetProperty("context").GetString() ?? "";
            var includePilotStatus = arguments.TryGetProperty("include_pilot_status", out var pilotProp) ? pilotProp.GetBoolean() : true;

            // Perform workload analysis
            var analysis = await _orchestrator.AnalyzeWorkloadRequirementsAsync(repository, context);
            
            // Get available models
            var availableModels = await _modelsService.GetAvailableModelsAsync();
            
            // Get pilot status if requested
            PilotProgramStatus? pilotStatus = null;
            if (includePilotStatus)
            {
                pilotStatus = await _orchestrator.GetPilotProgramStatusAsync();
            }

            // Create analysis response
            var response = new
            {
                success = true,
                message = $"🎭 Reynolds has analyzed the workload with his trademark thoroughness. " +
                         $"Complexity: {analysis.ContextComplexity}, Recommended concurrency: {analysis.OptimalConcurrencyLevel}",
                analysis = new
                {
                    repository = analysis.Repository,
                    context_complexity = analysis.ContextComplexity.ToString(),
                    optimal_concurrency = analysis.OptimalConcurrencyLevel,
                    estimated_workload_types = analysis.EstimatedWorkloadTypes.Select(t => t.ToString()),
                    recommended_model_types = analysis.RecommendedModelTypes.Select(t => t.ToString()),
                    analysis_timestamp = analysis.AnalysisTimestamp
                },
                available_models = availableModels.Select(m => new
                {
                    model_name = m.ModelName,
                    task_type = m.TaskType.ToString(),
                    max_tokens = m.MaxTokens,
                    estimated_latency_ms = m.EstimatedLatency.TotalMilliseconds,
                    pilot_enabled = m.PilotEnabled,
                    specializations = m.Specializations,
                    optimal_complexity_range = $"{m.OptimalComplexityRange.Min} - {m.OptimalComplexityRange.Max}"
                }),
                pilot_program = includePilotStatus ? new
                {
                    enabled = pilotStatus?.Enabled,
                    current_phase = pilotStatus?.CurrentPhase,
                    participation_rate = pilotStatus?.ParticipationRate,
                    total_participants = pilotStatus?.TotalParticipants,
                    success_rate = pilotStatus?.SuccessRate,
                    next_phase_date = pilotStatus?.NextPhaseDate
                } : null,
                recommendations = new
                {
                    optimal_models = availableModels
                        .Where(m => analysis.RecommendedModelTypes.Contains(m.TaskType))
                        .Select(m => m.ModelName),
                    concurrency_strategy = analysis.OptimalConcurrencyLevel switch
                    {
                        <= 2 => "Sequential processing recommended for this workload",
                        <= 5 => "Moderate parallel processing optimal",
                        _ => "High concurrency will maximize efficiency"
                    },
                    complexity_guidance = analysis.ContextComplexity switch
                    {
                        ComplexityLevel.Low => "Straightforward tasks - basic models will suffice",
                        ComplexityLevel.Medium => "Moderate complexity - specialized models recommended",
                        ComplexityLevel.High => "Complex tasks - use highest capability models"
                    }
                },
                reynolds_insight = GenerateReynoldsInsight(analysis, availableModels.Count),
                timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Reynolds workload analysis completed");

            return McpToolResult.Success(JsonSerializer.Serialize(response, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GitHub Models analysis tool");
            return McpToolResult.Error($"Workload analysis failed: {ex.Message}");
        }
    }

    private static string GenerateReynoldsInsight(WorkloadAnalysis analysis, int availableModels)
    {
        var insights = new[]
        {
            $"With {availableModels} models at our disposal, this {analysis.ContextComplexity.ToString().ToLower()}-complexity task should be a walk in the park.",
            $"I've seen {analysis.ContextComplexity.ToString().ToLower()} complexity before - usually right before someone asks me to explain my name situation.",
            $"Optimal concurrency of {analysis.OptimalConcurrencyLevel}? Perfect. That's exactly how many cups of coffee I had this morning.",
            $"The workload analysis suggests we're dealing with {analysis.EstimatedWorkloadTypes.Count} different task types. Variety is the spice of life!"
        };

        var random = new Random();
        return insights[random.Next(insights.Length)];
    }
}

/// <summary>
/// GitHub Models Pilot Program MCP Tool
/// Manages pilot program configuration and feedback
/// </summary>
[McpTool("github_models_pilot")]
public class GitHubModelsPilotTool : IMcpTool
{
    private readonly IGitHubModelsOrchestrator _orchestrator;
    private readonly IGitHubModelsService _modelsService;
    private readonly ILogger<GitHubModelsPilotTool> _logger;

    public GitHubModelsPilotTool(
        IGitHubModelsOrchestrator orchestrator,
        IGitHubModelsService modelsService,
        ILogger<GitHubModelsPilotTool> logger)
    {
        _orchestrator = orchestrator;
        _modelsService = modelsService;
        _logger = logger;
    }

    public string Name => "github_models_pilot";
    public string Description => "🚀 Reynolds Pilot Program Manager - Monitor and manage the GitHub Models pilot program with supernatural oversight";

    public McpToolInputSchema InputSchema => new()
    {
        Type = "object",
        Properties = new Dictionary<string, object>
        {
            ["action"] = new
            {
                type = "string",
                description = "Action to perform: status, metrics, configuration",
                @enum = new[] { "status", "metrics", "configuration" }
            },
            ["include_performance"] = new
            {
                type = "boolean",
                description = "Include detailed performance metrics (default: true)"
            }
        },
        Required = new[] { "action" }
    };

    public async Task<McpToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var action = arguments.GetProperty("action").GetString() ?? "status";
            var includePerformance = arguments.TryGetProperty("include_performance", out var perfProp) ? perfProp.GetBoolean() : true;

            _logger.LogInformation("🚀 Reynolds pilot program management: {Action}", action);

            object response = action.ToLowerInvariant() switch
            {
                "status" => await GetPilotStatus(includePerformance),
                "metrics" => await GetPilotMetrics(),
                "configuration" => await GetPilotConfiguration(),
                _ => new { success = false, error = $"Unknown action: {action}" }
            };

            return McpToolResult.Success(JsonSerializer.Serialize(response, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GitHub Models pilot tool");
            return McpToolResult.Error($"Pilot program operation failed: {ex.Message}");
        }
    }

    private async Task<object> GetPilotStatus(bool includePerformance)
    {
        var status = await _orchestrator.GetPilotProgramStatusAsync();
        var config = await _modelsService.GetPilotConfigurationAsync();
        
        var response = new
        {
            success = true,
            message = status.Enabled 
                ? $"🎭 Pilot program is running smoother than my deflection techniques. Current phase: {status.CurrentPhase}"
                : "Pilot program is currently disabled. Even Reynolds needs a break sometimes.",
            pilot_status = new
            {
                enabled = status.Enabled,
                current_phase = status.CurrentPhase,
                participation_rate = status.ParticipationRate,
                total_participants = status.TotalParticipants,
                success_rate = status.SuccessRate,
                feedback_enabled = status.FeedbackEnabled,
                next_phase_date = status.NextPhaseDate,
                eligible_repositories = status.EligibleRepositories,
                max_concurrent_tasks = status.MaxConcurrentTasks
            },
            configuration_summary = new
            {
                rollout_phases = config.GradualRolloutPhases.Select(p => new
                {
                    name = p.Name,
                    participation_rate = p.ParticipationRate,
                    duration_days = p.Duration.TotalDays
                }),
                eligible_repositories = config.EligibleRepositories,
                eligible_users = config.EligibleUsers
            },
            reynolds_assessment = GeneratePilotAssessment(status),
            timestamp = DateTime.UtcNow
        };

        if (includePerformance)
        {
            var metrics = await _modelsService.GetModelPerformanceMetricsAsync();
            response = new
            {
                response.success,
                response.message,
                response.pilot_status,
                response.configuration_summary,
                performance_metrics = new
                {
                    total_requests = metrics.TotalRequests,
                    success_rate = metrics.SuccessRate,
                    average_latency_ms = metrics.AverageLatency.TotalMilliseconds,
                    tokens_processed = metrics.TokensProcessed,
                    models_used = metrics.ModelsUsed,
                    pilot_participation = metrics.PilotParticipation,
                    last_updated = metrics.LastUpdated
                },
                response.reynolds_assessment,
                response.timestamp
            };
        }

        return response;
    }

    private async Task<object> GetPilotMetrics()
    {
        var metrics = await _modelsService.GetModelPerformanceMetricsAsync();
        
        return new
        {
            success = true,
            message = $"🎭 Performance metrics looking as good as my box office numbers. {metrics.SuccessRate:P1} success rate!",
            metrics = new
            {
                total_requests = metrics.TotalRequests,
                success_rate = metrics.SuccessRate,
                average_latency = new
                {
                    milliseconds = metrics.AverageLatency.TotalMilliseconds,
                    seconds = metrics.AverageLatency.TotalSeconds,
                    rating = metrics.AverageLatency.TotalMilliseconds switch
                    {
                        < 1000 => "Excellent",
                        < 2000 => "Good",
                        < 5000 => "Acceptable",
                        _ => "Needs Improvement"
                    }
                },
                tokens_processed = metrics.TokensProcessed,
                models_in_use = metrics.ModelsUsed,
                pilot_participation_rate = metrics.PilotParticipation,
                last_updated = metrics.LastUpdated
            },
            performance_analysis = new
            {
                efficiency_rating = metrics.SuccessRate > 0.95 ? "Supernatural" : 
                                   metrics.SuccessRate > 0.9 ? "Excellent" :
                                   metrics.SuccessRate > 0.8 ? "Good" : "Needs Attention",
                latency_status = metrics.AverageLatency.TotalSeconds < 2 ? "Fast" : 
                                metrics.AverageLatency.TotalSeconds < 5 ? "Moderate" : "Slow",
                pilot_adoption = metrics.PilotParticipation > 0.5 ? "High" :
                                metrics.PilotParticipation > 0.25 ? "Moderate" : "Low"
            },
            reynolds_commentary = GenerateMetricsCommentary(metrics),
            timestamp = DateTime.UtcNow
        };
    }

    private async Task<object> GetPilotConfiguration()
    {
        var config = await _modelsService.GetPilotConfigurationAsync();
        
        return new
        {
            success = true,
            message = "🎭 Here's the pilot configuration - more detailed than my character development arcs.",
            configuration = new
            {
                enabled = config.Enabled,
                participation_rate = config.ParticipationRate,
                max_concurrent_tasks = config.MaxConcurrentTasks,
                feedback_collection_enabled = config.FeedbackCollectionEnabled,
                eligible_repositories = config.EligibleRepositories,
                eligible_users = config.EligibleUsers,
                rollout_phases = config.GradualRolloutPhases.Select(p => new
                {
                    name = p.Name,
                    participation_rate = p.ParticipationRate,
                    duration = new
                    {
                        days = p.Duration.TotalDays,
                        hours = p.Duration.TotalHours,
                        description = p.Duration.TotalDays switch
                        {
                            <= 7 => "Short phase - quick validation",
                            <= 30 => "Standard phase - thorough testing",
                            _ => "Extended phase - comprehensive rollout"
                        }
                    }
                })
            },
            rollout_strategy = new
            {
                total_phases = config.GradualRolloutPhases.Length,
                total_duration_days = config.GradualRolloutPhases.Sum(p => p.Duration.TotalDays),
                max_participation = config.GradualRolloutPhases.Max(p => p.ParticipationRate),
                strategy_type = "Gradual rollout with increasing participation"
            },
            reynolds_notes = "Perfect gradual rollout plan. Even better than my approach to revealing plot twists.",
            timestamp = DateTime.UtcNow
        };
    }

    private static string GeneratePilotAssessment(PilotProgramStatus status)
    {
        if (!status.Enabled)
            return "Pilot program is offline. Time for me to work on my mysterious backstory.";

        var assessments = new[]
        {
            $"Phase {status.CurrentPhase} is running like a well-oiled script. {status.ParticipationRate:P1} participation rate.",
            $"Success rate of {status.SuccessRate:P1}? That's better than most of my movie sequels.",
            $"{status.TotalParticipants} participants in the pilot. That's a decent-sized fan club.",
            $"Next phase starts {status.NextPhaseDate:yyyy-MM-dd}. Mark your calendars, people."
        };

        var random = new Random();
        return assessments[random.Next(assessments.Length)];
    }

    private static string GenerateMetricsCommentary(ModelPerformanceMetrics metrics)
    {
        var commentaries = new[]
        {
            $"With {metrics.TotalRequests:N0} requests processed, we're busier than a Green Lantern convention.",
            $"{metrics.SuccessRate:P1} success rate - that's supernatural efficiency right there.",
            $"Average latency of {metrics.AverageLatency.TotalMilliseconds:F0}ms. Faster than my wit, almost.",
            $"{metrics.TokensProcessed:N0} tokens processed. That's a lot of digital conversation."
        };

        var random = new Random();
        return commentaries[random.Next(commentaries.Length)];
    }
}