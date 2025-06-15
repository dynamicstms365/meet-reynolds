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
    private readonly IGitHubIssuePRSynchronizationService _synchronizationService;
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
        IGitHubIssuePRSynchronizationService synchronizationService,
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
        _synchronizationService = synchronizationService;
        _logger = logger;
        _configuration = configuration;
    }

    // Webhook endpoint has been migrated to Octokit.Webhooks.AspNetCore
    // See OctokitWebhookEventProcessor for the new implementation
    // Endpoint is now mapped at /api/github/webhook using app.MapGitHubWebhooks()

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
            version = System.Environment.GetEnvironmentVariable("COPILOT_VERSION") ?? "dev-local"
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

    // GitHub Issue-PR Synchronization endpoints

    [HttpGet("sync/report/{repository}")]
    public async Task<ActionResult<IssuePRSynchronizationReport>> GetSynchronizationReport(string repository)
    {
        try
        {
            _logger.LogInformation("Generating synchronization report for repository: {Repository}", repository);
            
            var report = await _synchronizationService.GenerateSynchronizationReportAsync(repository);
            
            await _auditService.LogEventAsync(
                "GitHub_Sync_Report_Generated",
                action: "GenerateReport",
                result: "SUCCESS",
                details: new { 
                    Repository = repository,
                    TotalIssues = report.Summary.TotalIssues,
                    TotalPRs = report.Summary.TotalPRs,
                    OrphanedPRs = report.Summary.OrphanedPRs,
                    OrphanedIssues = report.Summary.OrphanedIssues
                });

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating synchronization report for repository: {Repository}", repository);
            
            await _auditService.LogEventAsync(
                "GitHub_Sync_Report_Generated",
                action: "GenerateReport",
                result: "ERROR",
                details: new { Repository = repository, Error = ex.Message });

            return StatusCode(500, new { error = "Internal server error generating synchronization report" });
        }
    }

    [HttpPost("sync/issue/{repository}/{issueNumber:int}")]
    public async Task<ActionResult<object>> SynchronizeIssue(string repository, int issueNumber)
    {
        try
        {
            _logger.LogInformation("Synchronizing issue #{IssueNumber} in repository: {Repository}", issueNumber, repository);
            
            var success = await _synchronizationService.SynchronizeIssueWithPRsAsync(repository, issueNumber);
            
            await _auditService.LogEventAsync(
                "GitHub_Issue_Synchronized",
                action: "SynchronizeIssue",
                result: success ? "SUCCESS" : "FAILED",
                details: new { Repository = repository, IssueNumber = issueNumber });

            return Ok(new { 
                success = success,
                message = success 
                    ? $"Issue #{issueNumber} synchronized successfully" 
                    : $"Failed to synchronize issue #{issueNumber}",
                issueNumber = issueNumber,
                repository = repository
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing issue #{IssueNumber} in repository: {Repository}", issueNumber, repository);
            
            await _auditService.LogEventAsync(
                "GitHub_Issue_Synchronized",
                action: "SynchronizeIssue",
                result: "ERROR",
                details: new { Repository = repository, IssueNumber = issueNumber, Error = ex.Message });

            return StatusCode(500, new { error = "Internal server error synchronizing issue" });
        }
    }

    [HttpPost("sync/all/{repository}")]
    public async Task<ActionResult<object>> SynchronizeAllIssues(string repository)
    {
        try
        {
            _logger.LogInformation("Synchronizing all issues in repository: {Repository}", repository);
            
            var synchronizedCount = await _synchronizationService.SynchronizeAllIssuesWithPRsAsync(repository);
            
            await _auditService.LogEventAsync(
                "GitHub_All_Issues_Synchronized",
                action: "SynchronizeAllIssues",
                result: "SUCCESS",
                details: new { Repository = repository, SynchronizedCount = synchronizedCount });

            return Ok(new { 
                success = true,
                message = $"Synchronized {synchronizedCount} issues in repository {repository}",
                synchronizedCount = synchronizedCount,
                repository = repository
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing all issues in repository: {Repository}", repository);
            
            await _auditService.LogEventAsync(
                "GitHub_All_Issues_Synchronized",
                action: "SynchronizeAllIssues",
                result: "ERROR",
                details: new { Repository = repository, Error = ex.Message });

            return StatusCode(500, new { error = "Internal server error synchronizing all issues" });
        }
    }

    [HttpGet("pullrequests/{repository}")]
    public async Task<ActionResult<IEnumerable<GitHubPullRequest>>> GetPullRequests(string repository, [FromQuery] string state = "all", [FromQuery] int limit = 100)
    {
        try
        {
            _logger.LogInformation("Getting pull requests for repository: {Repository} (state: {State}, limit: {Limit})", repository, state, limit);
            
            var pullRequests = await _synchronizationService.GetPullRequestsByRepositoryAsync(repository, state, limit);
            
            await _auditService.LogEventAsync(
                "GitHub_Pull_Requests_Retrieved",
                action: "GetPullRequests",
                result: "SUCCESS",
                details: new { Repository = repository, State = state, Limit = limit, Count = pullRequests.Count() });

            return Ok(pullRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pull requests for repository: {Repository}", repository);
            
            await _auditService.LogEventAsync(
                "GitHub_Pull_Requests_Retrieved",
                action: "GetPullRequests",
                result: "ERROR",
                details: new { Repository = repository, Error = ex.Message });

            return StatusCode(500, new { error = "Internal server error getting pull requests" });
        }
    }

    [HttpGet("pullrequests/{repository}/{pullRequestNumber:int}")]
    public async Task<ActionResult<GitHubPullRequest>> GetPullRequest(string repository, int pullRequestNumber)
    {
        try
        {
            _logger.LogInformation("Getting pull request #{PRNumber} for repository: {Repository}", pullRequestNumber, repository);
            
            var pullRequest = await _synchronizationService.GetPullRequestAsync(repository, pullRequestNumber);
            
            await _auditService.LogEventAsync(
                "GitHub_Pull_Request_Retrieved",
                action: "GetPullRequest",
                result: "SUCCESS",
                details: new { Repository = repository, PRNumber = pullRequestNumber });

            return Ok(pullRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pull request #{PRNumber} for repository: {Repository}", pullRequestNumber, repository);
            
            await _auditService.LogEventAsync(
                "GitHub_Pull_Request_Retrieved",
                action: "GetPullRequest",
                result: "ERROR",
                details: new { Repository = repository, PRNumber = pullRequestNumber, Error = ex.Message });

            return StatusCode(500, new { error = "Internal server error getting pull request" });
        }
    }

    [HttpGet("sync/linked-prs/{repository}/{issueNumber:int}")]
    public async Task<ActionResult<IEnumerable<GitHubPullRequest>>> GetLinkedPullRequests(string repository, int issueNumber)
    {
        try
        {
            _logger.LogInformation("Finding pull requests linked to issue #{IssueNumber} in repository: {Repository}", issueNumber, repository);
            
            var linkedPRs = await _synchronizationService.FindPullRequestsLinkedToIssueAsync(repository, issueNumber);
            
            await _auditService.LogEventAsync(
                "GitHub_Linked_PRs_Retrieved",
                action: "GetLinkedPRs",
                result: "SUCCESS",
                details: new { Repository = repository, IssueNumber = issueNumber, LinkedPRCount = linkedPRs.Count() });

            return Ok(linkedPRs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding pull requests linked to issue #{IssueNumber} in repository: {Repository}", issueNumber, repository);
            
            await _auditService.LogEventAsync(
                "GitHub_Linked_PRs_Retrieved",
                action: "GetLinkedPRs",
                result: "ERROR",
                details: new { Repository = repository, IssueNumber = issueNumber, Error = ex.Message });

            return StatusCode(500, new { error = "Internal server error finding linked pull requests" });
        }
    }

    [HttpGet("sync/linked-issues/{repository}/{pullRequestNumber:int}")]
    public async Task<ActionResult<IEnumerable<GitHubIssue>>> GetLinkedIssues(string repository, int pullRequestNumber)
    {
        try
        {
            _logger.LogInformation("Finding issues linked to pull request #{PRNumber} in repository: {Repository}", pullRequestNumber, repository);
            
            var linkedIssues = await _synchronizationService.FindIssuesLinkedToPullRequestAsync(repository, pullRequestNumber);
            
            await _auditService.LogEventAsync(
                "GitHub_Linked_Issues_Retrieved",
                action: "GetLinkedIssues",
                result: "SUCCESS",
                details: new { Repository = repository, PRNumber = pullRequestNumber, LinkedIssueCount = linkedIssues.Count() });

            return Ok(linkedIssues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding issues linked to pull request #{PRNumber} in repository: {Repository}", pullRequestNumber, repository);
            
            await _auditService.LogEventAsync(
                "GitHub_Linked_Issues_Retrieved",
                action: "GetLinkedIssues",
                result: "ERROR",
                details: new { Repository = repository, PRNumber = pullRequestNumber, Error = ex.Message });

            return StatusCode(500, new { error = "Internal server error finding linked issues" });
        }
    }
}