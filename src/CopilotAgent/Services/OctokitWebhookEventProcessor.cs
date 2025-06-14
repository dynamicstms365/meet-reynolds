using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.PullRequest;
using Octokit.Webhooks.Events.Issues;
using Octokit.Webhooks.Events.WorkflowRun;
using CopilotAgent.Services;
using Shared.Models;
using Microsoft.Extensions.Primitives;

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
    private readonly ICrossPlatformEventRouter _eventRouter;

    public OctokitWebhookEventProcessor(
        IGitHubWorkflowOrchestrator workflowOrchestrator,
        ISecurityAuditService auditService,
        ILogger<OctokitWebhookEventProcessor> logger,
        ICrossPlatformEventRouter eventRouter)
    {
        _workflowOrchestrator = workflowOrchestrator;
        _auditService = auditService;
        _logger = logger;
        _eventRouter = eventRouter;
    }

    /// <summary>
    /// Log detailed webhook event information for debugging
    /// </summary>
    private void LogWebhookEventDetails(string eventType, string action, WebhookHeaders headers, object webhookEvent)
    {
        var repository = GetRepositoryFromEvent(webhookEvent);
        var sender = GetSenderFromEvent(webhookEvent);
        var installation = GetInstallationFromEvent(webhookEvent);

        // Extract delivery ID and user agent from headers
        var deliveryId = ExtractHeaderValue(headers, "X-GitHub-Delivery");
        var userAgent = ExtractHeaderValue(headers, "User-Agent");

        _logger.LogInformation("WEBHOOK_EVENT_RECEIVED: Type={EventType}, Action={Action}, Repository={Repository}, Sender={Sender}, Installation={InstallationId}, DeliveryId={DeliveryId}, UserAgent={UserAgent}", 
            eventType, 
            action, 
            repository?.FullName ?? "unknown",
            sender?.Login ?? "unknown",
            installation?.Id ?? 0,
            deliveryId,
            userAgent);

        // Log additional event-specific metadata
        LogEventSpecificMetadata(eventType, webhookEvent);
    }

    /// <summary>
    /// Log event-specific metadata for enhanced debugging
    /// </summary>
    private void LogEventSpecificMetadata(string eventType, object webhookEvent)
    {
        switch (eventType)
        {
            case "pull_request" when webhookEvent is PullRequestEvent prEvent:
                _logger.LogInformation("WEBHOOK_PR_METADATA: PR_Number={Number}, State={State}, Draft={Draft}, User={User}, CreatedAt={CreatedAt}, UpdatedAt={UpdatedAt}",
                    prEvent.PullRequest.Number,
                    prEvent.PullRequest.State,
                    prEvent.PullRequest.Draft,
                    prEvent.PullRequest.User.Login,
                    prEvent.PullRequest.CreatedAt,
                    prEvent.PullRequest.UpdatedAt);
                break;
            case "issues" when webhookEvent is IssuesEvent issueEvent:
                _logger.LogInformation("WEBHOOK_ISSUE_METADATA: Issue_Number={Number}, State={State}, User={User}, CreatedAt={CreatedAt}, UpdatedAt={UpdatedAt}",
                    issueEvent.Issue.Number,
                    issueEvent.Issue.State,
                    issueEvent.Issue.User?.Login ?? "unknown",
                    issueEvent.Issue.CreatedAt,
                    issueEvent.Issue.UpdatedAt);
                break;
            case "workflow_run" when webhookEvent is WorkflowRunEvent wrEvent:
                _logger.LogInformation("WEBHOOK_WORKFLOW_METADATA: Workflow_Id={Id}, Name={Name}, Status={Status}, Conclusion={Conclusion}, CreatedAt={CreatedAt}, UpdatedAt={UpdatedAt}",
                    wrEvent.WorkflowRun.Id,
                    wrEvent.WorkflowRun.Name,
                    wrEvent.WorkflowRun.Status,
                    wrEvent.WorkflowRun.Conclusion,
                    wrEvent.WorkflowRun.CreatedAt,
                    wrEvent.WorkflowRun.UpdatedAt);
                break;
        }
    }

    protected override async Task ProcessPullRequestWebhookAsync(
        WebhookHeaders headers, 
        PullRequestEvent pullRequestEvent, 
        PullRequestAction action)
    {
        // Log detailed webhook event information
        LogWebhookEventDetails("pull_request", action.ToString(), headers, pullRequestEvent);

        _logger.LogInformation("Processing pull request webhook: {Action} for PR #{Number} in {Repository}", 
            action, pullRequestEvent.PullRequest.Number, pullRequestEvent.Repository?.FullName ?? "unknown");

        try
        {
            // Convert Octokit event to our internal payload format
            var payload = ConvertToInternalPayload(pullRequestEvent, "pull_request", action.ToString());
            
            // Create platform event for cross-platform routing
            var platformEvent = new PlatformEvent
            {
                EventType = "pull_request",
                SourcePlatform = "GitHub",
                Action = action.ToString().ToLowerInvariant(),
                Repository = pullRequestEvent.Repository?.FullName,
                UserId = pullRequestEvent.PullRequest.User?.Login,
                Content = $"PR #{pullRequestEvent.PullRequest.Number}: {pullRequestEvent.PullRequest.Title}\n{pullRequestEvent.PullRequest.Body}",
                Metadata = new Dictionary<string, object>
                {
                    ["pr_number"] = pullRequestEvent.PullRequest.Number,
                    ["pr_state"] = pullRequestEvent.PullRequest.State.ToString(),
                    ["draft"] = pullRequestEvent.PullRequest.Draft,
                    ["delivery_id"] = ExtractHeaderValue(headers, "X-GitHub-Delivery")
                }
            };

            // Route through cross-platform system
            var routingResult = await _eventRouter.RouteEventAsync(platformEvent);
            
            // Process through existing workflow orchestrator
            var result = await _workflowOrchestrator.ProcessWebhookEventAsync(payload);
            
            await _auditService.LogWebhookEventAsync(payload, result.Success ? "SUCCESS" : "FAILED");
            
            _logger.LogInformation("Pull request webhook processed: {Success}, Actions: {ActionsCount}, Cross-platform routes: {RouteCount}",
                result.Success, result.Actions.Count, routingResult.RouteResults.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pull request webhook for PR #{Number} in {Repository}: {Error}", 
                pullRequestEvent.PullRequest.Number, 
                pullRequestEvent.Repository?.FullName ?? "unknown",
                ex.Message);
            await _auditService.LogEventAsync("GitHub_Webhook_Processing", 
                action: "PULL_REQUEST_ERROR", 
                result: "FAILED",
                details: new { 
                    Error = ex.Message, 
                    Action = action.ToString(),
                    PRNumber = pullRequestEvent.PullRequest.Number,
                    Repository = pullRequestEvent.Repository?.FullName,
                    DeliveryId = ExtractHeaderValue(headers, "X-GitHub-Delivery")
                });
        }
    }

    protected override async Task ProcessIssuesWebhookAsync(
        WebhookHeaders headers,
        IssuesEvent issuesEvent,
        IssuesAction action)
    {
        // Log detailed webhook event information
        LogWebhookEventDetails("issues", action.ToString(), headers, issuesEvent);

        _logger.LogInformation("Processing issues webhook: {Action} for issue #{Number} in {Repository}", 
            action, issuesEvent.Issue.Number, issuesEvent.Repository?.FullName ?? "unknown");

        try
        {
            var payload = ConvertToInternalPayload(issuesEvent, "issues", action.ToString());
            
            // Create platform event for cross-platform routing
            var platformEvent = new PlatformEvent
            {
                EventType = "issues",
                SourcePlatform = "GitHub",
                Action = action.ToString().ToLowerInvariant(),
                Repository = issuesEvent.Repository?.FullName,
                UserId = issuesEvent.Issue.User?.Login,
                Content = $"Issue #{issuesEvent.Issue.Number}: {issuesEvent.Issue.Title}\n{issuesEvent.Issue.Body}",
                Metadata = new Dictionary<string, object>
                {
                    ["issue_number"] = issuesEvent.Issue.Number,
                    ["issue_state"] = issuesEvent.Issue.State?.ToString() ?? "unknown",
                    ["delivery_id"] = ExtractHeaderValue(headers, "X-GitHub-Delivery")
                }
            };

            // Route through cross-platform system
            var routingResult = await _eventRouter.RouteEventAsync(platformEvent);
            
            var result = await _workflowOrchestrator.ProcessWebhookEventAsync(payload);
            
            await _auditService.LogWebhookEventAsync(payload, result.Success ? "SUCCESS" : "FAILED");
            
            _logger.LogInformation("Issues webhook processed: {Success}, Actions: {ActionsCount}, Cross-platform routes: {RouteCount}",
                result.Success, result.Actions.Count, routingResult.RouteResults.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing issues webhook for issue #{Number} in {Repository}: {Error}", 
                issuesEvent.Issue.Number, 
                issuesEvent.Repository?.FullName ?? "unknown",
                ex.Message);
            await _auditService.LogEventAsync("GitHub_Webhook_Processing", 
                action: "ISSUES_ERROR", 
                result: "FAILED",
                details: new { 
                    Error = ex.Message, 
                    Action = action.ToString(),
                    IssueNumber = issuesEvent.Issue.Number,
                    Repository = issuesEvent.Repository?.FullName,
                    DeliveryId = ExtractHeaderValue(headers, "X-GitHub-Delivery")
                });
        }
    }

    protected override async Task ProcessWorkflowRunWebhookAsync(
        WebhookHeaders headers,
        WorkflowRunEvent workflowRunEvent,
        WorkflowRunAction action)
    {
        // Log detailed webhook event information
        LogWebhookEventDetails("workflow_run", action.ToString(), headers, workflowRunEvent);

        _logger.LogInformation("Processing workflow run webhook: {Action} for workflow '{Name}' in {Repository}", 
            action, workflowRunEvent.WorkflowRun.Name, workflowRunEvent.Repository?.FullName ?? "unknown");

        try
        {
            var payload = ConvertToInternalPayload(workflowRunEvent, "workflow_run", action.ToString());
            
            // Create platform event for cross-platform routing
            var platformEvent = new PlatformEvent
            {
                EventType = "workflow_run",
                SourcePlatform = "GitHub",
                Action = action.ToString().ToLowerInvariant(),
                Repository = workflowRunEvent.Repository?.FullName,
                UserId = workflowRunEvent.WorkflowRun.Actor?.Login,
                Content = $"Workflow '{workflowRunEvent.WorkflowRun.Name}': {workflowRunEvent.WorkflowRun.Status} -> {workflowRunEvent.WorkflowRun.Conclusion}",
                Metadata = new Dictionary<string, object>
                {
                    ["workflow_id"] = workflowRunEvent.WorkflowRun.Id,
                    ["workflow_name"] = workflowRunEvent.WorkflowRun.Name ?? "unknown",
                    ["status"] = workflowRunEvent.WorkflowRun.Status?.ToString() ?? "unknown",
                    ["conclusion"] = workflowRunEvent.WorkflowRun.Conclusion?.ToString() ?? "unknown",
                    ["delivery_id"] = ExtractHeaderValue(headers, "X-GitHub-Delivery"),
                    ["trigger_deployment"] = action.ToString().ToLowerInvariant() == "completed" &&
                                           workflowRunEvent.WorkflowRun.Conclusion?.ToString()?.ToLowerInvariant() == "success"
                }
            };

            // Route through cross-platform system
            var routingResult = await _eventRouter.RouteEventAsync(platformEvent);
            
            var result = await _workflowOrchestrator.ProcessWebhookEventAsync(payload);
            
            await _auditService.LogWebhookEventAsync(payload, result.Success ? "SUCCESS" : "FAILED");
            
            _logger.LogInformation("Workflow run webhook processed: {Success}, Actions: {ActionsCount}, Cross-platform routes: {RouteCount}",
                result.Success, result.Actions.Count, routingResult.RouteResults.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing workflow run webhook for '{Name}' in {Repository}: {Error}", 
                workflowRunEvent.WorkflowRun.Name, 
                workflowRunEvent.Repository?.FullName ?? "unknown",
                ex.Message);
            await _auditService.LogEventAsync("GitHub_Webhook_Processing", 
                action: "WORKFLOW_RUN_ERROR", 
                result: "FAILED",
                details: new { 
                    Error = ex.Message, 
                    Action = action.ToString(),
                    WorkflowName = workflowRunEvent.WorkflowRun.Name,
                    WorkflowId = workflowRunEvent.WorkflowRun.Id,
                    Repository = workflowRunEvent.Repository?.FullName,
                    DeliveryId = ExtractHeaderValue(headers, "X-GitHub-Delivery")
                });
        }
    }

    /// <summary>
    /// Override to handle unknown/unprocessed webhook events with detailed logging
    /// This helps identify events that are being received but not handled
    /// </summary>
    public override async Task ProcessWebhookAsync(WebhookHeaders headers, WebhookEvent payload)
    {
        var eventType = payload.GetType().Name.Replace("Event", "").ToLowerInvariant();
        
        _logger.LogWarning("WEBHOOK_EVENT_UNHANDLED: Type={EventType}, DeliveryId={DeliveryId}, UserAgent={UserAgent}. This event type is not explicitly handled but was received.",
            eventType,
            ExtractHeaderValue(headers, "X-GitHub-Delivery"),
            ExtractHeaderValue(headers, "User-Agent"));

        // Log basic event information
        var repositoryInfo = "unknown";
        var senderInfo = "unknown";
        var installationInfo = "unknown";

        try
        {
            // Try to extract basic information using reflection if available common properties exist
            var repositoryProperty = payload.GetType().GetProperty("Repository");
            if (repositoryProperty?.GetValue(payload) is object repo)
            {
                var fullNameProperty = repo.GetType().GetProperty("FullName");
                repositoryInfo = fullNameProperty?.GetValue(repo)?.ToString() ?? "unknown";
            }

            var senderProperty = payload.GetType().GetProperty("Sender");
            if (senderProperty?.GetValue(payload) is object sender)
            {
                var loginProperty = sender.GetType().GetProperty("Login");
                senderInfo = loginProperty?.GetValue(sender)?.ToString() ?? "unknown";
            }

            var installationProperty = payload.GetType().GetProperty("Installation");
            if (installationProperty?.GetValue(payload) is object installation)
            {
                var idProperty = installation.GetType().GetProperty("Id");
                installationInfo = idProperty?.GetValue(installation)?.ToString() ?? "unknown";
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Could not extract basic webhook event information via reflection: {Error}", ex.Message);
        }

        _logger.LogInformation("WEBHOOK_UNHANDLED_METADATA: Repository={Repository}, Sender={Sender}, Installation={Installation}",
            repositoryInfo,
            senderInfo,
            installationInfo);

        await _auditService.LogEventAsync("GitHub_Webhook_Unhandled", 
            action: eventType.ToUpperInvariant(), 
            result: "UNHANDLED",
            details: new { 
                EventType = eventType,
                Repository = repositoryInfo,
                Sender = senderInfo,
                Installation = installationInfo,
                DeliveryId = ExtractHeaderValue(headers, "X-GitHub-Delivery"),
                UserAgent = ExtractHeaderValue(headers, "User-Agent")
            });

        // Call base implementation to ensure proper webhook acknowledgment
        await base.ProcessWebhookAsync(headers, payload);
    }

    /// <summary>
    /// Helper method to extract header values from WebhookHeaders
    /// </summary>
    private string ExtractHeaderValue(WebhookHeaders headers, string headerName)
    {
        try
        {
            // WebhookHeaders likely has an indexer or GetValueOrDefault method
            // Use reflection as a fallback to access the headers
            var headersProperty = headers.GetType().GetProperty("Headers") ?? 
                                 headers.GetType().GetProperty("Values") ??
                                 headers.GetType().GetProperty("Items");
            
            if (headersProperty != null)
            {
                var headerDictionary = headersProperty.GetValue(headers);
                if (headerDictionary is IDictionary<string, Microsoft.Extensions.Primitives.StringValues> dict)
                {
                    if (dict.TryGetValue(headerName, out var values))
                    {
                        return values.FirstOrDefault() ?? "unknown";
                    }
                }
            }
            
            // Try direct indexer access
            var indexer = headers.GetType().GetMethod("get_Item", new[] { typeof(string) });
            if (indexer != null)
            {
                var result = indexer.Invoke(headers, new object[] { headerName });
                if (result is Microsoft.Extensions.Primitives.StringValues stringValues)
                {
                    return stringValues.FirstOrDefault() ?? "unknown";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Could not extract header value for {HeaderName}: {Error}", headerName, ex.Message);
        }
        return "unknown";
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