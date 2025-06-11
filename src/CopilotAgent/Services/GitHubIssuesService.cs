using System.Text;
using System.Text.Json;
using Shared.Models;

namespace CopilotAgent.Services;

public class GitHubIssuesService : IGitHubIssuesService
{
    private readonly IGitHubAppAuthService _authService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubIssuesService> _logger;
    private readonly IConfiguration _configuration;

    public GitHubIssuesService(
        IGitHubAppAuthService authService,
        HttpClient httpClient,
        ILogger<GitHubIssuesService> logger,
        IConfiguration configuration)
    {
        _authService = authService;
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IEnumerable<GitHubIssue>> SearchIssuesAsync(string query, string? repositoryFilter = null)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var searchQuery = $"{query} type:issue";
            if (!string.IsNullOrEmpty(repositoryFilter))
            {
                searchQuery += $" repo:{repositoryFilter}";
            }

            var encodedQuery = Uri.EscapeDataString(searchQuery);
            var response = await _httpClient.GetAsync($"https://api.github.com/search/issues?q={encodedQuery}&per_page=50");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to search issues: {StatusCode}", response.StatusCode);
                return Array.Empty<GitHubIssue>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<JsonElement>(content);
            
            var issues = new List<GitHubIssue>();
            if (searchResult.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    issues.Add(ParseIssueFromJson(item));
                }
            }

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching issues with query: {Query}", query);
            return Array.Empty<GitHubIssue>();
        }
    }

    public async Task<GitHubIssue> CreateIssueAsync(string repository, string title, string body, string[]? labels = null, string[]? assignees = null)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var issueData = new Dictionary<string, object>
            {
                { "title", title },
                { "body", body }
            };

            if (labels != null && labels.Length > 0)
                issueData.Add("labels", labels);

            if (assignees != null && assignees.Length > 0)
                issueData.Add("assignees", assignees);

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(issueData),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"https://api.github.com/repos/{repository}/issues", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create issue: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to create issue: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var issueElement = JsonSerializer.Deserialize<JsonElement>(content);
            
            return ParseIssueFromJson(issueElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating issue in repository: {Repository}", repository);
            throw;
        }
    }

    public async Task<GitHubComment> AddIssueCommentAsync(string repository, int issueNumber, string body)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var commentData = new { body = body };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(commentData),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"https://api.github.com/repos/{repository}/issues/{issueNumber}/comments", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to add issue comment: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to add issue comment: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var commentElement = JsonSerializer.Deserialize<JsonElement>(content);
            
            return ParseCommentFromJson(commentElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to issue {Number} in repository: {Repository}", issueNumber, repository);
            throw;
        }
    }

    public async Task<GitHubIssue> UpdateIssueAsync(string repository, int issueNumber, string? title = null, string? body = null, string? state = null, string[]? labels = null)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var updateData = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(title))
                updateData.Add("title", title);

            if (!string.IsNullOrEmpty(body))
                updateData.Add("body", body);

            if (!string.IsNullOrEmpty(state))
                updateData.Add("state", state);

            if (labels != null)
                updateData.Add("labels", labels);

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(updateData),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PatchAsync($"https://api.github.com/repos/{repository}/issues/{issueNumber}", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update issue: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to update issue: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var issueElement = JsonSerializer.Deserialize<JsonElement>(content);
            
            return ParseIssueFromJson(issueElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating issue {Number} in repository: {Repository}", issueNumber, repository);
            throw;
        }
    }

    public async Task<GitHubIssue> GetIssueAsync(string repository, int issueNumber)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{repository}/issues/{issueNumber}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get issue: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to get issue: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var issueElement = JsonSerializer.Deserialize<JsonElement>(content);
            
            var issue = ParseIssueFromJson(issueElement);

            // Get comments
            var comments = await GetIssueCommentsAsync(repository, issueNumber);
            issue.Comments = comments;

            return issue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting issue {Number} from repository: {Repository}", issueNumber, repository);
            throw;
        }
    }

    public async Task<IEnumerable<GitHubIssue>> GetIssuesByRepositoryAsync(string repository, string state = "open", int limit = 50)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{repository}/issues?state={state}&per_page={limit}&sort=updated&direction=desc");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get issues: {StatusCode}", response.StatusCode);
                return Array.Empty<GitHubIssue>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var issuesArray = JsonSerializer.Deserialize<JsonElement[]>(content);
            
            var issues = new List<GitHubIssue>();
            if (issuesArray != null)
            {
                foreach (var issueElement in issuesArray)
                {
                    // Filter out pull requests (GitHub API includes PRs in issues)
                    if (!issueElement.TryGetProperty("pull_request", out _))
                    {
                        issues.Add(ParseIssueFromJson(issueElement));
                    }
                }
            }

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting issues from repository: {Repository}", repository);
            return Array.Empty<GitHubIssue>();
        }
    }

    public async Task<IEnumerable<GitHubIssue>> GetOrganizationIssuesAsync(string organization, string state = "open", int limit = 100)
    {
        try
        {
            var issues = new List<GitHubIssue>();

            // Get all repositories for the organization
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var response = await _httpClient.GetAsync("https://api.github.com/installation/repositories");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get repositories: {StatusCode}", response.StatusCode);
                return Array.Empty<GitHubIssue>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var reposResult = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (reposResult.TryGetProperty("repositories", out var repositories))
            {
                var tasks = new List<Task<IEnumerable<GitHubIssue>>>();
                
                foreach (var repo in repositories.EnumerateArray())
                {
                    if (repo.TryGetProperty("full_name", out var fullName))
                    {
                        var repoName = fullName.GetString();
                        if (!string.IsNullOrEmpty(repoName) && repoName.StartsWith($"{organization}/"))
                        {
                            tasks.Add(GetIssuesByRepositoryAsync(repoName, state, limit / 10)); // Distribute limit across repos
                        }
                    }
                }

                var results = await Task.WhenAll(tasks);
                issues.AddRange(results.SelectMany(r => r).Take(limit));
            }

            return issues.OrderByDescending(i => i.UpdatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization issues: {Organization}", organization);
            return Array.Empty<GitHubIssue>();
        }
    }

    private async Task<IEnumerable<GitHubComment>> GetIssueCommentsAsync(string repository, int issueNumber)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{repository}/issues/{issueNumber}/comments");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get issue comments: {StatusCode}", response.StatusCode);
                return Array.Empty<GitHubComment>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var commentsArray = JsonSerializer.Deserialize<JsonElement[]>(content);
            
            var comments = new List<GitHubComment>();
            if (commentsArray != null)
            {
                foreach (var commentElement in commentsArray)
                {
                    comments.Add(ParseCommentFromJson(commentElement));
                }
            }

            return comments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for issue {Number} in repository: {Repository}", issueNumber, repository);
            return Array.Empty<GitHubComment>();
        }
    }

    private static GitHubIssue ParseIssueFromJson(JsonElement item)
    {
        var labels = new List<string>();
        if (item.TryGetProperty("labels", out var labelsArray))
        {
            foreach (var label in labelsArray.EnumerateArray())
            {
                if (label.TryGetProperty("name", out var name))
                {
                    labels.Add(name.GetString() ?? "");
                }
            }
        }

        var assignees = new List<string>();
        if (item.TryGetProperty("assignees", out var assigneesArray))
        {
            foreach (var assignee in assigneesArray.EnumerateArray())
            {
                if (assignee.TryGetProperty("login", out var login))
                {
                    assignees.Add(login.GetString() ?? "");
                }
            }
        }

        var repository = "";
        if (item.TryGetProperty("repository_url", out var repoUrl))
        {
            repository = ExtractRepositoryFromUrl(repoUrl.GetString() ?? "");
        }
        else if (item.TryGetProperty("url", out var url))
        {
            // Extract from issue URL: https://api.github.com/repos/owner/repo/issues/123
            var urlStr = url.GetString() ?? "";
            var parts = urlStr.Split('/');
            if (parts.Length >= 6)
            {
                repository = $"{parts[4]}/{parts[5]}";
            }
        }

        return new GitHubIssue
        {
            NodeId = item.TryGetProperty("node_id", out var nodeId) ? nodeId.GetString() ?? "" : "",
            Number = item.TryGetProperty("number", out var number) ? number.GetInt32() : 0,
            Title = item.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
            Body = item.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "",
            Url = item.TryGetProperty("html_url", out var htmlUrl) ? htmlUrl.GetString() ?? "" : "",
            Repository = repository,
            Author = item.TryGetProperty("user", out var user) && user.TryGetProperty("login", out var authorLogin) ? 
                authorLogin.GetString() ?? "" : "",
            State = item.TryGetProperty("state", out var state) ? state.GetString() ?? "" : "",
            CreatedAt = item.TryGetProperty("created_at", out var created) ? created.GetDateTime() : DateTime.MinValue,
            UpdatedAt = item.TryGetProperty("updated_at", out var updated) ? updated.GetDateTime() : DateTime.MinValue,
            Labels = labels.ToArray(),
            Assignees = assignees.ToArray(),
            CommentCount = item.TryGetProperty("comments", out var comments) ? comments.GetInt32() : 0
        };
    }

    private static GitHubComment ParseCommentFromJson(JsonElement comment)
    {
        return new GitHubComment
        {
            NodeId = comment.TryGetProperty("node_id", out var nodeId) ? nodeId.GetString() ?? "" : "",
            Id = comment.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
            Body = comment.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "",
            Author = comment.TryGetProperty("user", out var user) && user.TryGetProperty("login", out var login) ? 
                login.GetString() ?? "" : "",
            Url = comment.TryGetProperty("html_url", out var url) ? url.GetString() ?? "" : "",
            CreatedAt = comment.TryGetProperty("created_at", out var created) ? created.GetDateTime() : DateTime.MinValue,
            UpdatedAt = comment.TryGetProperty("updated_at", out var updated) ? updated.GetDateTime() : DateTime.MinValue
        };
    }

    private static string ExtractRepositoryFromUrl(string repositoryUrl)
    {
        // Extract repository name from URL like "https://api.github.com/repos/owner/repo"
        var parts = repositoryUrl.Split('/');
        if (parts.Length >= 2)
        {
            return $"{parts[^2]}/{parts[^1]}";
        }
        return "";
    }
}