using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CopilotAgent.MCP.Tools.GitHub;

/// <summary>
/// Reynolds-powered tool for organization-wide GitHub discussion analysis with supernatural insight
/// </summary>
[McpServerToolType]
public static class OrganizationDiscussionsTool
{
    [McpServerTool, Description("Analyze GitHub discussions across the entire organization with Reynolds-level strategic insight and community intelligence")]
    public static async Task<object> AnalyzeOrganizationDiscussions(
        [Description("Organization name")] string organization = "dynamicstms365",
        [Description("Analysis type: 'overview', 'trends', 'engagement', 'topics'")] string analysisType = "overview",
        [Description("Time range: 'week', 'month', 'quarter', 'all'")] string timeRange = "month",
        [Description("Filter by discussion categories (optional)")] string[] categoryFilter = null!,
        [Description("Include detailed engagement metrics")] bool includeMetrics = true,
        [Description("Maximum discussions to analyze")] int limit = 100)
    {
        // Validate inputs
        if (string.IsNullOrEmpty(organization))
        {
            throw new ArgumentException("Organization is required");
        }

        categoryFilter ??= Array.Empty<string>();

        await Task.CompletedTask; // Satisfy async requirement

        // Mock organization discussions data
        var totalDiscussions = Random.Shared.Next(20, 200);
        var mockDiscussions = GenerateMockDiscussions(totalDiscussions, timeRange, categoryFilter);

        // Generate analysis based on type
        var analysis = analysisType switch
        {
            "trends" => GenerateTrendAnalysis(mockDiscussions, timeRange),
            "engagement" => GenerateEngagementAnalysis(mockDiscussions),
            "topics" => GenerateTopicAnalysis(mockDiscussions),
            _ => GenerateOverviewAnalysis(mockDiscussions)
        };

        // Calculate metrics if requested
        object? metrics = null;
        if (includeMetrics)
        {
            metrics = CalculateOrganizationMetrics(mockDiscussions, timeRange);
        }

        return new
        {
            success = true,
            organization = organization,
            analysis_type = analysisType,
            time_range = timeRange,
            filters = new
            {
                categories = categoryFilter,
                total_analyzed = mockDiscussions.Count
            },
            analysis = analysis,
            metrics = metrics,
            reynolds_insights = GenerateReynoldsOrganizationalInsights(mockDiscussions, analysisType),
            community_health = AssessCommunityHealth(mockDiscussions),
            strategic_recommendations = GenerateStrategicRecommendations(mockDiscussions, organization),
            reynolds_note = "Organization discussion analysis completed with supernatural community intelligence",
            mock_data_notice = "This is mock data - actual GitHub API integration required for production",
            timestamp = DateTime.UtcNow
        };
    }

    private static List<dynamic> GenerateMockDiscussions(int count, string timeRange, string[] categoryFilter)
    {
        var discussions = new List<dynamic>();
        var categories = categoryFilter.Any() ? categoryFilter : new[] { "General", "Ideas", "Q&A", "Show and tell", "Announcements" };
        var repositories = new[] { "main-repo", "docs-repo", "api-repo", "tools-repo", "community-repo" };

        var cutoffDate = timeRange switch
        {
            "week" => DateTime.UtcNow.AddDays(-7),
            "month" => DateTime.UtcNow.AddDays(-30),
            "quarter" => DateTime.UtcNow.AddDays(-90),
            _ => DateTime.UtcNow.AddYears(-1)
        };

        for (int i = 0; i < count; i++)
        {
            var createdAt = Random.Shared.Next(0, 2) == 0 ? 
                cutoffDate.AddDays(Random.Shared.Next(0, (DateTime.UtcNow - cutoffDate).Days)) :
                DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 365));

            discussions.Add(new
            {
                Number = Random.Shared.Next(1, 1000),
                Title = $"Mock Discussion #{Random.Shared.Next(1, 1000)}: Community Topic",
                Body = "This is a mock discussion body with community insights and collaboration.",
                Repository = repositories[Random.Shared.Next(repositories.Length)],
                Category = categories[Random.Shared.Next(categories.Length)],
                Author = $"user-{Random.Shared.Next(1, 100)}",
                State = Random.Shared.Next(0, 10) > 8 ? "CLOSED" : "OPEN",
                CreatedAt = createdAt,
                UpdatedAt = createdAt.AddDays(Random.Shared.Next(0, 14)),
                CommentCount = Random.Shared.Next(0, 30)
            });
        }

        return discussions;
    }

    private static object GenerateOverviewAnalysis(List<dynamic> discussions)
    {
        var repositories = discussions.Select(d => d.Repository).Distinct().ToArray();
        var categories = discussions.Select(d => d.Category).Distinct().ToArray();

        return new
        {
            total_discussions = discussions.Count,
            active_repositories = repositories.Length,
            discussion_categories = categories,
            top_repositories = repositories.Take(10).ToArray(),
            recent_activity = discussions
                .OrderByDescending(d => d.UpdatedAt)
                .Take(5)
                .Select(d => new { d.Repository, d.Title, d.UpdatedAt })
                .ToArray(),
            overview_summary = "Comprehensive organization discussion landscape analysis"
        };
    }

    private static object GenerateTrendAnalysis(List<dynamic> discussions, string timeRange)
    {
        var groupedByDate = discussions
            .GroupBy(d => ((DateTime)d.CreatedAt).Date)
            .OrderBy(g => g.Key)
            .ToArray();

        return new
        {
            trend_period = timeRange,
            daily_activity = groupedByDate.Select(g => new
            {
                date = g.Key,
                discussion_count = g.Count(),
                total_comments = g.Sum(d => (int)d.CommentCount)
            }).ToArray(),
            peak_activity_day = groupedByDate.OrderByDescending(g => g.Count()).FirstOrDefault()?.Key,
            trend_direction = CalculateTrendDirection(groupedByDate),
            category_trends = discussions
                .GroupBy(d => d.Category)
                .Select(g => new { category = g.Key, count = g.Count() })
                .OrderByDescending(g => g.count)
                .ToArray()
        };
    }

    private static object GenerateEngagementAnalysis(List<dynamic> discussions)
    {
        return new
        {
            total_comments = discussions.Sum(d => (int)d.CommentCount),
            average_comments_per_discussion = discussions.Average(d => (int)d.CommentCount),
            highly_engaged_discussions = discussions
                .Where(d => (int)d.CommentCount > 5)
                .OrderByDescending(d => (int)d.CommentCount)
                .Take(10)
                .Select(d => new { d.Repository, d.Title, d.CommentCount })
                .ToArray(),
            engagement_distribution = new
            {
                no_comments = discussions.Count(d => (int)d.CommentCount == 0),
                low_engagement = discussions.Count(d => (int)d.CommentCount >= 1 && (int)d.CommentCount <= 3),
                medium_engagement = discussions.Count(d => (int)d.CommentCount >= 4 && (int)d.CommentCount <= 10),
                high_engagement = discussions.Count(d => (int)d.CommentCount > 10)
            }
        };
    }

    private static object GenerateTopicAnalysis(List<dynamic> discussions)
    {
        var commonTopics = new[] { "feature", "bug", "documentation", "performance", "security", "ui", "api", "database", "testing", "deployment" };

        return new
        {
            common_topics = commonTopics.Take(20).ToArray(),
            category_breakdown = discussions
                .GroupBy(d => d.Category)
                .Select(g => new
                {
                    category = g.Key,
                    count = g.Count(),
                    sample_titles = g.Take(3).Select(d => d.Title).ToArray()
                })
                .OrderByDescending(g => g.count)
                .ToArray(),
            trending_keywords = commonTopics.Take(10).ToArray()
        };
    }

    private static object CalculateOrganizationMetrics(List<dynamic> discussions, string timeRange)
    {
        var days = timeRange switch
        {
            "week" => 7,
            "month" => 30,
            "quarter" => 90,
            _ => 365
        };

        return new
        {
            period = timeRange,
            discussion_velocity = discussions.Count / (double)days,
            engagement_score = Math.Min(100, discussions.Average(d => (int)d.CommentCount) * 10),
            community_activity_index = Math.Min(100, (discussions.Count + discussions.Sum(d => (int)d.CommentCount)) / 10.0),
            repository_distribution = discussions
                .GroupBy(d => d.Repository)
                .Select(g => new { repository = g.Key, discussions = g.Count() })
                .OrderByDescending(g => g.discussions)
                .ToArray(),
            reynolds_efficiency_rating = Math.Min(100, discussions.Count * 2) // Playful metric
        };
    }

    private static string CalculateTrendDirection(IGrouping<DateTime, dynamic>[] groupedByDate)
    {
        if (groupedByDate.Length < 2) return "Stable";

        var recentCount = groupedByDate.TakeLast(3).Sum(g => g.Count());
        var earlierCount = groupedByDate.Take(3).Sum(g => g.Count());

        if (recentCount > earlierCount * 1.2) return "Increasing";
        if (recentCount < earlierCount * 0.8) return "Decreasing";
        return "Stable";
    }

    private static string AssessCommunityHealth(List<dynamic> discussions)
    {
        if (!discussions.Any()) return "Quiet";

        var engagementRate = discussions.Count(d => (int)d.CommentCount > 0) / (double)discussions.Count;
        var averageComments = discussions.Average(d => (int)d.CommentCount);

        return (engagementRate, averageComments) switch
        {
            (> 0.7, > 3) => "Thriving",
            (> 0.5, > 2) => "Healthy",
            (> 0.3, > 1) => "Moderate",
            _ => "Needs Attention"
        };
    }

    private static string GenerateReynoldsOrganizationalInsights(List<dynamic> discussions, string analysisType)
    {
        var insights = new List<string>();

        if (!discussions.Any())
        {
            insights.Add("Silence is golden, but not in community discussions - time for some Reynolds-style conversation starters");
        }
        else
        {
            var topRepo = discussions.GroupBy(d => d.Repository).OrderByDescending(g => g.Count()).FirstOrDefault();
            if (topRepo != null)
            {
                insights.Add($"'{topRepo.Key}' is the conversation hub - {topRepo.Count()} discussions and counting");
            }

            var engagedDiscussions = discussions.Count(d => (int)d.CommentCount > 5);
            if (engagedDiscussions > 0)
            {
                insights.Add($"{engagedDiscussions} highly engaged discussions - community energy at Van Wilder levels");
            }
        }

        return insights.Any() ? string.Join("; ", insights) : "Standard organizational discussion patterns with supernatural community coordination";
    }

    private static string[] GenerateStrategicRecommendations(List<dynamic> discussions, string organization)
    {
        var recommendations = new List<string>();

        var noCommentDiscussions = discussions.Count(d => (int)d.CommentCount == 0);
        if (noCommentDiscussions > discussions.Count * 0.3)
        {
            recommendations.Add("Consider implementing discussion starter templates to increase engagement");
        }

        var repositoryDistribution = discussions.GroupBy(d => d.Repository).Count();
        if (repositoryDistribution < 3)
        {
            recommendations.Add("Expand discussion culture across more repositories for better knowledge sharing");
        }

        var categories = discussions.Select(d => d.Category).Distinct().Count();
        if (categories < 3)
        {
            recommendations.Add("Diversify discussion categories to cover broader organizational topics");
        }

        if (!recommendations.Any())
        {
            recommendations.Add("Maintain current discussion momentum with Reynolds-style community engagement");
        }

        return recommendations.ToArray();
    }
}