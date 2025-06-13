using System.Text.Json;
using Shared.Models;

namespace CopilotAgent.Services;

public class ScopeCreepMonitoringService : IScopeCreepMonitoringService
{
    private readonly ILogger<ScopeCreepMonitoringService> _logger;
    private readonly IGitHubIssuesService _githubIssuesService;
    private readonly ISecurityAuditService _auditService;
    private readonly ScopeMonitoringOptions _options;
    private readonly Dictionary<string, List<ScopeCreepAlert>> _alertHistory = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // Reynolds-style messages for scope creep detection
    private readonly string[] _reynoldsMessages = new[]
    {
        "This is amazing work! Also, it's grown like 400% from the original issue. Coincidence?",
        "I'm getting 'feature creep' vibes here. Should we Aviation Gin this into two separate bottles?",
        "Love the additions! Quick check - are we still hitting our deadline or are we in 'time is a construct' territory?",
        "This started as a bicycle and became a Tesla. Impressive! But should we maybe document that?",
        "Hey team, your scope is expanding harder than Marvel adding another post-credits scene. Mind if we split this?",
        "This project grew 400% from the original scope. Not saying it's scope creep, but if it were a movie franchise, Disney would have bought it by now.",
        "Maximum effort on the minimal scope - let's keep this focused like a Deadpool one-liner.",
        "Scope creep is just surprise features wearing a trench coat. Let's unmask this thing."
    };

    public ScopeCreepMonitoringService(
        ILogger<ScopeCreepMonitoringService> logger,
        IGitHubIssuesService githubIssuesService,
        ISecurityAuditService auditService,
        ScopeMonitoringOptions? options = null)
    {
        _logger = logger;
        _githubIssuesService = githubIssuesService;
        _auditService = auditService;
        _options = options ?? new ScopeMonitoringOptions();
    }

    public async Task<ScopeDeviationMetrics> AnalyzeProjectScopeAsync(string repository, ProjectScopeParameters scopeParameters)
    {
        try
        {
            _logger.LogDebug("Analyzing project scope for repository: {Repository}", repository);

            // Get current project statistics from GitHub
            var currentStats = await GetCurrentProjectStatsAsync(repository, scopeParameters);

            // Calculate deviations
            var metrics = new ScopeDeviationMetrics
            {
                ProjectId = scopeParameters.ProjectId,
                Repository = repository,
                Timestamp = DateTime.UtcNow,
                
                // Current counts
                CurrentIssueCount = currentStats.IssueCount,
                CurrentPullRequestCount = currentStats.PullRequestCount,
                CurrentTaskCount = currentStats.TaskCount,
                
                // Expected counts
                ExpectedIssueCount = scopeParameters.ExpectedIssueCount,
                ExpectedPullRequestCount = scopeParameters.ExpectedPullRequestCount,
                ExpectedTaskCount = scopeParameters.ExpectedTaskCount
            };

            // Calculate deviations (positive values indicate growth beyond expected)
            metrics.IssueDeviation = CalculateDeviation(currentStats.IssueCount, scopeParameters.ExpectedIssueCount);
            metrics.PullRequestDeviation = CalculateDeviation(currentStats.PullRequestCount, scopeParameters.ExpectedPullRequestCount);
            metrics.TaskDeviation = CalculateDeviation(currentStats.TaskCount, scopeParameters.ExpectedTaskCount);
            
            // Overall deviation is the average of individual deviations
            metrics.OverallDeviation = (metrics.IssueDeviation + metrics.PullRequestDeviation + metrics.TaskDeviation) / 3.0;

            // Determine if scope creep exists
            metrics.HasScopeCreep = metrics.OverallDeviation > scopeParameters.ScopeDeviationThreshold;

            // Identify specific creep indicators
            if (metrics.IssueDeviation > scopeParameters.ScopeDeviationThreshold)
                metrics.CreepIndicators.Add($"Issue count exceeded expected by {metrics.IssueDeviation:P1}");
            
            if (metrics.PullRequestDeviation > scopeParameters.ScopeDeviationThreshold)
                metrics.CreepIndicators.Add($"Pull request count exceeded expected by {metrics.PullRequestDeviation:P1}");
            
            if (metrics.TaskDeviation > scopeParameters.ScopeDeviationThreshold)
                metrics.CreepIndicators.Add($"Task count exceeded expected by {metrics.TaskDeviation:P1}");

            // Get recent additions for context
            metrics.RecentAdditions = await GetRecentAdditionsAsync(repository, scopeParameters);

            _logger.LogDebug("Scope analysis completed for {Repository}: Overall deviation {Deviation:P2}, Has creep: {HasCreep}",
                repository, metrics.OverallDeviation, metrics.HasScopeCreep);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing project scope for repository: {Repository}", repository);
            throw;
        }
    }

    public async Task<ScopeCreepAlert?> CheckForScopeCreepAsync(string repository, ProjectScopeParameters scopeParameters)
    {
        try
        {
            var metrics = await AnalyzeProjectScopeAsync(repository, scopeParameters);

            if (!metrics.HasScopeCreep)
            {
                _logger.LogDebug("No scope creep detected for repository: {Repository}", repository);
                return null;
            }

            // Create scope creep alert
            var alert = new ScopeCreepAlert
            {
                ProjectId = scopeParameters.ProjectId,
                Repository = repository,
                Timestamp = DateTime.UtcNow,
                Severity = DetermineSeverity(metrics.OverallDeviation),
                Summary = GenerateSummary(metrics),
                ReynoldsMessage = GetRandomReynoldsMessage(),
                Changes = metrics.CreepIndicators.ToList(),
                Recommendations = GenerateRecommendations(metrics, scopeParameters),
                Metrics = metrics
            };

            _logger.LogWarning("Scope creep detected for {Repository}: {Severity} severity, {Deviation:P2} overall deviation",
                repository, alert.Severity, metrics.OverallDeviation);

            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for scope creep in repository: {Repository}", repository);
            throw;
        }
    }

    public async Task RecordScopeEventAsync(string repository, string eventType, Dictionary<string, object> eventData)
    {
        try
        {
            await _auditService.LogEventAsync(
                eventName: "Scope_Event",
                repository: repository,
                action: eventType,
                result: "Recorded",
                details: new { Repository = repository, EventData = eventData });

            _logger.LogDebug("Recorded scope event for {Repository}: {EventType}", repository, eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording scope event for repository: {Repository}, event: {EventType}", 
                repository, eventType);
        }
    }

    public async Task<List<ScopeCreepAlert>> GetRecentAlertsAsync(string repository, TimeSpan window)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_alertHistory.ContainsKey(repository))
                return new List<ScopeCreepAlert>();

            var cutoff = DateTime.UtcNow - window;
            return _alertHistory[repository]
                .Where(alert => alert.Timestamp >= cutoff)
                .OrderByDescending(alert => alert.Timestamp)
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> IsProjectWithinScopeAsync(string repository, ProjectScopeParameters scopeParameters)
    {
        try
        {
            var metrics = await AnalyzeProjectScopeAsync(repository, scopeParameters);
            return !metrics.HasScopeCreep;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if project is within scope for repository: {Repository}", repository);
            return false; // Assume out of scope if we can't determine
        }
    }

    public async Task SendScopeCreepAlertAsync(ScopeCreepAlert alert)
    {
        try
        {
            // Store alert in history
            await _semaphore.WaitAsync();
            try
            {
                if (!_alertHistory.ContainsKey(alert.Repository))
                    _alertHistory[alert.Repository] = new List<ScopeCreepAlert>();

                _alertHistory[alert.Repository].Add(alert);

                // Keep only recent alerts (cleanup old ones)
                var cutoff = DateTime.UtcNow - TimeSpan.FromDays(7);
                _alertHistory[alert.Repository] = _alertHistory[alert.Repository]
                    .Where(a => a.Timestamp >= cutoff)
                    .ToList();
            }
            finally
            {
                _semaphore.Release();
            }

            // Log the alert
            await _auditService.LogEventAsync(
                eventName: "Scope_Creep_Alert",
                repository: alert.Repository,
                action: alert.Severity.ToString(),
                result: "Alert_Sent",
                details: alert);

            _logger.LogWarning("Scope creep alert sent for {Repository}: {Summary}", 
                alert.Repository, alert.Summary);

            // In a real implementation, this would also send notifications 
            // (e.g., Teams message, email, GitHub comment, etc.)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending scope creep alert for repository: {Repository}", alert.Repository);
        }
    }

    private async Task<ProjectStats> GetCurrentProjectStatsAsync(string repository, ProjectScopeParameters scopeParameters)
    {
        try
        {
            // Get issues from GitHub repository
            var issues = await _githubIssuesService.GetIssuesByRepositoryAsync(repository, "open");
            var openIssues = issues.Count();

            // Get all issues (including closed) to get total count
            var allIssues = await _githubIssuesService.GetIssuesByRepositoryAsync(repository, "all");
            var totalIssues = allIssues.Count();

            // For this implementation, we'll simulate PR and task counts
            // In a real implementation, these would come from GitHub API calls for PRs and project items
            var estimatedPrs = (int)(totalIssues * 0.3); // Rough estimation: 30% of issues have PRs
            var estimatedTasks = totalIssues; // Assuming 1:1 ratio for simplicity

            return new ProjectStats
            {
                IssueCount = totalIssues,
                PullRequestCount = estimatedPrs,
                TaskCount = estimatedTasks
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current project stats for repository: {Repository}", repository);
            // Return default stats to avoid throwing
            return new ProjectStats { IssueCount = 0, PullRequestCount = 0, TaskCount = 0 };
        }
    }

    private static double CalculateDeviation(int current, int expected)
    {
        if (expected == 0)
            return current > 0 ? 1.0 : 0.0; // 100% deviation if we expected none but have some

        return Math.Max(0, (double)(current - expected) / expected);
    }

    private static ScopeCreepSeverity DetermineSeverity(double overallDeviation)
    {
        return overallDeviation switch
        {
            >= 1.0 => ScopeCreepSeverity.Critical,  // 100%+ deviation
            >= 0.5 => ScopeCreepSeverity.High,      // 50-99% deviation
            >= 0.25 => ScopeCreepSeverity.Medium,   // 25-49% deviation
            _ => ScopeCreepSeverity.Low              // Below 25% deviation
        };
    }

    private static string GenerateSummary(ScopeDeviationMetrics metrics)
    {
        var parts = new List<string>();
        
        if (metrics.IssueDeviation > 0)
            parts.Add($"Issues: {metrics.CurrentIssueCount} vs {metrics.ExpectedIssueCount} expected (+{metrics.IssueDeviation:P1})");
        
        if (metrics.PullRequestDeviation > 0)
            parts.Add($"PRs: {metrics.CurrentPullRequestCount} vs {metrics.ExpectedPullRequestCount} expected (+{metrics.PullRequestDeviation:P1})");
        
        if (metrics.TaskDeviation > 0)
            parts.Add($"Tasks: {metrics.CurrentTaskCount} vs {metrics.ExpectedTaskCount} expected (+{metrics.TaskDeviation:P1})");

        return string.Join(", ", parts);
    }

    private string GetRandomReynoldsMessage()
    {
        if (!_options.EnableReynoldsMessages)
            return string.Empty;

        var random = new Random();
        return _reynoldsMessages[random.Next(_reynoldsMessages.Length)];
    }

    private static List<string> GenerateRecommendations(ScopeDeviationMetrics metrics, ProjectScopeParameters scopeParameters)
    {
        var recommendations = new List<string>();

        if (metrics.OverallDeviation > 0.5) // High deviation
        {
            recommendations.Add("Consider splitting this project into smaller, more manageable phases");
            recommendations.Add("Review and update project scope parameters to reflect current reality");
            recommendations.Add("Assess timeline impact and communicate changes to stakeholders");
        }
        else if (metrics.OverallDeviation > 0.25) // Medium deviation
        {
            recommendations.Add("Monitor closely to prevent further scope expansion");
            recommendations.Add("Consider prioritizing features to stay within original scope");
            recommendations.Add("Update project documentation to reflect scope changes");
        }
        else // Low deviation
        {
            recommendations.Add("Keep monitoring scope to catch early signs of expansion");
            recommendations.Add("Document the reasons for current scope increase");
        }

        // Add specific recommendations based on which areas have the most creep
        if (metrics.IssueDeviation > metrics.PullRequestDeviation && metrics.IssueDeviation > metrics.TaskDeviation)
        {
            recommendations.Add("Focus on issue management - consider closing or consolidating similar issues");
        }
        else if (metrics.PullRequestDeviation > metrics.IssueDeviation && metrics.PullRequestDeviation > metrics.TaskDeviation)
        {
            recommendations.Add("Review PR scope - some PRs may be doing more than their original issues intended");
        }

        return recommendations;
    }

    private Task<List<string>> GetRecentAdditionsAsync(string repository, ProjectScopeParameters scopeParameters)
    {
        try
        {
            // This would typically query GitHub for recently created issues/PRs
            // For now, return a simplified list
            var recentAdditions = new List<string>();
            
            // In a real implementation, this would be populated from GitHub API calls
            // showing recently created issues, PRs, or project items
            
            return Task.FromResult(recentAdditions.Take(_options.MaxRecentAdditionsToTrack).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent additions for repository: {Repository}", repository);
            return Task.FromResult(new List<string>());
        }
    }

    private class ProjectStats
    {
        public int IssueCount { get; set; }
        public int PullRequestCount { get; set; }
        public int TaskCount { get; set; }
    }
}