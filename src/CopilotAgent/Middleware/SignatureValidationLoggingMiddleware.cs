using System.Text;
using Microsoft.AspNetCore.Http;
using Octokit.Webhooks.AspNetCore;
using CopilotAgent.Services;

namespace CopilotAgent.Middleware;

/// <summary>
/// Enhanced webhook middleware that captures and logs signature validation failures
/// This middleware works alongside Octokit's webhook processing to provide better error details
/// </summary>
public class SignatureValidationLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SignatureValidationLoggingMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SignatureValidationLoggingMiddleware(
        RequestDelegate next, 
        ILogger<SignatureValidationLoggingMiddleware> logger,
        IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/github/webhook"))
        {
            await _next(context);
            return;
        }

        // Capture the response status to detect signature validation failures
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log any exceptions during webhook processing
            await LogWebhookException(context, ex);
            throw;
        }
        finally
        {
            // Check the response for signature validation failures
            await CheckForSignatureValidationFailure(context, responseBody);
            
            // Copy the response body back to the original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task CheckForSignatureValidationFailure(HttpContext context, MemoryStream responseBody)
    {
        try
        {
            // Check if this looks like a signature validation failure
            if (context.Response.StatusCode == 401 || context.Response.StatusCode == 403)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
                
                // Check if the response indicates signature validation failure
                if (responseContent.Contains("signature", StringComparison.OrdinalIgnoreCase) ||
                    responseContent.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
                {
                    await LogSignatureValidationFailure(context, responseContent);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error checking for signature validation failure: {Error}", ex.Message);
        }
    }

    private async Task LogSignatureValidationFailure(HttpContext context, string responseContent)
    {
        var deliveryId = context.Request.Headers["X-GitHub-Delivery"].FirstOrDefault() ?? "unknown";
        var eventType = context.Request.Headers["X-GitHub-Event"].FirstOrDefault() ?? "unknown";
        var signature = context.Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
        var contentLength = context.Request.ContentLength?.ToString() ?? "unknown";
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogError("WEBHOOK_SIGNATURE_VALIDATION_FAILED: DeliveryId={DeliveryId}, EventType={EventType}, " +
                        "HasSignature={HasSignature}, UserAgent={UserAgent}, ContentLength={ContentLength}, " +
                        "RemoteIP={RemoteIP}, StatusCode={StatusCode}, Response={Response}",
            deliveryId,
            eventType,
            !string.IsNullOrEmpty(signature) ? "yes" : "no",
            userAgent,
            contentLength,
            remoteIp,
            context.Response.StatusCode,
            responseContent.Length > 500 ? responseContent.Substring(0, 500) + "..." : responseContent);

        // Provide specific troubleshooting information
        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogError("WEBHOOK_TROUBLESHOOTING: Missing X-Hub-Signature-256 header. " +
                           "Ensure your GitHub webhook is configured to include a secret and signature.");
        }
        else if (!signature.StartsWith("sha256="))
        {
            _logger.LogError("WEBHOOK_TROUBLESHOOTING: Invalid signature format '{SignaturePrefix}'. " +
                           "Expected format: sha256=<hash>. Check webhook configuration.",
                signature.Length > 10 ? signature.Substring(0, 10) : signature);
        }
        else
        {
            using var scope = _serviceProvider.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var hasSecret = !string.IsNullOrEmpty(configuration["NGL_DEVOPS_WEBHOOK_SECRET"]) ||
                           !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("NGL_DEVOPS_WEBHOOK_SECRET"));

            if (!hasSecret)
            {
                _logger.LogError("WEBHOOK_TROUBLESHOOTING: No webhook secret configured in NGL_DEVOPS_WEBHOOK_SECRET. " +
                               "This must match the secret configured in your GitHub webhook settings.");
            }
            else
            {
                _logger.LogError("WEBHOOK_TROUBLESHOOTING: Webhook secret is configured but signature validation failed. " +
                               "Verify the secret exactly matches the one in GitHub webhook settings. " +
                               "Check for leading/trailing whitespace or encoding issues.");
            }
        }

        // Log audit event for security monitoring
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var auditService = scope.ServiceProvider.GetRequiredService<ISecurityAuditService>();
            await auditService.LogEventAsync(
                "GitHub_Webhook_Signature_Validation_Failed",
                action: "SIGNATURE_VALIDATION_FAILED",
                result: "FAILED",
                details: new
                {
                    DeliveryId = deliveryId,
                    EventType = eventType,
                    HasSignature = !string.IsNullOrEmpty(signature),
                    StatusCode = context.Response.StatusCode,
                    RemoteIP = remoteIp,
                    UserAgent = userAgent,
                    ContentLength = contentLength
                });
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Could not log audit event for signature validation failure: {Error}", ex.Message);
        }
    }

    private async Task LogWebhookException(HttpContext context, Exception exception)
    {
        var deliveryId = context.Request.Headers["X-GitHub-Delivery"].FirstOrDefault() ?? "unknown";
        var eventType = context.Request.Headers["X-GitHub-Event"].FirstOrDefault() ?? "unknown";

        _logger.LogError(exception, "WEBHOOK_PROCESSING_EXCEPTION: DeliveryId={DeliveryId}, EventType={EventType}, " +
                        "Exception={ExceptionType}, Message={Message}",
            deliveryId,
            eventType,
            exception.GetType().Name,
            exception.Message);

        // Log specific guidance for common exceptions
        if (exception.Message.Contains("signature", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("WEBHOOK_TROUBLESHOOTING: This appears to be a signature validation exception. " +
                           "Check webhook secret configuration and signature header format.");
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var auditService = scope.ServiceProvider.GetRequiredService<ISecurityAuditService>();
            await auditService.LogEventAsync(
                "GitHub_Webhook_Processing_Exception",
                action: "WEBHOOK_EXCEPTION",
                result: "ERROR",
                details: new
                {
                    DeliveryId = deliveryId,
                    EventType = eventType,
                    ExceptionType = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace?.Substring(0, Math.Min(1000, exception.StackTrace.Length))
                });
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Could not log audit event for webhook exception: {Error}", ex.Message);
        }
    }
}

/// <summary>
/// Extension methods for registering signature validation logging middleware
/// </summary>
public static class SignatureValidationLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseSignatureValidationLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SignatureValidationLoggingMiddleware>();
    }
}