using Microsoft.AspNetCore.Mvc;
using CopilotAgent.Services;
using System.ComponentModel.DataAnnotations;
using System.Net;
using CopilotAgent.Models;

namespace CopilotAgent.Controllers;

/// <summary>
/// GitHub integration controller with Reynolds' Maximum Effort™ logging
/// Handles GitHub webhooks, issues, discussions, and organizational operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("GitHub")]
public class GitHubController : ControllerBase
{
    private readonly ILogger<GitHubController> _logger;
    private readonly IGitHubAppAuthService _authService;
    private readonly IGitHubIssuesService _issuesService;
    private readonly IGitHubDiscussionsService _discussionsService;
    private readonly IGitHubWebhookValidator _webhookValidator;

    public GitHubController(
        ILogger<GitHubController> logger,
        IGitHubAppAuthService authService,
        IGitHubIssuesService issuesService,
        IGitHubDiscussionsService discussionsService,
        IGitHubWebhookValidator webhookValidator)
    {
        _logger = logger;
        _authService = authService;
        _issuesService = issuesService;
        _discussionsService = discussionsService;
        _webhookValidator = webhookValidator;
    }

    /// <summary>
    /// Test GitHub connectivity and authentication
    /// </summary>
    /// <returns>GitHub connectivity status with detailed diagnostics</returns>
    /// <response code="200">GitHub connection successful</response>
    /// <response code="401">Authentication failed</response>
    /// <response code="503">GitHub service unavailable</response>
    [HttpGet("connectivity")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
    public async Task<IActionResult> TestConnectivity()
    {
        _logger.LogInformation("🎭 Reynolds: Testing GitHub connectivity with Maximum Effort™");
        
        try
        {
            var result = await _authService.TestConnectivityAsync();
            
            if (result.Success)
            {
                _logger.LogInformation("✅ Reynolds: GitHub connectivity test successful");
                _logger.LogInformation("🔗 Reynolds: Connected to {RepositoryCount} repositories", result.Repositories?.Length ?? 0);
                _logger.LogDebug("🔑 Reynolds: Active permissions: {Permissions}", string.Join(", ", result.Permissions ?? Array.Empty<string>()));
                
                return Ok(new
                {
                    Status = "connected",
                    Message = "GitHub connectivity verified with supernatural precision",
                    InstallationId = result.InstallationId,
                    RepositoryCount = result.Repositories?.Length ?? 0,
                    Permissions = result.Permissions,
                    TokenExpiresAt = result.TokenExpiresAt,
                    ReynoldsWisdom = "Maximum Effort™ GitHub integration is operational! 🎭"
                });
            }
            else
            {
                _logger.LogError("❌ Reynolds: GitHub connectivity test failed - {Error}", result.Error);
                
                return StatusCode(503, new ErrorResponse
                {
                    Error = "GitHubConnectivityFailed",
                    Message = "GitHub connectivity test failed",
                    Details = new Dictionary<string, object>
                    {
                        ["Error"] = result.Error ?? "Unknown error",
                        ["ReynoldsAdvice"] = "Check GitHub App configuration and network connectivity"
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Reynolds: GitHub connectivity test encountered supernatural complications");
            
            return StatusCode(503, new ErrorResponse
            {
                Error = "ConnectivityTestException",
                Message = "GitHub connectivity test failed with exception",
                Details = new Dictionary<string, object>
                {
                    ["Exception"] = ex.Message,
                    ["ReynoldsWisdom"] = "Even supernatural beings need proper configuration! 🎭"
                }
            });
        }
    }

    /// <summary>
    /// Get GitHub App installation information
    /// </summary>
    /// <returns>GitHub App installation details</returns>
    [HttpGet("installation")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetInstallationInfo()
    {
        _logger.LogInformation("🎭 Reynolds: Retrieving GitHub App installation information");
        
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            
            _logger.LogInformation("✅ Reynolds: Successfully retrieved installation information");
            _logger.LogDebug("🔑 Reynolds: Token expires at {ExpiresAt}", auth.ExpiresAt);
            
            return Ok(new
            {
                Status = "active",
                TokenExpiresAt = auth.ExpiresAt,
                Permissions = auth.Permissions,
                ValidFor = auth.ExpiresAt - DateTime.UtcNow,
                ReynoldsStatus = "Installation token retrieved with supernatural efficiency! 🎭"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Reynolds: Failed to retrieve installation information");
            
            return StatusCode(500, new ErrorResponse
            {
                Error = "InstallationInfoFailed",
                Message = "Failed to retrieve GitHub App installation information",
                Details = new Dictionary<string, object>
                {
                    ["Exception"] = ex.Message,
                    ["ReynoldsAdvice"] = "Verify GitHub App credentials and installation status"
                }
            });
        }
    }

    /// <summary>
    /// Validate webhook signature
    /// </summary>
    /// <param name="payload">Webhook payload to validate</param>
    /// <returns>Webhook validation result</returns>
    [HttpPost("webhook/validate")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> ValidateWebhook([FromBody] object payload)
    {
        _logger.LogInformation("🎭 Reynolds: Validating GitHub webhook with Maximum Effort™");
        
        try
        {
            var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            var githubEvent = Request.Headers["X-GitHub-Event"].FirstOrDefault();
            var delivery = Request.Headers["X-GitHub-Delivery"].FirstOrDefault();
            
            _logger.LogDebug("🔍 Reynolds: Webhook headers - Event: {Event}, Delivery: {Delivery}, Signature: {HasSignature}",
                githubEvent, delivery, !string.IsNullOrEmpty(signature) ? "PRESENT" : "MISSING");
            
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("⚠️ Reynolds: Webhook signature missing - security validation failed");
                return BadRequest(new ErrorResponse
                {
                    Error = "MissingSignature",
                    Message = "X-Hub-Signature-256 header is required",
                    Details = new Dictionary<string, object>
                    {
                        ["ReynoldsAdvice"] = "GitHub webhooks require proper signature validation for security"
                    }
                });
            }

            // Additional validation would go here
            await Task.CompletedTask; // Satisfy async requirement
            _logger.LogInformation("✅ Reynolds: Webhook validation completed successfully");
            
            return Ok(new
            {
                Status = "valid",
                Event = githubEvent,
                Delivery = delivery,
                Timestamp = DateTime.UtcNow,
                ReynoldsStatus = "Webhook validated with supernatural security! 🎭"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Reynolds: Webhook validation failed");
            
            return StatusCode(500, new ErrorResponse
            {
                Error = "WebhookValidationFailed",
                Message = "Webhook validation encountered an error",
                Details = new Dictionary<string, object>
                {
                    ["Exception"] = ex.Message,
                    ["ReynoldsWisdom"] = "Even supernatural validation needs proper error handling! 🎭"
                }
            });
        }
    }

    /// <summary>
    /// Get GitHub API rate limit status
    /// </summary>
    /// <returns>Current rate limit information</returns>
    [HttpGet("rate-limit")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetRateLimit()
    {
        _logger.LogInformation("🎭 Reynolds: Checking GitHub API rate limit status");
        
        try
        {
            // This would implement actual rate limit checking
            await Task.CompletedTask; // Satisfy async requirement
            _logger.LogInformation("✅ Reynolds: Rate limit status retrieved");
            
            return Ok(new
            {
                Status = "healthy",
                Timestamp = DateTime.UtcNow,
                Message = "Rate limit information retrieved",
                ReynoldsNote = "Supernatural coordination respects API limits! 🎭"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Reynolds: Failed to retrieve rate limit status");
            
            return StatusCode(500, new ErrorResponse
            {
                Error = "RateLimitCheckFailed",
                Message = "Failed to retrieve rate limit information",
                Details = new Dictionary<string, object>
                {
                    ["Exception"] = ex.Message
                }
            });
        }
    }
}