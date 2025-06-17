using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using CopilotAgent.Services;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using System.Net;
using CopilotAgent.Models;

namespace CopilotAgent.Controllers;

/// <summary>
/// GitHub Integration Controller for comprehensive repository management and orchestration
/// Designed for Azure APIM MCP integration with full OpenAPI 3.0 documentation
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("GitHub")]
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

    /// <summary>
    /// Test GitHub App connectivity and authentication
    /// </summary>
    /// <returns>Connectivity test results with repository access information</returns>
    /// <response code="200">GitHub connectivity test successful</response>
    /// <response code="500">GitHub connectivity test failed</response>
    [HttpGet("test")]
    [ProducesResponseType(typeof(GitHubConnectivityResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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

    /// <summary>
    /// GitHub service health check
    /// </summary>
    /// <returns>Health status of GitHub integration services</returns>
    /// <response code="200">GitHub services are healthy</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(GitHubHealthStatus), (int)HttpStatusCode.OK)]
    public ActionResult<GitHubHealthStatus> Health()
    {
        return Ok(new GitHubHealthStatus
        { 
            Status = "healthy",
            Service = "GitHub Integration",
            Timestamp = DateTime.UtcNow,
            Version = System.Environment.GetEnvironmentVariable("COPILOT_VERSION") ?? "dev-local",
            Capabilities = new[]
            {
                "ConnectivityTesting",
                "SemanticSearch", 
                "DiscussionManagement",
                "IssueManagement",
                "WorkflowOrchestration",
                "SecurityAudit"
            }
        });
    }

    /// <summary>
    /// Get GitHub App installation information
    /// </summary>
    /// <returns>GitHub App installation details and permissions</returns>
    /// <response code="200">Installation information retrieved successfully</response>
    /// <response code="500">Failed to retrieve installation information</response>
    [HttpGet("installation-info")]
    [ProducesResponseType(typeof(GitHubInstallationInfo), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<GitHubInstallationInfo>> GetInstallationInfo()
    {
        try
        {
            _logger.LogInformation("Getting GitHub App installation information");
            
            var auth = await _authService.GetInstallationTokenAsync();
            
            await _auditService.LogEventAsync(
                "GitHub_Installation_Info_Request",
                action: "GetInfo",
                result: "SUCCESS");

            return Ok(new GitHubInstallationInfo
            {
                Success = true,
                TokenExpiresAt = auth.ExpiresAt,
                Permissions = auth.Permissions,
                HasValidToken = auth.ExpiresAt > DateTime.UtcNow
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

            return StatusCode(500, new ErrorResponse { Error = "InstallationInfoFailed", Message = ex.Message });
        }
    }

    /// <summary>
    /// Perform semantic search across GitHub repositories
    /// </summary>
    /// <param name="request">Semantic search parameters</param>
    /// <returns>Semantic search results with relevance scoring</returns>
    /// <response code="200">Search completed successfully</response>
    /// <response code="400">Invalid search parameters</response>
    /// <response code="500">Search processing failed</response>
    [HttpPost("search")]
    [ProducesResponseType(typeof(SemanticSearchResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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
            
            return BadRequest(new ErrorResponse { Error = "SearchFailed", Message = result.Error ?? "Search operation failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing semantic search request");
            return StatusCode(500, new ErrorResponse { Error = "SearchProcessingFailed", Message = "Internal server error processing search request" });
        }
    }

    /// <summary>
    /// Create a new GitHub discussion
    /// </summary>
    /// <param name="request">Discussion creation parameters</param>
    /// <returns>Created discussion details</returns>
    /// <response code="200">Discussion created successfully</response>
    /// <response code="400">Invalid discussion parameters</response>
    /// <response code="500">Discussion creation failed</response>
    [HttpPost("discussions")]
    [ProducesResponseType(typeof(GitHubDiscussion), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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
            
            return BadRequest(new ErrorResponse { Error = "DiscussionCreationFailed", Message = result.Error ?? "Discussion creation failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating discussion");
            return StatusCode(500, new ErrorResponse { Error = "DiscussionCreationFailed", Message = "Internal server error creating discussion" });
        }
    }

    /// <summary>
    /// Create a new GitHub issue
    /// </summary>
    /// <param name="request">Issue creation parameters</param>
    /// <returns>Created issue details</returns>
    /// <response code="200">Issue created successfully</response>
    /// <response code="400">Invalid issue parameters</response>
    /// <response code="500">Issue creation failed</response>
    [HttpPost("issues")]
    [ProducesResponseType(typeof(GitHubIssue), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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
            
            return BadRequest(new ErrorResponse { Error = "IssueCreationFailed", Message = result.Error ?? "Issue creation failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating issue");
            return StatusCode(500, new ErrorResponse { Error = "IssueCreationFailed", Message = "Internal server error creating issue" });
        }
    }

    /// <summary>
    /// Add a comment to a GitHub issue or discussion
    /// </summary>
    /// <param name="request">Comment addition parameters</param>
    /// <returns>Created comment details</returns>
    /// <response code="200">Comment added successfully</response>
    /// <response code="400">Invalid comment parameters</response>
    /// <response code="500">Comment addition failed</response>
    [HttpPost("comments")]
    [ProducesResponseType(typeof(GitHubComment), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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
            
            return BadRequest(new ErrorResponse { Error = "CommentAdditionFailed", Message = result.Error ?? "Comment addition failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment");
            return StatusCode(500, new ErrorResponse { Error = "CommentAdditionFailed", Message = "Internal server error adding comment" });
        }
    }

    /// <summary>
    /// Update GitHub issue or discussion content
    /// </summary>
    /// <param name="request">Content update parameters</param>
    /// <returns>Update operation result</returns>
    /// <response code="200">Content updated successfully</response>
    /// <response code="400">Invalid update parameters</response>
    /// <response code="500">Content update failed</response>
    [HttpPut("content")]
    [ProducesResponseType(typeof(UpdateContentResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<UpdateContentResult>> UpdateContent([FromBody] UpdateContentRequest request)
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
            
            return BadRequest(new ErrorResponse { Error = "ContentUpdateFailed", Message = result.Error ?? "Content update failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating content");
            return StatusCode(500, new ErrorResponse { Error = "ContentUpdateFailed", Message = "Internal server error updating content" });
        }
    }

    /// <summary>
    /// Execute prompt-based GitHub actions using natural language
    /// </summary>
    /// <param name="request">Natural language prompt for GitHub operations</param>
    /// <returns>Execution result with operation details</returns>
    /// <response code="200">Prompt action executed successfully</response>
    /// <response code="400">Invalid prompt parameters</response>
    /// <response code="500">Prompt action execution failed</response>
    [HttpPost("prompt-action")]
    [ProducesResponseType(typeof(WorkflowResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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
            return StatusCode(500, new ErrorResponse { Error = "PromptActionFailed", Message = "Internal server error executing prompt-based action" });
        }
    }

    /// <summary>
    /// Get a specific GitHub discussion by repository and number
    /// </summary>
    /// <param name="repository">Repository name in format 'owner/repo'</param>
    /// <param name="discussionNumber">Discussion number</param>
    /// <returns>Discussion details</returns>
    /// <response code="200">Discussion retrieved successfully</response>
    /// <response code="404">Discussion not found</response>
    /// <response code="500">Discussion retrieval failed</response>
    [HttpGet("discussions/{repository}/{discussionNumber:int}")]
    [ProducesResponseType(typeof(GitHubDiscussion), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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
            return StatusCode(500, new ErrorResponse { Error = "DiscussionRetrievalFailed", Message = "Internal server error getting discussion" });
        }
    }

    /// <summary>
    /// Get a specific GitHub issue by repository and number
    /// </summary>
    /// <param name="repository">Repository name in format 'owner/repo'</param>
    /// <param name="issueNumber">Issue number</param>
    /// <returns>Issue details</returns>
    /// <response code="200">Issue retrieved successfully</response>
    /// <response code="404">Issue not found</response>
    /// <response code="500">Issue retrieval failed</response>
    [HttpGet("issues/{repository}/{issueNumber:int}")]
    [ProducesResponseType(typeof(GitHubIssue), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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
            return StatusCode(500, new ErrorResponse { Error = "IssueRetrievalFailed", Message = "Internal server error getting issue" });
        }
    }

    /// <summary>
    /// Get all discussions for a GitHub organization
    /// </summary>
    /// <param name="organization">Organization name</param>
    /// <param name="limit">Maximum number of discussions to retrieve (default: 50)</param>
    /// <returns>List of organization discussions</returns>
    /// <response code="200">Discussions retrieved successfully</response>
    /// <response code="500">Discussion retrieval failed</response>
    [HttpGet("organization/{organization}/discussions")]
    [ProducesResponseType(typeof(IEnumerable<GitHubDiscussion>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<IEnumerable<GitHubDiscussion>>> GetOrganizationDiscussions(
        string organization, 
        [FromQuery] int limit = 50)
    {
        try
        {
            var discussions = await _discussionsService.GetOrganizationDiscussionsAsync(organization, limit);
            return Ok(discussions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization discussions: {Organization}", organization);
            return StatusCode(500, new ErrorResponse { Error = "OrganizationDiscussionsRetrievalFailed", Message = "Internal server error getting organization discussions" });
        }
    }

    /// <summary>
    /// Get all issues for a GitHub organization
    /// </summary>
    /// <param name="organization">Organization name</param>
    /// <param name="state">Issue state filter (open, closed, all)</param>
    /// <param name="limit">Maximum number of issues to retrieve (default: 50)</param>
    /// <returns>List of organization issues</returns>
    /// <response code="200">Issues retrieved successfully</response>
    /// <response code="500">Issue retrieval failed</response>
    [HttpGet("organization/{organization}/issues")]
    [ProducesResponseType(typeof(IEnumerable<GitHubIssue>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<IEnumerable<GitHubIssue>>> GetOrganizationIssues(
        string organization, 
        [FromQuery] string state = "open", 
        [FromQuery] int limit = 50)
    {
        try
        {
            var issues = await _issuesService.GetOrganizationIssuesAsync(organization, state, limit);
            return Ok(issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization issues: {Organization}", organization);
            return StatusCode(500, new ErrorResponse { Error = "OrganizationIssuesRetrievalFailed", Message = "Internal server error getting organization issues" });
        }
    }

    // GitHub Issue-PR Synchronization endpoints

    /// <summary>
    /// Generate synchronization report between issues and pull requests
    /// </summary>
    /// <param name="repository">Repository name in format 'owner/repo'</param>
    /// <returns>Comprehensive synchronization report</returns>
    /// <response code="200">Synchronization report generated successfully</response>
    /// <response code="500">Report generation failed</response>
    [HttpGet("sync/report/{repository}")]
    [ProducesResponseType(typeof(IssuePRSynchronizationReport), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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

            return StatusCode(500, new ErrorResponse { Error = "SyncReportGenerationFailed", Message = "Internal server error generating synchronization report" });
        }
    }

    /// <summary>
    /// Synchronize a specific issue with its related pull requests
    /// </summary>
    /// <param name="repository">Repository name in format 'owner/repo'</param>
    /// <param name="issueNumber">Issue number to synchronize</param>
    /// <returns>Synchronization operation result</returns>
    /// <response code="200">Issue synchronized successfully</response>
    /// <response code="500">Issue synchronization failed</response>
    [HttpPost("sync/issue/{repository}/{issueNumber:int}")]
    [ProducesResponseType(typeof(SynchronizationResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<SynchronizationResult>> SynchronizeIssue(string repository, int issueNumber)
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

            return Ok(new SynchronizationResult
            { 
                Success = success,
                Message = success 
                    ? $"Issue #{issueNumber} synchronized successfully" 
                    : $"Failed to synchronize issue #{issueNumber}",
                IssueNumber = issueNumber,
                Repository = repository
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

            return StatusCode(500, new ErrorResponse { Error = "IssueSynchronizationFailed", Message = "Internal server error synchronizing issue" });
        }
    }

    /// <summary>
    /// Synchronize all issues in a repository with their pull requests
    /// </summary>
    /// <param name="repository">Repository name in format 'owner/repo'</param>
    /// <returns>Bulk synchronization operation result</returns>
    /// <response code="200">All issues synchronized successfully</response>
    /// <response code="500">Bulk synchronization failed</response>
    [HttpPost("sync/all/{repository}")]
    [ProducesResponseType(typeof(BulkSynchronizationResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<BulkSynchronizationResult>> SynchronizeAllIssues(string repository)
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

            return Ok(new BulkSynchronizationResult
            { 
                Success = true,
                Message = $"Synchronized {synchronizedCount} issues in repository {repository}",
                SynchronizedCount = synchronizedCount,
                Repository = repository
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

            return StatusCode(500, new ErrorResponse { Error = "BulkSynchronizationFailed", Message = "Internal server error synchronizing all issues" });
        }
    }

    /// <summary>
    /// Get all pull requests for a repository
    /// </summary>
    /// <param name="repository">Repository name in format 'owner/repo'</param>
    /// <param name="state">Pull request state filter (open, closed, all)</param>
    /// <param name="limit">Maximum number of pull requests to retrieve (default: 100)</param>
    /// <returns>List of pull requests</returns>
    /// <response code="200">Pull requests retrieved successfully</response>
    /// <response code="500">Pull request retrieval failed</response>
    [HttpGet("pullrequests/{repository}")]
    [ProducesResponseType(typeof(IEnumerable<GitHubPullRequest>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<IEnumerable<GitHubPullRequest>>> GetPullRequests(
        string repository, 
        [FromQuery] string state = "all", 
        [FromQuery] int limit = 100)
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

            return StatusCode(500, new ErrorResponse { Error = "PullRequestRetrievalFailed", Message = "Internal server error getting pull requests" });
        }
    }

    /// <summary>
    /// Get a specific pull request by repository and number
    /// </summary>
    /// <param name="repository">Repository name in format 'owner/repo'</param>
    /// <param name="pullRequestNumber">Pull request number</param>
    /// <returns>Pull request details</returns>
    /// <response code="200">Pull request retrieved successfully</response>
    /// <response code="404">Pull request not found</response>
    /// <response code="500">Pull request retrieval failed</response>
    [HttpGet("pullrequests/{repository}/{pullRequestNumber:int}")]
    [ProducesResponseType(typeof(GitHubPullRequest), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
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

            return StatusCode(500, new ErrorResponse { Error = "PullRequestRetrievalFailed", Message = "Internal server error getting pull request" });
        }
    }

    /// <summary>
    /// Get pull requests linked to a specific issue
    /// </summary>
    /// <param name="repository">Repository name in format 'owner/repo'</param>
    /// <param name="issueNumber">Issue number</param>
    /// <returns>List of linked pull requests</returns>
    /// <response code="200">Linked pull requests retrieved successfully</response>
    /// <response code="500">Linked pull request retrieval failed</response>
    [HttpGet("sync/linked-prs/{repository}/{issueNumber:int}")]
    [ProducesResponseType(typeof(IEnumerable<GitHubPullRequest>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<IEnumerable<GitHubPullRequest>>> GetLinkedPullRequests(string repository, int issueNumber)
    {
        try
        {
            _logger.LogInformation("Getting pull requests linked to issue #{IssueNumber} in repository: {Repository}", issueNumber, repository);
            
            // TODO: Implement GetLinkedPullRequestsAsync method in IGitHubIssuePRSynchronizationService
            var linkedPRs = Array.Empty<GitHubPullRequest>();
            
            await _auditService.LogEventAsync(
                "GitHub_Linked_PRs_Retrieved",
                action: "GetLinkedPRs",
                result: "SUCCESS",
                details: new { Repository = repository, IssueNumber = issueNumber, LinkedPRCount = linkedPRs.Count() });

            return Ok(linkedPRs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting linked pull requests for issue #{IssueNumber} in repository: {Repository}", issueNumber, repository);
            
            await _auditService.LogEventAsync(
                "GitHub_Linked_PRs_Retrieved",
                action: "GetLinkedPRs",
                result: "ERROR",
                details: new { Repository = repository, IssueNumber = issueNumber, Error = ex.Message });

            return StatusCode(500, new ErrorResponse { Error = "LinkedPRRetrievalFailed", Message = "Internal server error getting linked pull requests" });
        }
    }

    /// <summary>
    /// Get issues linked to a specific pull request
    /// </summary>
    /// <param name="repository">Repository name in format 'owner/repo'</param>
    /// <param name="pullRequestNumber">Pull request number</param>
    /// <returns>List of linked issues</returns>
    /// <response code="200">Linked issues retrieved successfully</response>
    /// <response code="500">Linked issue retrieval failed</response>
    [HttpGet("sync/linked-issues/{repository}/{pullRequestNumber:int}")]
    [ProducesResponseType(typeof(IEnumerable<GitHubIssue>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<IEnumerable<GitHubIssue>>> GetLinkedIssues(string repository, int pullRequestNumber)
    {
        try
        {
            _logger.LogInformation("Getting issues linked to pull request #{PRNumber} in repository: {Repository}", pullRequestNumber, repository);
            
            // TODO: Implement GetLinkedIssuesAsync method in IGitHubIssuePRSynchronizationService
            var linkedIssues = Array.Empty<GitHubIssue>();
            
            await _auditService.LogEventAsync(
                "GitHub_Linked_Issues_Retrieved",
                action: "GetLinkedIssues",
                result: "SUCCESS",
                details: new { Repository = repository, PRNumber = pullRequestNumber, LinkedIssueCount = linkedIssues.Count() });

            return Ok(linkedIssues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting linked issues for pull request #{PRNumber} in repository: {Repository}", pullRequestNumber, repository);
            
            await _auditService.LogEventAsync(
                "GitHub_Linked_Issues_Retrieved",
                action: "GetLinkedIssues",
                result: "ERROR",
                details: new { Repository = repository, PRNumber = pullRequestNumber, Error = ex.Message });

            return StatusCode(500, new ErrorResponse { Error = "LinkedIssueRetrievalFailed", Message = "Internal server error getting linked issues" });
        }
    }
}

// Supporting response models for OpenAPI documentation
public class GitHubHealthStatus
{
    public string Status { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public string[] Capabilities { get; set; } = Array.Empty<string>();
}

public class GitHubInstallationInfo
{
    public bool Success { get; set; }
    public DateTime TokenExpiresAt { get; set; }
    public object Permissions { get; set; } = new();
    public bool HasValidToken { get; set; }
}

public class UpdateContentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object Data { get; set; } = new();
}

public class SynchronizationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int IssueNumber { get; set; }
    public string Repository { get; set; } = string.Empty;
}

public class BulkSynchronizationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SynchronizedCount { get; set; }
    public string Repository { get; set; } = string.Empty;
}