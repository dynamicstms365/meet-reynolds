using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.PullRequest;
using Octokit.Webhooks.Events.Issues;
using Octokit.Webhooks.Events.WorkflowRun;
using CopilotAgent.Services;
using Shared.Models;

namespace CopilotAgent.Services;

/// <summary>
/// Octokit.NET webhook event processor replacing custom webhook implementation
/// Provides type-safe webhook processing with automatic signature validation
/// </summary>
public sealed class OctokitWebhookEventProcessor : WebhookEventProcessor
{
    private readonly IGitHubWorkflowOrchestrator _workflowOrchestrator;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<OctokitWebhookEventProcessor> _logger;

    public OctokitWebhookEventProcessor(
        IGitHubWorkflowOrchestrator workflowOrchestrator,
        ISecurityAuditService auditService,
        ILogger<OctokitWebhookEventProcessor> logger)
    {
        _workflowOrchestrator = workflowOrchestrator;
        _auditService = auditService;
        _logger = logger;
    }

    protected override async Task ProcessPullRequestWebhookAsync(
        WebhookHeaders headers, 
        PullRequestEvent pullRequestEvent, 
        PullRequestAction action)
    {
        _logger.LogInformation("Processing pull request webhook: {Action} for PR #{Number} in {Repository}", 
            action, pullRequestEvent.PullRequest.Number, pullRequestEvent.Repository?.FullName ?? "unknown");

        try
        {
            // Convert Octokit event to our internal payload format
            var payload = ConvertToInternalPayload(pullRequestEvent, "pull_request", action.ToString());
            
            // Process through existing workflow orchestrator
            var result = await _workflowOrchestrator.ProcessWebhookEventAsync(payload);
            
            await _auditService.LogWebhookEventAsync(payload, result.Success ? "SUCCESS" : "FAILED");
            
            _logger.LogInformation("Pull request webhook processed: {Success}, Actions: {ActionsCount}", 
                result.Success, result.Actions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pull request webhook");
            await _auditService.LogEventAsync("GitHub_Webhook_Processing", 
                action: "PULL_REQUEST_ERROR", 
                result: "FAILED",
                details: new { Error = ex.Message, Action = action.ToString() });
        }
    }

    protected override async Task ProcessIssuesWebhookAsync(
        WebhookHeaders headers,
        IssuesEvent issuesEvent,
        IssuesAction action)
    {
        _logger.LogInformation("Processing issues webhook: {Action} for issue #{Number} in {Repository}", 
            action, issuesEvent.Issue.Number, issuesEvent.Repository?.FullName ?? "unknown");

        try
        {
            var payload = ConvertToInternalPayload(issuesEvent, "issues", action.ToString());
            var result = await _workflowOrchestrator.ProcessWebhookEventAsync(payload);
            
            await _auditService.LogWebhookEventAsync(payload, result.Success ? "SUCCESS" : "FAILED");
            
            _logger.LogInformation("Issues webhook processed: {Success}, Actions: {ActionsCount}", 
                result.Success, result.Actions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing issues webhook");
            await _auditService.LogEventAsync("GitHub_Webhook_Processing", 
                action: "ISSUES_ERROR", 
                result: "FAILED",
                details: new { Error = ex.Message, Action = action.ToString() });
        }
    }

    protected override async Task ProcessWorkflowRunWebhookAsync(
        WebhookHeaders headers,
        WorkflowRunEvent workflowRunEvent,
        WorkflowRunAction action)
    {
        _logger.LogInformation("Processing workflow run webhook: {Action} for workflow '{Name}' in {Repository}", 
            action, workflowRunEvent.WorkflowRun.Name, workflowRunEvent.Repository?.FullName ?? "unknown");

        try
        {
            var payload = ConvertToInternalPayload(workflowRunEvent, "workflow_run", action.ToString());
            var result = await _workflowOrchestrator.ProcessWebhookEventAsync(payload);
            
            await _auditService.LogWebhookEventAsync(payload, result.Success ? "SUCCESS" : "FAILED");
            
            _logger.LogInformation("Workflow run webhook processed: {Success}, Actions: {ActionsCount}", 
                result.Success, result.Actions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing workflow run webhook");
            await _auditService.LogEventAsync("GitHub_Webhook_Processing", 
                action: "WORKFLOW_RUN_ERROR", 
                result: "FAILED",
                details: new { Error = ex.Message, Action = action.ToString() });
        }
    }

    /// <summary>
    /// Convert Octokit webhook events to our internal GitHubWebhookPayload format
    /// This maintains compatibility with existing workflow orchestrator
    /// </summary>
    private GitHubWebhookPayload ConvertToInternalPayload(object webhookEvent, string eventType, string action)
    {
        var payload = new GitHubWebhookPayload
        {
            Action = action.ToLowerInvariant(),
            Repository = GetRepositoryFromEvent(webhookEvent),
            Installation = GetInstallationFromEvent(webhookEvent),
            Sender = GetSenderFromEvent(webhookEvent)
        };

        // Set event-specific properties based on event type
        switch (eventType)
        {
            case "pull_request" when webhookEvent is PullRequestEvent prEvent:
                payload.PullRequest = new GitHubPullRequestPayload
                {
                    Id = prEvent.PullRequest.Id,
                    Number = (int)prEvent.PullRequest.Number, // Cast long to int
                    Title = prEvent.PullRequest.Title,
                    Body = prEvent.PullRequest.Body ?? string.Empty,
                    State = prEvent.PullRequest.State.ToString(),
                    User = new GitHubUser
                    {
                        Id = prEvent.PullRequest.User.Id,
                        Login = prEvent.PullRequest.User.Login,
                        Type = prEvent.PullRequest.User.Type.ToString()
                    },
                    CreatedAt = prEvent.PullRequest.CreatedAt.DateTime, // Convert DateTimeOffset to DateTime
                    UpdatedAt = prEvent.PullRequest.UpdatedAt.DateTime
                };
                break;
            case "issues" when webhookEvent is IssuesEvent issueEvent:
                payload.Issue = new GitHubIssuePayload
                {
                    Id = issueEvent.Issue.Id,
                    Number = (int)issueEvent.Issue.Number, // Cast long to int
                    Title = issueEvent.Issue.Title,
                    Body = issueEvent.Issue.Body ?? string.Empty,
                    State = issueEvent.Issue.State?.ToString() ?? "unknown",
                    User = new GitHubUser
                    {
                        Id = issueEvent.Issue.User?.Id ?? 0, // Handle null User
                        Login = issueEvent.Issue.User?.Login ?? "unknown",
                        Type = issueEvent.Issue.User?.Type.ToString() ?? "unknown"
                    },
                    CreatedAt = issueEvent.Issue.CreatedAt.DateTime, // Convert DateTimeOffset to DateTime
                    UpdatedAt = issueEvent.Issue.UpdatedAt.DateTime
                };
                break;
        }

        return payload;
    }

    private GitHubRepository? GetRepositoryFromEvent(object webhookEvent)
    {
        var repo = webhookEvent switch
        {
            PullRequestEvent prEvent => prEvent.Repository,
            IssuesEvent issueEvent => issueEvent.Repository,
            WorkflowRunEvent wrEvent => wrEvent.Repository,
            _ => null
        };

        if (repo == null) return null;

        return new GitHubRepository
        {
            Id = repo.Id,
            Name = repo.Name,
            FullName = repo.FullName,
            Description = repo.Description,
            Private = repo.Private
        };
    }

    private GitHubInstallation? GetInstallationFromEvent(object webhookEvent)
    {
        var installation = webhookEvent switch
        {
            PullRequestEvent prEvent => prEvent.Installation,
            IssuesEvent issueEvent => issueEvent.Installation,
            WorkflowRunEvent wrEvent => wrEvent.Installation,
            _ => null
        };

        if (installation == null) return null;

        return new GitHubInstallation
        {
            Id = installation.Id,
            NodeId = installation.NodeId ?? string.Empty,
            Account = string.Empty // Simplified to avoid compilation issues
        };
    }

    private GitHubUser? GetSenderFromEvent(object webhookEvent)
    {
        var sender = webhookEvent switch
        {
            PullRequestEvent prEvent => prEvent.Sender,
            IssuesEvent issueEvent => issueEvent.Sender,
            WorkflowRunEvent wrEvent => wrEvent.Sender,
            _ => null
        };

        if (sender == null) return null;

        return new GitHubUser
        {
            Id = sender.Id,
            Login = sender.Login,
            Type = sender.Type.ToString()
        };
    }
}