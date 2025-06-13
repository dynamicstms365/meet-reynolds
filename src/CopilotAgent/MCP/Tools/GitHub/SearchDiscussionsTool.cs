using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CopilotAgent.MCP.Tools.GitHub;

/// <summary>
/// Reynolds-powered tool for searching GitHub discussions with supernatural precision
/// </summary>
[McpServerToolType]
public static class SearchDiscussionsTool
{
    [McpServerTool, Description("Search across GitHub discussions with Reynolds-powered semantic analysis and supernatural relevance")]
    public static async Task<object> SearchDiscussions(
        [Description("Search query for discussions")] string query,
        [Description("Repository filter (owner/repo format, optional)")] string repository = "",
        [Description("Discussion category filter (optional)")] string category = "",
        [Description("Discussion state: 'open', 'closed', or 'all'")] string state = "all",
        [Description("Sort order: 'relevance', 'created', 'updated'")] string sort = "relevance",
        [Description("Maximum number of results")] int limit = 20,
        [Description("Use semantic search for better relevance")] bool useSemantic = true)
    {
        // Validate inputs
        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException("Query is required");
        }

        await Task.CompletedTask; // Satisfy async requirement

        // Generate mock search results
        var mockResults = GenerateMockDiscussionResults(query, repository, category, state, limit, useSemantic);

        // Apply sorting based on sort parameter
        var sortedResults = ApplySorting(mockResults, sort);

        return new
        {
            success = true,
            query = query,
            filters = new
            {
                repository = repository,
                category = category,
                state = state,
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
                reynolds_processing = "Maximum Effort™ semantic analysis applied"
            },
            reynolds_insights = GenerateSearchInsights(sortedResults, query),
            reynolds_note = "Discussion search completed with supernatural precision and relevance",
            mock_data_notice = "This is mock data - actual GitHub API and semantic search integration required for production",
            timestamp = DateTime.UtcNow
        };
    }

    private static object[] GenerateMockDiscussionResults(string query, string repository, string category, string state, int limit, bool useSemantic)
    {
        var results = new List<object>();
        var resultCount = Math.Min(limit, Random.Shared.Next(0, limit + 5));
        
        var mockRepositories = string.IsNullOrEmpty(repository) 
            ? new[] { "main-repo", "docs-repo", "community-repo", "api-repo", "tools-repo" }
            : new[] { repository };
            
        var mockCategories = string.IsNullOrEmpty(category)
            ? new[] { "General", "Ideas", "Q&A", "Show and tell", "Announcements" }
            : new[] { category };
            
        var mockStates = state == "all" 
            ? new[] { "OPEN", "CLOSED" }
            : new[] { state.ToUpper() };

        for (int i = 0; i < resultCount; i++)
        {
            var createdAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 180));
            var updatedAt = createdAt.AddDays(Random.Shared.Next(0, 30));
            var discussionNumber = Random.Shared.Next(1, 500);
            var selectedRepo = mockRepositories[Random.Shared.Next(mockRepositories.Length)];
            var selectedCategory = mockCategories[Random.Shared.Next(mockCategories.Length)];
            var selectedState = mockStates[Random.Shared.Next(mockStates.Length)];
            var commentCount = Random.Shared.Next(0, 25);

            var title = GenerateMockTitle(query, i + 1);
            var body = GenerateMockBody(query, title);
            var relevanceScore = CalculateRelevanceScore(title, body, query, useSemantic);

            results.Add(new
            {
                number = discussionNumber,
                title = title,
                body = body.Length > 200 ? body.Substring(0, 200) + "..." : body,
                url = $"https://github.com/{selectedRepo}/discussions/{discussionNumber}",
                repository = selectedRepo,
                author = new
                {
                    login = $"user-{Random.Shared.Next(1, 100)}",
                    id = Random.Shared.Next(10000, 99999),
                    type = "User"
                },
                category = selectedCategory,
                state = selectedState,
                created_at = createdAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                updated_at = updatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                comment_count = commentCount,
                relevance_score = relevanceScore
            });
        }

        return results.ToArray();
    }

    private static string GenerateMockTitle(string query, int index)
    {
        var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var primaryWord = queryWords.FirstOrDefault() ?? "discussion";
        
        var titleTemplates = new[]
        {
            $"Discussion about {primaryWord} implementation",
            $"How to handle {primaryWord} in our project",
            $"Best practices for {primaryWord}",
            $"Question about {primaryWord} approach",
            $"Proposal: Improve {primaryWord} workflow",
            $"Community input needed on {primaryWord}",
            $"RFC: {primaryWord} standardization",
            $"Help with {primaryWord} configuration"
        };

        return $"{titleTemplates[Random.Shared.Next(titleTemplates.Length)]} #{index}";
    }

    private static string GenerateMockBody(string query, string title)
    {
        var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var context = string.Join(" ", queryWords.Take(3));
        
        return $"This is a mock discussion body about {context}. The discussion covers various aspects " +
               $"related to the query and provides community insights. Original query context: '{query}'. " +
               $"This would contain detailed discussion content in a real implementation.";
    }

    private static double CalculateRelevanceScore(string title, string body, string query, bool useSemantic)
    {
        // Simple relevance scoring - in a real implementation this would be more sophisticated
        var score = 0.0;
        var queryLower = query.ToLowerInvariant();
        var titleLower = title.ToLowerInvariant();
        var bodyLower = body.ToLowerInvariant();

        // Base scoring
        if (titleLower.Contains(queryLower)) score += 0.5;
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

    private static object[] ApplySorting(object[] results, string sort)
    {
        return sort switch
        {
            "created" => results.OrderByDescending(r => DateTime.Parse(((dynamic)r).created_at)).ToArray(),
            "updated" => results.OrderByDescending(r => DateTime.Parse(((dynamic)r).updated_at)).ToArray(),
            _ => results.OrderByDescending(r => ((dynamic)r).relevance_score).ToArray() // relevance (default)
        };
    }

    private static string GenerateSearchInsights(object[] results, string query)
    {
        if (!results.Any())
            return "No discussions found - might need to adjust search terms or expand scope";

        var insights = new List<string>();

        if (results.Length == 1)
            insights.Add("Found the needle in the haystack - one perfect match");
        else if (results.Length > 15)
            insights.Add($"Rich discussion landscape - {results.Length} relevant conversations found");

        // Add query-specific insights
        var queryLower = query.ToLowerInvariant();
        if (queryLower.Contains("bug"))
            insights.Add("Bug-related discussions detected - perfect for issue triage");
        else if (queryLower.Contains("feature"))
            insights.Add("Feature discussions found - great for product planning");
        else if (queryLower.Contains("help") || queryLower.Contains("question"))
            insights.Add("Help-seeking discussions identified - community support opportunities");
        else if (queryLower.Contains("proposal") || queryLower.Contains("rfc"))
            insights.Add("Proposal discussions located - strategic planning insights available");

        // Engagement insights
        var highEngagement = results.Count(r => ((dynamic)r).comment_count > 10);
        if (highEngagement > 0)
            insights.Add($"{highEngagement} highly engaged discussions - Van Wilder-level community participation");

        // Recency insights
        var recentDiscussions = results.Count(r => 
            (DateTime.UtcNow - DateTime.Parse(((dynamic)r).updated_at)).Days < 7);
        if (recentDiscussions > 0)
            insights.Add($"{recentDiscussions} recently active discussions - fresh community energy");

        return insights.Any() ? string.Join("; ", insights) : "Standard search results with supernatural relevance applied";
    }
}