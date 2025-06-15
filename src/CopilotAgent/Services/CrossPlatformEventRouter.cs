using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using CopilotAgent.Bot;
using CopilotAgent.Services;
using Shared.Models;

namespace CopilotAgent.Services;

/// <summary>
/// Cross-Platform Event Router for Issue #73
/// Intelligent event classification and coordination across GitHub, Teams, and Azure platforms
/// Integrates with existing loop prevention and GitHub Models infrastructure
/// </summary>
public interface ICrossPlatformEventRouter
{
    Task<EventRoutingResult> RouteEventAsync(PlatformEvent platformEvent);
    Task<List<PlatformRoute>> AnalyzeRoutingOptionsAsync(PlatformEvent platformEvent);
    Task<bool> ShouldRouteToTeamsAsync(PlatformEvent platformEvent);
    Task<bool> ShouldRouteToGitHubAsync(PlatformEvent platformEvent);
    Task<bool> ShouldRouteToAzureAsync(PlatformEvent platformEvent);
    Task<EventClassification> ClassifyEventAsync(PlatformEvent platformEvent);
}

public class CrossPlatformEventRouter : ICrossPlatformEventRouter
{
    private readonly ILogger<CrossPlatformEventRouter> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEventClassificationService _classificationService;
    private readonly IReynoldsTeamsService _teamsService;
    private readonly IGitHubWorkflowOrchestrator _githubOrchestrator;
    private readonly IGitHubModelsOrchestrator _modelsOrchestrator;
    private readonly ISecurityAuditService _auditService;
    private readonly IAzureEventProcessor _azureEventProcessor;
    private readonly EventRoutingMetrics _metrics;
    
    // Loop prevention integration
    private readonly Dictionary<string, DateTime> _eventProcessingHistory;
    private readonly SemaphoreSlim _routingSemaphore;

    public CrossPlatformEventRouter(
        ILogger<CrossPlatformEventRouter> logger,
        IConfiguration configuration,
        IEventClassificationService classificationService,
        IReynoldsTeamsService teamsService,
        IGitHubWorkflowOrchestrator githubOrchestrator,
        IGitHubModelsOrchestrator modelsOrchestrator,
        ISecurityAuditService auditService,
        IAzureEventProcessor azureEventProcessor,
        EventRoutingMetrics metrics)
    {
        _logger = logger;
        _configuration = configuration;
        _classificationService = classificationService;
        _teamsService = teamsService;
        _githubOrchestrator = githubOrchestrator;
        _modelsOrchestrator = modelsOrchestrator;
        _auditService = auditService;
        _azureEventProcessor = azureEventProcessor;
        _metrics = metrics;
        
        _eventProcessingHistory = new Dictionary<string, DateTime>();
        _routingSemaphore = new SemaphoreSlim(10, 10); // Concurrency control
    }

    public async Task<EventRoutingResult> RouteEventAsync(PlatformEvent platformEvent)
    {
        var routingId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;
        
        await _routingSemaphore.WaitAsync();
        try
        {
            _logger.LogInformation("🎭 Reynolds Cross-Platform Router processing event: {EventType} from {Platform}", 
                platformEvent.EventType, platformEvent.SourcePlatform);

            // Step 1: Check for duplicate processing (loop prevention integration)
            if (await IsDuplicateEventAsync(platformEvent))
            {
                _logger.LogInformation("Duplicate event detected, skipping processing: {EventId}", platformEvent.EventId);
                return EventRoutingResult.Skipped("Duplicate event detected", routingId);
            }

            // Step 2: Classify the event
            var classification = await ClassifyEventAsync(platformEvent);
            
            // Step 3: Analyze routing options
            var routingOptions = await AnalyzeRoutingOptionsAsync(platformEvent);
            
            // Step 4: Execute routing with intelligent coordination
            var routingResults = new List<PlatformRoutingResult>();
            
            foreach (var route in routingOptions.Where(r => r.ShouldRoute))
            {
                try
                {
                    var routeResult = await ExecuteRouteAsync(platformEvent, route, classification);
                    routingResults.Add(routeResult);
                    
                    _logger.LogInformation("✅ Successfully routed to {Platform}: {Success}", 
                        route.TargetPlatform, routeResult.Success);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to route to {Platform}", route.TargetPlatform);
                    routingResults.Add(new PlatformRoutingResult
                    {
                        TargetPlatform = route.TargetPlatform,
                        Success = false,
                        Error = ex.Message,
                        ProcessingTime = DateTime.UtcNow - startTime
                    });
                }
            }

            // Step 5: Record event processing to prevent duplicates
            await RecordEventProcessingAsync(platformEvent);

            // Step 6: Update metrics and audit trail
            var overallSuccess = routingResults.All(r => r.Success);
            await UpdateMetricsAsync(platformEvent, classification, routingResults, startTime);
            
            await _auditService.LogEventAsync(
                "Cross_Platform_Event_Routed",
                repository: platformEvent.Repository,
                action: $"{platformEvent.SourcePlatform}_To_Multi_Platform",
                result: overallSuccess ? "SUCCESS" : "PARTIAL_SUCCESS",
                details: new {
                    EventType = platformEvent.EventType,
                    Classification = classification.Category,
                    Priority = classification.Priority,
                    RoutesExecuted = routingResults.Count,
                    SuccessfulRoutes = routingResults.Count(r => r.Success),
                    ProcessingTime = (DateTime.UtcNow - startTime).TotalMilliseconds
                });

            return new EventRoutingResult
            {
                RoutingId = routingId,
                Success = overallSuccess,
                Classification = classification,
                RouteResults = routingResults,
                ProcessingTime = DateTime.UtcNow - startTime,
                ReynoldsComment = GenerateReynoldsComment(platformEvent, classification, routingResults)
            };
        }
        finally
        {
            _routingSemaphore.Release();
        }
    }

    public async Task<List<PlatformRoute>> AnalyzeRoutingOptionsAsync(PlatformEvent platformEvent)
    {
        var routes = new List<PlatformRoute>();

        // GitHub routing analysis
        if (await ShouldRouteToGitHubAsync(platformEvent))
        {
            routes.Add(new PlatformRoute
            {
                TargetPlatform = "GitHub",
                ShouldRoute = true,
                RoutingReason = DetermineGitHubRoutingReason(platformEvent),
                Priority = DetermineRoutingPriority(platformEvent, "GitHub"),
                EstimatedLatency = TimeSpan.FromMilliseconds(500)
            });
        }

        // Teams routing analysis
        if (await ShouldRouteToTeamsAsync(platformEvent))
        {
            routes.Add(new PlatformRoute
            {
                TargetPlatform = "Teams",
                ShouldRoute = true,
                RoutingReason = DetermineTeamsRoutingReason(platformEvent),
                Priority = DetermineRoutingPriority(platformEvent, "Teams"),
                EstimatedLatency = TimeSpan.FromMilliseconds(1000)
            });
        }

        // Azure routing analysis
        if (await ShouldRouteToAzureAsync(platformEvent))
        {
            routes.Add(new PlatformRoute
            {
                TargetPlatform = "Azure",
                ShouldRoute = true,
                RoutingReason = DetermineAzureRoutingReason(platformEvent),
                Priority = DetermineRoutingPriority(platformEvent, "Azure"),
                EstimatedLatency = TimeSpan.FromMilliseconds(750)
            });
        }

        // Sort by priority for optimal execution order
        return routes.OrderByDescending(r => r.Priority).ToList();
    }

    public async Task<bool> ShouldRouteToTeamsAsync(PlatformEvent platformEvent)
    {
        // Teams routing logic based on event classification
        return platformEvent.SourcePlatform switch
        {
            "GitHub" => await ShouldNotifyTeamsForGitHubEvent(platformEvent),
            "Azure" => await ShouldNotifyTeamsForAzureEvent(platformEvent),
            _ => false
        };
    }

    public async Task<bool> ShouldRouteToGitHubAsync(PlatformEvent platformEvent)
    {
        // GitHub routing logic
        return platformEvent.SourcePlatform switch
        {
            "Teams" => await ShouldCreateGitHubActionForTeamsEvent(platformEvent),
            "Azure" => await ShouldCreateGitHubIssueForAzureEvent(platformEvent),
            _ => false
        };
    }

    public async Task<bool> ShouldRouteToAzureAsync(PlatformEvent platformEvent)
    {
        // Azure routing logic
        return platformEvent.SourcePlatform switch
        {
            "GitHub" => await ShouldTriggerAzureActionForGitHubEvent(platformEvent),
            "Teams" => await ShouldUpdateAzureResourceForTeamsEvent(platformEvent),
            _ => false
        };
    }

    public async Task<EventClassification> ClassifyEventAsync(PlatformEvent platformEvent)
    {
        return await _classificationService.ClassifyEventAsync(platformEvent);
    }

    // Private helper methods

    private async Task<bool> IsDuplicateEventAsync(PlatformEvent platformEvent)
    {
        var eventKey = $"{platformEvent.SourcePlatform}:{platformEvent.EventType}:{platformEvent.EventId}";
        
        if (_eventProcessingHistory.ContainsKey(eventKey))
        {
            var lastProcessed = _eventProcessingHistory[eventKey];
            // Consider duplicate if processed within last 5 minutes
            return DateTime.UtcNow - lastProcessed < TimeSpan.FromMinutes(5);
        }
        
        return false;
    }

    private async Task RecordEventProcessingAsync(PlatformEvent platformEvent)
    {
        var eventKey = $"{platformEvent.SourcePlatform}:{platformEvent.EventType}:{platformEvent.EventId}";
        _eventProcessingHistory[eventKey] = DateTime.UtcNow;
        
        // Cleanup old entries (keep only last hour)
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var oldKeys = _eventProcessingHistory
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();
            
        foreach (var oldKey in oldKeys)
        {
            _eventProcessingHistory.Remove(oldKey);
        }
    }

    private async Task<PlatformRoutingResult> ExecuteRouteAsync(
        PlatformEvent platformEvent, 
        PlatformRoute route, 
        EventClassification classification)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var success = route.TargetPlatform switch
            {
                "GitHub" => await ExecuteGitHubRouting(platformEvent, classification),
                "Teams" => await ExecuteTeamsRouting(platformEvent, classification),
                "Azure" => await ExecuteAzureRouting(platformEvent, classification),
                _ => false
            };

            return new PlatformRoutingResult
            {
                TargetPlatform = route.TargetPlatform,
                Success = success,
                ProcessingTime = DateTime.UtcNow - startTime,
                Message = $"Successfully routed {platformEvent.EventType} to {route.TargetPlatform}"
            };
        }
        catch (Exception ex)
        {
            return new PlatformRoutingResult
            {
                TargetPlatform = route.TargetPlatform,
                Success = false,
                Error = ex.Message,
                ProcessingTime = DateTime.UtcNow - startTime
            };
        }
    }

    private async Task<bool> ExecuteGitHubRouting(PlatformEvent platformEvent, EventClassification classification)
    {
        switch (platformEvent.SourcePlatform)
        {
            case "Teams":
                return await HandleTeamsToGitHubRouting(platformEvent, classification);
            case "Azure":
                return await HandleAzureToGitHubRouting(platformEvent, classification);
            default:
                return false;
        }
    }

    private async Task<bool> ExecuteTeamsRouting(PlatformEvent platformEvent, EventClassification classification)
    {
        switch (platformEvent.SourcePlatform)
        {
            case "GitHub":
                return await HandleGitHubToTeamsRouting(platformEvent, classification);
            case "Azure":
                return await HandleAzureToTeamsRouting(platformEvent, classification);
            default:
                return false;
        }
    }

    private async Task<bool> ExecuteAzureRouting(PlatformEvent platformEvent, EventClassification classification)
    {
        switch (platformEvent.SourcePlatform)
        {
            case "GitHub":
                return await HandleGitHubToAzureRouting(platformEvent, classification);
            case "Teams":
                return await HandleTeamsToAzureRouting(platformEvent, classification);
            default:
                return false;
        }
    }

    // GitHub to Teams routing
    private async Task<bool> HandleGitHubToTeamsRouting(PlatformEvent platformEvent, EventClassification classification)
    {
        try
        {
            var message = GenerateTeamsMessage(platformEvent, classification);
            var targetUsers = await DetermineTeamsTargetUsers(platformEvent, classification);

            foreach (var user in targetUsers)
            {
                if (classification.Priority >= EventPriority.High)
                {
                    await _teamsService.SendOrganizationalUpdateAsync(user, message);
                }
                else if (platformEvent.EventType.Contains("pull_request") && 
                         classification.Category == "scope_creep")
                {
                    await _teamsService.NotifyAboutScopeCreepAsync(user, 
                        platformEvent.Metadata.GetValueOrDefault("pr_number", "unknown").ToString()!,
                        platformEvent.Repository!);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing GitHub event to Teams");
            return false;
        }
    }

    // Teams to GitHub routing
    private async Task<bool> HandleTeamsToGitHubRouting(PlatformEvent platformEvent, EventClassification classification)
    {
        try
        {
            // Analyze Teams message for GitHub actions
            if (classification.Category == "github_command")
            {
                var prompt = platformEvent.Content ?? "";
                var promptRequest = new PromptBasedActionRequest
                {
                    Prompt = prompt,
                    Repository = platformEvent.Repository,
                    OnBehalfOf = platformEvent.UserId
                };

                var result = await _githubOrchestrator.ExecutePromptBasedActionAsync(promptRequest);
                return result.Success;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing Teams event to GitHub");
            return false;
        }
    }

    // Azure routing implementations
    private async Task<bool> HandleGitHubToAzureRouting(PlatformEvent platformEvent, EventClassification classification)
    {
        try
        {
            if (classification.Category == "deployment" || classification.Category == "workflow")
            {
                await _azureEventProcessor.ProcessGitHubEventAsync(platformEvent);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing GitHub event to Azure");
            return false;
        }
    }

    private async Task<bool> HandleAzureToTeamsRouting(PlatformEvent platformEvent, EventClassification classification)
    {
        try
        {
            var message = GenerateAzureTeamsMessage(platformEvent, classification);
            var targetUsers = await DetermineTeamsTargetUsers(platformEvent, classification);

            foreach (var user in targetUsers)
            {
                await _teamsService.SendOrganizationalUpdateAsync(user, message);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing Azure event to Teams");
            return false;
        }
    }

    private async Task<bool> HandleAzureToGitHubRouting(PlatformEvent platformEvent, EventClassification classification)
    {
        try
        {
            if (classification.Category == "infrastructure_alert" && classification.Priority >= EventPriority.High)
            {
                var issueRequest = new CreateIssueRequest
                {
                    Repository = platformEvent.Repository ?? "dynamicstms365/copilot-powerplatform",
                    Title = $"Azure Infrastructure Alert: {platformEvent.EventType}",
                    Body = GenerateGitHubIssueBody(platformEvent, classification),
                    Labels = new[] { "infrastructure", "azure", "alert" }
                };

                var result = await _githubOrchestrator.CreateIssueWorkflowAsync(issueRequest);
                return result.Success;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing Azure event to GitHub");
            return false;
        }
    }

    private async Task<bool> HandleTeamsToAzureRouting(PlatformEvent platformEvent, EventClassification classification)
    {
        try
        {
            if (classification.Category == "resource_management")
            {
                await _azureEventProcessor.ProcessTeamsEventAsync(platformEvent);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing Teams event to Azure");
            return false;
        }
    }

    // Helper methods for routing decisions
    private async Task<bool> ShouldNotifyTeamsForGitHubEvent(PlatformEvent platformEvent)
    {
        return platformEvent.EventType switch
        {
            "pull_request" when platformEvent.Action == "opened" => true,
            "issues" when platformEvent.Action == "opened" => true,
            "workflow_run" when platformEvent.Action == "failed" => true,
            "discussion" when platformEvent.Action == "created" => true,
            _ => false
        };
    }

    private async Task<bool> ShouldNotifyTeamsForAzureEvent(PlatformEvent platformEvent)
    {
        return platformEvent.EventType switch
        {
            "container_instance_failed" => true,
            "resource_health_degraded" => true,
            "deployment_completed" => true,
            _ => false
        };
    }

    private async Task<bool> ShouldCreateGitHubActionForTeamsEvent(PlatformEvent platformEvent)
    {
        var content = platformEvent.Content?.ToLowerInvariant() ?? "";
        return content.Contains("create issue") || 
               content.Contains("create discussion") ||
               content.Contains("github");
    }

    private async Task<bool> ShouldCreateGitHubIssueForAzureEvent(PlatformEvent platformEvent)
    {
        return platformEvent.EventType switch
        {
            "container_instance_failed" => true,
            "resource_health_critical" => true,
            _ => false
        };
    }

    private async Task<bool> ShouldTriggerAzureActionForGitHubEvent(PlatformEvent platformEvent)
    {
        return platformEvent.EventType == "workflow_run" && 
               platformEvent.Action == "completed" &&
               platformEvent.Metadata.ContainsKey("trigger_deployment");
    }

    private async Task<bool> ShouldUpdateAzureResourceForTeamsEvent(PlatformEvent platformEvent)
    {
        var content = platformEvent.Content?.ToLowerInvariant() ?? "";
        return content.Contains("deploy") || content.Contains("scale") || content.Contains("restart");
    }

    // Message generation methods
    private string GenerateTeamsMessage(PlatformEvent platformEvent, EventClassification classification)
    {
        return $@"🎭 **Reynolds Cross-Platform Update**

**Event**: {platformEvent.EventType} from {platformEvent.SourcePlatform}
**Repository**: {platformEvent.Repository}
**Priority**: {classification.Priority}

{platformEvent.Content}

*Reynolds coordinating across platforms with Maximum Effort™*";
    }

    private string GenerateAzureTeamsMessage(PlatformEvent platformEvent, EventClassification classification)
    {
        return $@"⚡ **Reynolds Azure Intelligence**

**Azure Event**: {platformEvent.EventType}
**Priority**: {classification.Priority}

{platformEvent.Content}

This is the kind of infrastructure awareness that keeps our entire organization running smoothly!

*Azure orchestration meets Reynolds charm.*";
    }

    private string GenerateGitHubIssueBody(PlatformEvent platformEvent, EventClassification classification)
    {
        return $@"## Azure Infrastructure Alert

**Event Type**: {platformEvent.EventType}
**Source Platform**: {platformEvent.SourcePlatform}
**Priority**: {classification.Priority}
**Classification**: {classification.Category}

### Details
{platformEvent.Content}

### Event Metadata
{JsonSerializer.Serialize(platformEvent.Metadata, new JsonSerializerOptions { WriteIndented = true })}

### Recommended Actions
{GenerateRecommendedActions(platformEvent, classification)}

---
*This issue was automatically created by Reynolds Cross-Platform Event Router*
*Reynolds: Maximum Effort™ on infrastructure monitoring*";
    }

    private string GenerateRecommendedActions(PlatformEvent platformEvent, EventClassification classification)
    {
        return classification.Category switch
        {
            "infrastructure_alert" => "1. Check Azure portal for detailed logs\n2. Verify container health\n3. Review resource utilization",
            "deployment" => "1. Verify deployment status\n2. Run health checks\n3. Monitor application logs",
            _ => "1. Investigate event details\n2. Check related systems\n3. Monitor for related events"
        };
    }

    private async Task<List<string>> DetermineTeamsTargetUsers(PlatformEvent platformEvent, EventClassification classification)
    {
        // Default to configuration-based user list
        var defaultUsers = _configuration.GetSection("CrossPlatformRouting:DefaultTeamsUsers").Get<string[]>() 
            ?? new[] { "admin@yourdomain.com" };

        if (classification.Priority >= EventPriority.Critical)
        {
            // Include escalation users for critical events
            var escalationUsers = _configuration.GetSection("CrossPlatformRouting:EscalationUsers").Get<string[]>()
                ?? Array.Empty<string>();
            return defaultUsers.Concat(escalationUsers).ToList();
        }

        return defaultUsers.ToList();
    }

    private string DetermineGitHubRoutingReason(PlatformEvent platformEvent)
    {
        return platformEvent.SourcePlatform switch
        {
            "Teams" => "Teams command requires GitHub action",
            "Azure" => "Azure event requires GitHub issue creation",
            _ => "Cross-platform coordination needed"
        };
    }

    private string DetermineTeamsRoutingReason(PlatformEvent platformEvent)
    {
        return platformEvent.SourcePlatform switch
        {
            "GitHub" => "GitHub event requires team notification",
            "Azure" => "Azure event requires operational awareness",
            _ => "Cross-platform coordination needed"
        };
    }

    private string DetermineAzureRoutingReason(PlatformEvent platformEvent)
    {
        return platformEvent.SourcePlatform switch
        {
            "GitHub" => "GitHub workflow requires Azure deployment",
            "Teams" => "Teams command requires Azure resource action",
            _ => "Cross-platform coordination needed"
        };
    }

    private EventPriority DetermineRoutingPriority(PlatformEvent platformEvent, string targetPlatform)
    {
        // Priority matrix based on event type and target platform
        return (platformEvent.EventType, targetPlatform) switch
        {
            ("workflow_run", "Teams") when platformEvent.Action == "failed" => EventPriority.High,
            ("container_instance_failed", "Teams") => EventPriority.Critical,
            ("container_instance_failed", "GitHub") => EventPriority.High,
            ("pull_request", "Teams") => EventPriority.Medium,
            _ => EventPriority.Low
        };
    }

    private string GenerateReynoldsComment(PlatformEvent platformEvent, EventClassification classification, List<PlatformRoutingResult> results)
    {
        var successCount = results.Count(r => r.Success);
        var totalCount = results.Count;

        return successCount == totalCount 
            ? $"Flawless cross-platform coordination! Routed {platformEvent.EventType} to {totalCount} platforms with supernatural efficiency. *adjusts imaginary tie*"
            : $"Mostly successful routing ({successCount}/{totalCount}). Even Reynolds has the occasional hiccup, but we're still better than most project managers! *deflects with charm*";
    }

    private async Task UpdateMetricsAsync(
        PlatformEvent platformEvent, 
        EventClassification classification, 
        List<PlatformRoutingResult> results, 
        DateTime startTime)
    {
        var processingTime = DateTime.UtcNow - startTime;
        await _metrics.RecordEventRoutingAsync(platformEvent.SourcePlatform, results.Count, 
            results.Count(r => r.Success), processingTime);
    }
}

// Supporting classes for cross-platform event routing
public class PlatformEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = "";
    public string SourcePlatform { get; set; } = "";
    public string Action { get; set; } = "";
    public string? Repository { get; set; }
    public string? UserId { get; set; }
    public string? Content { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class EventRoutingResult
{
    public string RoutingId { get; set; } = "";
    public bool Success { get; set; }
    public EventClassification? Classification { get; set; }
    public List<PlatformRoutingResult> RouteResults { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
    public string ReynoldsComment { get; set; } = "";

    public static EventRoutingResult Skipped(string reason, string routingId)
    {
        return new EventRoutingResult
        {
            RoutingId = routingId,
            Success = true,
            ReynoldsComment = $"Skipped processing: {reason}. Reynolds knows when not to overcomplicate things!"
        };
    }
}

public class PlatformRoutingResult
{
    public string TargetPlatform { get; set; } = "";
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}

public class PlatformRoute
{
    public string TargetPlatform { get; set; } = "";
    public bool ShouldRoute { get; set; }
    public string RoutingReason { get; set; } = "";
    public EventPriority Priority { get; set; }
    public TimeSpan EstimatedLatency { get; set; }
}

public enum EventPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}