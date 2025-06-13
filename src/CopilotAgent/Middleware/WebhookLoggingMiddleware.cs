using System.Text;
using Microsoft.AspNetCore.Http;
using CopilotAgent.Services;

namespace CopilotAgent.Middleware;

/// <summary>
/// Middleware to capture and log webhook request details before signature validation
/// This helps debug signature validation failures by providing request context
/// </summary>
public class WebhookLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WebhookLoggingMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public WebhookLoggingMiddleware(RequestDelegate next, ILogger<WebhookLoggingMiddleware> logger, IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process webhook requests
        if (context.Request.Path.StartsWithSegments("/api/github/webhook"))
        {
            LogWebhookRequest(context);
        }

        await _next(context);
    }

    private void LogWebhookRequest(HttpContext context)
    {
        try
        {
            // Extract headers for debugging
            var deliveryId = context.Request.Headers["X-GitHub-Delivery"].FirstOrDefault();
            var eventType = context.Request.Headers["X-GitHub-Event"].FirstOrDefault();
            var signature = context.Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
            var contentType = context.Request.Headers["Content-Type"].FirstOrDefault();

            // Log incoming webhook request details
            _logger.LogInformation("WEBHOOK_REQUEST_RECEIVED: Event={EventType}, DeliveryId={DeliveryId}, Signature={HasSignature}, UserAgent={UserAgent}, ContentType={ContentType}, RemoteIP={RemoteIP}",
                eventType ?? "unknown",
                deliveryId ?? "unknown",
                !string.IsNullOrEmpty(signature) ? "present" : "missing",
                userAgent ?? "unknown",
                contentType ?? "unknown",
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

            // Log payload size for debugging
            if (context.Request.ContentLength.HasValue)
            {
                _logger.LogDebug("WEBHOOK_REQUEST_SIZE: PayloadSize={PayloadSize} bytes", context.Request.ContentLength.Value);
            }

            // Additional validation troubleshooting
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("WEBHOOK_SIGNATURE_MISSING: No X-Hub-Signature-256 header found in request. This will cause signature validation to fail.");
            }
            else if (!signature.StartsWith("sha256="))
            {
                _logger.LogWarning("WEBHOOK_SIGNATURE_INVALID_FORMAT: Signature header does not start with 'sha256='. Format: {SignatureFormat}", signature.Substring(0, Math.Min(20, signature.Length)));
            }

            // Log webhook secret configuration status (without exposing the secret)
            using var scope = _serviceProvider.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var hasWebhookSecret = !string.IsNullOrEmpty(configuration["NGL_DEVOPS_WEBHOOK_SECRET"]) ||
                                 !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("NGL_DEVOPS_WEBHOOK_SECRET"));
            
            if (!hasWebhookSecret)
            {
                _logger.LogError("WEBHOOK_SECRET_NOT_CONFIGURED: No webhook secret found in configuration. This will cause all signature validations to fail.");
            }
            else
            {
                _logger.LogDebug("WEBHOOK_SECRET_STATUS: Webhook secret is configured");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging webhook request details: {Error}", ex.Message);
        }
    }
}

/// <summary>
/// Extension methods for registering webhook logging middleware
/// </summary>
public static class WebhookLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseWebhookLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<WebhookLoggingMiddleware>();
    }
}