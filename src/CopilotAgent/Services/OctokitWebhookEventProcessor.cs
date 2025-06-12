using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.PullRequest;
using Octokit.Webhooks.Events.Issues;
using Octokit.Webhooks.Events.WorkflowRun;
using Octokit.Webhooks.Events.Push;
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
            action, pullRequestEvent.PullRequest.Number, pullRequestEvent.Repository.FullName);

        try
        {
            // Convert Octokit event to our internal payload format
            var payload = ConvertToInternalPayload(pullRequestEvent, "pull_request");
            
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
            action, issuesEvent.Issue.Number, issuesEvent.Repository.FullName);

        try
        {
            var payload = ConvertToInternalPayload(issuesEvent, "issues");
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
            action, workflowRunEvent.WorkflowRun.Name, workflowRunEvent.Repository.FullName);

        try
        {
            var payload = ConvertToInternalPayload(workflowRunEvent, "workflow_run");
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

    protected override async Task ProcessPushWebhookAsync(WebhookHeaders headers, PushEvent pushEvent)
    {
        _logger.LogInformation("Processing push webhook for {Repository}, ref: {Ref}", 
            pushEvent.Repository.FullName, pushEvent.Ref);

        try
        {
            var payload = ConvertToInternalPayload(pushEvent, "push");
            var result = await _workflowOrchestrator.ProcessWebhookEventAsync(payload);
            
            await _auditService.LogWebhookEventAsync(payload, result.Success ? "SUCCESS" : "FAILED");
            
            _logger.LogInformation("Push webhook processed: {Success}, Actions: {ActionsCount}", 
                result.Success, result.Actions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing push webhook");
            await _auditService.LogEventAsync("GitHub_Webhook_Processing", 
                action: "PUSH_ERROR", 
                result: "FAILED",
                details: new { Error = ex.Message, Repository = pushEvent.Repository.FullName });
        }
    }

    protected override async Task ProcessPingWebhookAsync(WebhookHeaders headers, PingEvent pingEvent)
    {
        _logger.LogInformation("Processing ping webhook from {Repository}", 
            pingEvent.Repository?.FullName ?? "unknown");

        try
        {
            var payload = ConvertToInternalPayload(pingEvent, "ping");
            
            // Ping events don't need full workflow processing but should be logged
            await _auditService.LogWebhookEventAsync(payload, "SUCCESS");
            
            _logger.LogInformation("Ping webhook processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ping webhook");
            await _auditService.LogEventAsync("GitHub_Webhook_Processing", 
                action: "PING_ERROR", 
                result: "FAILED",
                details: new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Convert Octokit webhook events to our internal GitHubWebhookPayload format
    /// This maintains compatibility with existing workflow orchestrator
    /// </summary>
    private GitHubWebhookPayload ConvertToInternalPayload(object webhookEvent, string eventType)
    {
        var payload = new GitHubWebhookPayload
        {
            Action = GetActionFromEvent(webhookEvent),
            Repository = GetRepositoryFromEvent(webhookEvent),
            Installation = GetInstallationFromEvent(webhookEvent),
            Sender = GetSenderFromEvent(webhookEvent)
        };

        // Set event-specific properties
        switch (eventType)
        {
            case "pull_request" when webhookEvent is PullRequestEvent prEvent:
                payload.PullRequest = ConvertPullRequest(prEvent.PullRequest);
                break;
            case "issues" when webhookEvent is IssuesEvent issueEvent:
                payload.Issue = ConvertIssue(issueEvent.Issue);
                break;
            case "workflow_run" when webhookEvent is WorkflowRunEvent wrEvent:
                payload.WorkflowRun = ConvertWorkflowRun(wrEvent.WorkflowRun);
                break;
            case "push" when webhookEvent is PushEvent pushEvent:
                payload.Ref = pushEvent.Ref;
                payload.Commits = ConvertCommits(pushEvent.Commits);
                break;
        }

        return payload;
    }

    private string? GetActionFromEvent(object webhookEvent)
    {
        return webhookEvent switch
        {
            PullRequestEvent prEvent => prEvent.Action.ToString().ToLowerInvariant(),
            IssuesEvent issueEvent => issueEvent.Action.ToString().ToLowerInvariant(),
            WorkflowRunEvent wrEvent => wrEvent.Action.ToString().ToLowerInvariant(),
            _ => null
        };
    }

    private GitHubRepository? GetRepositoryFromEvent(object webhookEvent)
    {
        var repo = webhookEvent switch
        {
            PullRequestEvent prEvent => prEvent.Repository,
            IssuesEvent issueEvent => issueEvent.Repository,
            WorkflowRunEvent wrEvent => wrEvent.Repository,
            PushEvent pushEvent => pushEvent.Repository,
            PingEvent pingEvent => pingEvent.Repository,
            _ => null
        };

        if (repo == null) return null;

        return new GitHubRepository
        {
            Id = repo.Id,
            Name = repo.Name,
            FullName = repo.FullName,
            Owner = new GitHubUser
            {
                Id = repo.Owner.Id,
                Login = repo.Owner.Login,
                Type = repo.Owner.Type.ToString()
            },
            Private = repo.Private,
            DefaultBranch = repo.DefaultBranch
        };
    }

    private GitHubInstallation? GetInstallationFromEvent(object webhookEvent)
    {
        var installation = webhookEvent switch
        {
            PullRequestEvent prEvent => prEvent.Installation,
            IssuesEvent issueEvent => issueEvent.Installation,
            WorkflowRunEvent wrEvent => wrEvent.Installation,
            PushEvent pushEvent => pushEvent.Installation,
            PingEvent pingEvent => pingEvent.Installation,
            _ => null
        };

        if (installation == null) return null;

        return new GitHubInstallation
        {
            Id = installation.Id,
            Account = new GitHubUser
            {
                Id = installation.Account.Id,
                Login = installation.Account.Login,
                Type = installation.Account.Type.ToString()
            }
        };
    }

    private GitHubUser? GetSenderFromEvent(object webhookEvent)
    {
        var sender = webhookEvent switch
        {
            PullRequestEvent prEvent => prEvent.Sender,
            IssuesEvent issueEvent => issueEvent.Sender,
            WorkflowRunEvent wrEvent => wrEvent.Sender,
            PushEvent pushEvent => pushEvent.Sender,
            PingEvent pingEvent => pingEvent.Sender,
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

    private GitHubPullRequest ConvertPullRequest(Octokit.Webhooks.Models.PullRequest pr)
    {
        return new GitHubPullRequest
        {
            Id = pr.Id,
            Number = pr.Number,
            Title = pr.Title,
            Body = pr.Body,
            State = pr.State.ToString(),
            User = new GitHubUser
            {
                Id = pr.User.Id,
                Login = pr.User.Login,
                Type = pr.User.Type.ToString()
            },
            Head = new GitHubPullRequestHead
            {
                Sha = pr.Head.Sha,
                Ref = pr.Head.Ref,
                Repo = GetRepositoryFromPrBranch(pr.Head.Repo)
            },
            Base = new GitHubPullRequestBase
            {
                Sha = pr.Base.Sha,
                Ref = pr.Base.Ref,
                Repo = GetRepositoryFromPrBranch(pr.Base.Repo)
            }
        };
    }

    private GitHubRepository? GetRepositoryFromPrBranch(Octokit.Webhooks.Models.Repository? repo)
    {
        if (repo == null) return null;
        
        return new GitHubRepository
        {
            Id = repo.Id,
            Name = repo.Name,
            FullName = repo.FullName,
            Owner = new GitHubUser
            {
                Id = repo.Owner.Id,
                Login = repo.Owner.Login,
                Type = repo.Owner.Type.ToString()
            },
            Private = repo.Private,
            DefaultBranch = repo.DefaultBranch
        };
    }

    private GitHubIssue ConvertIssue(Octokit.Webhooks.Models.Issue issue)
    {
        return new GitHubIssue
        {
            Id = issue.Id,
            Number = issue.Number,
            Title = issue.Title,
            Body = issue.Body,
            State = issue.State.ToString(),
            User = new GitHubUser
            {
                Id = issue.User.Id,
                Login = issue.User.Login,
                Type = issue.User.Type.ToString()
            },
            Labels = issue.Labels?.Select(l => l.Name).ToArray() ?? Array.Empty<string>()
        };
    }

    private GitHubWorkflowRun ConvertWorkflowRun(Octokit.Webhooks.Models.WorkflowRun workflowRun)
    {
        return new GitHubWorkflowRun
        {
            Id = workflowRun.Id,
            Name = workflowRun.Name,
            Status = workflowRun.Status?.ToString(),
            Conclusion = workflowRun.Conclusion?.ToString(),
            HeadBranch = workflowRun.HeadBranch,
            HeadSha = workflowRun.HeadSha,
            RunNumber = workflowRun.RunNumber,
            Event = workflowRun.Event,
            WorkflowId = workflowRun.WorkflowId
        };
    }

    private GitHubCommit[]? ConvertCommits(IReadOnlyList<Octokit.Webhooks.Models.Commit>? commits)
    {
        if (commits == null || !commits.Any()) return null;

        return commits.Select(c => new GitHubCommit
        {
            Id = c.Id,
            Message = c.Message,
            Url = c.Url?.ToString(),
            Author = new GitHubCommitAuthor
            {
                Name = c.Author.Name,
                Email = c.Author.Email,
                Username = c.Author.Username
            }
        }).ToArray();
    }
}