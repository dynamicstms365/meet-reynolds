using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using CopilotAgent.Services;
using Shared.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;
using CopilotAgent.Models;

namespace CopilotAgent.Controllers;

/// <summary>
/// Cross-Platform Event Controller for comprehensive event routing and orchestration
/// Designed for Azure APIM MCP integration with full cross-platform coordination capabilities
/// Integrates Teams, GitHub, and Azure event sources with Reynolds-level coordination
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("CrossPlatform")]
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
    /// Route a platform event across multiple platforms with Reynolds coordination
    /// </summary>
    /// <param name="platformEvent">Platform event to route across systems</param>
    /// <returns>Event routing result with execution details</returns>
    /// <response code="200">Event routed successfully across platforms</response>
    /// <response code="400">Invalid event parameters</response>
    /// <response code="500">Event routing failed</response>
    [HttpPost("route")]
    [ProducesResponseType(typeof(EventRoutingResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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
                return BadRequest(new ErrorResponse 
                { 
                    Error = "EventRoutingFailed", 
                    Message = "Event routing failed",
                    Details = new Dictionary<string, object> { ["RoutingResult"] = result }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing platform event");
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "EventRoutingException", 
                Message = "Internal server error during event routing",
                Details = new Dictionary<string, object> { ["Exception"] = ex.Message }
            });
        }
    }

    /// <summary>
    /// Classify an event without routing it for analysis purposes
    /// </summary>
    /// <param name="platformEvent">Platform event to classify</param>
    /// <returns>Event classification results with categories and confidence scores</returns>
    /// <response code="200">Event classified successfully</response>
    /// <response code="400">Invalid event parameters</response>
    /// <response code="500">Event classification failed</response>
    [HttpPost("classify")]
    [ProducesResponseType(typeof(EventClassification), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "EventClassificationFailed", 
                Message = "Event classification failed",
                Details = new Dictionary<string, object> { ["Exception"] = ex.Message }
            });
        }
    }

    /// <summary>
    /// Analyze routing options for an event without executing them
    /// </summary>
    /// <param name="platformEvent">Platform event to analyze routing options for</param>
    /// <returns>List of available routing options with recommendations</returns>
    /// <response code="200">Routing options analyzed successfully</response>
    /// <response code="400">Invalid event parameters</response>
    /// <response code="500">Routing analysis failed</response>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(List<PlatformRoute>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "RoutingAnalysisFailed", 
                Message = "Routing analysis failed",
                Details = new Dictionary<string, object> { ["Exception"] = ex.Message }
            });
        }
    }

    /// <summary>
    /// Get event routing statistics and performance metrics
    /// </summary>
    /// <param name="hours">Number of hours to include in statistics (default: 24, max: 168)</param>
    /// <returns>Comprehensive routing statistics and metrics</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    /// <response code="500">Statistics retrieval failed</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(EventRoutingStats), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "StatisticsRetrievalFailed", 
                Message = "Statistics retrieval failed",
                Details = new Dictionary<string, object> { ["Exception"] = ex.Message }
            });
        }
    }

    /// <summary>
    /// Get Prometheus-compatible metrics for monitoring integration
    /// </summary>
    /// <returns>Prometheus-compatible metrics data</returns>
    /// <response code="200">Metrics retrieved successfully</response>
    /// <response code="500">Metrics retrieval failed</response>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(Dictionary<string, object>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "MetricsRetrievalFailed", 
                Message = "Metrics retrieval failed",
                Details = new Dictionary<string, object> { ["Exception"] = ex.Message }
            });
        }
    }

    /// <summary>
    /// Health check endpoint for cross-platform event routing service
    /// </summary>
    /// <returns>Health status of cross-platform routing services</returns>
    /// <response code="200">Cross-platform routing services are healthy</response>
    /// <response code="503">Cross-platform routing services are unhealthy</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(CrossPlatformHealthStatus), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
    public async Task<ActionResult<CrossPlatformHealthStatus>> GetHealth()
    {
        try
        {
            var recentStats = await _metrics.GetRoutingStatsAsync(TimeSpan.FromHours(1));
            
            var health = new CrossPlatformHealthStatus
            {
                Status = "Healthy",
                Service = "Reynolds Cross-Platform Event Router",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                RecentActivity = new RecentActivityMetrics
                {
                    EventsProcessed = (int)recentStats.TotalEventsProcessed,
                    SuccessRate = recentStats.SuccessRate,
                    AverageProcessingTime = $"{recentStats.AverageProcessingTime.TotalMilliseconds:F2}ms"
                },
                Capabilities = new[]
                {
                    "GitHubEventRouting",
                    "TeamsIntegration", 
                    "AzureEventProcessing",
                    "EventClassification",
                    "MetricsCollection",
                    "PrometheusIntegration"
                },
                ReynoldsStatus = "Ready for supernatural cross-platform orchestration! 🎭✨"
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health status");
            return StatusCode(503, new ErrorResponse 
            { 
                Error = "HealthCheckFailed",
                Message = "Cross-platform health check failed",
                Details = new Dictionary<string, object>
                { 
                    ["Exception"] = ex.Message,
                    ["ReynoldsStatus"] = "Reynolds is experiencing technical difficulties. Even supernatural beings have off days! 🎭"
                }
            });
        }
    }

    /// <summary>
    /// Webhook endpoint for GitHub events with cross-platform routing integration
    /// </summary>
    /// <param name="payload">GitHub webhook payload</param>
    /// <returns>Webhook processing result</returns>
    /// <response code="200">GitHub webhook processed successfully</response>
    /// <response code="400">Invalid webhook payload</response>
    /// <response code="500">Webhook processing failed</response>
    [HttpPost("webhook/github")]
    [ProducesResponseType(typeof(WebhookProcessingResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<WebhookProcessingResult>> ProcessGitHubWebhook([FromBody] GitHubWebhookPayload payload)
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
                return Ok(new WebhookProcessingResult 
                { 
                    Success = true,
                    Message = "GitHub webhook processed and routed successfully", 
                    RoutingId = result.RoutingId,
                    RoutedPlatforms = result.RouteResults.Select(r => r.TargetPlatform).ToArray()
                });
            }
            else
            {
                return StatusCode(500, new ErrorResponse 
                { 
                    Error = "WebhookRoutingFailed", 
                    Message = "Event routing failed",
                    Details = new Dictionary<string, object> { ["RoutingResult"] = result }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub webhook");
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "WebhookProcessingFailed", 
                Message = "Webhook processing failed",
                Details = new Dictionary<string, object> { ["Exception"] = ex.Message }
            });
        }
    }

    /// <summary>
    /// Manual event injection for testing and debugging purposes
    /// </summary>
    /// <param name="request">Test event parameters</param>
    /// <returns>Test event routing result</returns>
    /// <response code="200">Test event injected and routed successfully</response>
    /// <response code="400">Invalid test event parameters</response>
    /// <response code="500">Test event injection failed</response>
    [HttpPost("inject")]
    [ProducesResponseType(typeof(EventRoutingResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "TestEventInjectionFailed", 
                Message = "Test event injection failed",
                Details = new Dictionary<string, object> { ["Exception"] = ex.Message }
            });
        }
    }

    /// <summary>
    /// Get supported platforms and their routing capabilities
    /// </summary>
    /// <returns>List of supported platforms and their capabilities</returns>
    /// <response code="200">Platform capabilities retrieved successfully</response>
    [HttpGet("platforms")]
    [ProducesResponseType(typeof(SupportedPlatforms), (int)HttpStatusCode.OK)]
    public ActionResult<SupportedPlatforms> GetSupportedPlatforms()
    {
        return Ok(new SupportedPlatforms
        {
            Platforms = new[]
            {
                new PlatformCapability
                {
                    Name = "GitHub",
                    EventTypes = new[] { "issues", "pull_request", "push", "release", "discussion" },
                    RoutingCapabilities = new[] { "webhook_processing", "api_integration", "semantic_search" }
                },
                new PlatformCapability
                {
                    Name = "Teams",
                    EventTypes = new[] { "message", "mention", "reaction", "channel_event" },
                    RoutingCapabilities = new[] { "message_routing", "chat_creation", "notification_delivery" }
                },
                new PlatformCapability
                {
                    Name = "Azure",
                    EventTypes = new[] { "resource_event", "deployment", "alert", "metric" },
                    RoutingCapabilities = new[] { "event_grid_integration", "monitoring_alerts", "resource_management" }
                }
            },
            ReynoldsCoordination = "Maximum Effort™ cross-platform orchestration with supernatural precision! 🎭"
        });
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
}

// Supporting response models for OpenAPI documentation
public class CrossPlatformHealthStatus
{
    public string Status { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public RecentActivityMetrics RecentActivity { get; set; } = new();
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public string ReynoldsStatus { get; set; } = string.Empty;
}

public class RecentActivityMetrics
{
    public int EventsProcessed { get; set; }
    public double SuccessRate { get; set; }
    public string AverageProcessingTime { get; set; } = string.Empty;
}

public class WebhookProcessingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string RoutingId { get; set; } = string.Empty;
    public string[] RoutedPlatforms { get; set; } = Array.Empty<string>();
}

public class SupportedPlatforms
{
    public PlatformCapability[] Platforms { get; set; } = Array.Empty<PlatformCapability>();
    public string ReynoldsCoordination { get; set; } = string.Empty;
}

public class PlatformCapability
{
    public string Name { get; set; } = string.Empty;
    public string[] EventTypes { get; set; } = Array.Empty<string>();
    public string[] RoutingCapabilities { get; set; } = Array.Empty<string>();
}

public class TestEventRequest
{
    [Required]
    public string EventType { get; set; } = "";
    
    [Required]
    public string SourcePlatform { get; set; } = "";
    
    public string? Action { get; set; }
    public string? Repository { get; set; }
    public string? UserId { get; set; }
    public string? Content { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
