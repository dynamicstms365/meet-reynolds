using System.Text.Json;
using Shared.Models;

namespace CopilotAgent.Services;

public interface IGitHubWorkflowOrchestrator
{
    Task<WorkflowResult> ProcessWebhookEventAsync(GitHubWebhookPayload payload);
    Task<WorkflowResult> HandleSemanticSearchRequestAsync(SemanticSearchRequest request);
    Task<WorkflowResult> CreateDiscussionWorkflowAsync(CreateDiscussionRequest request);
    Task<WorkflowResult> CreateIssueWorkflowAsync(CreateIssueRequest request);
    Task<WorkflowResult> UpdateContentWorkflowAsync(UpdateContentRequest request);
    Task<WorkflowResult> AddCommentWorkflowAsync(AddCommentRequest request);
    Task<WorkflowResult> ExecutePromptBasedActionAsync(PromptBasedActionRequest request);
}

public class GitHubWorkflowOrchestrator : IGitHubWorkflowOrchestrator
{
    private readonly IGitHubDiscussionsService _discussionsService;
    private readonly IGitHubIssuesService _issuesService;
    private readonly IGitHubSemanticSearchService _semanticSearchService;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<GitHubWorkflowOrchestrator> _logger;
    private readonly IConfiguration _configuration;

    public GitHubWorkflowOrchestrator(
        IGitHubDiscussionsService discussionsService,
        IGitHubIssuesService issuesService,
        IGitHubSemanticSearchService semanticSearchService,
        ISecurityAuditService auditService,
        ILogger<GitHubWorkflowOrchestrator> logger,
        IConfiguration configuration)
    {
        _discussionsService = discussionsService;
        _issuesService = issuesService;
        _semanticSearchService = semanticSearchService;
        _auditService = auditService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<WorkflowResult> ProcessWebhookEventAsync(GitHubWebhookPayload payload)
    {
        try
        {
            _logger.LogInformation("Processing webhook event: {Event} - {Action} for repository: {Repository}",
                payload.Event, payload.Action, payload.Repository?.FullName);

            var result = payload.Event?.ToLowerInvariant() switch
            {
                "discussion" => await HandleDiscussionEventAsync(payload),
                "discussion_comment" => await HandleDiscussionCommentEventAsync(payload),
                "issues" => await HandleIssueEventAsync(payload),
                "issue_comment" => await HandleIssueCommentEventAsync(payload),
                "pull_request" => await HandlePullRequestEventAsync(payload),
                "repository_dispatch" => await HandleRepositoryDispatchEventAsync(payload),
                _ => await HandleGenericEventAsync(payload)
            };

            await _auditService.LogEventAsync(
                "GitHub_Workflow_Processed",
                repository: payload.Repository?.FullName,
                action: $"{payload.Event}_{payload.Action}",
                result: result.Success ? "SUCCESS" : "FAILED",
                details: new { 
                    WorkflowType = result.WorkflowType,
                    Actions = result.Actions.Count,
                    Error = result.Error 
                });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event: {Event}", payload.Event);
            return WorkflowResult.Failed($"Error processing webhook: {ex.Message}");
        }
    }

    public async Task<WorkflowResult> HandleSemanticSearchRequestAsync(SemanticSearchRequest request)
    {
        try
        {
            _logger.LogInformation("Handling semantic search request: {Query} with scope: {Scope}", 
                request.Query, request.Scope);

            var searchResult = await _semanticSearchService.SearchAcrossRepositoriesAsync(
                request.Query, 
                request.Scope, 
                request.MaxResults);

            var actions = new List<WorkflowAction>
            {
                new WorkflowAction
                {
                    Type = "semantic_search_completed",
                    Description = $"Searched across repositories with query: {request.Query}",
                    Data = searchResult,
                    Timestamp = DateTime.UtcNow
                }
            };

            // If auto-actions are enabled, process the search results
            if (request.AutoProcessResults)
            {
                var autoActions = await ProcessSearchResultsForAutoActionsAsync(searchResult, request);
                actions.AddRange(autoActions);
            }

            await _auditService.LogEventAsync(
                "Semantic_Search_Completed",
                action: "SearchAcrossRepositories",
                result: "SUCCESS",
                details: new { 
                    Query = request.Query,
                    Scope = request.Scope,
                    ResultCount = searchResult.Items.Count(),
                    AutoActionsTriggered = request.AutoProcessResults
                });

            return new WorkflowResult
            {
                Success = true,
                WorkflowType = "semantic_search",
                Actions = actions,
                Data = searchResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling semantic search request: {Query}", request.Query);
            return WorkflowResult.Failed($"Semantic search failed: {ex.Message}");
        }
    }

    public async Task<WorkflowResult> CreateDiscussionWorkflowAsync(CreateDiscussionRequest request)
    {
        try
        {
            _logger.LogInformation("Creating discussion: {Title} in repository: {Repository}", 
                request.Title, request.Repository);

            // Check for similar existing discussions first
            var similarDiscussions = await _discussionsService.SearchDiscussionsAsync(
                request.Title, request.Repository);

            var actions = new List<WorkflowAction>();

            // If similar discussions exist, decide whether to create new or comment on existing
            if (similarDiscussions.Any() && request.CheckForDuplicates)
            {
                var mostSimilar = similarDiscussions.First();
                var similarity = await _semanticSearchService.CalculateSemanticSimilarityAsync(
                    request.Title + " " + request.Body, 
                    mostSimilar.Title + " " + mostSimilar.Body);

                if (similarity > 0.7) // High similarity threshold
                {
                    _logger.LogInformation("Similar discussion found, adding comment instead of creating new discussion");
                    
                    var comment = await _discussionsService.AddDiscussionCommentAsync(
                        request.Repository, 
                        mostSimilar.NodeId, 
                        $"**Related Content**: {request.Body}\n\n*This comment was automatically added due to similar content.*");

                    actions.Add(new WorkflowAction
                    {
                        Type = "discussion_comment_added",
                        Description = $"Added comment to similar discussion #{mostSimilar.Number}",
                        Data = comment,
                        Timestamp = DateTime.UtcNow
                    });

                    return new WorkflowResult
                    {
                        Success = true,
                        WorkflowType = "discussion_comment_instead_of_create",
                        Actions = actions,
                        Data = comment
                    };
                }
            }

            // Create new discussion
            var discussion = await _discussionsService.CreateDiscussionAsync(
                request.Repository, 
                request.Title, 
                request.Body, 
                request.Category);

            actions.Add(new WorkflowAction
            {
                Type = "discussion_created",
                Description = $"Created discussion #{discussion.Number}: {discussion.Title}",
                Data = discussion,
                Timestamp = DateTime.UtcNow
            });

            // Add initial comments if provided
            foreach (var comment in request.InitialComments)
            {
                var addedComment = await _discussionsService.AddDiscussionCommentAsync(
                    request.Repository, 
                    discussion.NodeId, 
                    comment);

                actions.Add(new WorkflowAction
                {
                    Type = "discussion_comment_added",
                    Description = "Added initial comment to discussion",
                    Data = addedComment,
                    Timestamp = DateTime.UtcNow
                });
            }

            await _auditService.LogEventAsync(
                "Discussion_Workflow_Completed",
                repository: request.Repository,
                action: "CreateDiscussion",
                result: "SUCCESS",
                details: new { 
                    DiscussionNumber = discussion.Number,
                    Title = discussion.Title,
                    InitialComments = request.InitialComments.Count
                });

            return new WorkflowResult
            {
                Success = true,
                WorkflowType = "create_discussion",
                Actions = actions,
                Data = discussion
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating discussion workflow for: {Repository}", request.Repository);
            return WorkflowResult.Failed($"Failed to create discussion: {ex.Message}");
        }
    }

    public async Task<WorkflowResult> CreateIssueWorkflowAsync(CreateIssueRequest request)
    {
        try
        {
            _logger.LogInformation("Creating issue: {Title} in repository: {Repository}", 
                request.Title, request.Repository);

            var actions = new List<WorkflowAction>();

            // Check for similar existing issues first
            if (request.CheckForDuplicates)
            {
                var similarIssues = await _issuesService.SearchIssuesAsync(request.Title, request.Repository);
                
                if (similarIssues.Any())
                {
                    var mostSimilar = similarIssues.First();
                    var similarity = await _semanticSearchService.CalculateSemanticSimilarityAsync(
                        request.Title + " " + request.Body, 
                        mostSimilar.Title + " " + mostSimilar.Body);

                    if (similarity > 0.8) // Very high similarity threshold for issues
                    {
                        _logger.LogInformation("Similar issue found, adding comment instead of creating new issue");
                        
                        var comment = await _issuesService.AddIssueCommentAsync(
                            request.Repository, 
                            mostSimilar.Number, 
                            $"**Related Report**: {request.Body}\n\n*This comment was automatically added due to similar content.*");

                        actions.Add(new WorkflowAction
                        {
                            Type = "issue_comment_added",
                            Description = $"Added comment to similar issue #{mostSimilar.Number}",
                            Data = comment,
                            Timestamp = DateTime.UtcNow
                        });

                        return new WorkflowResult
                        {
                            Success = true,
                            WorkflowType = "issue_comment_instead_of_create",
                            Actions = actions,
                            Data = comment
                        };
                    }
                }
            }

            // Create new issue
            var issue = await _issuesService.CreateIssueAsync(
                request.Repository, 
                request.Title, 
                request.Body, 
                request.Labels, 
                request.Assignees);

            actions.Add(new WorkflowAction
            {
                Type = "issue_created",
                Description = $"Created issue #{issue.Number}: {issue.Title}",
                Data = issue,
                Timestamp = DateTime.UtcNow
            });

            // Add initial comments if provided
            foreach (var comment in request.InitialComments)
            {
                var addedComment = await _issuesService.AddIssueCommentAsync(
                    request.Repository, 
                    issue.Number, 
                    comment);

                actions.Add(new WorkflowAction
                {
                    Type = "issue_comment_added",
                    Description = "Added initial comment to issue",
                    Data = addedComment,
                    Timestamp = DateTime.UtcNow
                });
            }

            await _auditService.LogEventAsync(
                "Issue_Workflow_Completed",
                repository: request.Repository,
                action: "CreateIssue",
                result: "SUCCESS",
                details: new { 
                    IssueNumber = issue.Number,
                    Title = issue.Title,
                    Labels = request.Labels,
                    InitialComments = request.InitialComments.Count
                });

            return new WorkflowResult
            {
                Success = true,
                WorkflowType = "create_issue",
                Actions = actions,
                Data = issue
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating issue workflow for: {Repository}", request.Repository);
            return WorkflowResult.Failed($"Failed to create issue: {ex.Message}");
        }
    }

    public async Task<WorkflowResult> UpdateContentWorkflowAsync(UpdateContentRequest request)
    {
        try
        {
            var actions = new List<WorkflowAction>();

            if (request.ContentType.ToLowerInvariant() == "discussion")
            {
                var discussion = await _discussionsService.UpdateDiscussionAsync(
                    request.Repository, 
                    request.NodeId, 
                    request.Title, 
                    request.Body);

                actions.Add(new WorkflowAction
                {
                    Type = "discussion_updated",
                    Description = $"Updated discussion #{discussion.Number}",
                    Data = discussion,
                    Timestamp = DateTime.UtcNow
                });
            }
            else if (request.ContentType.ToLowerInvariant() == "issue")
            {
                var issue = await _issuesService.UpdateIssueAsync(
                    request.Repository, 
                    request.Number!.Value, 
                    request.Title, 
                    request.Body, 
                    request.State, 
                    request.Labels);

                actions.Add(new WorkflowAction
                {
                    Type = "issue_updated",
                    Description = $"Updated issue #{issue.Number}",
                    Data = issue,
                    Timestamp = DateTime.UtcNow
                });
            }

            await _auditService.LogEventAsync(
                "Content_Update_Workflow_Completed",
                repository: request.Repository,
                action: $"Update{request.ContentType}",
                result: "SUCCESS",
                details: new { 
                    ContentType = request.ContentType,
                    NodeId = request.NodeId,
                    Number = request.Number
                });

            return new WorkflowResult
            {
                Success = true,
                WorkflowType = "update_content",
                Actions = actions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating content workflow");
            return WorkflowResult.Failed($"Failed to update content: {ex.Message}");
        }
    }

    public async Task<WorkflowResult> AddCommentWorkflowAsync(AddCommentRequest request)
    {
        try
        {
            var actions = new List<WorkflowAction>();
            GitHubComment comment;

            if (request.ContentType.ToLowerInvariant() == "discussion")
            {
                comment = await _discussionsService.AddDiscussionCommentAsync(
                    request.Repository, 
                    request.NodeId, 
                    request.Body);

                actions.Add(new WorkflowAction
                {
                    Type = "discussion_comment_added",
                    Description = $"Added comment to discussion",
                    Data = comment,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                comment = await _issuesService.AddIssueCommentAsync(
                    request.Repository, 
                    request.Number!.Value, 
                    request.Body);

                actions.Add(new WorkflowAction
                {
                    Type = "issue_comment_added",
                    Description = $"Added comment to issue #{request.Number}",
                    Data = comment,
                    Timestamp = DateTime.UtcNow
                });
            }

            await _auditService.LogEventAsync(
                "Comment_Workflow_Completed",
                repository: request.Repository,
                action: $"AddComment{request.ContentType}",
                result: "SUCCESS",
                details: new { 
                    ContentType = request.ContentType,
                    NodeId = request.NodeId,
                    Number = request.Number,
                    OnBehalfOf = request.OnBehalfOf
                });

            return new WorkflowResult
            {
                Success = true,
                WorkflowType = "add_comment",
                Actions = actions,
                Data = comment
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment workflow");
            return WorkflowResult.Failed($"Failed to add comment: {ex.Message}");
        }
    }

    public async Task<WorkflowResult> ExecutePromptBasedActionAsync(PromptBasedActionRequest request)
    {
        try
        {
            _logger.LogInformation("Executing prompt-based action: {Prompt}", request.Prompt);

            var actions = new List<WorkflowAction>();

            // Analyze the prompt to determine action
            var actionType = AnalyzePromptForActionType(request.Prompt);
            
            switch (actionType)
            {
                case "search":
                    var searchQuery = ExtractSearchQueryFromPrompt(request.Prompt);
                    var searchRequest = new SemanticSearchRequest
                    {
                        Query = searchQuery,
                        Scope = request.Scope ?? SearchScope.All,
                        MaxResults = request.MaxResults ?? 20,
                        AutoProcessResults = true
                    };
                    return await HandleSemanticSearchRequestAsync(searchRequest);

                case "create_discussion":
                    var discussionData = ExtractDiscussionDataFromPrompt(request.Prompt);
                    var createDiscussionRequest = new CreateDiscussionRequest
                    {
                        Repository = request.Repository ?? "dynamicstms365/discussions",
                        Title = discussionData.Title,
                        Body = discussionData.Body,
                        Category = discussionData.Category,
                        CheckForDuplicates = true
                    };
                    return await CreateDiscussionWorkflowAsync(createDiscussionRequest);

                case "create_issue":
                    var issueData = ExtractIssueDataFromPrompt(request.Prompt);
                    var createIssueRequest = new CreateIssueRequest
                    {
                        Repository = request.Repository ?? DetermineRepositoryFromPrompt(request.Prompt),
                        Title = issueData.Title,
                        Body = issueData.Body,
                        Labels = issueData.Labels,
                        CheckForDuplicates = true
                    };
                    return await CreateIssueWorkflowAsync(createIssueRequest);

                case "update_content":
                case "add_comment":
                default:
                    actions.Add(new WorkflowAction
                    {
                        Type = "prompt_analyzed",
                        Description = $"Analyzed prompt and determined action type: {actionType}",
                        Data = new { Prompt = request.Prompt, ActionType = actionType },
                        Timestamp = DateTime.UtcNow
                    });
                    break;
            }

            return new WorkflowResult
            {
                Success = true,
                WorkflowType = "prompt_based_action",
                Actions = actions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing prompt-based action");
            return WorkflowResult.Failed($"Failed to execute prompt-based action: {ex.Message}");
        }
    }

    private Task<WorkflowResult> HandleDiscussionEventAsync(GitHubWebhookPayload payload)
    {
        var actions = new List<WorkflowAction>();
        
        switch (payload.Action?.ToLowerInvariant())
        {
            case "created":
                actions.Add(new WorkflowAction
                {
                    Type = "discussion_event_processed",
                    Description = $"New discussion created: {payload.Discussion?.Title}",
                    Data = payload.Discussion,
                    Timestamp = DateTime.UtcNow
                });
                break;
                
            case "edited":
                actions.Add(new WorkflowAction
                {
                    Type = "discussion_event_processed",
                    Description = $"Discussion edited: {payload.Discussion?.Title}",
                    Data = payload.Discussion,
                    Timestamp = DateTime.UtcNow
                });
                break;
        }

        return Task.FromResult(new WorkflowResult
        {
            Success = true,
            WorkflowType = "discussion_webhook",
            Actions = actions
        });
    }

    private Task<WorkflowResult> HandleDiscussionCommentEventAsync(GitHubWebhookPayload payload)
    {
        var actions = new List<WorkflowAction>
        {
            new WorkflowAction
            {
                Type = "discussion_comment_event_processed",
                Description = $"Discussion comment {payload.Action}: {payload.Comment?.Body?.Substring(0, Math.Min(50, payload.Comment?.Body?.Length ?? 0))}...",
                Data = payload.Comment,
                Timestamp = DateTime.UtcNow
            }
        };

        return Task.FromResult(new WorkflowResult
        {
            Success = true,
            WorkflowType = "discussion_comment_webhook",
            Actions = actions
        });
    }

    private Task<WorkflowResult> HandleIssueEventAsync(GitHubWebhookPayload payload)
    {
        var actions = new List<WorkflowAction>();
        
        switch (payload.Action?.ToLowerInvariant())
        {
            case "opened":
                actions.Add(new WorkflowAction
                {
                    Type = "issue_event_processed",
                    Description = $"New issue opened: {payload.Issue?.Title}",
                    Data = payload.Issue,
                    Timestamp = DateTime.UtcNow
                });
                break;
                
            case "edited":
                actions.Add(new WorkflowAction
                {
                    Type = "issue_event_processed",
                    Description = $"Issue edited: {payload.Issue?.Title}",
                    Data = payload.Issue,
                    Timestamp = DateTime.UtcNow
                });
                break;
                
            case "closed":
                actions.Add(new WorkflowAction
                {
                    Type = "issue_event_processed",
                    Description = $"Issue closed: {payload.Issue?.Title}",
                    Data = payload.Issue,
                    Timestamp = DateTime.UtcNow
                });
                break;
        }

        return Task.FromResult(new WorkflowResult
        {
            Success = true,
            WorkflowType = "issue_webhook",
            Actions = actions
        });
    }

    private Task<WorkflowResult> HandleIssueCommentEventAsync(GitHubWebhookPayload payload)
    {
        var actions = new List<WorkflowAction>
        {
            new WorkflowAction
            {
                Type = "issue_comment_event_processed",
                Description = $"Issue comment {payload.Action}: {payload.Comment?.Body?.Substring(0, Math.Min(50, payload.Comment?.Body?.Length ?? 0))}...",
                Data = payload.Comment,
                Timestamp = DateTime.UtcNow
            }
        };

        return Task.FromResult(new WorkflowResult
        {
            Success = true,
            WorkflowType = "issue_comment_webhook",
            Actions = actions
        });
    }

    private Task<WorkflowResult> HandlePullRequestEventAsync(GitHubWebhookPayload payload)
    {
        var actions = new List<WorkflowAction>
        {
            new WorkflowAction
            {
                Type = "pull_request_event_processed",
                Description = $"Pull request {payload.Action}: {payload.PullRequest?.Title}",
                Data = payload.PullRequest,
                Timestamp = DateTime.UtcNow
            }
        };

        return Task.FromResult(new WorkflowResult
        {
            Success = true,
            WorkflowType = "pull_request_webhook",
            Actions = actions
        });
    }

    private Task<WorkflowResult> HandleRepositoryDispatchEventAsync(GitHubWebhookPayload payload)
    {
        var actions = new List<WorkflowAction>
        {
            new WorkflowAction
            {
                Type = "repository_dispatch_processed",
                Description = $"Repository dispatch event received: {payload.Action}",
                Data = payload.ClientPayload,
                Timestamp = DateTime.UtcNow
            }
        };

        return Task.FromResult(new WorkflowResult
        {
            Success = true,
            WorkflowType = "repository_dispatch_webhook",
            Actions = actions
        });
    }

    private Task<WorkflowResult> HandleGenericEventAsync(GitHubWebhookPayload payload)
    {
        var actions = new List<WorkflowAction>
        {
            new WorkflowAction
            {
                Type = "generic_event_processed",
                Description = $"Processed webhook event: {payload.Event} - {payload.Action}",
                Data = new { Event = payload.Event, Action = payload.Action },
                Timestamp = DateTime.UtcNow
            }
        };

        return Task.FromResult(new WorkflowResult
        {
            Success = true,
            WorkflowType = "generic_webhook",
            Actions = actions
        });
    }

    private Task<List<WorkflowAction>> ProcessSearchResultsForAutoActionsAsync(SemanticSearchResult searchResult, SemanticSearchRequest request)
    {
        var actions = new List<WorkflowAction>();

        // Example auto-actions based on search results
        if (request.AutoCreateDiscussion && searchResult.Items.Count() < 3)
        {
            // If very few results, might indicate a gap in knowledge - create discussion
            actions.Add(new WorkflowAction
            {
                Type = "auto_action_suggested",
                Description = "Suggested creating discussion due to limited search results",
                Data = new {
                    Suggestion = "create_discussion",
                    Reason = "Limited results suggest knowledge gap",
                    Query = request.Query
                },
                Timestamp = DateTime.UtcNow
            });
        }

        return Task.FromResult(actions);
    }

    private static string AnalyzePromptForActionType(string prompt)
    {
        var lowerPrompt = prompt.ToLowerInvariant();
        
        if (lowerPrompt.Contains("search") || lowerPrompt.Contains("find") || lowerPrompt.Contains("look for"))
            return "search";
        
        if (lowerPrompt.Contains("create discussion") || lowerPrompt.Contains("start discussion"))
            return "create_discussion";
        
        if (lowerPrompt.Contains("create issue") || lowerPrompt.Contains("report issue") || lowerPrompt.Contains("file bug"))
            return "create_issue";
        
        if (lowerPrompt.Contains("update") || lowerPrompt.Contains("modify") || lowerPrompt.Contains("edit"))
            return "update_content";
        
        if (lowerPrompt.Contains("comment") || lowerPrompt.Contains("reply"))
            return "add_comment";
        
        return "analyze";
    }

    private static string ExtractSearchQueryFromPrompt(string prompt)
    {
        // Simple extraction - can be enhanced with NLP
        var patterns = new[]
        {
            @"search for (.+)",
            @"find (.+)",
            @"look for (.+)",
            @"about (.+)"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(prompt, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        return prompt; // Return the whole prompt if no pattern matches
    }

    private static (string Title, string Body, string Category) ExtractDiscussionDataFromPrompt(string prompt)
    {
        // Simple extraction logic - can be enhanced
        var lines = prompt.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var title = lines.FirstOrDefault() ?? "Discussion from automated prompt";
        var body = string.Join("\n", lines.Skip(1));
        
        return (title, body, "General");
    }

    private static (string Title, string Body, string[] Labels) ExtractIssueDataFromPrompt(string prompt)
    {
        // Simple extraction logic - can be enhanced
        var lines = prompt.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var title = lines.FirstOrDefault() ?? "Issue from automated prompt";
        var body = string.Join("\n", lines.Skip(1));
        var labels = new[] { "automated", "prompt-generated" };
        
        return (title, body, labels);
    }

    private static string DetermineRepositoryFromPrompt(string prompt)
    {
        // Default repository for issues if not specified
        return "dynamicstms365/github-copilot-bot";
    }
}

// Supporting classes for workflow requests and results

public class WorkflowResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public List<WorkflowAction> Actions { get; set; } = new();
    public object? Data { get; set; }

    public static WorkflowResult Failed(string error) => new()
    {
        Success = false,
        Error = error,
        WorkflowType = "failed"
    };
}

public class WorkflowAction
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object? Data { get; set; }
    public DateTime Timestamp { get; set; }
}

public class SemanticSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public SearchScope Scope { get; set; } = SearchScope.All;
    public int MaxResults { get; set; } = 20;
    public bool AutoProcessResults { get; set; } = false;
    public bool AutoCreateDiscussion { get; set; } = false;
    public string? RepositoryFilter { get; set; }
}

public class CreateDiscussionRequest
{
    public string Repository { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public bool CheckForDuplicates { get; set; } = true;
    public List<string> InitialComments { get; set; } = new();
    public string? OnBehalfOf { get; set; }
}

public class CreateIssueRequest
{
    public string Repository { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string[]? Labels { get; set; }
    public string[]? Assignees { get; set; }
    public bool CheckForDuplicates { get; set; } = true;
    public List<string> InitialComments { get; set; } = new();
    public string? OnBehalfOf { get; set; }
}

public class UpdateContentRequest
{
    public string Repository { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty; // "discussion" or "issue"
    public string NodeId { get; set; } = string.Empty;
    public int? Number { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? State { get; set; }
    public string[]? Labels { get; set; }
    public string? OnBehalfOf { get; set; }
}

public class AddCommentRequest
{
    public string Repository { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty; // "discussion" or "issue"
    public string NodeId { get; set; } = string.Empty;
    public int? Number { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? OnBehalfOf { get; set; }
}

public class PromptBasedActionRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string? Repository { get; set; }
    public SearchScope? Scope { get; set; }
    public int? MaxResults { get; set; }
    public string? OnBehalfOf { get; set; }
}