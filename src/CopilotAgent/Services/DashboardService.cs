using System.Collections.Concurrent;
using Shared.Models;

namespace CopilotAgent.Services;

public class DashboardService : IDashboardService
{
    private readonly ILogger<DashboardService> _logger;
    private readonly IGitHubIssuesService _issuesService;
    private readonly IGitHubDiscussionsService _discussionsService;
    private readonly IStakeholderVisibilityService _stakeholderService;
    private readonly ISecurityAuditService _auditService;

    // In-memory cache for dashboard configurations
    private readonly ConcurrentDictionary<string, DashboardConfiguration> _dashboardConfigs = new();

    public DashboardService(
        ILogger<DashboardService> logger,
        IGitHubIssuesService issuesService,
        IGitHubDiscussionsService discussionsService,
        IStakeholderVisibilityService stakeholderService,
        ISecurityAuditService auditService)
    {
        _logger = logger;
        _issuesService = issuesService;
        _discussionsService = discussionsService;
        _stakeholderService = stakeholderService;
        _auditService = auditService;
    }

    public async Task<ProjectProgressSummary> GenerateProjectSummaryAsync(string repository, string? stakeholderId = null)
    {
        try
        {
            _logger.LogInformation("Generating project summary for repository {Repository}", repository);

            var summary = new ProjectProgressSummary
            {
                Repository = repository,
                GeneratedAt = DateTime.UtcNow
            };

            // Get stakeholder configuration if provided
            StakeholderConfiguration? stakeholderConfig = null;
            if (!string.IsNullOrEmpty(stakeholderId))
            {
                stakeholderConfig = await _stakeholderService.GetStakeholderAsync(stakeholderId);
            }

            // Generate issues summary
            summary.Issues = await GenerateIssuesSummaryAsync(repository, stakeholderConfig);

            // Generate discussions summary
            summary.Discussions = await GenerateDiscussionsSummaryAsync(repository, stakeholderConfig);

            // Generate pull requests summary (mock data for now - would need PR service)
            summary.PullRequests = await GeneratePullRequestsSummaryAsync(repository, stakeholderConfig);

            // Generate project metrics
            summary.Metrics = await GenerateProjectMetricsAsync(repository, summary);

            // Generate recent activity
            summary.RecentActivities = await GenerateRecentActivityAsync(repository, stakeholderConfig);

            await _auditService.LogEventAsync(
                "Dashboard_Summary_Generated",
                repository: repository,
                action: "GenerateProjectSummary",
                result: "SUCCESS",
                details: new { StakeholderId = stakeholderId, Repository = repository });

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating project summary for repository {Repository}", repository);
            await _auditService.LogEventAsync(
                "Dashboard_Summary_Failed",
                repository: repository,
                action: "GenerateProjectSummary",
                result: "FAILURE",
                details: new { Repository = repository, Error = ex.Message });
            throw;
        }
    }

    public async Task<ProjectProgressSummary> GenerateOrganizationSummaryAsync(string organization, string? stakeholderId = null)
    {
        try 
        {
            _logger.LogInformation("Generating organization summary for {Organization}", organization);

            var summary = new ProjectProgressSummary
            {
                Repository = organization,
                GeneratedAt = DateTime.UtcNow
            };

            // Get stakeholder configuration if provided
            StakeholderConfiguration? stakeholderConfig = null;
            if (!string.IsNullOrEmpty(stakeholderId))
            {
                stakeholderConfig = await _stakeholderService.GetStakeholderAsync(stakeholderId);
            }

            // Get organization-wide data
            var orgIssues = await _issuesService.GetOrganizationIssuesAsync(organization);
            var orgDiscussions = await _discussionsService.GetOrganizationDiscussionsAsync(organization);

            // Generate aggregated summaries
            summary.Issues = GenerateIssuesSummaryFromData(orgIssues, stakeholderConfig);
            summary.Discussions = GenerateDiscussionsSummaryFromData(orgDiscussions, stakeholderConfig);
            summary.PullRequests = new PullRequestsSummary(); // Mock data
            summary.Metrics = await GenerateProjectMetricsAsync(organization, summary);
            summary.RecentActivities = GenerateRecentActivityFromData(orgIssues, orgDiscussions, stakeholderConfig);

            await _auditService.LogEventAsync(
                "Dashboard_OrgSummary_Generated",
                action: "GenerateOrganizationSummary",
                result: "SUCCESS",
                details: new { Organization = organization, StakeholderId = stakeholderId });

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating organization summary for {Organization}", organization);
            throw;
        }
    }

    public async Task<DashboardConfiguration> GetDashboardConfigurationAsync(string stakeholderId)
    {
        try
        {
            if (_dashboardConfigs.TryGetValue(stakeholderId, out var config))
            {
                return config;
            }

            // Return default configuration if none exists
            var defaultConfig = new DashboardConfiguration
            {
                Title = "Project Dashboard",
                Widgets = new[]
                {
                    new DashboardWidget { Id = "issues", Title = "Issues Summary", Type = WidgetType.IssuesSummary, Order = 1 },
                    new DashboardWidget { Id = "prs", Title = "Pull Requests", Type = WidgetType.PullRequestsSummary, Order = 2 },
                    new DashboardWidget { Id = "activity", Title = "Recent Activity", Type = WidgetType.RecentActivity, Order = 3 },
                    new DashboardWidget { Id = "metrics", Title = "Project Metrics", Type = WidgetType.ProjectMetrics, Order = 4 }
                }
            };

            _dashboardConfigs[stakeholderId] = defaultConfig;
            return await Task.FromResult(defaultConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard configuration for stakeholder {StakeholderId}", stakeholderId);
            throw;
        }
    }

    public async Task<DashboardConfiguration> UpdateDashboardConfigurationAsync(string stakeholderId, DashboardConfiguration config)
    {
        try
        {
            _dashboardConfigs[stakeholderId] = config;

            await _auditService.LogEventAsync(
                "Dashboard_Config_Updated",
                action: "UpdateDashboardConfiguration",
                result: "SUCCESS",
                details: new { StakeholderId = stakeholderId });

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dashboard configuration for stakeholder {StakeholderId}", stakeholderId);
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetWidgetDataAsync(string stakeholderId, string widgetId)
    {
        try
        {
            var stakeholder = await _stakeholderService.GetStakeholderAsync(stakeholderId);
            if (stakeholder == null)
            {
                throw new InvalidOperationException($"Stakeholder {stakeholderId} not found");
            }

            var data = new Dictionary<string, object>();

            // Get data based on widget type and stakeholder repositories
            foreach (var repository in stakeholder.Repositories)
            {
                switch (widgetId.ToLower())
                {
                    case "issues":
                        var issues = await _issuesService.GetIssuesByRepositoryAsync(repository);
                        data[$"{repository}_issues"] = issues;
                        break;
                    case "discussions":
                        var discussions = await _discussionsService.GetDiscussionsByRepositoryAsync(repository);
                        data[$"{repository}_discussions"] = discussions;
                        break;
                    default:
                        data[$"{repository}_general"] = "Data not available";
                        break;
                }
            }

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting widget data for stakeholder {StakeholderId}, widget {WidgetId}", stakeholderId, widgetId);
            throw;
        }
    }

    private async Task<IssuesSummary> GenerateIssuesSummaryAsync(string repository, StakeholderConfiguration? stakeholderConfig)
    {
        var openIssues = await _issuesService.GetIssuesByRepositoryAsync(repository, "open");
        var closedIssues = await _issuesService.GetIssuesByRepositoryAsync(repository, "closed", 100);

        return GenerateIssuesSummaryFromData(openIssues.Concat(closedIssues), stakeholderConfig);
    }

    private IssuesSummary GenerateIssuesSummaryFromData(IEnumerable<GitHubIssue> issues, StakeholderConfiguration? stakeholderConfig)
    {
        var issuesList = issues.ToList();
        var openIssues = issuesList.Where(i => i.State == "open").ToList();
        var closedIssues = issuesList.Where(i => i.State == "closed").ToList();
        var oneWeekAgo = DateTime.UtcNow.AddDays(-7);

        // Filter by stakeholder preferences if provided
        if (stakeholderConfig?.UpdatePreferences.ImportantLabels?.Length > 0)
        {
            var importantLabels = stakeholderConfig.UpdatePreferences.ImportantLabels;
            openIssues = openIssues.Where(i => i.Labels.Any(l => importantLabels.Contains(l))).ToList();
        }

        return new IssuesSummary
        {
            TotalOpen = openIssues.Count,
            TotalClosed = closedIssues.Count,
            OpenCritical = openIssues.Count(i => i.Labels.Contains("critical")),
            OpenHigh = openIssues.Count(i => i.Labels.Contains("high")),
            ClosedThisWeek = closedIssues.Count(i => i.UpdatedAt >= oneWeekAgo),
            NewThisWeek = openIssues.Count(i => i.CreatedAt >= oneWeekAgo),
            AverageClosureTimeHours = closedIssues.Any() ? 
                closedIssues.Average(i => (i.UpdatedAt - i.CreatedAt).TotalHours) : 0,
            RecentIssues = openIssues.OrderByDescending(i => i.UpdatedAt).Take(10).ToArray()
        };
    }

    private async Task<DiscussionsSummary> GenerateDiscussionsSummaryAsync(string repository, StakeholderConfiguration? stakeholderConfig)
    {
        var discussions = await _discussionsService.GetDiscussionsByRepositoryAsync(repository);
        return GenerateDiscussionsSummaryFromData(discussions, stakeholderConfig);
    }

    private DiscussionsSummary GenerateDiscussionsSummaryFromData(IEnumerable<GitHubDiscussion> discussions, StakeholderConfiguration? stakeholderConfig)
    {
        var discussionsList = discussions.ToList();
        var oneWeekAgo = DateTime.UtcNow.AddDays(-7);

        return new DiscussionsSummary
        {
            TotalActive = discussionsList.Count(d => d.State == "open"),
            NewThisWeek = discussionsList.Count(d => d.CreatedAt >= oneWeekAgo),
            AnsweredThisWeek = discussionsList.Count(d => d.UpdatedAt >= oneWeekAgo && d.State == "answered"),
            RecentDiscussions = discussionsList.OrderByDescending(d => d.UpdatedAt).Take(5).ToArray()
        };
    }

    private async Task<PullRequestsSummary> GeneratePullRequestsSummaryAsync(string repository, StakeholderConfiguration? stakeholderConfig)
    {
        // Mock data for now - would need a GitHubPullRequestService
        return await Task.FromResult(new PullRequestsSummary
        {
            TotalOpen = 5,
            TotalMerged = 25,
            AwaitingReview = 3,
            MergedThisWeek = 4,
            NewThisWeek = 2,
            AverageMergeTimeHours = 48.5,
            RecentPullRequests = Array.Empty<GitHubPullRequest>()
        });
    }

    private async Task<ProjectMetrics> GenerateProjectMetricsAsync(string repository, ProjectProgressSummary summary)
    {
        // Calculate basic project health metrics
        var issueResolutionRate = summary.Issues.TotalClosed + summary.Issues.TotalOpen > 0 ? 
            (double)summary.Issues.TotalClosed / (summary.Issues.TotalClosed + summary.Issues.TotalOpen) : 0;

        var projectHealth = Math.Min(100, Math.Max(0, issueResolutionRate * 100));
        var productivityScore = Math.Min(100, summary.Issues.ClosedThisWeek * 10 + summary.PullRequests.MergedThisWeek * 15);

        return await Task.FromResult(new ProjectMetrics
        {
            ProjectHealth = projectHealth,
            TeamProductivityScore = productivityScore,
            IssueResolutionTrend = issueResolutionRate,
            CodeQualityScore = 85.0, // Mock data
            CustomMetrics = new Dictionary<string, object>
            {
                { "ActiveContributors", 8 },
                { "CodeCoverage", 78.5 },
                { "SecurityScore", 92.0 }
            }
        });
    }

    private async Task<RecentActivity[]> GenerateRecentActivityAsync(string repository, StakeholderConfiguration? stakeholderConfig)
    {
        var activities = new List<RecentActivity>();

        // Get recent issues
        var recentIssues = await _issuesService.GetIssuesByRepositoryAsync(repository, "all", 10);
        activities.AddRange(recentIssues.Select(issue => new RecentActivity
        {
            Type = "issue",
            Title = issue.Title,
            Description = $"Issue #{issue.Number} {issue.State}",
            Url = issue.Url,
            Author = issue.Author,
            Timestamp = issue.UpdatedAt
        }));

        // Get recent discussions
        var recentDiscussions = await _discussionsService.GetDiscussionsByRepositoryAsync(repository, 5);
        activities.AddRange(recentDiscussions.Select(discussion => new RecentActivity
        {
            Type = "discussion",
            Title = discussion.Title,
            Description = $"Discussion #{discussion.Number}",
            Url = discussion.Url,
            Author = discussion.Author,
            Timestamp = discussion.UpdatedAt
        }));

        return activities.OrderByDescending(a => a.Timestamp).Take(20).ToArray();
    }

    private RecentActivity[] GenerateRecentActivityFromData(IEnumerable<GitHubIssue> issues, IEnumerable<GitHubDiscussion> discussions, StakeholderConfiguration? stakeholderConfig)
    {
        var activities = new List<RecentActivity>();

        activities.AddRange(issues.Select(issue => new RecentActivity
        {
            Type = "issue",
            Title = issue.Title,
            Description = $"Issue #{issue.Number} {issue.State}",
            Url = issue.Url,
            Author = issue.Author,
            Timestamp = issue.UpdatedAt
        }));

        activities.AddRange(discussions.Select(discussion => new RecentActivity
        {
            Type = "discussion",
            Title = discussion.Title,
            Description = $"Discussion #{discussion.Number}",
            Url = discussion.Url,
            Author = discussion.Author,
            Timestamp = discussion.UpdatedAt
        }));

        return activities.OrderByDescending(a => a.Timestamp).Take(20).ToArray();
    }
}