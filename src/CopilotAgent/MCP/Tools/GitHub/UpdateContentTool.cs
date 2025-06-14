using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CopilotAgent.MCP.Tools.GitHub;

/// <summary>
/// Reynolds-powered tool for updating GitHub content with validation and precision
/// </summary>
[McpServerToolType]
public static class UpdateContentTool
{
    [McpServerTool, Description("Update GitHub issues or discussions with Reynolds validation and supernatural accuracy")]
    public static async Task<object> UpdateContent(
        [Description("Repository (owner/repo format)")] string repository,
        [Description("Content type: 'issue' or 'discussion'")] string contentType,
        [Description("Issue number or discussion number")] int contentNumber = 0,
        [Description("Node ID for discussions (alternative to content_number)")] string contentNodeId = "",
        [Description("New title (optional)")] string title = "",
        [Description("New body content (optional)")] string body = "",
        [Description("New state for issues: 'open' or 'closed' (optional)")] string state = "",
        [Description("New labels for issues (optional)")] string[] labels = null!)
    {
        // Validate inputs
        if (string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(contentType))
        {
            throw new ArgumentException("Repository and content_type are required");
        }

        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body))
        {
            throw new ArgumentException("At least one of title or body must be provided for update");
        }

        labels ??= Array.Empty<string>();

        await Task.CompletedTask; // Satisfy async requirement

        // Mock content update based on type
        if (contentType.ToLowerInvariant() == "issue")
        {
            if (contentNumber == 0)
            {
                throw new ArgumentException("content_number is required for issue updates");
            }

            var updatedIssue = GenerateMockUpdatedIssue(repository, contentNumber, title, body, state, labels);

            return new
            {
                success = true,
                updated_content = new
                {
                    type = "issue",
                    number = ((dynamic)updatedIssue).number,
                    title = ((dynamic)updatedIssue).title,
                    body = ((dynamic)updatedIssue).body,
                    state = ((dynamic)updatedIssue).state,
                    url = ((dynamic)updatedIssue).url,
                    updated_at = ((dynamic)updatedIssue).updated_at,
                    labels = ((dynamic)updatedIssue).labels
                },
                repository = repository,
                update_summary = GenerateUpdateSummary("issue", title, body, state, labels),
                reynolds_note = "Issue updated with Maximum Effort™ precision and validation",
                mock_data_notice = "This is mock data - actual GitHub API integration required for production",
                timestamp = DateTime.UtcNow
            };
        }
        else if (contentType.ToLowerInvariant() == "discussion")
        {
            if (string.IsNullOrEmpty(contentNodeId) && contentNumber == 0)
            {
                throw new ArgumentException("For discussions, either content_node_id or content_number must be provided");
            }

            var nodeId = !string.IsNullOrEmpty(contentNodeId) ? contentNodeId : 
                $"D_{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{repository}_{contentNumber}"))}";

            var updatedDiscussion = GenerateMockUpdatedDiscussion(repository, contentNumber, nodeId, title, body);

            return new
            {
                success = true,
                updated_content = new
                {
                    type = "discussion",
                    number = ((dynamic)updatedDiscussion).number,
                    node_id = ((dynamic)updatedDiscussion).node_id,
                    title = ((dynamic)updatedDiscussion).title,
                    body = ((dynamic)updatedDiscussion).body,
                    url = ((dynamic)updatedDiscussion).url,
                    updated_at = ((dynamic)updatedDiscussion).updated_at,
                    category = ((dynamic)updatedDiscussion).category
                },
                repository = repository,
                update_summary = GenerateUpdateSummary("discussion", title, body, "", Array.Empty<string>()),
                reynolds_note = "Discussion updated with supernatural accuracy and coordination",
                mock_data_notice = "This is mock data - actual GitHub API integration required for production",
                timestamp = DateTime.UtcNow
            };
        }
        else
        {
            throw new ArgumentException($"Invalid content_type: {contentType}. Must be 'issue' or 'discussion'");
        }
    }

    private static object GenerateMockUpdatedIssue(string repository, int number, string title, string body, string state, string[] labels)
    {
        var currentTime = DateTime.UtcNow;
        var existingTitle = $"Existing Issue #{number} Title";
        var existingBody = "This is the existing issue body content that would be preserved if not updated.";
        var existingState = "open";
        var existingLabels = new[] { "existing-label", "needs-review" };

        return new
        {
            number = number,
            title = !string.IsNullOrEmpty(title) ? title : existingTitle,
            body = !string.IsNullOrEmpty(body) ? body : existingBody,
            state = !string.IsNullOrEmpty(state) ? state : existingState,
            url = $"https://github.com/{repository}/issues/{number}",
            updated_at = currentTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            labels = labels.Any() ? labels : existingLabels,
            author = new
            {
                login = $"user-{Random.Shared.Next(1, 100)}",
                id = Random.Shared.Next(10000, 99999),
                type = "User"
            }
        };
    }

    private static object GenerateMockUpdatedDiscussion(string repository, int number, string nodeId, string title, string body)
    {
        var currentTime = DateTime.UtcNow;
        var existingTitle = $"Existing Discussion #{number} Title";
        var existingBody = "This is the existing discussion body content that would be preserved if not updated.";
        var discussionNumber = number > 0 ? number : Random.Shared.Next(1, 500);

        return new
        {
            number = discussionNumber,
            node_id = nodeId,
            title = !string.IsNullOrEmpty(title) ? title : existingTitle,
            body = !string.IsNullOrEmpty(body) ? body : existingBody,
            url = $"https://github.com/{repository}/discussions/{discussionNumber}",
            updated_at = currentTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            category = new
            {
                name = "General",
                emoji = "💬",
                description = "General discussions"
            },
            author = new
            {
                login = $"discussion-author-{Random.Shared.Next(1, 50)}",
                id = Random.Shared.Next(10000, 99999),
                type = "User"
            }
        };
    }

    private static object GenerateUpdateSummary(string contentType, string title, string body, string state, string[] labels)
    {
        var changes = new List<string>();

        if (!string.IsNullOrEmpty(title))
            changes.Add("title updated");
        
        if (!string.IsNullOrEmpty(body))
            changes.Add("body content updated");
        
        if (!string.IsNullOrEmpty(state) && contentType == "issue")
            changes.Add($"state changed to {state}");
        
        if (labels.Any() && contentType == "issue")
            changes.Add($"labels updated ({labels.Length} labels)");

        return new
        {
            changes_made = changes.ToArray(),
            total_changes = changes.Count,
            content_type = contentType,
            update_timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            reynolds_validation = "All updates validated with supernatural precision",
            change_summary = changes.Any() ? string.Join(", ", changes) : "No changes detected"
        };
    }
}