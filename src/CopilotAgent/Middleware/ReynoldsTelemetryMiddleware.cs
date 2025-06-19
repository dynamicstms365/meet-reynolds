using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;

namespace CopilotAgent.Middleware;

/// <summary>
/// Reynolds Telemetry Middleware - Maximum Effort™ Request Tracking
/// Captures supernatural visibility into every request with enterprise-grade telemetry
/// </summary>
public class ReynoldsTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<ReynoldsTelemetryMiddleware> _logger;

    public ReynoldsTelemetryMiddleware(
        RequestDelegate next,
        TelemetryClient telemetryClient,
        ILogger<ReynoldsTelemetryMiddleware> logger)
    {
        _next = next;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        // Reynolds: Create custom request telemetry
        var requestTelemetry = new RequestTelemetry
        {
            Name = $"{context.Request.Method} {context.Request.Path}",
            Url = new Uri($"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}"),
            Timestamp = DateTimeOffset.UtcNow,
            Id = requestId
        };

        // Reynolds: Add supernatural context properties
        requestTelemetry.Properties["Reynolds.RequestId"] = requestId;
        requestTelemetry.Properties["Reynolds.UserAgent"] = context.Request.Headers.UserAgent.ToString();
        requestTelemetry.Properties["Reynolds.RemoteIP"] = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        requestTelemetry.Properties["Reynolds.Protocol"] = context.Request.Protocol;
        requestTelemetry.Properties["Reynolds.ContentLength"] = context.Request.ContentLength?.ToString() ?? "0";

        // Reynolds: Detect and track specific operation types
        var operationType = DetermineOperationType(context);
        requestTelemetry.Properties["Reynolds.OperationType"] = operationType;

        // Reynolds: Track GitHub webhook events
        if (context.Request.Headers.ContainsKey("X-GitHub-Event"))
        {
            var githubEvent = context.Request.Headers["X-GitHub-Event"].ToString();
            requestTelemetry.Properties["Reynolds.GitHubEvent"] = githubEvent;
            requestTelemetry.Properties["Reynolds.GitHubDelivery"] = context.Request.Headers["X-GitHub-Delivery"].ToString();
            
            // Track as custom event
            _telemetryClient.TrackEvent("Reynolds.GitHubWebhook", new Dictionary<string, string>
            {
                ["Event"] = githubEvent,
                ["Delivery"] = context.Request.Headers["X-GitHub-Delivery"].ToString(),
                ["Repository"] = ExtractRepositoryFromWebhook(context),
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }

        // Reynolds: Track MCP operations
        if (context.Request.Path.StartsWithSegments("/mcp") || 
            context.Request.Headers.ContainsKey("X-MCP-Operation"))
        {
            var mcpOperation = context.Request.Headers["X-MCP-Operation"].ToString() 
                ?? ExtractMcpOperationFromPath(context.Request.Path);
            
            requestTelemetry.Properties["Reynolds.MCPOperation"] = mcpOperation;
            
            // Track as custom event
            _telemetryClient.TrackEvent("Reynolds.MCPOperation", new Dictionary<string, string>
            {
                ["Operation"] = mcpOperation,
                ["Path"] = context.Request.Path,
                ["Method"] = context.Request.Method,
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }

        // Reynolds: Track Teams Bot interactions
        if (context.Request.Path.StartsWithSegments("/api/messages") ||
            context.Request.Headers.ContainsKey("X-Teams-Context"))
        {
            requestTelemetry.Properties["Reynolds.TeamsInteraction"] = "true";
            
            _telemetryClient.TrackEvent("Reynolds.TeamsInteraction", new Dictionary<string, string>
            {
                ["Path"] = context.Request.Path,
                ["Method"] = context.Request.Method,
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }

        try
        {
            // Reynolds: Execute the request with supernatural monitoring
            await _next(context);
            
            // Reynolds: Capture response details
            stopwatch.Stop();
            requestTelemetry.Duration = stopwatch.Elapsed;
            requestTelemetry.ResponseCode = context.Response.StatusCode.ToString();
            requestTelemetry.Success = context.Response.StatusCode < 400;
            
            requestTelemetry.Properties["Reynolds.ResponseSize"] = 
                context.Response.Headers.ContentLength?.ToString() ?? "Unknown";
            requestTelemetry.Properties["Reynolds.ResponseContentType"] = 
                context.Response.ContentType ?? "Unknown";

            // Reynolds: Track performance metrics
            _telemetryClient.TrackMetric("Reynolds.RequestDuration", stopwatch.ElapsedMilliseconds);
            _telemetryClient.TrackMetric("Reynolds.ResponseStatusCode", context.Response.StatusCode);

            // Reynolds: Track successful operations
            if (context.Response.StatusCode < 400)
            {
                _telemetryClient.TrackEvent("Reynolds.SuccessfulOperation", new Dictionary<string, string>
                {
                    ["OperationType"] = operationType,
                    ["Duration"] = stopwatch.ElapsedMilliseconds.ToString(),
                    ["StatusCode"] = context.Response.StatusCode.ToString()
                });
            }
        }
        catch (Exception ex)
        {
            // Reynolds: Capture and track exceptions with Maximum Effort™
            stopwatch.Stop();
            requestTelemetry.Duration = stopwatch.Elapsed;
            requestTelemetry.Success = false;
            requestTelemetry.ResponseCode = "500";

            // Track exception
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                ["Reynolds.RequestId"] = requestId,
                ["Reynolds.OperationType"] = operationType,
                ["Reynolds.RequestPath"] = context.Request.Path,
                ["Reynolds.RequestMethod"] = context.Request.Method,
                ["Reynolds.Duration"] = stopwatch.ElapsedMilliseconds.ToString()
            });

            _logger.LogError(ex, "🎭 Reynolds: Request execution encountered exceptional circumstances - tracking with supernatural precision");
            throw;
        }
        finally
        {
            // Reynolds: Always track the request telemetry
            _telemetryClient.TrackRequest(requestTelemetry);
        }
    }

    private static string DetermineOperationType(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        
        if (path.Contains("/mcp")) return "MCP";
        if (path.Contains("/api/messages")) return "Teams";
        if (path.Contains("/webhooks/github")) return "GitHub";
        if (path.Contains("/health")) return "Health";
        if (path.Contains("/swagger") || path.Contains("/openapi")) return "Documentation";
        if (path.Contains("/api/")) return "API";
        
        return "Web";
    }

    private static string ExtractRepositoryFromWebhook(HttpContext context)
    {
        // This would typically parse the webhook payload to extract repository info
        // For now, return a placeholder - in real implementation, you'd parse the JSON body
        return "Unknown";
    }

    private static string ExtractMcpOperationFromPath(PathString path)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments?.Length > 1 ? segments.Last() : "Unknown";
    }
}