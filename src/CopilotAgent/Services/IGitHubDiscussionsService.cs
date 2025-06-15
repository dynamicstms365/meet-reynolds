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

public interface IGitHubIssuePRSynchronizationService
{
    Task<IEnumerable<GitHubPullRequest>> GetPullRequestsByRepositoryAsync(string repository, string state = "all", int limit = 100);
    Task<GitHubPullRequest> GetPullRequestAsync(string repository, int pullRequestNumber);
    Task<IEnumerable<GitHubPullRequest>> FindPullRequestsLinkedToIssueAsync(string repository, int issueNumber);
    Task<IEnumerable<GitHubIssue>> FindIssuesLinkedToPullRequestAsync(string repository, int pullRequestNumber);
    Task<IssuePRSynchronizationReport> GenerateSynchronizationReportAsync(string repository);
    Task<bool> SynchronizeIssueWithPRsAsync(string repository, int issueNumber);
    Task<int> SynchronizeAllIssuesWithPRsAsync(string repository);
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

public class GitHubPullRequest
{
    public string NodeId { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty; // open, closed
    public bool IsMerged { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? MergedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string HeadBranch { get; set; } = string.Empty;
    public string BaseBranch { get; set; } = string.Empty;
    public string[] Labels { get; set; } = Array.Empty<string>();
    public string[] Assignees { get; set; } = Array.Empty<string>();
    public int[] LinkedIssueNumbers { get; set; } = Array.Empty<int>(); // Issues referenced in PR
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class IssuePRSynchronizationReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string Repository { get; set; } = string.Empty;
    public IEnumerable<IssuePRRelation> IssuePRRelations { get; set; } = new List<IssuePRRelation>();
    public IEnumerable<GitHubPullRequest> OrphanedPRs { get; set; } = new List<GitHubPullRequest>(); // PRs without linked issues
    public IEnumerable<GitHubIssue> OrphanedIssues { get; set; } = new List<GitHubIssue>(); // Issues without linked PRs
    public IssuePRSynchronizationSummary Summary { get; set; } = new();
}

public class IssuePRRelation
{
    public GitHubIssue Issue { get; set; } = new();
    public IEnumerable<GitHubPullRequest> RelatedPRs { get; set; } = new List<GitHubPullRequest>();
    public string SynchronizationStatus { get; set; } = string.Empty; // "synchronized", "needs_update", "conflict"
    public string RecommendedAction { get; set; } = string.Empty;
}

public class IssuePRSynchronizationSummary
{
    public int TotalIssues { get; set; }
    public int TotalPRs { get; set; }
    public int SynchronizedRelations { get; set; }
    public int NeedsUpdateRelations { get; set; }
    public int ConflictedRelations { get; set; }
    public int OrphanedPRs { get; set; }
    public int OrphanedIssues { get; set; }
}