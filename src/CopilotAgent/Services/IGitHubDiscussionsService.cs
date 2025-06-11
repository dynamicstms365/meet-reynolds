using System.Text.Json;
using Shared.Models;

namespace CopilotAgent.Services;

public interface IGitHubDiscussionsService
{
    Task<IEnumerable<GitHubDiscussion>> SearchDiscussionsAsync(string query, string? repositoryFilter = null);
    Task<GitHubDiscussion> CreateDiscussionAsync(string repository, string title, string body, string category);
    Task<GitHubComment> AddDiscussionCommentAsync(string repository, string discussionNodeId, string body);
    Task<GitHubDiscussion> UpdateDiscussionAsync(string repository, string discussionNodeId, string? title = null, string? body = null);
    Task<GitHubDiscussion> GetDiscussionAsync(string repository, int discussionNumber);
    Task<IEnumerable<GitHubDiscussion>> GetDiscussionsByRepositoryAsync(string repository, int limit = 50);
    Task<IEnumerable<GitHubDiscussion>> GetOrganizationDiscussionsAsync(string organization, int limit = 100);
}

public interface IGitHubIssuesService
{
    Task<IEnumerable<GitHubIssue>> SearchIssuesAsync(string query, string? repositoryFilter = null);
    Task<GitHubIssue> CreateIssueAsync(string repository, string title, string body, string[]? labels = null, string[]? assignees = null);
    Task<GitHubComment> AddIssueCommentAsync(string repository, int issueNumber, string body);
    Task<GitHubIssue> UpdateIssueAsync(string repository, int issueNumber, string? title = null, string? body = null, string? state = null, string[]? labels = null);
    Task<GitHubIssue> GetIssueAsync(string repository, int issueNumber);
    Task<IEnumerable<GitHubIssue>> GetIssuesByRepositoryAsync(string repository, string state = "open", int limit = 50);
    Task<IEnumerable<GitHubIssue>> GetOrganizationIssuesAsync(string organization, string state = "open", int limit = 100);
}

public interface IGitHubSemanticSearchService
{
    Task<SemanticSearchResult> SearchAcrossRepositoriesAsync(string query, SearchScope scope = SearchScope.All, int maxResults = 20);
    Task<SemanticSearchResult> SearchDiscussionsSemanticAsync(string query, string? repositoryFilter = null, int maxResults = 10);
    Task<SemanticSearchResult> SearchIssuesSemanticAsync(string query, string? repositoryFilter = null, int maxResults = 10);
    Task<string> ExtractKeywordsAsync(string content);
    Task<double> CalculateSemanticSimilarityAsync(string text1, string text2);
}

public enum SearchScope
{
    All,
    Discussions,
    Issues,
    Code,
    PullRequests
}

public class SemanticSearchResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public IEnumerable<SearchResultItem> Items { get; set; } = new List<SearchResultItem>();
    public string Query { get; set; } = string.Empty;
    public SearchScope Scope { get; set; }
    public int TotalResults { get; set; }
    public double SearchDurationMs { get; set; }
}

public class SearchResultItem
{
    public string Type { get; set; } = string.Empty; // "discussion", "issue", "code", etc.
    public string Repository { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public int? Number { get; set; }
    public double RelevanceScore { get; set; }
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string[] Labels { get; set; } = Array.Empty<string>();
    public string State { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class GitHubDiscussion
{
    public string NodeId { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CommentCount { get; set; }
    public IEnumerable<GitHubComment> Comments { get; set; } = new List<GitHubComment>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class GitHubIssue
{
    public string NodeId { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string[] Labels { get; set; } = Array.Empty<string>();
    public string[] Assignees { get; set; } = Array.Empty<string>();
    public int CommentCount { get; set; }
    public IEnumerable<GitHubComment> Comments { get; set; } = new List<GitHubComment>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class GitHubComment
{
    public string NodeId { get; set; } = string.Empty;
    public long Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}