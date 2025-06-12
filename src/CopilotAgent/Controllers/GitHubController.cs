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
    private readonly IGitHubWorkflowOrchestrator _workflowOrchestrator;
    private readonly IGitHubSemanticSearchService _semanticSearchService;
    private readonly IGitHubDiscussionsService _discussionsService;
    private readonly IGitHubIssuesService _issuesService;
    private readonly ILogger<GitHubController> _logger;
    private readonly IConfiguration _configuration;

    public GitHubController(
        IGitHubAppAuthService authService,
        ISecurityAuditService auditService,
        IGitHubWebhookValidator webhookValidator,
        IGitHubWorkflowOrchestrator workflowOrchestrator,
        IGitHubSemanticSearchService semanticSearchService,
        IGitHubDiscussionsService discussionsService,
        IGitHubIssuesService issuesService,
        ILogger<GitHubController> logger,
        IConfiguration configuration)
    {
        _authService = authService;
        _auditService = auditService;
        _webhookValidator = webhookValidator;
        _workflowOrchestrator = workflowOrchestrator;
        _semanticSearchService = semanticSearchService;
        _discussionsService = discussionsService;
        _issuesService = issuesService;
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
            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogError("Webhook request body is empty");
                return BadRequest(new { error = "Empty request body" });
            }
            
            // Get GitHub signature from headers
            var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            var gitHubEvent = Request.Headers["X-GitHub-Event"].FirstOrDefault();
            _logger.LogInformation("Received GitHub webhook: {Event}, Body: {Body}", gitHubEvent, body);

            // Validate webhook signature
            var webhookSecret = _configuration["NGL_DEVOPS_WEBHOOK_SECRET"] ??
                              System.Environment.GetEnvironmentVariable("NGL_DEVOPS_WEBHOOK_SECRET");
            
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


            // Parse payload
            var payload = System.Text.Json.JsonSerializer.Deserialize<GitHubWebhookPayload>(body);
            
            // Ping events and some other events don't require installation context
            if (gitHubEvent != "ping" && payload?.Installation?.Id == null)
            {
                _logger.LogWarning("Webhook payload missing installation ID for event: {Event}", gitHubEvent);
                if (payload != null)
                {
                    await _auditService.LogWebhookEventAsync(payload, "REJECTED_MISSING_INSTALLATION");
                }
                return BadRequest("Invalid webhook payload: missing installation");
            }

            if (payload == null)
            {
                _logger.LogWarning("Failed to deserialize webhook payload");
                return BadRequest("Invalid webhook payload format");
            }

            // Process webhook event through workflow orchestrator
            var workflowResult = await _workflowOrchestrator.ProcessWebhookEventAsync(payload);
            
            await _auditService.LogWebhookEventAsync(payload, workflowResult.Success ? "SUCCESS" : "FAILED");
            
            return Ok(new {
                success = workflowResult.Success,
                workflowType = workflowResult.WorkflowType,
                actionsCount = workflowResult.Actions.Count,
                error = workflowResult.Error
            });
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

    [HttpPost("search")]
    public async Task<ActionResult<SemanticSearchResult>> SearchContent([FromBody] SemanticSearchRequest request)
    {
        try
        {
            _logger.LogInformation("Received semantic search request: {Query}", request.Query);

            var result = await _workflowOrchestrator.HandleSemanticSearchRequestAsync(request);
            
            if (result.Success)
            {
                return Ok(result.Data);
            }
            
            return BadRequest(new { error = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing semantic search request");
            return StatusCode(500, new { error = "Internal server error processing search request" });
        }
    }

    [HttpPost("discussions")]
    public async Task<ActionResult<GitHubDiscussion>> CreateDiscussion([FromBody] CreateDiscussionRequest request)
    {
        try
        {
            _logger.LogInformation("Creating discussion: {Title} in repository: {Repository}",
                request.Title, request.Repository);

            var result = await _workflowOrchestrator.CreateDiscussionWorkflowAsync(request);
            
            if (result.Success)
            {
                return Ok(result.Data);
            }
            
            return BadRequest(new { error = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating discussion");
            return StatusCode(500, new { error = "Internal server error creating discussion" });
        }
    }

    [HttpPost("issues")]
    public async Task<ActionResult<GitHubIssue>> CreateIssue([FromBody] CreateIssueRequest request)
    {
        try
        {
            _logger.LogInformation("Creating issue: {Title} in repository: {Repository}",
                request.Title, request.Repository);

            var result = await _workflowOrchestrator.CreateIssueWorkflowAsync(request);
            
            if (result.Success)
            {
                return Ok(result.Data);
            }
            
            return BadRequest(new { error = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating issue");
            return StatusCode(500, new { error = "Internal server error creating issue" });
        }
    }

    [HttpPost("comments")]
    public async Task<ActionResult<GitHubComment>> AddComment([FromBody] AddCommentRequest request)
    {
        try
        {
            _logger.LogInformation("Adding comment to {ContentType} in repository: {Repository}",
                request.ContentType, request.Repository);

            var result = await _workflowOrchestrator.AddCommentWorkflowAsync(request);
            
            if (result.Success)
            {
                return Ok(result.Data);
            }
            
            return BadRequest(new { error = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment");
            return StatusCode(500, new { error = "Internal server error adding comment" });
        }
    }

    [HttpPut("content")]
    public async Task<ActionResult> UpdateContent([FromBody] UpdateContentRequest request)
    {
        try
        {
            _logger.LogInformation("Updating {ContentType} in repository: {Repository}",
                request.ContentType, request.Repository);

            var result = await _workflowOrchestrator.UpdateContentWorkflowAsync(request);
            
            if (result.Success)
            {
                return Ok(result.Data);
            }
            
            return BadRequest(new { error = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating content");
            return StatusCode(500, new { error = "Internal server error updating content" });
        }
    }

    [HttpPost("prompt-action")]
    public async Task<ActionResult<WorkflowResult>> ExecutePromptBasedAction([FromBody] PromptBasedActionRequest request)
    {
        try
        {
            _logger.LogInformation("Executing prompt-based action: {Prompt}", request.Prompt);

            var result = await _workflowOrchestrator.ExecutePromptBasedActionAsync(request);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing prompt-based action");
            return StatusCode(500, new { error = "Internal server error executing prompt-based action" });
        }
    }

    [HttpGet("discussions/{repository}/{discussionNumber:int}")]
    public async Task<ActionResult<GitHubDiscussion>> GetDiscussion(string repository, int discussionNumber)
    {
        try
        {
            var discussion = await _discussionsService.GetDiscussionAsync(repository, discussionNumber);
            return Ok(discussion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting discussion {Number} from repository: {Repository}", discussionNumber, repository);
            return StatusCode(500, new { error = "Internal server error getting discussion" });
        }
    }

    [HttpGet("issues/{repository}/{issueNumber:int}")]
    public async Task<ActionResult<GitHubIssue>> GetIssue(string repository, int issueNumber)
    {
        try
        {
            var issue = await _issuesService.GetIssueAsync(repository, issueNumber);
            return Ok(issue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting issue {Number} from repository: {Repository}", issueNumber, repository);
            return StatusCode(500, new { error = "Internal server error getting issue" });
        }
    }

    [HttpGet("organization/{organization}/discussions")]
    public async Task<ActionResult<IEnumerable<GitHubDiscussion>>> GetOrganizationDiscussions(string organization, [FromQuery] int limit = 50)
    {
        try
        {
            var discussions = await _discussionsService.GetOrganizationDiscussionsAsync(organization, limit);
            return Ok(discussions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization discussions: {Organization}", organization);
            return StatusCode(500, new { error = "Internal server error getting organization discussions" });
        }
    }

    [HttpGet("organization/{organization}/issues")]
    public async Task<ActionResult<IEnumerable<GitHubIssue>>> GetOrganizationIssues(string organization, [FromQuery] string state = "open", [FromQuery] int limit = 50)
    {
        try
        {
            var issues = await _issuesService.GetOrganizationIssuesAsync(organization, state, limit);
            return Ok(issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization issues: {Organization}", organization);
            return StatusCode(500, new { error = "Internal server error getting organization issues" });
        }
    }
}