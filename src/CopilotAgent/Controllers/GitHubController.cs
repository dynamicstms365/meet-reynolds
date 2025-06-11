using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using CopilotAgent.Services;
using System.Text.Json;

namespace CopilotAgent.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GitHubController : ControllerBase
{
    private readonly IGitHubAppAuthService _authService;
    private readonly ISecurityAuditService _auditService;
    private readonly IGitHubWebhookValidator _webhookValidator;
    private readonly ILogger<GitHubController> _logger;
    private readonly IConfiguration _configuration;

    public GitHubController(
        IGitHubAppAuthService authService,
        ISecurityAuditService auditService,
        IGitHubWebhookValidator webhookValidator,
        ILogger<GitHubController> logger,
        IConfiguration configuration)
    {
        _authService = authService;
        _auditService = auditService;
        _webhookValidator = webhookValidator;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        try
        {
            // Read raw body for signature validation
            var body = await new StreamReader(Request.Body).ReadToEndAsync();
            
            // Get GitHub signature from headers
            var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            var gitHubEvent = Request.Headers["X-GitHub-Event"].FirstOrDefault();
            
            _logger.LogInformation("Received GitHub webhook: {Event}", gitHubEvent);

            // Validate webhook signature
            var webhookSecret = _configuration["NGL_DEVOPS_WEBHOOK_SECRET"] ??
                              Environment.GetEnvironmentVariable("NGL_DEVOPS_WEBHOOK_SECRET");
            
            if (!string.IsNullOrEmpty(webhookSecret))
            {
                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("Missing X-Hub-Signature-256 header");
                    return Unauthorized("Missing signature");
                }

                if (!_webhookValidator.ValidateSignature(body, signature, webhookSecret))
                {
                    _logger.LogWarning("Invalid webhook signature");
                    await _auditService.LogEventAsync("GitHub_Webhook_Security",
                        action: "SIGNATURE_VALIDATION_FAILED",
                        result: "REJECTED");
                    return Unauthorized("Invalid signature");
                }
            }
            else
            {
                _logger.LogWarning("Webhook secret not configured - signature validation skipped");
            }

            // Validate source IP (optional additional security)
            var clientIP = Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                          Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            
            if (!string.IsNullOrEmpty(clientIP) && !_webhookValidator.IsValidGitHubIP(clientIP))
            {
                _logger.LogWarning("Webhook from suspicious IP: {IP}", clientIP);
                // Don't reject based on IP alone as this can be unreliable behind proxies
            }

            // Parse payload
            var payload = System.Text.Json.JsonSerializer.Deserialize<GitHubWebhookPayload>(body);
            
            if (payload?.Installation?.Id == null)
            {
                _logger.LogWarning("Webhook payload missing installation ID");
                await _auditService.LogWebhookEventAsync(payload, "REJECTED_MISSING_INSTALLATION");
                return BadRequest("Invalid webhook payload: missing installation");
            }

            // Process different webhook events
            var result = await ProcessWebhookEventAsync(payload);
            
            await _auditService.LogWebhookEventAsync(payload, result ? "SUCCESS" : "FAILED");
            
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub webhook");
            return StatusCode(500, new { error = "Internal server error processing webhook" });
        }
    }

    [HttpGet("test")]
    public async Task<ActionResult<GitHubConnectivityResult>> TestConnectivity()
    {
        try
        {
            _logger.LogInformation("Testing GitHub App connectivity");
            
            var result = await _authService.TestConnectivityAsync();
            
            await _auditService.LogEventAsync(
                "GitHub_Connectivity_Test",
                action: "TestConnection",
                result: result.Success ? "SUCCESS" : "FAILED",
                details: new { Error = result.Error, RepositoryCount = result.Repositories.Length });

            _logger.LogInformation("GitHub connectivity test completed: {Success}", result.Success);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub connectivity test failed");
            
            await _auditService.LogEventAsync(
                "GitHub_Connectivity_Test",
                action: "TestConnection",
                result: "ERROR",
                details: new { Error = ex.Message });

            return StatusCode(500, new GitHubConnectivityResult
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    [HttpGet("health")]
    public ActionResult<object> Health()
    {
        return Ok(new 
        { 
            status = "healthy",
            service = "GitHub Integration",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    [HttpGet("installation-info")]
    public async Task<ActionResult<object>> GetInstallationInfo()
    {
        try
        {
            _logger.LogInformation("Getting GitHub App installation information");
            
            var auth = await _authService.GetInstallationTokenAsync();
            
            await _auditService.LogEventAsync(
                "GitHub_Installation_Info_Request",
                action: "GetInfo",
                result: "SUCCESS");

            return Ok(new
            {
                success = true,
                tokenExpiresAt = auth.ExpiresAt,
                permissions = auth.Permissions,
                hasValidToken = auth.ExpiresAt > DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get GitHub App installation information");
            
            await _auditService.LogEventAsync(
                "GitHub_Installation_Info_Request",
                action: "GetInfo",
                result: "ERROR",
                details: new { Error = ex.Message });

            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    private async Task<bool> ProcessWebhookEventAsync(GitHubWebhookPayload payload)
    {
        try
        {
            _logger.LogInformation("Processing webhook event: {Event} - {Action} for repository: {Repository}",
                payload.Event, payload.Action, payload.Repository?.FullName);

            // Handle different webhook events
            switch (payload.Event?.ToLowerInvariant())
            {
                case "repository_dispatch":
                    return await HandleRepositoryDispatchAsync(payload);
                case "installation":
                case "installation_repositories":
                    return await HandleInstallationEventAsync(payload);
                case "push":
                    return await HandlePushEventAsync(payload);
                case "pull_request":
                    return await HandlePullRequestEventAsync(payload);
                default:
                    _logger.LogInformation("Unhandled webhook event type: {Event}", payload.Event);
                    return true; // Return success for unhandled events to avoid retries
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event: {Event}", payload.Event);
            return false;
        }
    }

    private async Task<bool> HandleRepositoryDispatchAsync(GitHubWebhookPayload payload)
    {
        _logger.LogInformation("Handling repository dispatch event");
        
        // This is where we would trigger Power Platform automation based on repository dispatch events
        // For now, we just log the event
        
        await _auditService.LogEventAsync(
            "Repository_Dispatch_Processed",
            repository: payload.Repository?.FullName,
            action: payload.Action,
            result: "PROCESSED",
            details: payload.ClientPayload);

        return true;
    }

    private async Task<bool> HandleInstallationEventAsync(GitHubWebhookPayload payload)
    {
        _logger.LogInformation("Handling installation event: {Action}", payload.Action);
        
        await _auditService.LogEventAsync(
            "Installation_Event_Processed",
            action: payload.Action,
            result: "PROCESSED",
            details: new { InstallationId = payload.Installation?.Id });

        return true;
    }

    private async Task<bool> HandlePushEventAsync(GitHubWebhookPayload payload)
    {
        _logger.LogInformation("Handling push event for repository: {Repository}", payload.Repository?.FullName);
        
        // This is where we could trigger CI/CD workflows or other automation
        
        await _auditService.LogEventAsync(
            "Push_Event_Processed",
            repository: payload.Repository?.FullName,
            action: "push",
            result: "PROCESSED");

        return true;
    }

    private async Task<bool> HandlePullRequestEventAsync(GitHubWebhookPayload payload)
    {
        _logger.LogInformation("Handling pull request event: {Action} for repository: {Repository}", 
            payload.Action, payload.Repository?.FullName);
        
        // This is where we could trigger PR validation, reviews, or other automation
        
        await _auditService.LogEventAsync(
            "Pull_Request_Event_Processed",
            repository: payload.Repository?.FullName,
            action: payload.Action,
            result: "PROCESSED");

        return true;
    }
}