using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CopilotAgent.MCP.Tools.GitHub;

/// <summary>
/// Reynolds-powered tool for retrieving GitHub discussion details with comprehensive context
/// </summary>
[McpServerToolType]
public static class GetDiscussionTool
{
    [McpServerTool, Description("Retrieve GitHub discussion details with Reynolds-level context and supernatural insight")]
    public static async Task<object> GetDiscussion(
        [Description("Repository (owner/repo format)")] string repository,
        [Description("Discussion number to retrieve")] int discussionNumber,
        [Description("Include discussion comments in response")] bool includeComments = true)
    {
        // Validate inputs
        if (string.IsNullOrEmpty(repository))
        {
            throw new ArgumentException("Repository is required");
        }

        await Task.CompletedTask; // Satisfy async requirement

        // Mock discussion data
        var nodeId = $"D_{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{repository}_{discussionNumber}"))}";
        var createdAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30));
        var updatedAt = createdAt.AddDays(Random.Shared.Next(0, 7));
        var commentCount = Random.Shared.Next(0, 25);

        var mockComments = includeComments ? GenerateMockComments(commentCount, repository, discussionNumber) : null;

        var discussion = new
        {
            node_id = nodeId,
            number = discussionNumber,
            title = $"Discussion #{discussionNumber}: Mock Discussion Topic",
            body = "This is a mock discussion body with detailed content about the topic at hand.",
            url = $"https://github.com/{repository}/discussions/{discussionNumber}",
            author = new
            {
                login = "discussion-author",
                id = Random.Shared.Next(10000, 99999),
                type = "User"
            },
            category = new
            {
                name = "General",
                emoji = "💬",
                description = "General discussions"
            },
            state = "OPEN",
            created_at = createdAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            updated_at = updatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            comment_count = commentCount,
            comments = mockComments,
            metadata = new
            {
                is_answered = commentCount > 3,
                answer_chosen_at = commentCount > 3 ? updatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ") : null
            }
        };

        return new
        {
            success = true,
            discussion = discussion,
            repository = repository,
            context_analysis = new
            {
                engagement_level = CalculateEngagementLevel(commentCount, createdAt),
                discussion_health = AssessDiscussionHealth(updatedAt, commentCount),
                reynolds_insights = GenerateReynoldsInsights(commentCount, createdAt, updatedAt, "OPEN")
            },
            reynolds_note = "Discussion retrieved with comprehensive context and supernatural insight",
            mock_data_notice = "This is mock data - actual GitHub API integration required for production",
            timestamp = DateTime.UtcNow
        };
    }

    private static object[] GenerateMockComments(int count, string repository, int discussionNumber)
    {
        var comments = new List<object>();
        for (int i = 0; i < count; i++)
        {
            var commentId = Random.Shared.Next(1000000, 9999999);
            comments.Add(new
            {
                id = commentId,
                node_id = $"DC_{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{repository}_{discussionNumber}_{commentId}"))}",
                body = $"Mock comment #{i + 1} with thoughtful insights and Reynolds-level engagement.",
                author = new
                {
                    login = $"user-{Random.Shared.Next(1, 100)}",
                    id = Random.Shared.Next(10000, 99999),
                    type = "User"
                },
                url = $"https://github.com/{repository}/discussions/{discussionNumber}#discussioncomment-{commentId}",
                created_at = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 20)).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                updated_at = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 15)).ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }
        return comments.ToArray();
    }

    private static string CalculateEngagementLevel(int commentCount, DateTime createdAt)
    {
        var commentsPerDay = (double)commentCount / Math.Max(1, (DateTime.UtcNow - createdAt).Days);
        
        return commentsPerDay switch
        {
            > 5.0 => "Very High",
            > 2.0 => "High",
            > 0.5 => "Moderate",
            > 0.0 => "Low",
            _ => "None"
        };
    }

    private static string AssessDiscussionHealth(DateTime updatedAt, int commentCount)
    {
        var daysSinceUpdate = (DateTime.UtcNow - updatedAt).Days;
        var hasComments = commentCount > 0;
        
        return (daysSinceUpdate, hasComments) switch
        {
            (< 1, true) => "Active",
            (< 7, true) => "Recent Activity",
            (< 30, _) => "Moderate",
            (< 90, _) => "Aging",
            _ => "Stale"
        };
    }

    private static string GenerateReynoldsInsights(int commentCount, DateTime createdAt, DateTime updatedAt, string state)
    {
        var insights = new List<string>();

        if (commentCount == 0)
        {
            insights.Add("Cricket sounds - might need some conversation starters");
        }
        else if (commentCount > 20)
        {
            insights.Add("This discussion is more popular than a Van Wilder party");
        }

        var daysSinceCreated = (DateTime.UtcNow - createdAt).Days;
        if (daysSinceCreated < 1 && commentCount > 5)
        {
            insights.Add("Fast-moving discussion - Reynolds-level engagement happening");
        }

        if (state.ToLowerInvariant() == "closed" && commentCount > 0)
        {
            insights.Add("Resolved with community input - Maximum Effort™ collaboration achieved");
        }

        return insights.Any() ? string.Join("; ", insights) : "Standard discussion flow - everything proceeding with supernatural normalcy";
    }
}