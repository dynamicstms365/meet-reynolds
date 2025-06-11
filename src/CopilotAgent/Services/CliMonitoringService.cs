using Shared.Models;
using System.Collections.Concurrent;

namespace CopilotAgent.Services;

public interface ICliMonitoringService
{
    Task RecordOperationAsync(CliOperationMetrics metrics);
    Task<CliMonitoringStats> GetStatsAsync(TimeSpan window);
    Task<bool> IsHealthyAsync();
    Task CheckAlertsAsync();
}

public class CliMonitoringService : ICliMonitoringService
{
    private readonly ILogger<CliMonitoringService> _logger;
    private readonly ISecurityAuditService _auditService;
    private readonly CliMonitoringOptions _options;
    private readonly ConcurrentQueue<CliOperationMetrics> _operationHistory = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public CliMonitoringService(
        ILogger<CliMonitoringService> logger, 
        ISecurityAuditService auditService,
        CliMonitoringOptions? options = null)
    {
        _logger = logger;
        _auditService = auditService;
        _options = options ?? new CliMonitoringOptions();
    }

    public async Task RecordOperationAsync(CliOperationMetrics metrics)
    {
        try
        {
            _operationHistory.Enqueue(metrics);
            
            // Clean up old entries
            await CleanupOldEntriesAsync();
            
            // Check if we need to trigger alerts
            if (_options.EnableAlerting)
            {
                await CheckAlertsAsync();
            }

            _logger.LogDebug("Recorded CLI operation: {Tool} - {Success} - {ExecutionTime}ms", 
                metrics.CliTool, metrics.Success, metrics.ExecutionTime.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording CLI operation metrics");
        }
    }

    public async Task<CliMonitoringStats> GetStatsAsync(TimeSpan window)
    {
        await _semaphore.WaitAsync();
        try
        {
            var cutoff = DateTime.UtcNow - window;
            var recentOperations = _operationHistory
                .Where(op => op.Timestamp >= cutoff)
                .ToList();

            if (!recentOperations.Any())
            {
                return new CliMonitoringStats
                {
                    Window = window,
                    TotalOperations = 0,
                    SuccessRate = 1.0,
                    AverageExecutionTime = TimeSpan.Zero,
                    ToolStats = new Dictionary<string, CliToolStats>()
                };
            }

            var successfulOps = recentOperations.Count(op => op.Success);
            var totalOps = recentOperations.Count;
            var successRate = (double)successfulOps / totalOps;

            var avgExecutionTime = TimeSpan.FromMilliseconds(
                recentOperations.Average(op => op.ExecutionTime.TotalMilliseconds));

            var toolStats = recentOperations
                .GroupBy(op => op.CliTool)
                .ToDictionary(g => g.Key, g => new CliToolStats
                {
                    TotalOperations = g.Count(),
                    SuccessfulOperations = g.Count(op => op.Success),
                    SuccessRate = (double)g.Count(op => op.Success) / g.Count(),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(
                        g.Average(op => op.ExecutionTime.TotalMilliseconds)),
                    TotalRetries = g.Sum(op => op.RetryCount)
                });

            return new CliMonitoringStats
            {
                Window = window,
                TotalOperations = totalOps,
                SuccessfulOperations = successfulOps,
                SuccessRate = successRate,
                AverageExecutionTime = avgExecutionTime,
                ToolStats = toolStats
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var stats = await GetStatsAsync(_options.MonitoringWindow);
            
            if (stats.TotalOperations == 0)
            {
                return true; // No operations to assess
            }

            var isHealthy = stats.SuccessRate >= _options.SuccessRateThreshold;
            
            if (!isHealthy)
            {
                _logger.LogWarning("CLI service health check failed: Success rate {SuccessRate:P2} is below threshold {Threshold:P2}",
                    stats.SuccessRate, _options.SuccessRateThreshold);
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking CLI service health");
            return false;
        }
    }

    public async Task CheckAlertsAsync()
    {
        try
        {
            var stats = await GetStatsAsync(_options.MonitoringWindow);
            
            if (stats.TotalOperations == 0)
            {
                return; // No operations to check
            }

            // Check overall success rate
            if (stats.SuccessRate < _options.SuccessRateThreshold)
            {
                await TriggerAlertAsync("CLI_SuccessRate_Low", new
                {
                    CurrentSuccessRate = stats.SuccessRate,
                    Threshold = _options.SuccessRateThreshold,
                    Window = _options.MonitoringWindow.TotalMinutes,
                    TotalOperations = stats.TotalOperations
                });
            }

            // Check per-tool success rates
            foreach (var toolStat in stats.ToolStats)
            {
                if (toolStat.Value.SuccessRate < _options.SuccessRateThreshold)
                {
                    await TriggerAlertAsync("CLI_Tool_SuccessRate_Low", new
                    {
                        Tool = toolStat.Key,
                        CurrentSuccessRate = toolStat.Value.SuccessRate,
                        Threshold = _options.SuccessRateThreshold,
                        Window = _options.MonitoringWindow.TotalMinutes,
                        TotalOperations = toolStat.Value.TotalOperations
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking CLI alerts");
        }
    }

    private async Task CleanupOldEntriesAsync()
    {
        var cutoff = DateTime.UtcNow - TimeSpan.FromHours(24); // Keep 24 hours of history
        var entriesToRemove = new List<CliOperationMetrics>();

        // This is a simple cleanup - in production, you'd want more efficient data structures
        while (_operationHistory.TryPeek(out var oldestEntry) && oldestEntry.Timestamp < cutoff)
        {
            _operationHistory.TryDequeue(out _);
        }

        await Task.CompletedTask;
    }

    private async Task TriggerAlertAsync(string alertType, object details)
    {
        _logger.LogWarning("CLI Alert triggered: {AlertType} - {Details}", alertType, details);
        
        await _auditService.LogEventAsync(
            eventName: "CLI_Alert",
            action: alertType,
            result: "Alert_Triggered",
            details: details);
    }
}

public class CliMonitoringStats
{
    public TimeSpan Window { get; set; }
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public Dictionary<string, CliToolStats> ToolStats { get; set; } = new();
}

public class CliToolStats
{
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public int TotalRetries { get; set; }
}