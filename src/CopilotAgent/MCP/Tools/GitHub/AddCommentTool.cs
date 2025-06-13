using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CopilotAgent.MCP.Tools.GitHub;

/// <summary>
/// Reynolds-powered tool for adding comments to GitHub discussions and issues with supernatural precision
/// </summary>
[McpServerToolType]
public static class AddCommentTool
{
    [McpServerTool, Description("Add comments to GitHub issues or discussions with Reynolds-level clarity and engagement")]
    public static async Task<object> AddComment(
        [Description("Repository (owner/repo format)")] string repository,
        [Description("Target type: 'issue' or 'discussion'")] string targetType = "issue",
        [Description("Issue number or discussion number")] int targetNumber = 1,
        [Description("Node ID for discussions (alternative to target_number)")] string targetNodeId = "",
        [Description("Comment body content")] string body = "")
    {
        // Validate inputs
        if (string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(targetType) || string.IsNullOrEmpty(body))
        {
            throw new ArgumentException("Repository, target_type, and body are required");
        }

        await Task.CompletedTask; // Satisfy async requirement

        // Mock successful comment addition
        var commentId = Random.Shared.Next(1000000, 9999999);
        var nodeId = $"IC_{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{repository}_{commentId}"))}";
        
        return Task.FromResult<object>(new
        {
            success = true,
            comment = new
            {
                id = commentId,
                node_id = nodeId,
                body = body,
                author = new
                {
                    login = "reynolds-bot",
                    id = 12345,
                    type = "Bot"
                },
                url = $"https://github.com/{repository}/issues/{targetNumber}#issuecomment-{commentId}",
                created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            },
            repository = repository,
            target_type = targetType,
            target_number = targetNumber,
            reynolds_note = "Comment added with Maximum Effort™ clarity and engagement",
            engagement_metrics = new
            {
                clarity_score = 95,
                engagement_potential = "High",
                reynolds_approval = "✅ Deadpool-level wit detected"
            },
            mock_data_notice = "This is mock data - actual GitHub API integration required for production",
            timestamp = DateTime.UtcNow
        });
    }
}