using System.Text;
using System.Text.Json;
using Shared.Models;

namespace CopilotAgent.Services;

public class GitHubDiscussionsService : IGitHubDiscussionsService
{
    private readonly IGitHubAppAuthService _authService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubDiscussionsService> _logger;
    private readonly IConfiguration _configuration;

    public GitHubDiscussionsService(
        IGitHubAppAuthService authService,
        HttpClient httpClient,
        ILogger<GitHubDiscussionsService> logger,
        IConfiguration configuration)
    {
        _authService = authService;
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IEnumerable<GitHubDiscussion>> SearchDiscussionsAsync(string query, string? repositoryFilter = null)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var searchQuery = $"{query} type:discussion";
            if (!string.IsNullOrEmpty(repositoryFilter))
            {
                searchQuery += $" repo:{repositoryFilter}";
            }

            var encodedQuery = Uri.EscapeDataString(searchQuery);
            var response = await _httpClient.GetAsync($"https://api.github.com/search/issues?q={encodedQuery}&per_page=50");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to search discussions: {StatusCode}", response.StatusCode);
                return Array.Empty<GitHubDiscussion>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<JsonElement>(content);
            
            var discussions = new List<GitHubDiscussion>();
            if (searchResult.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    discussions.Add(ParseDiscussionFromSearchResult(item));
                }
            }

            return discussions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching discussions with query: {Query}", query);
            return Array.Empty<GitHubDiscussion>();
        }
    }

    public async Task<GitHubDiscussion> CreateDiscussionAsync(string repository, string title, string body, string category)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            // First, get the repository ID and category ID
            var repoInfo = await GetRepositoryInfoAsync(repository);
            var categoryId = await GetDiscussionCategoryIdAsync(repository, category);

            var mutation = @"
                mutation($repositoryId: ID!, $categoryId: ID!, $title: String!, $body: String!) {
                    createDiscussion(input: {
                        repositoryId: $repositoryId,
                        categoryId: $categoryId,
                        title: $title,
                        body: $body
                    }) {
                        discussion {
                            id
                            number
                            title
                            body
                            url
                            createdAt
                            updatedAt
                            author {
                                login
                            }
                            category {
                                name
                            }
                        }
                    }
                }";

            var variables = new
            {
                repositoryId = repoInfo.NodeId,
                categoryId = categoryId,
                title = title,
                body = body
            };

            var graphqlRequest = new
            {
                query = mutation,
                variables = variables
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(graphqlRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("https://api.github.com/graphql", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create discussion: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to create discussion: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (result.TryGetProperty("data", out var data) &&
                data.TryGetProperty("createDiscussion", out var createData) &&
                createData.TryGetProperty("discussion", out var discussion))
            {
                return ParseDiscussionFromGraphQL(discussion, repository);
            }

            throw new InvalidOperationException("Unexpected response format when creating discussion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating discussion in repository: {Repository}", repository);
            throw;
        }
    }

    public async Task<GitHubComment> AddDiscussionCommentAsync(string repository, string discussionNodeId, string body)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var mutation = @"
                mutation($discussionId: ID!, $body: String!) {
                    addDiscussionComment(input: {
                        discussionId: $discussionId,
                        body: $body
                    }) {
                        comment {
                            id
                            body
                            url
                            createdAt
                            updatedAt
                            author {
                                login
                            }
                        }
                    }
                }";

            var variables = new
            {
                discussionId = discussionNodeId,
                body = body
            };

            var graphqlRequest = new
            {
                query = mutation,
                variables = variables
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(graphqlRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("https://api.github.com/graphql", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to add discussion comment: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to add discussion comment: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (result.TryGetProperty("data", out var data) &&
                data.TryGetProperty("addDiscussionComment", out var addData) &&
                addData.TryGetProperty("comment", out var comment))
            {
                return ParseCommentFromGraphQL(comment);
            }

            throw new InvalidOperationException("Unexpected response format when adding discussion comment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to discussion: {DiscussionId}", discussionNodeId);
            throw;
        }
    }

    public async Task<GitHubDiscussion> UpdateDiscussionAsync(string repository, string discussionNodeId, string? title = null, string? body = null)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var mutation = @"
                mutation($discussionId: ID!, $title: String, $body: String) {
                    updateDiscussion(input: {
                        discussionId: $discussionId,
                        title: $title,
                        body: $body
                    }) {
                        discussion {
                            id
                            number
                            title
                            body
                            url
                            createdAt
                            updatedAt
                            author {
                                login
                            }
                            category {
                                name
                            }
                        }
                    }
                }";

            var variables = new Dictionary<string, object>
            {
                { "discussionId", discussionNodeId }
            };

            if (!string.IsNullOrEmpty(title))
                variables.Add("title", title);
            if (!string.IsNullOrEmpty(body))
                variables.Add("body", body);

            var graphqlRequest = new
            {
                query = mutation,
                variables = variables
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(graphqlRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("https://api.github.com/graphql", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update discussion: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to update discussion: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (result.TryGetProperty("data", out var data) &&
                data.TryGetProperty("updateDiscussion", out var updateData) &&
                updateData.TryGetProperty("discussion", out var discussion))
            {
                return ParseDiscussionFromGraphQL(discussion, repository);
            }

            throw new InvalidOperationException("Unexpected response format when updating discussion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating discussion: {DiscussionId}", discussionNodeId);
            throw;
        }
    }

    public async Task<GitHubDiscussion> GetDiscussionAsync(string repository, int discussionNumber)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var query = @"
                query($owner: String!, $name: String!, $number: Int!) {
                    repository(owner: $owner, name: $name) {
                        discussion(number: $number) {
                            id
                            number
                            title
                            body
                            url
                            createdAt
                            updatedAt
                            author {
                                login
                            }
                            category {
                                name
                            }
                            comments(first: 50) {
                                nodes {
                                    id
                                    body
                                    url
                                    createdAt
                                    updatedAt
                                    author {
                                        login
                                    }
                                }
                            }
                        }
                    }
                }";

            var parts = repository.Split('/');
            var variables = new
            {
                owner = parts[0],
                name = parts[1],
                number = discussionNumber
            };

            var graphqlRequest = new
            {
                query = query,
                variables = variables
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(graphqlRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("https://api.github.com/graphql", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get discussion: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to get discussion: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (result.TryGetProperty("data", out var data) &&
                data.TryGetProperty("repository", out var repo) &&
                repo.TryGetProperty("discussion", out var discussion))
            {
                return ParseDiscussionFromGraphQL(discussion, repository);
            }

            throw new InvalidOperationException("Discussion not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting discussion {Number} from repository: {Repository}", discussionNumber, repository);
            throw;
        }
    }

    public async Task<IEnumerable<GitHubDiscussion>> GetDiscussionsByRepositoryAsync(string repository, int limit = 50)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var query = @"
                query($owner: String!, $name: String!, $first: Int!) {
                    repository(owner: $owner, name: $name) {
                        discussions(first: $first, orderBy: {field: UPDATED_AT, direction: DESC}) {
                            nodes {
                                id
                                number
                                title
                                body
                                url
                                createdAt
                                updatedAt
                                author {
                                    login
                                }
                                category {
                                    name
                                }
                            }
                        }
                    }
                }";

            var parts = repository.Split('/');
            var variables = new
            {
                owner = parts[0],
                name = parts[1],
                first = limit
            };

            var graphqlRequest = new
            {
                query = query,
                variables = variables
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(graphqlRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("https://api.github.com/graphql", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get discussions: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return Array.Empty<GitHubDiscussion>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            
            var discussions = new List<GitHubDiscussion>();
            if (result.TryGetProperty("data", out var data) &&
                data.TryGetProperty("repository", out var repo) &&
                repo.TryGetProperty("discussions", out var discussionsData) &&
                discussionsData.TryGetProperty("nodes", out var nodes))
            {
                foreach (var discussion in nodes.EnumerateArray())
                {
                    discussions.Add(ParseDiscussionFromGraphQL(discussion, repository));
                }
            }

            return discussions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting discussions from repository: {Repository}", repository);
            return Array.Empty<GitHubDiscussion>();
        }
    }

    public async Task<IEnumerable<GitHubDiscussion>> GetOrganizationDiscussionsAsync(string organization, int limit = 100)
    {
        try
        {
            var discussions = new List<GitHubDiscussion>();

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
                return Array.Empty<GitHubDiscussion>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var reposResult = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (reposResult.TryGetProperty("repositories", out var repositories))
            {
                var tasks = new List<Task<IEnumerable<GitHubDiscussion>>>();
                
                foreach (var repo in repositories.EnumerateArray())
                {
                    if (repo.TryGetProperty("full_name", out var fullName))
                    {
                        var repoName = fullName.GetString();
                        if (!string.IsNullOrEmpty(repoName) && repoName.StartsWith($"{organization}/"))
                        {
                            tasks.Add(GetDiscussionsByRepositoryAsync(repoName, limit / 10)); // Distribute limit across repos
                        }
                    }
                }

                var results = await Task.WhenAll(tasks);
                discussions.AddRange(results.SelectMany(r => r).Take(limit));
            }

            return discussions.OrderByDescending(d => d.UpdatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization discussions: {Organization}", organization);
            return Array.Empty<GitHubDiscussion>();
        }
    }

    private async Task<(string NodeId, string Name)> GetRepositoryInfoAsync(string repository)
    {
        var parts = repository.Split('/');
        var query = @"
            query($owner: String!, $name: String!) {
                repository(owner: $owner, name: $name) {
                    id
                    name
                }
            }";

        var variables = new
        {
            owner = parts[0],
            name = parts[1]
        };

        var graphqlRequest = new
        {
            query = query,
            variables = variables
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(graphqlRequest),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync("https://api.github.com/graphql", jsonContent);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        if (result.TryGetProperty("data", out var data) &&
            data.TryGetProperty("repository", out var repo))
        {
            var nodeId = repo.GetProperty("id").GetString() ?? "";
            var name = repo.GetProperty("name").GetString() ?? "";
            return (nodeId, name);
        }

        throw new InvalidOperationException($"Repository {repository} not found");
    }

    private async Task<string> GetDiscussionCategoryIdAsync(string repository, string category)
    {
        var parts = repository.Split('/');
        var query = @"
            query($owner: String!, $name: String!) {
                repository(owner: $owner, name: $name) {
                    discussionCategories(first: 50) {
                        nodes {
                            id
                            name
                        }
                    }
                }
            }";

        var variables = new
        {
            owner = parts[0],
            name = parts[1]
        };

        var graphqlRequest = new
        {
            query = query,
            variables = variables
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(graphqlRequest),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync("https://api.github.com/graphql", jsonContent);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        if (result.TryGetProperty("data", out var data) &&
            data.TryGetProperty("repository", out var repo) &&
            repo.TryGetProperty("discussionCategories", out var categories) &&
            categories.TryGetProperty("nodes", out var nodes))
        {
            foreach (var cat in nodes.EnumerateArray())
            {
                if (cat.TryGetProperty("name", out var nameElement) &&
                    nameElement.GetString()?.Equals(category, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return cat.GetProperty("id").GetString() ?? "";
                }
            }
        }

        // Default to "General" category if not found
        return "DIC_kwDOG6G-1c4CAUA8"; // This would need to be dynamic
    }

    private static GitHubDiscussion ParseDiscussionFromSearchResult(JsonElement item)
    {
        return new GitHubDiscussion
        {
            NodeId = item.TryGetProperty("node_id", out var nodeId) ? nodeId.GetString() ?? "" : "",
            Number = item.TryGetProperty("number", out var number) ? number.GetInt32() : 0,
            Title = item.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
            Body = item.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "",
            Url = item.TryGetProperty("html_url", out var url) ? url.GetString() ?? "" : "",
            Repository = item.TryGetProperty("repository_url", out var repoUrl) ? 
                ExtractRepositoryFromUrl(repoUrl.GetString() ?? "") : "",
            Author = item.TryGetProperty("user", out var user) && user.TryGetProperty("login", out var login) ? 
                login.GetString() ?? "" : "",
            State = item.TryGetProperty("state", out var state) ? state.GetString() ?? "" : "",
            CreatedAt = item.TryGetProperty("created_at", out var created) ? created.GetDateTime() : DateTime.MinValue,
            UpdatedAt = item.TryGetProperty("updated_at", out var updated) ? updated.GetDateTime() : DateTime.MinValue,
            CommentCount = item.TryGetProperty("comments", out var comments) ? comments.GetInt32() : 0
        };
    }

    private static GitHubDiscussion ParseDiscussionFromGraphQL(JsonElement discussion, string repository)
    {
        var comments = new List<GitHubComment>();
        if (discussion.TryGetProperty("comments", out var commentsData) &&
            commentsData.TryGetProperty("nodes", out var commentNodes))
        {
            foreach (var comment in commentNodes.EnumerateArray())
            {
                comments.Add(ParseCommentFromGraphQL(comment));
            }
        }

        return new GitHubDiscussion
        {
            NodeId = discussion.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
            Number = discussion.TryGetProperty("number", out var number) ? number.GetInt32() : 0,
            Title = discussion.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
            Body = discussion.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "",
            Url = discussion.TryGetProperty("url", out var url) ? url.GetString() ?? "" : "",
            Repository = repository,
            Author = discussion.TryGetProperty("author", out var author) && author.TryGetProperty("login", out var login) ? 
                login.GetString() ?? "" : "",
            Category = discussion.TryGetProperty("category", out var category) && category.TryGetProperty("name", out var catName) ? 
                catName.GetString() ?? "" : "",
            CreatedAt = discussion.TryGetProperty("createdAt", out var created) ? created.GetDateTime() : DateTime.MinValue,
            UpdatedAt = discussion.TryGetProperty("updatedAt", out var updated) ? updated.GetDateTime() : DateTime.MinValue,
            CommentCount = comments.Count,
            Comments = comments
        };
    }

    private static GitHubComment ParseCommentFromGraphQL(JsonElement comment)
    {
        return new GitHubComment
        {
            NodeId = comment.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
            Body = comment.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "",
            Author = comment.TryGetProperty("author", out var author) && author.TryGetProperty("login", out var login) ? 
                login.GetString() ?? "" : "",
            Url = comment.TryGetProperty("url", out var url) ? url.GetString() ?? "" : "",
            CreatedAt = comment.TryGetProperty("createdAt", out var created) ? created.GetDateTime() : DateTime.MinValue,
            UpdatedAt = comment.TryGetProperty("updatedAt", out var updated) ? updated.GetDateTime() : DateTime.MinValue
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