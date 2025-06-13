using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CopilotAgent.MCP.Tools.GitHub;

/// <summary>
/// Create GitHub issues with Reynolds-level efficiency and precision
/// </summary>
[McpServerToolType]
public static class CreateIssueTool
{
    [McpServerTool, Description("Create GitHub issues with Reynolds-level efficiency and precision")]
    public static async Task<object> CreateIssue(
        [Description("Repository (owner/repo format)")] string repository,
        [Description("Issue title")] string title,
        [Description("Issue body/description")] string body,
        [Description("Issue labels")] string[] labels = null!,
        [Description("Assignees")] string[] assignees = null!)
    {
        // Validate inputs
        if (string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(title) || string.IsNullOrEmpty(body))
        {
            throw new ArgumentException("Repository, title, and body are required");
        }

        labels ??= Array.Empty<string>();
        assignees ??= Array.Empty<string>();

        await Task.CompletedTask; // Satisfy async requirement

        // Mock successful issue creation
        var issueNumber = Random.Shared.Next(1, 10000);
        var nodeId = $"I_{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{repository}_{issueNumber}"))}";
        
        return new
        {
            success = true,
            issue = new
            {
                number = issueNumber,
                node_id = nodeId,
                title = title,
                body = body,
                url = $"https://github.com/{repository}/issues/{issueNumber}",
                html_url = $"https://github.com/{repository}/issues/{issueNumber}",
                author = new
                {
                    login = "reynolds-bot",
                    id = 12345,
                    type = "Bot"
                },
                labels = labels.Select(label => new
                {
                    name = label,
                    color = GenerateLabelColor(label),
                    description = $"Label: {label}"
                }).ToArray(),
                assignees = assignees.Select(assignee => new
                {
                    login = assignee,
                    id = Random.Shared.Next(10000, 99999),
                    type = "User"
                }).ToArray(),
                state = "open",
                created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                updated_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            },
            repository = repository,
            reynolds_note = "Issue created with Maximum Effort™ efficiency",
            efficiency_metrics = new
            {
                creation_speed = "Supernatural",
                clarity_score = 96,
                reynolds_approval = "✅ Deadpool-level precision achieved"
            },
            mock_data_notice = "This is mock data - actual GitHub API integration required for production",
            timestamp = DateTime.UtcNow
        };
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
}