using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Shared.Models;

namespace CopilotAgent.Services;

public class GitHubSemanticSearchService : IGitHubSemanticSearchService
{
    private readonly IGitHubDiscussionsService _discussionsService;
    private readonly IGitHubIssuesService _issuesService;
    private readonly IGitHubAppAuthService _authService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubSemanticSearchService> _logger;
    private readonly IConfiguration _configuration;

    public GitHubSemanticSearchService(
        IGitHubDiscussionsService discussionsService,
        IGitHubIssuesService issuesService,
        IGitHubAppAuthService authService,
        HttpClient httpClient,
        ILogger<GitHubSemanticSearchService> logger,
        IConfiguration configuration)
    {
        _discussionsService = discussionsService;
        _issuesService = issuesService;
        _authService = authService;
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<SemanticSearchResult> SearchAcrossRepositoriesAsync(string query, SearchScope scope = SearchScope.All, int maxResults = 20)
    {
        var startTime = DateTime.UtcNow;
        var allResults = new List<SearchResultItem>();

        try
        {
            var tasks = new List<Task<IEnumerable<SearchResultItem>>>();

            if (scope == SearchScope.All || scope == SearchScope.Discussions)
            {
                tasks.Add(SearchDiscussionsInternalAsync(query, maxResults / 2));
            }

            if (scope == SearchScope.All || scope == SearchScope.Issues)
            {
                tasks.Add(SearchIssuesInternalAsync(query, maxResults / 2));
            }

            if (scope == SearchScope.All || scope == SearchScope.Code)
            {
                tasks.Add(SearchCodeAsync(query, maxResults / 4));
            }

            if (scope == SearchScope.All || scope == SearchScope.PullRequests)
            {
                tasks.Add(SearchPullRequestsAsync(query, maxResults / 4));
            }

            var results = await Task.WhenAll(tasks);
            allResults.AddRange(results.SelectMany(r => r));

            // Sort by relevance score and limit results
            var sortedResults = allResults
                .OrderByDescending(r => r.RelevanceScore)
                .Take(maxResults)
                .ToList();

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return new SemanticSearchResult
            {
                Success = true,
                Items = sortedResults,
                Query = query,
                Scope = scope,
                TotalResults = sortedResults.Count,
                SearchDurationMs = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing semantic search with query: {Query}", query);
            return new SemanticSearchResult
            {
                Success = false,
                Error = ex.Message,
                Query = query,
                Scope = scope
            };
        }
    }

    public async Task<SemanticSearchResult> SearchDiscussionsSemanticAsync(string query, string? repositoryFilter = null, int maxResults = 10)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var discussions = await _discussionsService.SearchDiscussionsAsync(query, repositoryFilter);
            var enhancedKeywords = await ExtractKeywordsAsync(query);
            
            var results = new List<SearchResultItem>();
            foreach (var discussion in discussions.Take(maxResults))
            {
                var relevanceScore = await CalculateRelevanceScoreAsync(query, enhancedKeywords, discussion.Title, discussion.Body);
                
                results.Add(new SearchResultItem
                {
                    Type = "discussion",
                    Repository = discussion.Repository,
                    Title = discussion.Title,
                    Body = TruncateText(discussion.Body, 500),
                    Url = discussion.Url,
                    NodeId = discussion.NodeId,
                    Number = discussion.Number,
                    RelevanceScore = relevanceScore,
                    Author = discussion.Author,
                    CreatedAt = discussion.CreatedAt,
                    UpdatedAt = discussion.UpdatedAt,
                    State = discussion.State,
                    Metadata = new Dictionary<string, object>
                    {
                        { "category", discussion.Category },
                        { "commentCount", discussion.CommentCount }
                    }
                });
            }

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return new SemanticSearchResult
            {
                Success = true,
                Items = results.OrderByDescending(r => r.RelevanceScore),
                Query = query,
                Scope = SearchScope.Discussions,
                TotalResults = results.Count,
                SearchDurationMs = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing semantic search on discussions with query: {Query}", query);
            return new SemanticSearchResult
            {
                Success = false,
                Error = ex.Message,
                Query = query,
                Scope = SearchScope.Discussions
            };
        }
    }

    public async Task<SemanticSearchResult> SearchIssuesSemanticAsync(string query, string? repositoryFilter = null, int maxResults = 10)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var issues = await _issuesService.SearchIssuesAsync(query, repositoryFilter);
            var enhancedKeywords = await ExtractKeywordsAsync(query);
            
            var results = new List<SearchResultItem>();
            foreach (var issue in issues.Take(maxResults))
            {
                var relevanceScore = await CalculateRelevanceScoreAsync(query, enhancedKeywords, issue.Title, issue.Body);
                
                results.Add(new SearchResultItem
                {
                    Type = "issue",
                    Repository = issue.Repository,
                    Title = issue.Title,
                    Body = TruncateText(issue.Body, 500),
                    Url = issue.Url,
                    NodeId = issue.NodeId,
                    Number = issue.Number,
                    RelevanceScore = relevanceScore,
                    Author = issue.Author,
                    CreatedAt = issue.CreatedAt,
                    UpdatedAt = issue.UpdatedAt,
                    State = issue.State,
                    Labels = issue.Labels,
                    Metadata = new Dictionary<string, object>
                    {
                        { "assignees", issue.Assignees },
                        { "commentCount", issue.CommentCount }
                    }
                });
            }

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return new SemanticSearchResult
            {
                Success = true,
                Items = results.OrderByDescending(r => r.RelevanceScore),
                Query = query,
                Scope = SearchScope.Issues,
                TotalResults = results.Count,
                SearchDurationMs = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing semantic search on issues with query: {Query}", query);
            return new SemanticSearchResult
            {
                Success = false,
                Error = ex.Message,
                Query = query,
                Scope = SearchScope.Issues
            };
        }
    }

    public Task<string> ExtractKeywordsAsync(string content)
    {
        try
        {
            // Simple keyword extraction - can be enhanced with NLP libraries
            var words = Regex.Split(content.ToLowerInvariant(), @"\W+")
                .Where(w => w.Length > 3)
                .Where(w => !IsStopWord(w))
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key);

            return Task.FromResult(string.Join(" ", words));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting keywords from content");
            return Task.FromResult(content);
        }
    }

    public Task<double> CalculateSemanticSimilarityAsync(string text1, string text2)
    {
        try
        {
            // Simple similarity calculation - can be enhanced with ML models
            var words1 = ExtractWords(text1.ToLowerInvariant());
            var words2 = ExtractWords(text2.ToLowerInvariant());

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            if (union == 0) return Task.FromResult(0.0);

            // Jaccard similarity
            var jaccardSimilarity = (double)intersection / union;

            // Boost score if exact phrases are found
            var phraseBoost = 0.0;
            var phrases1 = ExtractPhrases(text1, 2);
            var phrases2 = ExtractPhrases(text2, 2);
            var commonPhrases = phrases1.Intersect(phrases2).Count();
            
            if (phrases1.Count > 0 && phrases2.Count > 0)
            {
                phraseBoost = (double)commonPhrases / Math.Max(phrases1.Count, phrases2.Count) * 0.3;
            }

            return Task.FromResult(Math.Min(1.0, jaccardSimilarity + phraseBoost));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating semantic similarity");
            return Task.FromResult(0.0);
        }
    }

    private async Task<IEnumerable<SearchResultItem>> SearchDiscussionsInternalAsync(string query, int maxResults)
    {
        var discussions = await _discussionsService.SearchDiscussionsAsync(query);
        var keywords = await ExtractKeywordsAsync(query);
        
        var results = new List<SearchResultItem>();
        foreach (var discussion in discussions.Take(maxResults))
        {
            var relevanceScore = await CalculateRelevanceScoreAsync(query, keywords, discussion.Title, discussion.Body);
            
            results.Add(new SearchResultItem
            {
                Type = "discussion",
                Repository = discussion.Repository,
                Title = discussion.Title,
                Body = TruncateText(discussion.Body, 300),
                Url = discussion.Url,
                NodeId = discussion.NodeId,
                Number = discussion.Number,
                RelevanceScore = relevanceScore,
                Author = discussion.Author,
                CreatedAt = discussion.CreatedAt,
                UpdatedAt = discussion.UpdatedAt,
                State = discussion.State
            });
        }

        return results;
    }

    private async Task<IEnumerable<SearchResultItem>> SearchIssuesInternalAsync(string query, int maxResults)
    {
        var issues = await _issuesService.SearchIssuesAsync(query);
        var keywords = await ExtractKeywordsAsync(query);
        
        var results = new List<SearchResultItem>();
        foreach (var issue in issues.Take(maxResults))
        {
            var relevanceScore = await CalculateRelevanceScoreAsync(query, keywords, issue.Title, issue.Body);
            
            results.Add(new SearchResultItem
            {
                Type = "issue",
                Repository = issue.Repository,
                Title = issue.Title,
                Body = TruncateText(issue.Body, 300),
                Url = issue.Url,
                NodeId = issue.NodeId,
                Number = issue.Number,
                RelevanceScore = relevanceScore,
                Author = issue.Author,
                CreatedAt = issue.CreatedAt,
                UpdatedAt = issue.UpdatedAt,
                State = issue.State,
                Labels = issue.Labels
            });
        }

        return results;
    }

    private async Task<IEnumerable<SearchResultItem>> SearchCodeAsync(string query, int maxResults)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var encodedQuery = Uri.EscapeDataString(query);
            var response = await _httpClient.GetAsync($"https://api.github.com/search/code?q={encodedQuery}&per_page={maxResults}");

            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<SearchResultItem>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<JsonElement>(content);
            
            var results = new List<SearchResultItem>();
            if (searchResult.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var title = item.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "";
                    var path = item.TryGetProperty("path", out var pathProp) ? pathProp.GetString() ?? "" : "";
                    var repo = item.TryGetProperty("repository", out var repoProp) && 
                              repoProp.TryGetProperty("full_name", out var repoName) ? repoName.GetString() ?? "" : "";

                    results.Add(new SearchResultItem
                    {
                        Type = "code",
                        Repository = repo,
                        Title = $"{title} ({path})",
                        Body = "",
                        Url = item.TryGetProperty("html_url", out var url) ? url.GetString() ?? "" : "",
                        RelevanceScore = 0.7, // Default relevance for code matches
                        Author = "",
                        CreatedAt = DateTime.MinValue,
                        UpdatedAt = DateTime.MinValue,
                        State = "file"
                    });
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching code with query: {Query}", query);
            return Array.Empty<SearchResultItem>();
        }
    }

    private async Task<IEnumerable<SearchResultItem>> SearchPullRequestsAsync(string query, int maxResults)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var searchQuery = $"{query} type:pr";
            var encodedQuery = Uri.EscapeDataString(searchQuery);
            var response = await _httpClient.GetAsync($"https://api.github.com/search/issues?q={encodedQuery}&per_page={maxResults}");

            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<SearchResultItem>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<JsonElement>(content);
            
            var results = new List<SearchResultItem>();
            if (searchResult.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    results.Add(new SearchResultItem
                    {
                        Type = "pull_request",
                        Repository = ExtractRepositoryFromUrl(item.TryGetProperty("repository_url", out var repoUrl) ? 
                            repoUrl.GetString() ?? "" : ""),
                        Title = item.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                        Body = TruncateText(item.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "", 300),
                        Url = item.TryGetProperty("html_url", out var url) ? url.GetString() ?? "" : "",
                        Number = item.TryGetProperty("number", out var number) ? number.GetInt32() : 0,
                        RelevanceScore = 0.6, // Default relevance for PR matches
                        Author = item.TryGetProperty("user", out var user) && user.TryGetProperty("login", out var login) ? 
                            login.GetString() ?? "" : "",
                        CreatedAt = item.TryGetProperty("created_at", out var created) ? created.GetDateTime() : DateTime.MinValue,
                        UpdatedAt = item.TryGetProperty("updated_at", out var updated) ? updated.GetDateTime() : DateTime.MinValue,
                        State = item.TryGetProperty("state", out var state) ? state.GetString() ?? "" : ""
                    });
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching pull requests with query: {Query}", query);
            return Array.Empty<SearchResultItem>();
        }
    }

    private async Task<double> CalculateRelevanceScoreAsync(string query, string keywords, string title, string body)
    {
        var titleSimilarity = await CalculateSemanticSimilarityAsync(query, title);
        var bodySimilarity = await CalculateSemanticSimilarityAsync(query, body);
        var keywordSimilarity = await CalculateSemanticSimilarityAsync(keywords, $"{title} {body}");

        // Weighted average: title gets highest weight, then keywords, then body
        return (titleSimilarity * 0.5) + (keywordSimilarity * 0.3) + (bodySimilarity * 0.2);
    }

    private static HashSet<string> ExtractWords(string text)
    {
        return new HashSet<string>(
            Regex.Split(text, @"\W+")
                .Where(w => w.Length > 2)
                .Where(w => !IsStopWord(w))
        );
    }

    private static List<string> ExtractPhrases(string text, int phraseLength)
    {
        var words = Regex.Split(text.ToLowerInvariant(), @"\W+")
            .Where(w => w.Length > 2)
            .ToList();

        var phrases = new List<string>();
        for (int i = 0; i <= words.Count - phraseLength; i++)
        {
            phrases.Add(string.Join(" ", words.Skip(i).Take(phraseLength)));
        }

        return phrases;
    }

    private static bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string>
        {
            "the", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
            "from", "up", "about", "into", "through", "during", "before", "after", "above",
            "below", "between", "among", "is", "are", "was", "were", "be", "been", "being",
            "have", "has", "had", "do", "does", "did", "will", "would", "could", "should",
            "may", "might", "must", "can", "shall", "this", "that", "these", "those"
        };

        return stopWords.Contains(word.ToLowerInvariant());
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength - 3) + "...";
    }

    private static string ExtractRepositoryFromUrl(string repositoryUrl)
    {
        var parts = repositoryUrl.Split('/');
        if (parts.Length >= 2)
        {
            return $"{parts[^2]}/{parts[^1]}";
        }
        return "";
    }
}