using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CopilotAgent.MCP.Tools.GitHub;

/// <summary>
/// Reynolds-powered tool for searching GitHub issues with supernatural accuracy and insight
/// </summary>
[McpServerToolType]
public static class SearchIssuesTool
{
    [McpServerTool, Description("Search across GitHub issues with Reynolds-powered semantic analysis and Maximum Effort™ precision")]
    public static async Task<object> SearchIssues(
        [Description("Search query for issues")] string query,
        [Description("Repository filter (owner/repo format, optional)")] string repository = "",
        [Description("Filter by labels (optional)")] string[] labels = null!,
        [Description("Issue state: 'open', 'closed', or 'all'")] string state = "all",
        [Description("Filter by assignee username (optional)")] string assignee = "",
        [Description("Filter by author username (optional)")] string author = "",
        [Description("Sort order: 'relevance', 'created', 'updated', 'comments'")] string sort = "relevance",
        [Description("Maximum number of results")] int limit = 20,
        [Description("Use semantic search for enhanced relevance")] bool useSemantic = true)
    {
        // Validate inputs
        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException("Query is required");
        }

        labels ??= Array.Empty<string>();

        await Task.CompletedTask; // Satisfy async requirement

        // Generate mock search results
        var mockResults = GenerateMockIssueResults(query, repository, labels, state, assignee, author, limit, useSemantic);

        // Apply sorting based on sort parameter
        var sortedResults = ApplySorting(mockResults, sort);

        return new
        {
            success = true,
            query = query,
            filters = new
            {
                repository = repository,
                labels = labels,
                state = state,
                assignee = assignee,
                author = author,
                sort = sort,
                use_semantic = useSemantic
            },
            results = sortedResults,
            total_found = sortedResults.Length,
            search_metadata = new
            {
                semantic_search_used = useSemantic && sort == "relevance",
                semantic_search_duration_ms = Random.Shared.Next(50, 300),
                relevance_enhanced = useSemantic,
                reynolds_processing = "Maximum Effort™ issue analysis applied"
            },
            analytics = GenerateSearchAnalytics(sortedResults),
            reynolds_insights = GenerateSearchInsights(sortedResults, query),
            reynolds_note = "Issue search completed with supernatural precision and project management insight",
            mock_data_notice = "This is mock data - actual GitHub API and semantic search integration required for production",
            timestamp = DateTime.UtcNow
        };
    }

    private static object[] GenerateMockIssueResults(string query, string repository, string[] labels, string state, string assignee, string author, int limit, bool useSemantic)
    {
        var results = new List<object>();
        var resultCount = Math.Min(limit, Random.Shared.Next(0, limit + 5));
        
        var mockRepositories = string.IsNullOrEmpty(repository) 
            ? new[] { "main-repo", "api-service", "frontend-app", "mobile-app", "docs-repo" }
            : new[] { repository };
            
        var mockLabels = labels.Any() ? labels : new[] { "bug", "enhancement", "documentation", "good first issue", "help wanted" };
        var mockStates = state == "all" ? new[] { "open", "closed" } : new[] { state };

        for (int i = 0; i < resultCount; i++)
        {
            var createdAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 180));
            var updatedAt = createdAt.AddDays(Random.Shared.Next(0, 30));
            var issueNumber = Random.Shared.Next(1, 10000);
            var selectedRepo = mockRepositories[Random.Shared.Next(mockRepositories.Length)];
            var selectedState = mockStates[Random.Shared.Next(mockStates.Length)];
            var commentCount = Random.Shared.Next(0, 50);
            var selectedLabels = mockLabels.OrderBy(x => Random.Shared.Next()).Take(Random.Shared.Next(0, 4)).ToArray();
            var assignees = GenerateMockAssignees(assignee);
            var issueAuthor = !string.IsNullOrEmpty(author) ? author : $"user-{Random.Shared.Next(1, 100)}";

            var title = GenerateMockIssueTitle(query, i + 1);
            var body = GenerateMockIssueBody(query, title);
            var relevanceScore = CalculateRelevanceScore(title, body, query, useSemantic);
            var priorityAssessment = AssessPriority(selectedLabels);
            var ageCategory = CategorizeAge(createdAt);

            results.Add(new
            {
                number = issueNumber,
                title = title,
                body = body.Length > 200 ? body.Substring(0, 200) + "..." : body,
                url = $"https://github.com/{selectedRepo}/issues/{issueNumber}",
                repository = selectedRepo,
                author = issueAuthor,
                state = selectedState,
                created_at = createdAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                updated_at = updatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                labels = selectedLabels,
                assignees = assignees,
                comment_count = commentCount,
                relevance_score = relevanceScore,
                priority_assessment = priorityAssessment,
                age_category = ageCategory
            });
        }

        return results.ToArray();
    }

    private static string[] GenerateMockAssignees(string specificAssignee)
    {
        if (!string.IsNullOrEmpty(specificAssignee))
        {
            return new[] { specificAssignee };
        }

        var assigneeCount = Random.Shared.Next(0, 3);
        var assignees = new List<string>();
        
        for (int i = 0; i < assigneeCount; i++)
        {
            assignees.Add($"assignee-{Random.Shared.Next(1, 50)}");
        }
        
        return assignees.ToArray();
    }

    private static string GenerateMockIssueTitle(string query, int index)
    {
        var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var primaryWord = queryWords.FirstOrDefault() ?? "issue";
        
        var titleTemplates = new[]
        {
            $"Fix {primaryWord} implementation bug",
            $"Feature request: Enhance {primaryWord}",
            $"Documentation update for {primaryWord}",
            $"Bug: {primaryWord} not working correctly",
            $"Improvement: Optimize {primaryWord} performance",
            $"Question: How to configure {primaryWord}",
            $"RFC: Standardize {primaryWord} approach",
            $"Security: {primaryWord} vulnerability found"
        };

        return $"{titleTemplates[Random.Shared.Next(titleTemplates.Length)]} #{index}";
    }

    private static string GenerateMockIssueBody(string query, string title)
    {
        var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var context = string.Join(" ", queryWords.Take(3));
        
        return $"This is a mock issue body about {context}. The issue describes a problem or request " +
               $"related to the search query. Original query context: '{query}'. " +
               $"This would contain detailed issue description, steps to reproduce, expected behavior, " +
               $"and actual behavior in a real implementation.";
    }

    private static double CalculateRelevanceScore(string title, string body, string query, bool useSemantic)
    {
        var score = 0.0;
        var queryLower = query.ToLowerInvariant();
        var titleLower = title.ToLowerInvariant();
        var bodyLower = body.ToLowerInvariant();

        // Title match is most important
        if (titleLower.Contains(queryLower)) score += 0.6;
        if (bodyLower.Contains(queryLower)) score += 0.3;
        
        // Word-level matching
        var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in queryWords)
        {
            var wordLower = word.ToLowerInvariant();
            if (titleLower.Contains(wordLower)) score += 0.1;
            if (bodyLower.Contains(wordLower)) score += 0.05;
        }

        // Semantic boost if enabled
        if (useSemantic)
        {
            score += Random.Shared.NextDouble() * 0.2; // Simulate semantic enhancement
        }

        return Math.Min(1.0, score);
    }

    private static string AssessPriority(string[] labels)
    {
        var priorityLabels = labels.Where(l => l.ToLowerInvariant().Contains("priority")).ToArray();
        if (priorityLabels.Any(l => l.ToLowerInvariant().Contains("critical") || l.ToLowerInvariant().Contains("high")))
            return "High";
        if (priorityLabels.Any(l => l.ToLowerInvariant().Contains("medium")))
            return "Medium";
        if (priorityLabels.Any(l => l.ToLowerInvariant().Contains("low")))
            return "Low";

        // Infer priority from labels
        if (labels.Any(l => l.ToLowerInvariant().Contains("bug") || l.ToLowerInvariant().Contains("security")))
            return "High";
        
        return "Normal";
    }

    private static string CategorizeAge(DateTime createdAt)
    {
        var days = (DateTime.UtcNow - createdAt).Days;
        return days switch
        {
            < 1 => "Fresh",
            < 7 => "Recent",
            < 30 => "Active",
            < 90 => "Mature",
            _ => "Aged"
        };
    }

    private static object[] ApplySorting(object[] results, string sort)
    {
        return sort switch
        {
            "created" => results.OrderByDescending(r => DateTime.Parse(((dynamic)r).created_at)).ToArray(),
            "updated" => results.OrderByDescending(r => DateTime.Parse(((dynamic)r).updated_at)).ToArray(),
            "comments" => results.OrderByDescending(r => ((dynamic)r).comment_count).ToArray(),
            _ => results.OrderByDescending(r => ((dynamic)r).relevance_score).ToArray() // relevance (default)
        };
    }

    private static object GenerateSearchAnalytics(object[] results)
    {
        var openCount = results.Count(r => ((dynamic)r).state == "open");
        var closedCount = results.Count(r => ((dynamic)r).state == "closed");
        
        var priorityCounts = new Dictionary<string, int>();
        foreach (var result in results)
        {
            var priority = ((dynamic)result).priority_assessment;
            if (priorityCounts.ContainsKey(priority))
                priorityCounts[priority]++;
            else
                priorityCounts[priority] = 1;
        }

        return new
        {
            total_results = results.Length,
            open_issues = openCount,
            closed_issues = closedCount,
            priority_distribution = priorityCounts,
            reynolds_efficiency_score = Math.Min(100, results.Length * 5) // Playful metric
        };
    }

    private static string GenerateSearchInsights(object[] results, string query)
    {
        if (!results.Any())
            return "No issues found - either excellent bug management or need to expand search criteria";

        var insights = new List<string>();

        if (results.Length == 1)
            insights.Add("Surgical precision - found exactly what you're looking for");
        else if (results.Length > 20)
            insights.Add($"Rich issue landscape - {results.Length} results found with supernatural accuracy");

        // Query-specific insights
        var queryLower = query.ToLowerInvariant();
        if (queryLower.Contains("bug"))
            insights.Add("Bug hunting mode activated - perfect for quality assurance efforts");
        else if (queryLower.Contains("feature"))
            insights.Add("Feature request analysis - great for product roadmap planning");
        else if (queryLower.Contains("documentation"))
            insights.Add("Documentation focus detected - knowledge sharing at its finest");
        else if (queryLower.Contains("security"))
            insights.Add("Security-related issues found - Maximum Effort™ protection protocols engaged");

        // State insights
        var openCount = results.Count(r => ((dynamic)r).state == "open");
        if (openCount == results.Length)
            insights.Add("All open issues - active development opportunities identified");
        else if (openCount == 0)
            insights.Add("All resolved issues - excellent closure rate achieved");

        // Priority insights
        var highPriority = results.Count(r => ((dynamic)r).priority_assessment == "High");
        if (highPriority > 0)
            insights.Add($"{highPriority} high-priority issues detected - immediate attention recommended");

        // Engagement insights
        var highEngagement = results.Count(r => ((dynamic)r).comment_count > 10);
        if (highEngagement > 0)
            insights.Add($"{highEngagement} highly discussed issues - community engagement at Van Wilder levels");

        return insights.Any() ? string.Join("; ", insights) : "Standard issue search with Maximum Effort™ relevance applied";
    }
}