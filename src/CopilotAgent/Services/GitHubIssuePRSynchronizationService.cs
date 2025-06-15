using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Shared.Models;

namespace CopilotAgent.Services;

public class GitHubIssuePRSynchronizationService : IGitHubIssuePRSynchronizationService
{
    private readonly IGitHubAppAuthService _authService;
    private readonly IGitHubIssuesService _issuesService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubIssuePRSynchronizationService> _logger;
    private readonly IConfiguration _configuration;

    public GitHubIssuePRSynchronizationService(
        IGitHubAppAuthService authService,
        IGitHubIssuesService issuesService,
        HttpClient httpClient,
        ILogger<GitHubIssuePRSynchronizationService> logger,
        IConfiguration configuration)
    {
        _authService = authService;
        _issuesService = issuesService;
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IEnumerable<GitHubPullRequest>> GetPullRequestsByRepositoryAsync(string repository, string state = "all", int limit = 100)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{repository}/pulls?state={state}&per_page={limit}&sort=updated&direction=desc");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get pull requests: {StatusCode}", response.StatusCode);
                return Array.Empty<GitHubPullRequest>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var pullRequestsArray = JsonSerializer.Deserialize<JsonElement[]>(content);
            
            var pullRequests = new List<GitHubPullRequest>();
            if (pullRequestsArray != null)
            {
                foreach (var prElement in pullRequestsArray)
                {
                    pullRequests.Add(ParsePullRequestFromJson(prElement));
                }
            }

            return pullRequests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pull requests from repository: {Repository}", repository);
            return Array.Empty<GitHubPullRequest>();
        }
    }

    public async Task<GitHubPullRequest> GetPullRequestAsync(string repository, int pullRequestNumber)
    {
        try
        {
            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{repository}/pulls/{pullRequestNumber}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get pull request: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to get pull request: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var prElement = JsonSerializer.Deserialize<JsonElement>(content);
            
            return ParsePullRequestFromJson(prElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pull request {Number} from repository: {Repository}", pullRequestNumber, repository);
            throw;
        }
    }

    public async Task<IEnumerable<GitHubPullRequest>> FindPullRequestsLinkedToIssueAsync(string repository, int issueNumber)
    {
        try
        {
            // Get all pull requests for the repository
            var allPRs = await GetPullRequestsByRepositoryAsync(repository, "all", 200);
            
            // Filter PRs that reference the issue number
            var linkedPRs = allPRs.Where(pr => 
                pr.LinkedIssueNumbers.Contains(issueNumber) ||
                ContainsIssueReference(pr.Title, issueNumber) ||
                ContainsIssueReference(pr.Body, issueNumber)
            ).ToList();

            _logger.LogInformation("Found {Count} pull requests linked to issue #{IssueNumber} in {Repository}", 
                linkedPRs.Count, issueNumber, repository);

            return linkedPRs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding pull requests linked to issue #{IssueNumber} in repository: {Repository}", 
                issueNumber, repository);
            return Array.Empty<GitHubPullRequest>();
        }
    }

    public async Task<IEnumerable<GitHubIssue>> FindIssuesLinkedToPullRequestAsync(string repository, int pullRequestNumber)
    {
        try
        {
            // Get the pull request details
            var pullRequest = await GetPullRequestAsync(repository, pullRequestNumber);
            
            // Extract issue numbers from PR title and body
            var issueNumbers = ExtractIssueNumbers(pullRequest.Title)
                .Concat(ExtractIssueNumbers(pullRequest.Body))
                .Distinct()
                .ToList();

            // Get the issues
            var linkedIssues = new List<GitHubIssue>();
            foreach (var issueNumber in issueNumbers)
            {
                try
                {
                    var issue = await _issuesService.GetIssueAsync(repository, issueNumber);
                    linkedIssues.Add(issue);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch issue #{IssueNumber} referenced in PR #{PRNumber}", 
                        issueNumber, pullRequestNumber);
                }
            }

            _logger.LogInformation("Found {Count} issues linked to pull request #{PRNumber} in {Repository}", 
                linkedIssues.Count, pullRequestNumber, repository);

            return linkedIssues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding issues linked to pull request #{PRNumber} in repository: {Repository}", 
                pullRequestNumber, repository);
            return Array.Empty<GitHubIssue>();
        }
    }

    public async Task<IssuePRSynchronizationReport> GenerateSynchronizationReportAsync(string repository)
    {
        try
        {
            _logger.LogInformation("Generating synchronization report for repository: {Repository}", repository);

            var report = new IssuePRSynchronizationReport
            {
                Repository = repository,
                GeneratedAt = DateTime.UtcNow
            };

            // Get all issues and PRs
            var issues = await _issuesService.GetIssuesByRepositoryAsync(repository, "all", 200);
            var pullRequests = await GetPullRequestsByRepositoryAsync(repository, "all", 200);

            var issuePRRelations = new List<IssuePRRelation>();
            var orphanedPRs = new List<GitHubPullRequest>();
            var orphanedIssues = new List<GitHubIssue>();

            // Track which PRs and issues have been linked
            var linkedPRNumbers = new HashSet<int>();
            var linkedIssueNumbers = new HashSet<int>();

            // Build issue-PR relationships
            foreach (var issue in issues)
            {
                var relatedPRs = await FindPullRequestsLinkedToIssueAsync(repository, issue.Number);
                var relatedPRsList = relatedPRs.ToList();

                if (relatedPRsList.Any())
                {
                    // Mark PRs and issue as linked
                    foreach (var pr in relatedPRsList)
                    {
                        linkedPRNumbers.Add(pr.Number);
                    }
                    linkedIssueNumbers.Add(issue.Number);

                    // Determine synchronization status
                    var syncStatus = DetermineSynchronizationStatus(issue, relatedPRsList);
                    var recommendedAction = DetermineRecommendedAction(issue, relatedPRsList);

                    issuePRRelations.Add(new IssuePRRelation
                    {
                        Issue = issue,
                        RelatedPRs = relatedPRsList,
                        SynchronizationStatus = syncStatus,
                        RecommendedAction = recommendedAction
                    });
                }
            }

            // Find orphaned PRs (PRs without linked issues)
            orphanedPRs.AddRange(pullRequests.Where(pr => !linkedPRNumbers.Contains(pr.Number)));

            // Find orphaned issues (issues without linked PRs)
            orphanedIssues.AddRange(issues.Where(issue => !linkedIssueNumbers.Contains(issue.Number)));

            // Build summary
            var summary = new IssuePRSynchronizationSummary
            {
                TotalIssues = issues.Count(),
                TotalPRs = pullRequests.Count(),
                SynchronizedRelations = issuePRRelations.Count(r => r.SynchronizationStatus == "synchronized"),
                NeedsUpdateRelations = issuePRRelations.Count(r => r.SynchronizationStatus == "needs_update"),
                ConflictedRelations = issuePRRelations.Count(r => r.SynchronizationStatus == "conflict"),
                OrphanedPRs = orphanedPRs.Count,
                OrphanedIssues = orphanedIssues.Count
            };

            report.IssuePRRelations = issuePRRelations;
            report.OrphanedPRs = orphanedPRs;
            report.OrphanedIssues = orphanedIssues;
            report.Summary = summary;

            _logger.LogInformation("Synchronization report generated for {Repository}: {SynchronizedCount} synchronized, {NeedsUpdateCount} need updates, {ConflictCount} conflicts, {OrphanedPRCount} orphaned PRs, {OrphanedIssueCount} orphaned issues",
                repository, summary.SynchronizedRelations, summary.NeedsUpdateRelations, summary.ConflictedRelations, summary.OrphanedPRs, summary.OrphanedIssues);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating synchronization report for repository: {Repository}", repository);
            throw;
        }
    }

    public async Task<bool> SynchronizeIssueWithPRsAsync(string repository, int issueNumber)
    {
        try
        {
            _logger.LogInformation("Synchronizing issue #{IssueNumber} with related PRs in {Repository}", issueNumber, repository);

            var issue = await _issuesService.GetIssueAsync(repository, issueNumber);
            var relatedPRs = await FindPullRequestsLinkedToIssueAsync(repository, issueNumber);
            var relatedPRsList = relatedPRs.ToList();

            if (!relatedPRsList.Any())
            {
                _logger.LogInformation("No related PRs found for issue #{IssueNumber}", issueNumber);
                return true; // Nothing to synchronize
            }

            // Determine the appropriate issue state based on PR states
            var newIssueState = DetermineIssueStateFromPRs(relatedPRsList);
            var currentIssueState = issue.State.ToLowerInvariant();

            if (newIssueState != currentIssueState)
            {
                // Update the issue state
                await _issuesService.UpdateIssueAsync(repository, issueNumber, state: newIssueState);
                
                // Add a comment explaining the synchronization
                var comment = GenerateSynchronizationComment(relatedPRsList, currentIssueState, newIssueState);
                await _issuesService.AddIssueCommentAsync(repository, issueNumber, comment);

                _logger.LogInformation("Updated issue #{IssueNumber} state from '{OldState}' to '{NewState}' based on related PRs", 
                    issueNumber, currentIssueState, newIssueState);
                
                return true;
            }
            else
            {
                _logger.LogInformation("Issue #{IssueNumber} is already synchronized with related PRs", issueNumber);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing issue #{IssueNumber} with PRs in repository: {Repository}", 
                issueNumber, repository);
            return false;
        }
    }

    public async Task<int> SynchronizeAllIssuesWithPRsAsync(string repository)
    {
        try
        {
            _logger.LogInformation("Starting synchronization of all issues with PRs in repository: {Repository}", repository);

            var issues = await _issuesService.GetIssuesByRepositoryAsync(repository, "all", 200);
            var synchronizedCount = 0;

            foreach (var issue in issues)
            {
                var success = await SynchronizeIssueWithPRsAsync(repository, issue.Number);
                if (success)
                {
                    synchronizedCount++;
                }
                
                // Add a small delay to avoid rate limiting
                await Task.Delay(100);
            }

            _logger.LogInformation("Synchronized {Count} issues with their related PRs in repository: {Repository}", 
                synchronizedCount, repository);

            return synchronizedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing all issues with PRs in repository: {Repository}", repository);
            throw;
        }
    }

    private static GitHubPullRequest ParsePullRequestFromJson(JsonElement prElement)
    {
        var labels = new List<string>();
        if (prElement.TryGetProperty("labels", out var labelsArray))
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
        if (prElement.TryGetProperty("assignees", out var assigneesArray))
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
        if (prElement.TryGetProperty("base", out var baseInfo) && 
            baseInfo.TryGetProperty("repo", out var repoInfo) &&
            repoInfo.TryGetProperty("full_name", out var fullName))
        {
            repository = fullName.GetString() ?? "";
        }

        var title = prElement.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";
        var body = prElement.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
        var linkedIssueNumbers = ExtractIssueNumbers(title).Concat(ExtractIssueNumbers(body)).Distinct().ToArray();

        return new GitHubPullRequest
        {
            NodeId = prElement.TryGetProperty("node_id", out var nodeId) ? nodeId.GetString() ?? "" : "",
            Number = prElement.TryGetProperty("number", out var number) ? number.GetInt32() : 0,
            Title = title,
            Body = body,
            Url = prElement.TryGetProperty("html_url", out var htmlUrl) ? htmlUrl.GetString() ?? "" : "",
            Repository = repository,
            Author = prElement.TryGetProperty("user", out var user) && user.TryGetProperty("login", out var authorLogin) ? 
                authorLogin.GetString() ?? "" : "",
            State = prElement.TryGetProperty("state", out var state) ? state.GetString() ?? "" : "",
            IsMerged = prElement.TryGetProperty("merged", out var merged) ? merged.GetBoolean() : false,
            CreatedAt = prElement.TryGetProperty("created_at", out var created) ? created.GetDateTime() : DateTime.MinValue,
            UpdatedAt = prElement.TryGetProperty("updated_at", out var updated) ? updated.GetDateTime() : DateTime.MinValue,
            MergedAt = prElement.TryGetProperty("merged_at", out var mergedAt) && !mergedAt.ValueKind.Equals(JsonValueKind.Null) ? 
                mergedAt.GetDateTime() : null,
            ClosedAt = prElement.TryGetProperty("closed_at", out var closedAt) && !closedAt.ValueKind.Equals(JsonValueKind.Null) ? 
                closedAt.GetDateTime() : null,
            HeadBranch = prElement.TryGetProperty("head", out var head) && head.TryGetProperty("ref", out var headRef) ? 
                headRef.GetString() ?? "" : "",
            BaseBranch = prElement.TryGetProperty("base", out var baseBranch) && baseBranch.TryGetProperty("ref", out var baseRef) ? 
                baseRef.GetString() ?? "" : "",
            Labels = labels.ToArray(),
            Assignees = assignees.ToArray(),
            LinkedIssueNumbers = linkedIssueNumbers
        };
    }

    private static bool ContainsIssueReference(string text, int issueNumber)
    {
        if (string.IsNullOrEmpty(text)) return false;
        
        // Look for patterns like #123, fixes #123, closes #123, resolves #123
        var patterns = new[]
        {
            $@"#\s*{issueNumber}\b",
            $@"\b(?:fix|fixes|fixed|close|closes|closed|resolve|resolves|resolved)\s+#\s*{issueNumber}\b"
        };

        return patterns.Any(pattern => Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase));
    }

    private static IEnumerable<int> ExtractIssueNumbers(string text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<int>();

        var issueNumbers = new List<int>();
        
        // Pattern to match issue references like #123, fixes #123, closes #123, etc.
        var pattern = @"(?:#|(?:fix|fixes|fixed|close|closes|closed|resolve|resolves|resolved)\s+#)\s*(\d+)";
        var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out var issueNumber))
            {
                issueNumbers.Add(issueNumber);
            }
        }

        return issueNumbers.Distinct();
    }

    private static string DetermineSynchronizationStatus(GitHubIssue issue, IList<GitHubPullRequest> relatedPRs)
    {
        var issueState = issue.State.ToLowerInvariant();
        var expectedState = DetermineIssueStateFromPRs(relatedPRs);

        if (issueState == expectedState)
        {
            return "synchronized";
        }

        // Check for conflicts (multiple PRs with different states)
        var prStates = relatedPRs.Select(pr => pr.IsMerged ? "merged" : pr.State.ToLowerInvariant()).Distinct().ToList();
        if (prStates.Count > 1 && prStates.Contains("merged") && prStates.Contains("open"))
        {
            return "conflict";
        }

        return "needs_update";
    }

    private static string DetermineRecommendedAction(GitHubIssue issue, IList<GitHubPullRequest> relatedPRs)
    {
        var issueState = issue.State.ToLowerInvariant();
        var expectedState = DetermineIssueStateFromPRs(relatedPRs);

        if (issueState == expectedState)
        {
            return "No action needed - issue is synchronized";
        }

        var mergedPRs = relatedPRs.Where(pr => pr.IsMerged).ToList();
        var openPRs = relatedPRs.Where(pr => pr.State.ToLowerInvariant() == "open").ToList();
        var closedPRs = relatedPRs.Where(pr => pr.State.ToLowerInvariant() == "closed" && !pr.IsMerged).ToList();

        if (mergedPRs.Any() && issueState == "open")
        {
            return $"Close issue - {mergedPRs.Count} related PR(s) have been merged";
        }

        if (openPRs.Any() && issueState == "closed")
        {
            return $"Reopen issue - {openPRs.Count} related PR(s) are still open";
        }

        if (closedPRs.Any() && !openPRs.Any() && !mergedPRs.Any() && issueState == "open")
        {
            return $"Consider closing issue - all {closedPRs.Count} related PR(s) have been closed";
        }

        return "Review manually - complex state relationship";
    }

    private static string DetermineIssueStateFromPRs(IList<GitHubPullRequest> relatedPRs)
    {
        if (!relatedPRs.Any()) return "open";

        // If any PR is merged, issue should be closed
        if (relatedPRs.Any(pr => pr.IsMerged))
        {
            return "closed";
        }

        // If any PR is open, issue should remain open
        if (relatedPRs.Any(pr => pr.State.ToLowerInvariant() == "open"))
        {
            return "open";
        }

        // If all PRs are closed (but not merged), issue should be closed
        return "closed";
    }

    private static string GenerateSynchronizationComment(IList<GitHubPullRequest> relatedPRs, string oldState, string newState)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🤖 **Automated Issue Synchronization**");
        sb.AppendLine();
        sb.AppendLine($"This issue has been automatically updated from `{oldState}` to `{newState}` based on the status of related pull requests:");
        sb.AppendLine();

        foreach (var pr in relatedPRs)
        {
            var status = pr.IsMerged ? "✅ Merged" : pr.State.ToLowerInvariant() == "open" ? "🔄 Open" : "❌ Closed";
            sb.AppendLine($"- {status}: #{pr.Number} - {pr.Title}");
        }

        sb.AppendLine();
        sb.AppendLine("*This synchronization was performed by the Reynolds Copilot agent to maintain consistency between issues and pull requests.*");

        return sb.ToString();
    }
}