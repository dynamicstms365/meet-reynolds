using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using CopilotAgent.Services;
using Shared.Models;

namespace CopilotAgent.Controllers;

/// <summary>
/// Cross-Platform Event Controller for Issue #73
/// Provides REST endpoints for cross-platform event routing and management
/// Integrates with Teams, GitHub, and Azure event sources
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CrossPlatformEventController : ControllerBase
{
    private readonly ILogger<CrossPlatformEventController> _logger;
    private readonly ICrossPlatformEventRouter _eventRouter;
    private readonly IEventClassificationService _classificationService;
    private readonly IEventRoutingMetrics _metrics;
    private readonly ISecurityAuditService _auditService;

    public CrossPlatformEventController(
        ILogger<CrossPlatformEventController> logger,
        ICrossPlatformEventRouter eventRouter,
        IEventClassificationService classificationService,
        IEventRoutingMetrics metrics,
        ISecurityAuditService auditService)
    {
        _logger = logger;
        _eventRouter = eventRouter;
        _classificationService = classificationService;
        _metrics = metrics;
        _auditService = auditService;
    }

    /// <summary>
    /// Route a platform event across multiple platforms
    /// </summary>
    [HttpPost("route")]
    public async Task<ActionResult<EventRoutingResult>> RouteEvent([FromBody] PlatformEvent platformEvent)
    {
        try
        {
            _logger.LogInformation("🎭 Reynolds Cross-Platform Router received event: {EventType} from {Platform}", 
                platformEvent.EventType, platformEvent.SourcePlatform);

            var result = await _eventRouter.RouteEventAsync(platformEvent);

            await _auditService.LogEventAsync(
                "Cross_Platform_Event_API_Request",
                repository: platformEvent.Repository,
                action: "RouteEvent",
                result: result.Success ? "SUCCESS" : "FAILED",
                details: new {
                    EventType = platformEvent.EventType,
                    SourcePlatform = platformEvent.SourcePlatform,
                    RoutingId = result.RoutingId,
                    RoutesExecuted = result.RouteResults.Count
                });

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(new { error = "Event routing failed", details = result });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing platform event");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Classify an event without routing it
    /// </summary>
    [HttpPost("classify")]
    public async Task<ActionResult<EventClassification>> ClassifyEvent([FromBody] PlatformEvent platformEvent)
    {
        try
        {
            _logger.LogInformation("🧠 Classifying event: {EventType} from {Platform}", 
                platformEvent.EventType, platformEvent.SourcePlatform);

            var classification = await _classificationService.ClassifyEventAsync(platformEvent);

            return Ok(classification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error classifying event");
            return StatusCode(500, new { error = "Classification failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Analyze routing options for an event without executing them
    /// </summary>
    [HttpPost("analyze")]
    public async Task<ActionResult<List<PlatformRoute>>> AnalyzeRoutingOptions([FromBody] PlatformEvent platformEvent)
    {
        try
        {
            _logger.LogInformation("🔍 Analyzing routing options for: {EventType} from {Platform}", 
                platformEvent.EventType, platformEvent.SourcePlatform);

            var routingOptions = await _eventRouter.AnalyzeRoutingOptionsAsync(platformEvent);

            return Ok(routingOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing routing options");
            return StatusCode(500, new { error = "Analysis failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Get event routing statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<EventRoutingStats>> GetRoutingStats([FromQuery] int hours = 24)
    {
        try
        {
            var period = TimeSpan.FromHours(Math.Min(hours, 168)); // Max 1 week
            var stats = await _metrics.GetRoutingStatsAsync(period);

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting routing statistics");
            return StatusCode(500, new { error = "Statistics retrieval failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Get Prometheus metrics for monitoring
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<Dictionary<string, object>>> GetMetrics()
    {
        try
        {
            var metrics = await _metrics.GetPrometheusMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics");
            return StatusCode(500, new { error = "Metrics retrieval failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint for cross-platform event routing
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<object>> GetHealth()
    {
        try
        {
            var recentStats = await _metrics.GetRoutingStatsAsync(TimeSpan.FromHours(1));
            
            var health = new
            {
                Status = "Healthy",
                Service = "Reynolds Cross-Platform Event Router",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                RecentActivity = new
                {
                    EventsProcessed = recentStats.TotalEventsProcessed,
                    SuccessRate = recentStats.SuccessRate,
                    AverageProcessingTime = $"{recentStats.AverageProcessingTime.TotalMilliseconds:F2}ms"
                },
                ReynoldsStatus = "Ready for supernatural cross-platform orchestration! 🎭✨"
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health status");
            return StatusCode(500, new { 
                Status = "Unhealthy",
                Error = ex.Message,
                ReynoldsStatus = "Reynolds is experiencing technical difficulties. Even supernatural beings have off days! 🎭"
            });
        }
    }

    /// <summary>
    /// Webhook endpoint for GitHub events (integrated with existing webhook processor)
    /// </summary>
    [HttpPost("webhook/github")]
    public async Task<ActionResult> ProcessGitHubWebhook([FromBody] GitHubWebhookPayload payload)
    {
        try
        {
            _logger.LogInformation("🐙 Processing GitHub webhook for cross-platform routing: {Event}", payload.Event);

            // Convert GitHub webhook payload to platform event
            var platformEvent = ConvertGitHubWebhookToPlatformEvent(payload);
            
            // Route the event
            var result = await _eventRouter.RouteEventAsync(platformEvent);

            if (result.Success)
            {
                return Ok(new { message = "GitHub webhook processed and routed successfully", routingId = result.RoutingId });
            }
            else
            {
                return StatusCode(500, new { error = "Event routing failed", details = result });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub webhook");
            return StatusCode(500, new { error = "Webhook processing failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Webhook endpoint for Azure events
    /// </summary>
    [HttpPost("webhook/azure")]
    public async Task<ActionResult> ProcessAzureWebhook([FromBody] AzureEventPayload payload)
    {
        try
        {
            _logger.LogInformation("⚡ Processing Azure webhook for cross-platform routing: {EventType}", payload.EventType);

            // Convert Azure event payload to platform event
            var platformEvent = ConvertAzureEventToPlatformEvent(payload);
            
            // Route the event
            var result = await _eventRouter.RouteEventAsync(platformEvent);

            if (result.Success)
            {
                return Ok(new { message = "Azure webhook processed and routed successfully", routingId = result.RoutingId });
            }
            else
            {
                return StatusCode(500, new { error = "Event routing failed", details = result });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Azure webhook");
            return StatusCode(500, new { error = "Webhook processing failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint for Teams messages to trigger cross-platform actions
    /// </summary>
    [HttpPost("teams/message")]
    public async Task<ActionResult> ProcessTeamsMessage([FromBody] TeamsMessagePayload payload)
    {
        try
        {
            _logger.LogInformation("💬 Processing Teams message for cross-platform routing: {MessageType}", payload.MessageType);

            // Convert Teams message to platform event
            var platformEvent = ConvertTeamsMessageToPlatformEvent(payload);
            
            // Route the event
            var result = await _eventRouter.RouteEventAsync(platformEvent);

            if (result.Success)
            {
                return Ok(new { 
                    message = "Teams message processed and routed successfully", 
                    routingId = result.RoutingId,
                    reynoldsResponse = result.ReynoldsComment
                });
            }
            else
            {
                return StatusCode(500, new { error = "Event routing failed", details = result });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Teams message");
            return StatusCode(500, new { error = "Teams message processing failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Manual event injection for testing and debugging
    /// </summary>
    [HttpPost("inject")]
    public async Task<ActionResult<EventRoutingResult>> InjectTestEvent([FromBody] TestEventRequest request)
    {
        try
        {
            _logger.LogInformation("🧪 Injecting test event: {EventType} from {Platform}", 
                request.EventType, request.SourcePlatform);

            var platformEvent = new PlatformEvent
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = request.EventType,
                SourcePlatform = request.SourcePlatform,
                Action = request.Action ?? "test",
                Repository = request.Repository,
                UserId = request.UserId ?? "test-user",
                Content = request.Content,
                Metadata = request.Metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            };

            var result = await _eventRouter.RouteEventAsync(platformEvent);

            await _auditService.LogEventAsync(
                "Cross_Platform_Test_Event_Injected",
                repository: request.Repository,
                action: "InjectTestEvent",
                result: result.Success ? "SUCCESS" : "FAILED",
                details: new {
                    EventType = request.EventType,
                    SourcePlatform = request.SourcePlatform,
                    RoutingId = result.RoutingId
                });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error injecting test event");
            return StatusCode(500, new { error = "Test event injection failed", message = ex.Message });
        }
    }

    // Private helper methods for event conversion

    private PlatformEvent ConvertGitHubWebhookToPlatformEvent(GitHubWebhookPayload payload)
    {
        return new PlatformEvent
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = payload.Event ?? "unknown",
            SourcePlatform = "GitHub",
            Action = payload.Action ?? "unknown",
            Repository = payload.Repository?.FullName,
            UserId = payload.Sender?.Login,
            Content = ExtractContentFromGitHubPayload(payload),
            Metadata = ExtractMetadataFromGitHubPayload(payload),
            Timestamp = DateTime.UtcNow
        };
    }

    private PlatformEvent ConvertAzureEventToPlatformEvent(AzureEventPayload payload)
    {
        return new PlatformEvent
        {
            EventId = payload.EventId,
            EventType = payload.EventType,
            SourcePlatform = "Azure",
            Action = payload.Action,
            Repository = ExtractRepositoryFromAzureEvent(payload),
            Content = $"Azure {payload.EventType}: {payload.Action}",
            Metadata = new Dictionary<string, object>
            {
                ["resource_id"] = payload.ResourceId,
                ["subscription_id"] = payload.SubscriptionId,
                ["resource_group"] = payload.ResourceGroupName,
                ["azure_properties"] = payload.Properties
            },
            Timestamp = payload.Timestamp
        };
    }

    private PlatformEvent ConvertTeamsMessageToPlatformEvent(TeamsMessagePayload payload)
    {
        return new PlatformEvent
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = payload.MessageType,
            SourcePlatform = "Teams",
            Action = "message_received",
            UserId = payload.UserId,
            Content = payload.Content,
            Metadata = new Dictionary<string, object>
            {
                ["channel_id"] = payload.ChannelId ?? "",
                ["conversation_id"] = payload.ConversationId ?? "",
                ["teams_metadata"] = payload.Metadata ?? new Dictionary<string, object>()
            },
            Timestamp = payload.Timestamp
        };
    }

    private string ExtractContentFromGitHubPayload(GitHubWebhookPayload payload)
    {
        if (payload.PullRequest != null)
        {
            return $"PR #{payload.PullRequest.Number}: {payload.PullRequest.Title}\n{payload.PullRequest.Body}";
        }
        
        if (payload.Issue != null)
        {
            return $"Issue #{payload.Issue.Number}: {payload.Issue.Title}\n{payload.Issue.Body}";
        }

        return $"GitHub {payload.Event}: {payload.Action}";
    }

    private Dictionary<string, object> ExtractMetadataFromGitHubPayload(GitHubWebhookPayload payload)
    {
        var metadata = new Dictionary<string, object>();

        if (payload.PullRequest != null)
        {
            metadata["pr_number"] = payload.PullRequest.Number;
            metadata["pr_state"] = payload.PullRequest.State;
            metadata["lines_changed"] = EstimateChangedLines(payload.PullRequest);
        }

        if (payload.Issue != null)
        {
            metadata["issue_number"] = payload.Issue.Number;
            metadata["issue_state"] = payload.Issue.State;
        }

        if (payload.Repository != null)
        {
            metadata["repository_private"] = payload.Repository.Private;
        }

        return metadata;
    }

    private int EstimateChangedLines(GitHubPullRequestPayload pr)
    {
        // Simple estimation based on PR content length
        // In a real implementation, this would use GitHub API to get actual diff stats
        var content = pr.Body ?? "";
        return Math.Max(50, content.Length / 10); // Rough estimation
    }

    private string? ExtractRepositoryFromAzureEvent(AzureEventPayload payload)
    {
        // Try to extract repository information from Azure resource tags or metadata
        if (payload.Properties.TryGetValue("repository", out var repo))
        {
            return repo.ToString();
        }

        // Default to main repository if resource is related to copilot-powerplatform
        if (payload.ResourceId.Contains("copilot", StringComparison.OrdinalIgnoreCase))
        {
            return "dynamicstms365/copilot-powerplatform";
        }

        return null;
    }
}

// Supporting classes for API payloads
public class TeamsMessagePayload
{
    public string MessageType { get; set; } = "message";
    public string UserId { get; set; } = "";
    public string Content { get; set; } = "";
    public string? ChannelId { get; set; }
    public string? ConversationId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class TestEventRequest
{
    public string EventType { get; set; } = "";
    public string SourcePlatform { get; set; } = "";
    public string? Action { get; set; }
    public string? Repository { get; set; }
    public string? UserId { get; set; }
    public string? Content { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}