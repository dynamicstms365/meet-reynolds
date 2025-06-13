using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CopilotAgent.MCP.Tools.GitHub;

/// <summary>
/// Reynolds-powered tool for organization-wide GitHub issue analysis with Maximum Effort™ project intelligence
/// </summary>
[McpServerToolType]
public static class OrganizationIssuesTool
{
    [McpServerTool, Description("Analyze GitHub issues across the entire organization with Reynolds-level project management insight and supernatural issue intelligence")]
    public static async Task<object> AnalyzeOrganizationIssues(
        [Description("Organization name")] string organization = "dynamicstms365",
        [Description("Analysis type: 'overview', 'velocity', 'backlog', 'quality', 'blockers'")] string analysisType = "overview",
        [Description("Issue state filter: 'open', 'closed', 'all'")] string stateFilter = "open",
        [Description("Filter by priority labels (optional)")] string[] priorityFilter = null!,
        [Description("Time range for analysis: 'week', 'month', 'quarter', 'all'")] string timeRange = "month",
        [Description("Include detailed project metrics")] bool includeMetrics = true,
        [Description("Maximum issues to analyze")] int limit = 150)
    {
        // Validate inputs
        if (string.IsNullOrEmpty(organization))
        {
            throw new ArgumentException("Organization is required");
        }

        priorityFilter ??= Array.Empty<string>();

        await Task.CompletedTask; // Satisfy async requirement

        // Mock organization issues data
        var totalIssues = Random.Shared.Next(50, 500);
        var mockIssues = GenerateMockIssues(totalIssues, stateFilter, timeRange, priorityFilter);

        // Generate analysis based on type
        var analysis = analysisType switch
        {
            "velocity" => GenerateVelocityAnalysis(mockIssues, timeRange),
            "backlog" => GenerateBacklogAnalysis(mockIssues),
            "quality" => GenerateQualityAnalysis(mockIssues),
            "blockers" => GenerateBlockerAnalysis(mockIssues),
            _ => GenerateOverviewAnalysis(mockIssues)
        };

        // Calculate metrics if requested
        object? metrics = null;
        if (includeMetrics)
        {
            metrics = CalculateOrganizationMetrics(mockIssues, timeRange);
        }

        return new
        {
            success = true,
            organization = organization,
            analysis_type = analysisType,
            filters = new
            {
                state = stateFilter,
                priorities = priorityFilter,
                time_range = timeRange,
                total_analyzed = mockIssues.Count
            },
            analysis = analysis,
            metrics = metrics,
            reynolds_insights = GenerateReynoldsProjectInsights(mockIssues, analysisType),
            project_health_score = CalculateProjectHealthScore(mockIssues),
            strategic_recommendations = GenerateStrategicRecommendations(mockIssues, organization),
            reynolds_note = "Organization issue analysis completed with Maximum Effort™ project management intelligence",
            mock_data_notice = "This is mock data - actual GitHub API integration required for production",
            timestamp = DateTime.UtcNow
        };
    }

    private static List<dynamic> GenerateMockIssues(int count, string stateFilter, string timeRange, string[] priorityFilter)
    {
        var issues = new List<dynamic>();
        var repositories = new[] { "main-repo", "api-service", "frontend-app", "mobile-app", "documentation", "infrastructure" };
        var labels = new[] { "bug", "enhancement", "documentation", "good first issue", "help wanted", "question", "priority:high", "priority:medium", "priority:low" };
        var states = stateFilter == "all" ? new[] { "open", "closed" } : new[] { stateFilter };

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

            var state = states[Random.Shared.Next(states.Length)];
            var issueLabels = labels.OrderBy(x => Random.Shared.Next()).Take(Random.Shared.Next(0, 4)).ToArray();
            var assigneeCount = Random.Shared.Next(0, 3);

            // Apply priority filter
            if (priorityFilter.Any() && !issueLabels.Any(l => priorityFilter.Any(p => l.Contains(p, StringComparison.OrdinalIgnoreCase))))
            {
                continue;
            }

            issues.Add(new
            {
                Number = Random.Shared.Next(1, 10000),
                Title = $"Issue #{Random.Shared.Next(1, 1000)}: Mock Issue for Analysis",
                Body = "This is a mock issue body with detailed description and requirements.",
                Repository = repositories[Random.Shared.Next(repositories.Length)],
                Author = $"user-{Random.Shared.Next(1, 100)}",
                State = state,
                Labels = issueLabels,
                Assignees = GenerateMockAssignees(assigneeCount),
                CreatedAt = createdAt,
                UpdatedAt = state == "closed" ? createdAt.AddDays(Random.Shared.Next(1, 30)) : createdAt.AddDays(Random.Shared.Next(0, 15)),
                CommentCount = Random.Shared.Next(0, 25)
            });
        }

        return issues;
    }

    private static string[] GenerateMockAssignees(int count)
    {
        var assignees = new List<string>();
        for (int i = 0; i < count; i++)
        {
            assignees.Add($"assignee-{Random.Shared.Next(1, 50)}");
        }
        return assignees.ToArray();
    }

    private static object GenerateOverviewAnalysis(List<dynamic> issues)
    {
        var repositories = issues.Select(i => i.Repository).Distinct().ToArray();
        var openIssues = issues.Where(i => i.State == "open").ToList();
        var closedIssues = issues.Where(i => i.State == "closed").ToList();

        return new
        {
            total_issues = issues.Count,
            open_issues = openIssues.Count,
            closed_issues = closedIssues.Count,
            active_repositories = repositories.Length,
            priority_breakdown = AnalyzePriorityDistribution(issues),
            top_repositories = repositories
                .Select(r => new { 
                    repository = r, 
                    issue_count = issues.Count(i => i.Repository == r),
                    open_count = openIssues.Count(i => i.Repository == r)
                })
                .OrderByDescending(r => r.issue_count)
                .Take(10)
                .ToArray(),
            recent_activity = issues
                .OrderByDescending(i => i.UpdatedAt)
                .Take(10)
                .Select(i => new { i.Repository, i.Title, i.State, i.UpdatedAt })
                .ToArray()
        };
    }

    private static object GenerateVelocityAnalysis(List<dynamic> issues, string timeRange)
    {
        var closedIssues = issues.Where(i => i.State == "closed").ToList();
        
        var velocityByDate = closedIssues
            .GroupBy(i => ((DateTime)i.UpdatedAt).Date)
            .OrderBy(g => g.Key)
            .ToArray();

        var averageResolutionTime = closedIssues.Any() 
            ? closedIssues.Average(i => ((DateTime)i.UpdatedAt - (DateTime)i.CreatedAt).TotalDays)
            : 0;

        return new
        {
            period = timeRange,
            issues_closed = closedIssues.Count,
            average_resolution_days = Math.Round(averageResolutionTime, 1),
            daily_closure_rate = velocityByDate.Select(g => new
            {
                date = g.Key,
                closed_count = g.Count()
            }).ToArray(),
            velocity_trend = CalculateVelocityTrend(velocityByDate),
            repository_velocity = issues
                .GroupBy(i => i.Repository)
                .Select(g => new
                {
                    repository = g.Key,
                    total_issues = g.Count(),
                    closed_issues = g.Count(i => i.State == "closed"),
                    closure_rate = g.Count() > 0 ? (double)g.Count(i => i.State == "closed") / g.Count() : 0
                })
                .OrderByDescending(r => r.closure_rate)
                .ToArray()
        };
    }

    private static object GenerateBacklogAnalysis(List<dynamic> issues)
    {
        var openIssues = issues.Where(i => i.State == "open").ToList();

        return new
        {
            total_backlog = openIssues.Count,
            age_distribution = new
            {
                fresh = openIssues.Count(i => (DateTime.UtcNow - (DateTime)i.CreatedAt).Days < 7),
                recent = openIssues.Count(i => (DateTime.UtcNow - (DateTime)i.CreatedAt).Days >= 7 && (DateTime.UtcNow - (DateTime)i.CreatedAt).Days < 30),
                mature = openIssues.Count(i => (DateTime.UtcNow - (DateTime)i.CreatedAt).Days >= 30 && (DateTime.UtcNow - (DateTime)i.CreatedAt).Days < 90),
                stale = openIssues.Count(i => (DateTime.UtcNow - (DateTime)i.CreatedAt).Days >= 90)
            },
            assigned_vs_unassigned = new
            {
                assigned = openIssues.Count(i => ((string[])i.Assignees).Any()),
                unassigned = openIssues.Count(i => !((string[])i.Assignees).Any())
            },
            priority_backlog = AnalyzePriorityDistribution(openIssues.Cast<dynamic>().ToList()),
            oldest_issues = openIssues
                .OrderBy(i => i.CreatedAt)
                .Take(10)
                .Select(i => new { i.Repository, i.Title, i.CreatedAt, days_old = (DateTime.UtcNow - (DateTime)i.CreatedAt).Days })
                .ToArray(),
            backlog_by_repository = openIssues
                .GroupBy(i => i.Repository)
                .Select(g => new { repository = g.Key, backlog_count = g.Count() })
                .OrderByDescending(r => r.backlog_count)
                .ToArray()
        };
    }

    private static object GenerateQualityAnalysis(List<dynamic> issues)
    {
        return new
        {
            quality_indicators = new
            {
                well_labeled = issues.Count(i => ((string[])i.Labels).Length >= 2),
                has_assignees = issues.Count(i => ((string[])i.Assignees).Any()),
                has_description = issues.Count(i => !string.IsNullOrWhiteSpace(i.Body) && i.Body.Length > 50),
                has_comments = issues.Count(i => (int)i.CommentCount > 0)
            },
            label_usage = issues
                .SelectMany(i => (string[])i.Labels)
                .GroupBy(l => l)
                .Select(g => new { label = g.Key, usage_count = g.Count() })
                .OrderByDescending(l => l.usage_count)
                .Take(20)
                .ToArray(),
            engagement_metrics = new
            {
                average_comments = issues.Average(i => (int)i.CommentCount),
                highly_engaged = issues.Count(i => (int)i.CommentCount > 5),
                no_engagement = issues.Count(i => (int)i.CommentCount == 0)
            },
            quality_score_by_repo = issues
                .GroupBy(i => i.Repository)
                .Select(g => new
                {
                    repository = g.Key,
                    quality_score = CalculateRepositoryQualityScore(g.ToList())
                })
                .OrderByDescending(r => r.quality_score)
                .ToArray()
        };
    }

    private static object GenerateBlockerAnalysis(List<dynamic> issues)
    {
        var blockerKeywords = new[] { "blocked", "blocker", "dependency", "waiting" };
        
        var potentialBlockers = issues.Where(i => 
            ((string[])i.Labels).Any(l => blockerKeywords.Any(k => l.Contains(k, StringComparison.OrdinalIgnoreCase))) ||
            blockerKeywords.Any(k => i.Title.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
            blockerKeywords.Any(k => i.Body.Contains(k, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        return new
        {
            potential_blockers = potentialBlockers.Count,
            blocker_issues = potentialBlockers
                .Select(i => new
                {
                    i.Repository,
                    i.Title,
                    i.State,
                    i.CreatedAt,
                    blocker_indicators = ((string[])i.Labels).Where(l => 
                        blockerKeywords.Any(k => l.Contains(k, StringComparison.OrdinalIgnoreCase))).ToArray(),
                    days_open = i.State == "open" ? (DateTime.UtcNow - (DateTime)i.CreatedAt).Days : 0
                })
                .OrderByDescending(i => i.days_open)
                .ToArray(),
            blockers_by_repository = potentialBlockers
                .GroupBy(i => i.Repository)
                .Select(g => new { repository = g.Key, blocker_count = g.Count() })
                .OrderByDescending(r => r.blocker_count)
                .ToArray(),
            dependency_analysis = new
            {
                cross_repo_dependencies = new[] { "Simplified cross-repo analysis - detailed implementation needed" },
                long_running_blockers = potentialBlockers.Count(i => 
                    i.State == "open" && (DateTime.UtcNow - (DateTime)i.CreatedAt).Days > 30)
            }
        };
    }

    private static object AnalyzePriorityDistribution(List<dynamic> issues)
    {
        return new
        {
            critical = issues.Count(i => ((string[])i.Labels).Any(l => l.ToLowerInvariant().Contains("critical"))),
            high = issues.Count(i => ((string[])i.Labels).Any(l => l.ToLowerInvariant().Contains("high"))),
            medium = issues.Count(i => ((string[])i.Labels).Any(l => l.ToLowerInvariant().Contains("medium"))),
            low = issues.Count(i => ((string[])i.Labels).Any(l => l.ToLowerInvariant().Contains("low"))),
            unlabeled = issues.Count(i => !((string[])i.Labels).Any(l => 
                new[] { "critical", "high", "medium", "low" }.Any(p => l.ToLowerInvariant().Contains(p))))
        };
    }

    private static string CalculateVelocityTrend(IGrouping<DateTime, dynamic>[] velocityByDate)
    {
        if (velocityByDate.Length < 2) return "Stable";

        var recentVelocity = velocityByDate.TakeLast(7).Sum(g => g.Count());
        var earlierVelocity = velocityByDate.Take(7).Sum(g => g.Count());

        if (recentVelocity > earlierVelocity * 1.2) return "Accelerating";
        if (recentVelocity < earlierVelocity * 0.8) return "Slowing";
        return "Stable";
    }

    private static double CalculateRepositoryQualityScore(List<dynamic> repoIssues)
    {
        if (!repoIssues.Any()) return 0;

        var labeledScore = repoIssues.Count(i => ((string[])i.Labels).Any()) / (double)repoIssues.Count * 25;
        var assignedScore = repoIssues.Count(i => ((string[])i.Assignees).Any()) / (double)repoIssues.Count * 25;
        var descriptionScore = repoIssues.Count(i => !string.IsNullOrWhiteSpace(i.Body) && i.Body.Length > 50) / (double)repoIssues.Count * 25;
        var engagementScore = repoIssues.Count(i => (int)i.CommentCount > 0) / (double)repoIssues.Count * 25;

        return Math.Round(labeledScore + assignedScore + descriptionScore + engagementScore, 1);
    }

    private static object CalculateOrganizationMetrics(List<dynamic> issues, string timeRange)
    {
        var openIssues = issues.Where(i => i.State == "open").ToList();
        var closedIssues = issues.Where(i => i.State == "closed").ToList();

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
            issue_velocity = issues.Count / (double)days,
            resolution_efficiency = closedIssues.Any() ? closedIssues.Count / (double)issues.Count : 0,
            average_resolution_time_days = closedIssues.Any() 
                ? closedIssues.Average(i => ((DateTime)i.UpdatedAt - (DateTime)i.CreatedAt).TotalDays) : 0,
            assignment_rate = issues.Count > 0 ? issues.Count(i => ((string[])i.Assignees).Any()) / (double)issues.Count : 0,
            engagement_index = issues.Any() ? issues.Average(i => (int)i.CommentCount) : 0,
            reynolds_productivity_index = Math.Min(100, closedIssues.Count * 3) // Playful metric
        };
    }

    private static double CalculateProjectHealthScore(List<dynamic> issues)
    {
        if (!issues.Any()) return 100;

        var openIssues = issues.Where(i => i.State == "open").ToList();
        
        // Health factors
        var resolutionRate = issues.Count > 0 ? 
            issues.Count(i => i.State == "closed") / (double)issues.Count : 0;
        var assignmentRate = openIssues.Count > 0 ? 
            openIssues.Count(i => ((string[])i.Assignees).Any()) / (double)openIssues.Count : 1;
        var labelingRate = issues.Count > 0 ? 
            issues.Count(i => ((string[])i.Labels).Any()) / (double)issues.Count : 0;
        var engagementRate = issues.Count > 0 ? 
            issues.Count(i => (int)i.CommentCount > 0) / (double)issues.Count : 0;

        var healthScore = (resolutionRate * 40 + assignmentRate * 25 + labelingRate * 20 + engagementRate * 15) * 100;
        return Math.Round(healthScore, 1);
    }

    private static string GenerateReynoldsProjectInsights(List<dynamic> issues, string analysisType)
    {
        var insights = new List<string>();

        if (!issues.Any())
        {
            insights.Add("Clean slate - either exceptional issue management or time to get the development party started");
            return string.Join("; ", insights);
        }

        var openIssues = issues.Where(i => i.State == "open").ToList();
        var unassignedIssues = openIssues.Count(i => !((string[])i.Assignees).Any());

        if (unassignedIssues > openIssues.Count * 0.5)
        {
            insights.Add("Lots of orphaned issues - time for some Reynolds-style assignment coordination");
        }

        var staleIssues = openIssues.Count(i => (DateTime.UtcNow - (DateTime)i.CreatedAt).Days > 90);
        if (staleIssues > 0)
        {
            insights.Add($"{staleIssues} issues aging like fine wine - might need some Maximum Effort™ attention");
        }

        var highEngagement = issues.Count(i => (int)i.CommentCount > 10);
        if (highEngagement > 0)
        {
            insights.Add($"{highEngagement} hot topic issues with Van Wilder-level discussion energy");
        }

        return insights.Any() ? string.Join("; ", insights) : "Standard project flow with supernatural efficiency metrics";
    }

    private static string[] GenerateStrategicRecommendations(List<dynamic> issues, string organization)
    {
        var recommendations = new List<string>();
        var openIssues = issues.Where(i => i.State == "open").ToList();

        var unassignedRate = openIssues.Count > 0 ? openIssues.Count(i => !((string[])i.Assignees).Any()) / (double)openIssues.Count : 0;
        if (unassignedRate > 0.4)
        {
            recommendations.Add("Implement assignment workflow to reduce unassigned issue backlog");
        }

        var unlabeledRate = issues.Count > 0 ? issues.Count(i => !((string[])i.Labels).Any()) / (double)issues.Count : 0;
        if (unlabeledRate > 0.3)
        {
            recommendations.Add("Enhance issue labeling process for better categorization and prioritization");
        }

        var staleIssues = openIssues.Count(i => (DateTime.UtcNow - (DateTime)i.CreatedAt).Days > 90);
        if (staleIssues > openIssues.Count * 0.2)
        {
            recommendations.Add("Review and address stale issues to maintain project momentum");
        }

        if (!recommendations.Any())
        {
            recommendations.Add("Maintain current project velocity with Reynolds-style Maximum Effort™ coordination");
        }

        return recommendations.ToArray();
    }
}