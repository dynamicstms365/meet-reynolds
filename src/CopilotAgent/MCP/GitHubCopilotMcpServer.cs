using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using CopilotAgent.Services;
using Shared.Models;

namespace CopilotAgent.MCP;

[ApiController]
[Route("mcp")]
public class GitHubCopilotMcpServer : ControllerBase
{
    private readonly IGitHubWorkflowOrchestrator _workflowOrchestrator;
    private readonly IGitHubSemanticSearchService _semanticSearchService;
    private readonly IGitHubDiscussionsService _discussionsService;
    private readonly IGitHubIssuesService _issuesService;
    private readonly IGitHubAppAuthService _authService;
    private readonly ILogger<GitHubCopilotMcpServer> _logger;

    public GitHubCopilotMcpServer(
        IGitHubWorkflowOrchestrator workflowOrchestrator,
        IGitHubSemanticSearchService semanticSearchService,
        IGitHubDiscussionsService discussionsService,
        IGitHubIssuesService issuesService,
        IGitHubAppAuthService authService,
        ILogger<GitHubCopilotMcpServer> logger)
    {
        _workflowOrchestrator = workflowOrchestrator;
        _semanticSearchService = semanticSearchService;
        _discussionsService = discussionsService;
        _issuesService = issuesService;
        _authService = authService;
        _logger = logger;
    }

    [HttpGet("capabilities")]
    public IActionResult GetCapabilities()
    {
        var capabilities = new
        {
            name = "github-copilot-bot",
            version = "1.0.0",
            description = "GitHub Copilot Bot MCP Server for managing discussions, issues, and semantic search across GitHub repositories",
            tools = GetAvailableTools(),
            resources = GetAvailableResources()
        };

        return Ok(capabilities);
    }

    [HttpPost("tools/{toolName}")]
    public async Task<IActionResult> ExecuteTool(string toolName, [FromBody] JsonElement arguments)
    {
        // Validate authentication before executing tools
        if (!await ValidateAuthenticationAsync())
        {
            return Unauthorized(new { success = false, error = "Authentication required" });
        }

        try
        {
            _logger.LogInformation("Executing MCP tool: {ToolName}", toolName);

            var result = toolName.ToLowerInvariant() switch
            {
                "semantic_search" => await ExecuteSemanticSearchAsync(arguments),
                "create_discussion" => await ExecuteCreateDiscussionAsync(arguments),
                "create_issue" => await ExecuteCreateIssueAsync(arguments),
                "add_comment" => await ExecuteAddCommentAsync(arguments),
                "update_content" => await ExecuteUpdateContentAsync(arguments),
                "get_discussion" => await ExecuteGetDiscussionAsync(arguments),
                "get_issue" => await ExecuteGetIssueAsync(arguments),
                "search_discussions" => await ExecuteSearchDiscussionsAsync(arguments),
                "search_issues" => await ExecuteSearchIssuesAsync(arguments),
                "organization_discussions" => await ExecuteGetOrganizationDiscussionsAsync(arguments),
                "organization_issues" => await ExecuteGetOrganizationIssuesAsync(arguments),
                "prompt_action" => await ExecutePromptBasedActionAsync(arguments),
                _ => new { success = false, error = $"Unknown tool: {toolName}" }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing MCP tool: {ToolName}", toolName);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpGet("resources/{resourceUri}")]
    public async Task<IActionResult> GetResource(string resourceUri)
    {
        // Validate authentication before accessing resources
        if (!await ValidateAuthenticationAsync())
        {
            return Unauthorized(new { success = false, error = "Authentication required" });
        }

        try
        {
            var decodedUri = Uri.UnescapeDataString(resourceUri);
            _logger.LogInformation("Accessing MCP resource: {ResourceUri}", decodedUri);

            var parts = decodedUri.Split('/');
            if (parts.Length < 2)
            {
                return BadRequest(new { error = "Invalid resource URI format" });
            }

            var resourceType = parts[0];
            var result = resourceType.ToLowerInvariant() switch
            {
                "discussions" => await GetDiscussionsResourceAsync(parts),
                "issues" => await GetIssuesResourceAsync(parts),
                "search" => await GetSearchResourceAsync(parts),
                "organization" => await GetOrganizationResourceAsync(parts),
                _ => new { success = false, error = $"Unknown resource type: {resourceType}" }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing MCP resource: {ResourceUri}", resourceUri);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpGet("sse")]
    public async Task ServerSentEvents()
    {
        // Validate authentication before establishing SSE connection
        if (!await ValidateAuthenticationAsync())
        {
            Response.StatusCode = 401;
            await Response.WriteAsync("Unauthorized: Invalid or missing authentication");
            return;
        }

        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["Access-Control-Allow-Origin"] = "*";
        Response.Headers["Access-Control-Allow-Headers"] = "Authorization";

        var writer = new StreamWriter(Response.Body);

        try
        {
            // Send initial connection event
            await writer.WriteLineAsync("event: connected");
            await writer.WriteLineAsync($"data: {{\"timestamp\": \"{DateTime.UtcNow:O}\", \"message\": \"Connected to GitHub Copilot Bot MCP Server\", \"authenticated\": true}}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();

            _logger.LogInformation("SSE connection established with authentication");

            // Keep connection alive and send periodic heartbeats
            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                try
                {
                    await writer.WriteLineAsync("event: heartbeat");
                    await writer.WriteLineAsync($"data: {{\"timestamp\": \"{DateTime.UtcNow:O}\", \"status\": \"alive\"}}");
                    await writer.WriteLineAsync();
                    await writer.FlushAsync();

                    await Task.Delay(30000, HttpContext.RequestAborted); // 30 second heartbeat
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending SSE heartbeat");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE connection");
            try
            {
                await writer.WriteLineAsync("event: error");
                await writer.WriteLineAsync($"data: {{\"timestamp\": \"{DateTime.UtcNow:O}\", \"error\": \"Connection error\"}}");
                await writer.WriteLineAsync();
                await writer.FlushAsync();
            }
            catch
            {
                // Ignore errors when trying to send error message
            }
        }
        finally
        {
            _logger.LogInformation("SSE connection closed");
        }
    }

    private async Task<bool> ValidateAuthenticationAsync()
    {
        try
        {
            // Check for Authorization header (Bearer token)
            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var headerValue = authHeader.FirstOrDefault();
                if (!string.IsNullOrEmpty(headerValue) && headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = headerValue.Substring("Bearer ".Length).Trim();
                    return await ValidateTokenAsync(token);
                }
            }

            // Check for API key in query parameters
            if (Request.Query.TryGetValue("api_key", out var apiKey))
            {
                return await ValidateApiKeyAsync(apiKey.FirstOrDefault());
            }

            // Check for GitHub App installation token validation
            if (Request.Query.TryGetValue("github_token", out var githubToken))
            {
                return await ValidateGitHubTokenAsync(githubToken.FirstOrDefault());
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating authentication for SSE connection");
            return false;
        }
    }

    private async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            // Validate against GitHub App installation token
            var auth = await _authService.GetInstallationTokenAsync();
            return !string.IsNullOrEmpty(auth.Token) && auth.Token == token;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate bearer token");
            return false;
        }
    }

    private async Task<bool> ValidateApiKeyAsync(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return false;

        try
        {
            // For now, accept any valid GitHub App installation token as API key
            // In production, you might want a separate API key management system
            var auth = await _authService.GetInstallationTokenAsync();
            return !string.IsNullOrEmpty(auth.Token) && auth.Token == apiKey;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate API key");
            return false;
        }
    }

    private async Task<bool> ValidateGitHubTokenAsync(string? githubToken)
    {
        if (string.IsNullOrEmpty(githubToken))
            return false;

        try
        {
            // Test the provided GitHub token by making a simple API call
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", githubToken);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var response = await httpClient.GetAsync("https://api.github.com/user");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate GitHub token");
            return false;
        }
    }

    private static object GetAvailableTools()
    {
        return new object[]
        {
            new
            {
                name = "semantic_search",
                description = "Search across GitHub repositories using semantic search with relevance scoring",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Search query text" },
                        scope = new { type = "string", description = "Search scope: All, Discussions, Issues, Code, PullRequests", @default = "All" },
                        repository = new { type = "string", description = "Optional repository filter (owner/repo)" },
                        maxResults = new { type = "integer", description = "Maximum number of results to return", @default = 20 }
                    },
                    required = new[] { "query" }
                }
            },
            new
            {
                name = "create_discussion",
                description = "Create a new GitHub discussion with duplicate detection",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        repository = new { type = "string", description = "Repository name (owner/repo)" },
                        title = new { type = "string", description = "Discussion title" },
                        body = new { type = "string", description = "Discussion body content" },
                        category = new { type = "string", description = "Discussion category", @default = "General" },
                        checkForDuplicates = new { type = "boolean", description = "Check for similar discussions", @default = true },
                        onBehalfOf = new { type = "string", description = "User creating on behalf of" }
                    },
                    required = new[] { "repository", "title", "body" }
                }
            },
            new
            {
                name = "create_issue",
                description = "Create a new GitHub issue with duplicate detection",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        repository = new { type = "string", description = "Repository name (owner/repo)" },
                        title = new { type = "string", description = "Issue title" },
                        body = new { type = "string", description = "Issue body content" },
                        labels = new { type = "array", items = new { type = "string" }, description = "Issue labels" },
                        assignees = new { type = "array", items = new { type = "string" }, description = "Issue assignees" },
                        checkForDuplicates = new { type = "boolean", description = "Check for similar issues", @default = true },
                        onBehalfOf = new { type = "string", description = "User creating on behalf of" }
                    },
                    required = new[] { "repository", "title", "body" }
                }
            },
            new
            {
                name = "add_comment",
                description = "Add a comment to a discussion or issue",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        repository = new { type = "string", description = "Repository name (owner/repo)" },
                        contentType = new { type = "string", description = "Content type: discussion or issue" },
                        nodeId = new { type = "string", description = "Node ID for discussions" },
                        number = new { type = "integer", description = "Issue/PR number" },
                        body = new { type = "string", description = "Comment body content" },
                        onBehalfOf = new { type = "string", description = "User commenting on behalf of" }
                    },
                    required = new[] { "repository", "contentType", "body" }
                }
            },
            new
            {
                name = "update_content",
                description = "Update a discussion or issue",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        repository = new { type = "string", description = "Repository name (owner/repo)" },
                        contentType = new { type = "string", description = "Content type: discussion or issue" },
                        nodeId = new { type = "string", description = "Node ID for discussions" },
                        number = new { type = "integer", description = "Issue/PR number" },
                        title = new { type = "string", description = "New title" },
                        body = new { type = "string", description = "New body content" },
                        state = new { type = "string", description = "New state (for issues)" },
                        labels = new { type = "array", items = new { type = "string" }, description = "New labels (for issues)" },
                        onBehalfOf = new { type = "string", description = "User updating on behalf of" }
                    },
                    required = new[] { "repository", "contentType" }
                }
            },
            new
            {
                name = "get_discussion",
                description = "Get a specific discussion by repository and number",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        repository = new { type = "string", description = "Repository name (owner/repo)" },
                        discussionNumber = new { type = "integer", description = "Discussion number" }
                    },
                    required = new[] { "repository", "discussionNumber" }
                }
            },
            new
            {
                name = "get_issue",
                description = "Get a specific issue by repository and number",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        repository = new { type = "string", description = "Repository name (owner/repo)" },
                        issueNumber = new { type = "integer", description = "Issue number" }
                    },
                    required = new[] { "repository", "issueNumber" }
                }
            },
            new
            {
                name = "search_discussions",
                description = "Search discussions within repositories",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Search query" },
                        repository = new { type = "string", description = "Optional repository filter" },
                        maxResults = new { type = "integer", description = "Maximum results", @default = 10 }
                    },
                    required = new[] { "query" }
                }
            },
            new
            {
                name = "search_issues",
                description = "Search issues within repositories",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Search query" },
                        repository = new { type = "string", description = "Optional repository filter" },
                        maxResults = new { type = "integer", description = "Maximum results", @default = 10 }
                    },
                    required = new[] { "query" }
                }
            },
            new
            {
                name = "organization_discussions",
                description = "Get discussions from an entire organization",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        organization = new { type = "string", description = "Organization name" },
                        limit = new { type = "integer", description = "Maximum results", @default = 50 }
                    },
                    required = new[] { "organization" }
                }
            },
            new
            {
                name = "organization_issues",
                description = "Get issues from an entire organization",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        organization = new { type = "string", description = "Organization name" },
                        state = new { type = "string", description = "Issue state: open, closed, all", @default = "open" },
                        limit = new { type = "integer", description = "Maximum results", @default = 50 }
                    },
                    required = new[] { "organization" }
                }
            },
            new
            {
                name = "prompt_action",
                description = "Execute actions based on natural language prompts",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        prompt = new { type = "string", description = "Natural language prompt describing the action to take" },
                        repository = new { type = "string", description = "Optional default repository" },
                        scope = new { type = "string", description = "Optional search scope for prompt-based searches" },
                        onBehalfOf = new { type = "string", description = "User executing on behalf of" }
                    },
                    required = new[] { "prompt" }
                }
            }
        };
    }

    private static object GetAvailableResources()
    {
        return new[]
        {
            new
            {
                uri = "discussions/{repository}",
                name = "Repository Discussions",
                description = "Access discussions from a specific repository"
            },
            new
            {
                uri = "discussions/{repository}/{number}",
                name = "Specific Discussion",
                description = "Access a specific discussion by number"
            },
            new
            {
                uri = "issues/{repository}",
                name = "Repository Issues",
                description = "Access issues from a specific repository"
            },
            new
            {
                uri = "issues/{repository}/{number}",
                name = "Specific Issue",
                description = "Access a specific issue by number"
            },
            new
            {
                uri = "search/{query}",
                name = "Search Results",
                description = "Access search results for a query"
            },
            new
            {
                uri = "organization/{org}/discussions",
                name = "Organization Discussions",
                description = "Access all discussions from an organization"
            },
            new
            {
                uri = "organization/{org}/issues",
                name = "Organization Issues",
                description = "Access all issues from an organization"
            }
        };
    }

    private async Task<object> ExecuteSemanticSearchAsync(JsonElement arguments)
    {
        var query = arguments.GetProperty("query").GetString() ?? "";
        var scopeStr = arguments.TryGetProperty("scope", out var scopeProp) ? scopeProp.GetString() : "All";
        var repository = arguments.TryGetProperty("repository", out var repoProp) ? repoProp.GetString() : null;
        var maxResults = arguments.TryGetProperty("maxResults", out var maxProp) ? maxProp.GetInt32() : 20;

        if (!Enum.TryParse<SearchScope>(scopeStr, true, out var scope))
        {
            scope = SearchScope.All;
        }

        var request = new SemanticSearchRequest
        {
            Query = query,
            Scope = scope,
            MaxResults = maxResults,
            RepositoryFilter = repository,
            AutoProcessResults = false
        };

        var result = await _workflowOrchestrator.HandleSemanticSearchRequestAsync(request);
        return new { success = result.Success, data = result.Data, error = result.Error };
    }

    private async Task<object> ExecuteCreateDiscussionAsync(JsonElement arguments)
    {
        var request = new CreateDiscussionRequest
        {
            Repository = arguments.GetProperty("repository").GetString() ?? "",
            Title = arguments.GetProperty("title").GetString() ?? "",
            Body = arguments.GetProperty("body").GetString() ?? "",
            Category = arguments.TryGetProperty("category", out var catProp) ? catProp.GetString() ?? "General" : "General",
            CheckForDuplicates = arguments.TryGetProperty("checkForDuplicates", out var checkProp) ? checkProp.GetBoolean() : true,
            OnBehalfOf = arguments.TryGetProperty("onBehalfOf", out var behalfProp) ? behalfProp.GetString() : null
        };

        var result = await _workflowOrchestrator.CreateDiscussionWorkflowAsync(request);
        return new { success = result.Success, data = result.Data, error = result.Error, workflow = result };
    }

    private async Task<object> ExecuteCreateIssueAsync(JsonElement arguments)
    {
        var labels = new List<string>();
        if (arguments.TryGetProperty("labels", out var labelsProp))
        {
            foreach (var label in labelsProp.EnumerateArray())
            {
                if (label.ValueKind == JsonValueKind.String)
                {
                    labels.Add(label.GetString() ?? "");
                }
            }
        }

        var assignees = new List<string>();
        if (arguments.TryGetProperty("assignees", out var assigneesProp))
        {
            foreach (var assignee in assigneesProp.EnumerateArray())
            {
                if (assignee.ValueKind == JsonValueKind.String)
                {
                    assignees.Add(assignee.GetString() ?? "");
                }
            }
        }

        var request = new CreateIssueRequest
        {
            Repository = arguments.GetProperty("repository").GetString() ?? "",
            Title = arguments.GetProperty("title").GetString() ?? "",
            Body = arguments.GetProperty("body").GetString() ?? "",
            Labels = labels.ToArray(),
            Assignees = assignees.ToArray(),
            CheckForDuplicates = arguments.TryGetProperty("checkForDuplicates", out var checkProp) ? checkProp.GetBoolean() : true,
            OnBehalfOf = arguments.TryGetProperty("onBehalfOf", out var behalfProp) ? behalfProp.GetString() : null
        };

        var result = await _workflowOrchestrator.CreateIssueWorkflowAsync(request);
        return new { success = result.Success, data = result.Data, error = result.Error, workflow = result };
    }

    private async Task<object> ExecuteAddCommentAsync(JsonElement arguments)
    {
        var request = new AddCommentRequest
        {
            Repository = arguments.GetProperty("repository").GetString() ?? "",
            ContentType = arguments.GetProperty("contentType").GetString() ?? "",
            NodeId = arguments.TryGetProperty("nodeId", out var nodeProp) ? nodeProp.GetString() ?? "" : "",
            Number = arguments.TryGetProperty("number", out var numProp) ? numProp.GetInt32() : null,
            Body = arguments.GetProperty("body").GetString() ?? "",
            OnBehalfOf = arguments.TryGetProperty("onBehalfOf", out var behalfProp) ? behalfProp.GetString() : null
        };

        var result = await _workflowOrchestrator.AddCommentWorkflowAsync(request);
        return new { success = result.Success, data = result.Data, error = result.Error };
    }

    private async Task<object> ExecuteUpdateContentAsync(JsonElement arguments)
    {
        var labels = new List<string>();
        if (arguments.TryGetProperty("labels", out var labelsProp))
        {
            foreach (var label in labelsProp.EnumerateArray())
            {
                if (label.ValueKind == JsonValueKind.String)
                {
                    labels.Add(label.GetString() ?? "");
                }
            }
        }

        var request = new UpdateContentRequest
        {
            Repository = arguments.GetProperty("repository").GetString() ?? "",
            ContentType = arguments.GetProperty("contentType").GetString() ?? "",
            NodeId = arguments.TryGetProperty("nodeId", out var nodeProp) ? nodeProp.GetString() ?? "" : "",
            Number = arguments.TryGetProperty("number", out var numProp) ? numProp.GetInt32() : null,
            Title = arguments.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null,
            Body = arguments.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : null,
            State = arguments.TryGetProperty("state", out var stateProp) ? stateProp.GetString() : null,
            Labels = labels.Any() ? labels.ToArray() : null,
            OnBehalfOf = arguments.TryGetProperty("onBehalfOf", out var behalfProp) ? behalfProp.GetString() : null
        };

        var result = await _workflowOrchestrator.UpdateContentWorkflowAsync(request);
        return new { success = result.Success, data = result.Data, error = result.Error };
    }

    private async Task<object> ExecuteGetDiscussionAsync(JsonElement arguments)
    {
        var repository = arguments.GetProperty("repository").GetString() ?? "";
        var discussionNumber = arguments.GetProperty("discussionNumber").GetInt32();

        var discussion = await _discussionsService.GetDiscussionAsync(repository, discussionNumber);
        return new { success = true, data = discussion };
    }

    private async Task<object> ExecuteGetIssueAsync(JsonElement arguments)
    {
        var repository = arguments.GetProperty("repository").GetString() ?? "";
        var issueNumber = arguments.GetProperty("issueNumber").GetInt32();

        var issue = await _issuesService.GetIssueAsync(repository, issueNumber);
        return new { success = true, data = issue };
    }

    private async Task<object> ExecuteSearchDiscussionsAsync(JsonElement arguments)
    {
        var query = arguments.GetProperty("query").GetString() ?? "";
        var repository = arguments.TryGetProperty("repository", out var repoProp) ? repoProp.GetString() : null;
        var maxResults = arguments.TryGetProperty("maxResults", out var maxProp) ? maxProp.GetInt32() : 10;

        var result = await _semanticSearchService.SearchDiscussionsSemanticAsync(query, repository, maxResults);
        return new { success = result.Success, data = result, error = result.Error };
    }

    private async Task<object> ExecuteSearchIssuesAsync(JsonElement arguments)
    {
        var query = arguments.GetProperty("query").GetString() ?? "";
        var repository = arguments.TryGetProperty("repository", out var repoProp) ? repoProp.GetString() : null;
        var maxResults = arguments.TryGetProperty("maxResults", out var maxProp) ? maxProp.GetInt32() : 10;

        var result = await _semanticSearchService.SearchIssuesSemanticAsync(query, repository, maxResults);
        return new { success = result.Success, data = result, error = result.Error };
    }

    private async Task<object> ExecuteGetOrganizationDiscussionsAsync(JsonElement arguments)
    {
        var organization = arguments.GetProperty("organization").GetString() ?? "";
        var limit = arguments.TryGetProperty("limit", out var limitProp) ? limitProp.GetInt32() : 50;

        var discussions = await _discussionsService.GetOrganizationDiscussionsAsync(organization, limit);
        return new { success = true, data = discussions };
    }

    private async Task<object> ExecuteGetOrganizationIssuesAsync(JsonElement arguments)
    {
        var organization = arguments.GetProperty("organization").GetString() ?? "";
        var state = arguments.TryGetProperty("state", out var stateProp) ? stateProp.GetString() ?? "open" : "open";
        var limit = arguments.TryGetProperty("limit", out var limitProp) ? limitProp.GetInt32() : 50;

        var issues = await _issuesService.GetOrganizationIssuesAsync(organization, state, limit);
        return new { success = true, data = issues };
    }

    private async Task<object> ExecutePromptBasedActionAsync(JsonElement arguments)
    {
        var request = new PromptBasedActionRequest
        {
            Prompt = arguments.GetProperty("prompt").GetString() ?? "",
            Repository = arguments.TryGetProperty("repository", out var repoProp) ? repoProp.GetString() : null,
            Scope = arguments.TryGetProperty("scope", out var scopeProp) && Enum.TryParse<SearchScope>(scopeProp.GetString(), true, out var scope) ? scope : null,
            OnBehalfOf = arguments.TryGetProperty("onBehalfOf", out var behalfProp) ? behalfProp.GetString() : null
        };

        var result = await _workflowOrchestrator.ExecutePromptBasedActionAsync(request);
        return result;
    }

    private async Task<object> GetDiscussionsResourceAsync(string[] parts)
    {
        if (parts.Length >= 3)
        {
            // discussions/{repository}/{number}
            var repository = parts[1];
            if (int.TryParse(parts[2], out var number))
            {
                var discussion = await _discussionsService.GetDiscussionAsync(repository, number);
                return new { success = true, data = discussion };
            }
        }
        else if (parts.Length >= 2)
        {
            // discussions/{repository}
            var repository = parts[1];
            var discussions = await _discussionsService.GetDiscussionsByRepositoryAsync(repository);
            return new { success = true, data = discussions };
        }

        return new { success = false, error = "Invalid discussions resource URI" };
    }

    private async Task<object> GetIssuesResourceAsync(string[] parts)
    {
        if (parts.Length >= 3)
        {
            // issues/{repository}/{number}
            var repository = parts[1];
            if (int.TryParse(parts[2], out var number))
            {
                var issue = await _issuesService.GetIssueAsync(repository, number);
                return new { success = true, data = issue };
            }
        }
        else if (parts.Length >= 2)
        {
            // issues/{repository}
            var repository = parts[1];
            var issues = await _issuesService.GetIssuesByRepositoryAsync(repository);
            return new { success = true, data = issues };
        }

        return new { success = false, error = "Invalid issues resource URI" };
    }

    private async Task<object> GetSearchResourceAsync(string[] parts)
    {
        if (parts.Length >= 2)
        {
            // search/{query}
            var query = Uri.UnescapeDataString(parts[1]);
            var result = await _semanticSearchService.SearchAcrossRepositoriesAsync(query);
            return new { success = result.Success, data = result, error = result.Error };
        }

        return new { success = false, error = "Invalid search resource URI" };
    }

    private async Task<object> GetOrganizationResourceAsync(string[] parts)
    {
        if (parts.Length >= 3)
        {
            var organization = parts[1];
            var resourceType = parts[2];

            return resourceType.ToLowerInvariant() switch
            {
                "discussions" => new { success = true, data = await _discussionsService.GetOrganizationDiscussionsAsync(organization) },
                "issues" => new { success = true, data = await _issuesService.GetOrganizationIssuesAsync(organization) },
                _ => new { success = false, error = $"Unknown organization resource type: {resourceType}" }
            };
        }

        return new { success = false, error = "Invalid organization resource URI" };
    }
}