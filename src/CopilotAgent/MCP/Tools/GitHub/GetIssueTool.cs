using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CopilotAgent.MCP.Tools.GitHub;

/// <summary>
/// Reynolds-powered tool for retrieving GitHub issue details with comprehensive analysis
/// </summary>
[McpServerToolType]
public static class GetIssueTool
{
    [McpServerTool, Description("Retrieve GitHub issue details with Reynolds-level analysis and supernatural project insight")]
    public static async Task<object> GetIssue(
        [Description("Repository (owner/repo format)")] string repository,
        [Description("Issue number to retrieve")] int issueNumber,
        [Description("Include issue comments in response")] bool includeComments = true)
    {
        // Validate inputs
        if (string.IsNullOrEmpty(repository))
        {
            throw new ArgumentException("Repository is required");
        }

        await Task.CompletedTask; // Satisfy async requirement

        // Mock issue data
        var nodeId = $"I_{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{repository}_{issueNumber}"))}";
        var createdAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 90));
        var updatedAt = createdAt.AddDays(Random.Shared.Next(0, 30));
        var commentCount = Random.Shared.Next(0, 50);
        var labels = GenerateMockLabels();
        var assignees = GenerateMockAssignees();

        var mockComments = includeComments ? GenerateMockComments(commentCount, repository, issueNumber) : null;

        var issue = new
        {
            node_id = nodeId,
            number = issueNumber,
            title = $"Issue #{issueNumber}: Mock Issue for Testing",
            body = "This is a mock issue body with detailed description of the problem or feature request.",
            url = $"https://api.github.com/repos/{repository}/issues/{issueNumber}",
            html_url = $"https://github.com/{repository}/issues/{issueNumber}",
            author = new
            {
                login = "issue-creator",
                id = Random.Shared.Next(10000, 99999),
                type = "User"
            },
            state = Random.Shared.Next(0, 10) > 7 ? "closed" : "open",
            created_at = createdAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            updated_at = updatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            labels = labels,
            assignees = assignees,
            comment_count = commentCount,
            comments = mockComments,
            metadata = new
            {
                reactions = new
                {
                    total_count = Random.Shared.Next(0, 20),
                    plus_one = Random.Shared.Next(0, 10),
                    minus_one = Random.Shared.Next(0, 2),
                    laugh = Random.Shared.Next(0, 5),
                    hooray = Random.Shared.Next(0, 5),
                    confused = Random.Shared.Next(0, 3),
                    heart = Random.Shared.Next(0, 8),
                    rocket = Random.Shared.Next(0, 6),
                    eyes = Random.Shared.Next(0, 4)
                }
            }
        };

        return new
        {
            success = true,
            issue = issue,
            repository = repository,
            analysis = new
            {
                priority_assessment = AssessPriority(labels),
                complexity_estimate = EstimateComplexity(issue.body.Length, assignees.Length, labels.Length, commentCount),
                lifecycle_stage = DetermineLifecycleStage(issue.state, assignees.Length, commentCount, createdAt, updatedAt),
                collaboration_health = AssessCollaborationHealth(commentCount, createdAt, assignees.Length, labels.Length),
                reynolds_insights = GenerateReynoldsInsights(commentCount, createdAt, updatedAt, assignees.Length, labels, issue.state)
            },
            reynolds_note = "Issue retrieved with comprehensive analysis and supernatural project management insight",
            mock_data_notice = "This is mock data - actual GitHub API integration required for production",
            timestamp = DateTime.UtcNow
        };
    }

    private static object[] GenerateMockLabels()
    {
        var possibleLabels = new[] { "bug", "enhancement", "documentation", "good first issue", "help wanted", "question", "wontfix", "duplicate", "invalid" };
        var labelCount = Random.Shared.Next(0, 4);
        var selectedLabels = possibleLabels.OrderBy(x => Random.Shared.Next()).Take(labelCount);
        
        return selectedLabels.Select(label => new
        {
            name = label,
            color = GenerateLabelColor(label),
            description = $"Label: {label}"
        }).ToArray();
    }

    private static object[] GenerateMockAssignees()
    {
        var assigneeCount = Random.Shared.Next(0, 3);
        var assignees = new List<object>();
        
        for (int i = 0; i < assigneeCount; i++)
        {
            assignees.Add(new
            {
                login = $"assignee-{Random.Shared.Next(1, 100)}",
                id = Random.Shared.Next(10000, 99999),
                type = "User"
            });
        }
        
        return assignees.ToArray();
    }

    private static object[] GenerateMockComments(int count, string repository, int issueNumber)
    {
        var comments = new List<object>();
        for (int i = 0; i < count; i++)
        {
            var commentId = Random.Shared.Next(1000000, 9999999);
            comments.Add(new
            {
                id = commentId,
                node_id = $"IC_{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{repository}_{issueNumber}_{commentId}"))}",
                body = $"Mock comment #{i + 1} with helpful information and Reynolds-level insights.",
                author = new
                {
                    login = $"commenter-{Random.Shared.Next(1, 50)}",
                    id = Random.Shared.Next(10000, 99999),
                    type = "User"
                },
                url = $"https://api.github.com/repos/{repository}/issues/comments/{commentId}",
                created_at = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 30)).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                updated_at = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 25)).ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }
        return comments.ToArray();
    }

    private static string GenerateLabelColor(string label)
    {
        return label.ToLowerInvariant() switch
        {
            "bug" => "d73a49",
            "enhancement" => "a2eeef",
            "documentation" => "0075ca",
            "good first issue" => "7057ff",
            "help wanted" => "008672",
            "question" => "d876e3",
            "wontfix" => "ffffff",
            _ => Random.Shared.Next(0, 16777215).ToString("x6")
        };
    }

    private static string AssessPriority(object[] labels)
    {
        var labelNames = labels.Cast<dynamic>().Select(l => l.name.ToString().ToLowerInvariant()).ToArray();
        
        if (labelNames.Any(l => l.Contains("critical") || l.Contains("high")))
            return "High";
        if (labelNames.Any(l => l.Contains("medium")))
            return "Medium";
        if (labelNames.Any(l => l.Contains("low")))
            return "Low";
        if (labelNames.Any(l => l.Contains("bug") || l.Contains("security")))
            return "High";
        if (labelNames.Any(l => l.Contains("enhancement") || l.Contains("feature")))
            return "Medium";
        
        return "Normal";
    }

    private static string EstimateComplexity(int bodyLength, int assigneeCount, int labelCount, int commentCount)
    {
        var complexityScore = 0;
        if (bodyLength > 1000) complexityScore += 2;
        else if (bodyLength > 500) complexityScore += 1;
        
        if (assigneeCount > 0) complexityScore += 1;
        if (labelCount > 3) complexityScore += 1;
        if (commentCount > 10) complexityScore += 2;
        else if (commentCount > 3) complexityScore += 1;

        return complexityScore switch
        {
            >= 5 => "High",
            >= 3 => "Medium",
            >= 1 => "Low",
            _ => "Simple"
        };
    }

    private static string DetermineLifecycleStage(string state, int assigneeCount, int commentCount, DateTime createdAt, DateTime updatedAt)
    {
        var daysSinceCreated = (DateTime.UtcNow - createdAt).Days;
        var daysSinceUpdated = (DateTime.UtcNow - updatedAt).Days;

        if (state.ToLowerInvariant() == "closed")
            return "Completed";
        
        if (assigneeCount > 0 && daysSinceUpdated < 7)
            return "Active Development";
        
        if (assigneeCount > 0)
            return "Assigned";
        
        if (commentCount > 0 && daysSinceUpdated < 14)
            return "Under Discussion";
        
        if (daysSinceCreated < 7)
            return "Newly Created";
        
        if (daysSinceUpdated > 30)
            return "Stale";
        
        return "Open/Waiting";
    }

    private static string AssessCollaborationHealth(int commentCount, DateTime createdAt, int assigneeCount, int labelCount)
    {
        var commentRatio = commentCount / Math.Max(1, (DateTime.UtcNow - createdAt).Days);
        var hasAssignees = assigneeCount > 0;
        var hasLabels = labelCount > 0;

        if (commentRatio > 1 && hasAssignees && hasLabels)
            return "Excellent";
        if (commentRatio > 0.5 && (hasAssignees || hasLabels))
            return "Good";
        if (commentRatio > 0 || hasAssignees || hasLabels)
            return "Fair";
        
        return "Needs Attention";
    }

    private static string GenerateReynoldsInsights(int commentCount, DateTime createdAt, DateTime updatedAt, int assigneeCount, object[] labels, string state)
    {
        var insights = new List<string>();
        var labelNames = labels.Cast<dynamic>().Select(l => l.name.ToString().ToLowerInvariant()).ToArray();

        if (commentCount == 0 && (DateTime.UtcNow - createdAt).Days > 3)
        {
            insights.Add("Silent treatment - might need some Reynolds charm to get conversation started");
        }

        if (assigneeCount > 3)
        {
            insights.Add("More assignees than a Van Wilder group project - might want to designate a lead");
        }

        if (labelNames.Any(l => l.Contains("good first issue")) && commentCount == 0)
        {
            insights.Add("Perfect entry point for new contributors - like a superhero origin story waiting to happen");
        }

        if (state.ToLowerInvariant() == "open" && (DateTime.UtcNow - updatedAt).Days > 30)
        {
            insights.Add("This issue has been aging like a fine wine - might be time for some Maximum Effort™ attention");
        }

        if (commentCount > 20 && state.ToLowerInvariant() == "open")
        {
            insights.Add("Hot discussion topic - more engagement than a Reynolds press conference");
        }

        return insights.Any() ? string.Join("; ", insights) : "Standard issue flow - everything proceeding with supernatural efficiency";
    }
}