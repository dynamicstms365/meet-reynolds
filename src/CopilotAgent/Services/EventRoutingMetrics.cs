using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CopilotAgent.Services;

/// <summary>
/// Event Routing Metrics Service for Issue #73
/// Provides comprehensive metrics collection and monitoring for cross-platform event routing
/// Integrates with existing loop prevention and monitoring infrastructure
/// </summary>
public interface IEventRoutingMetrics
{
    Task RecordEventRoutingAsync(string sourcePlatform, int routeCount, int successCount, TimeSpan processingTime);
    Task RecordClassificationMetricsAsync(string category, EventPriority priority, double confidence, TimeSpan processingTime);
    Task RecordPlatformLatencyAsync(string targetPlatform, TimeSpan latency, bool success);
    Task<EventRoutingStats> GetRoutingStatsAsync(TimeSpan period);
    Task<Dictionary<string, object>> GetPrometheusMetricsAsync();
    Task IncrementDuplicateEventCountAsync(string platform);
    Task RecordRoutingFailureAsync(string sourcePlatform, string targetPlatform, string error);
}

public class EventRoutingMetrics : IEventRoutingMetrics
{
    private readonly ILogger<EventRoutingMetrics> _logger;
    private readonly IConfiguration _configuration;
    
    // Thread-safe metrics collections
    private readonly ConcurrentDictionary<string, RoutingMetric> _routingMetrics;
    private readonly ConcurrentDictionary<string, ClassificationMetric> _classificationMetrics;
    private readonly ConcurrentDictionary<string, PlatformLatencyMetric> _latencyMetrics;
    private readonly ConcurrentDictionary<string, long> _duplicateEventCounts;
    private readonly ConcurrentDictionary<string, FailureMetric> _failureMetrics;
    
    // Performance counters
    private readonly ConcurrentDictionary<string, PerformanceCounter> _performanceCounters;
    
    // Metrics retention settings
    private readonly TimeSpan _metricsRetention;
    private readonly Timer _cleanupTimer;

    public EventRoutingMetrics(
        ILogger<EventRoutingMetrics> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        _routingMetrics = new ConcurrentDictionary<string, RoutingMetric>();
        _classificationMetrics = new ConcurrentDictionary<string, ClassificationMetric>();
        _latencyMetrics = new ConcurrentDictionary<string, PlatformLatencyMetric>();
        _duplicateEventCounts = new ConcurrentDictionary<string, long>();
        _failureMetrics = new ConcurrentDictionary<string, FailureMetric>();
        _performanceCounters = new ConcurrentDictionary<string, PerformanceCounter>();
        
        _metricsRetention = TimeSpan.FromHours(configuration.GetValue<int>("EventRouting:MetricsRetentionHours", 24));
        
        // Setup cleanup timer to run every hour
        _cleanupTimer = new Timer(CleanupOldMetrics, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
    }

    public async Task RecordEventRoutingAsync(string sourcePlatform, int routeCount, int successCount, TimeSpan processingTime)
    {
        try
        {
            var key = $"{sourcePlatform}_{DateTime.UtcNow:yyyy-MM-dd-HH}";
            
            _routingMetrics.AddOrUpdate(key, 
                new RoutingMetric
                {
                    SourcePlatform = sourcePlatform,
                    TotalEvents = 1,
                    TotalRoutes = routeCount,
                    SuccessfulRoutes = successCount,
                    FailedRoutes = routeCount - successCount,
                    TotalProcessingTime = processingTime,
                    Timestamp = DateTime.UtcNow
                },
                (k, existing) => 
                {
                    existing.TotalEvents++;
                    existing.TotalRoutes += routeCount;
                    existing.SuccessfulRoutes += successCount;
                    existing.FailedRoutes += (routeCount - successCount);
                    existing.TotalProcessingTime += processingTime;
                    return existing;
                });

            // Update performance counters
            UpdatePerformanceCounter("total_events_routed", 1);
            UpdatePerformanceCounter("total_routes_executed", routeCount);
            UpdatePerformanceCounter("successful_routes", successCount);
            UpdatePerformanceCounter($"{sourcePlatform.ToLower()}_events", 1);

            _logger.LogDebug("Recorded routing metrics: {SourcePlatform}, Routes: {RouteCount}, Success: {SuccessCount}, Time: {ProcessingTime}ms",
                sourcePlatform, routeCount, successCount, processingTime.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording routing metrics");
        }
    }

    public async Task RecordClassificationMetricsAsync(string category, EventPriority priority, double confidence, TimeSpan processingTime)
    {
        try
        {
            var key = $"{category}_{priority}_{DateTime.UtcNow:yyyy-MM-dd-HH}";
            
            _classificationMetrics.AddOrUpdate(key,
                new ClassificationMetric
                {
                    Category = category,
                    Priority = priority,
                    Count = 1,
                    TotalConfidence = confidence,
                    AverageConfidence = confidence,
                    TotalProcessingTime = processingTime,
                    Timestamp = DateTime.UtcNow
                },
                (k, existing) =>
                {
                    existing.Count++;
                    existing.TotalConfidence += confidence;
                    existing.AverageConfidence = existing.TotalConfidence / existing.Count;
                    existing.TotalProcessingTime += processingTime;
                    return existing;
                });

            // Update performance counters for classifications
            UpdatePerformanceCounter("total_classifications", 1);
            UpdatePerformanceCounter($"category_{category.ToLower()}", 1);
            UpdatePerformanceCounter($"priority_{priority.ToString().ToLower()}", 1);

            if (confidence >= 0.9)
                UpdatePerformanceCounter("high_confidence_classifications", 1);
            else if (confidence < 0.5)
                UpdatePerformanceCounter("low_confidence_classifications", 1);

            _logger.LogDebug("Recorded classification metrics: {Category}, {Priority}, Confidence: {Confidence:P1}, Time: {ProcessingTime}ms",
                category, priority, confidence, processingTime.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording classification metrics");
        }
    }

    public async Task RecordPlatformLatencyAsync(string targetPlatform, TimeSpan latency, bool success)
    {
        try
        {
            var key = $"{targetPlatform}_{DateTime.UtcNow:yyyy-MM-dd-HH}";
            
            _latencyMetrics.AddOrUpdate(key,
                new PlatformLatencyMetric
                {
                    TargetPlatform = targetPlatform,
                    Count = 1,
                    TotalLatency = latency,
                    AverageLatency = latency,
                    MinLatency = latency,
                    MaxLatency = latency,
                    SuccessCount = success ? 1 : 0,
                    FailureCount = success ? 0 : 1,
                    Timestamp = DateTime.UtcNow
                },
                (k, existing) =>
                {
                    existing.Count++;
                    existing.TotalLatency += latency;
                    existing.AverageLatency = TimeSpan.FromMilliseconds(existing.TotalLatency.TotalMilliseconds / existing.Count);
                    existing.MinLatency = latency < existing.MinLatency ? latency : existing.MinLatency;
                    existing.MaxLatency = latency > existing.MaxLatency ? latency : existing.MaxLatency;
                    
                    if (success)
                        existing.SuccessCount++;
                    else
                        existing.FailureCount++;
                    
                    return existing;
                });

            // Update performance counters for platform latency
            UpdatePerformanceCounter($"{targetPlatform.ToLower()}_requests", 1);
            if (success)
                UpdatePerformanceCounter($"{targetPlatform.ToLower()}_successes", 1);
            else
                UpdatePerformanceCounter($"{targetPlatform.ToLower()}_failures", 1);

            _logger.LogDebug("Recorded platform latency: {TargetPlatform}, Latency: {Latency}ms, Success: {Success}",
                targetPlatform, latency.TotalMilliseconds, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording platform latency metrics");
        }
    }

    public async Task<EventRoutingStats> GetRoutingStatsAsync(TimeSpan period)
    {
        try
        {
            var cutoff = DateTime.UtcNow - period;
            
            var relevantRoutingMetrics = _routingMetrics.Values
                .Where(m => m.Timestamp >= cutoff)
                .ToList();
                
            var relevantClassificationMetrics = _classificationMetrics.Values
                .Where(m => m.Timestamp >= cutoff)
                .ToList();
                
            var relevantLatencyMetrics = _latencyMetrics.Values
                .Where(m => m.Timestamp >= cutoff)
                .ToList();

            var stats = new EventRoutingStats
            {
                Period = period,
                GeneratedAt = DateTime.UtcNow,
                
                // Routing statistics
                TotalEventsProcessed = relevantRoutingMetrics.Sum(m => m.TotalEvents),
                TotalRoutesExecuted = relevantRoutingMetrics.Sum(m => m.TotalRoutes),
                SuccessfulRoutes = relevantRoutingMetrics.Sum(m => m.SuccessfulRoutes),
                FailedRoutes = relevantRoutingMetrics.Sum(m => m.FailedRoutes),
                
                // Classification statistics
                TotalClassifications = relevantClassificationMetrics.Sum(m => m.Count),
                AverageClassificationConfidence = relevantClassificationMetrics.Any() 
                    ? relevantClassificationMetrics.Average(m => m.AverageConfidence) 
                    : 0.0,
                
                // Platform statistics
                PlatformStats = GeneratePlatformStats(relevantRoutingMetrics, relevantLatencyMetrics),
                
                // Performance metrics
                AverageProcessingTime = relevantRoutingMetrics.Any()
                    ? TimeSpan.FromMilliseconds(relevantRoutingMetrics.Average(m => m.TotalProcessingTime.TotalMilliseconds / m.TotalEvents))
                    : TimeSpan.Zero,
                
                // Category breakdown
                CategoryBreakdown = GenerateCategoryBreakdown(relevantClassificationMetrics),
                
                // Priority breakdown
                PriorityBreakdown = GeneratePriorityBreakdown(relevantClassificationMetrics)
            };

            stats.SuccessRate = stats.TotalRoutesExecuted > 0 
                ? (double)stats.SuccessfulRoutes / stats.TotalRoutesExecuted 
                : 1.0;

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating routing statistics");
            return new EventRoutingStats { Period = period, GeneratedAt = DateTime.UtcNow };
        }
    }

    public async Task<Dictionary<string, object>> GetPrometheusMetricsAsync()
    {
        try
        {
            var metrics = new Dictionary<string, object>();
            
            // Add performance counters as Prometheus metrics
            foreach (var counter in _performanceCounters)
            {
                metrics[$"reynolds_event_routing_{counter.Key}"] = counter.Value.Value;
            }

            // Add aggregated metrics
            var recentStats = await GetRoutingStatsAsync(TimeSpan.FromHours(1));
            
            metrics["reynolds_event_routing_success_rate"] = recentStats.SuccessRate;
            metrics["reynolds_event_routing_average_processing_time_ms"] = recentStats.AverageProcessingTime.TotalMilliseconds;
            metrics["reynolds_event_routing_events_processed_total"] = recentStats.TotalEventsProcessed;
            metrics["reynolds_event_routing_routes_executed_total"] = recentStats.TotalRoutesExecuted;
            metrics["reynolds_event_routing_classification_confidence_average"] = recentStats.AverageClassificationConfidence;

            // Add platform-specific metrics
            foreach (var platformStat in recentStats.PlatformStats)
            {
                var platformName = platformStat.Key.ToLower();
                metrics[$"reynolds_event_routing_{platformName}_events_total"] = platformStat.Value.EventCount;
                metrics[$"reynolds_event_routing_{platformName}_success_rate"] = platformStat.Value.SuccessRate;
                metrics[$"reynolds_event_routing_{platformName}_average_latency_ms"] = platformStat.Value.AverageLatency.TotalMilliseconds;
            }

            // Add duplicate event counts
            foreach (var duplicateCount in _duplicateEventCounts)
            {
                metrics[$"reynolds_event_routing_duplicate_events_{duplicateCount.Key.ToLower()}"] = duplicateCount.Value;
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Prometheus metrics");
            return new Dictionary<string, object>();
        }
    }

    public async Task IncrementDuplicateEventCountAsync(string platform)
    {
        try
        {
            _duplicateEventCounts.AddOrUpdate(platform, 1, (key, value) => value + 1);
            UpdatePerformanceCounter("duplicate_events_detected", 1);
            UpdatePerformanceCounter($"duplicate_events_{platform.ToLower()}", 1);
            
            _logger.LogDebug("Incremented duplicate event count for platform: {Platform}", platform);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing duplicate event count");
        }
    }

    public async Task RecordRoutingFailureAsync(string sourcePlatform, string targetPlatform, string error)
    {
        try
        {
            var key = $"{sourcePlatform}_to_{targetPlatform}_{DateTime.UtcNow:yyyy-MM-dd-HH}";
            
            _failureMetrics.AddOrUpdate(key,
                new FailureMetric
                {
                    SourcePlatform = sourcePlatform,
                    TargetPlatform = targetPlatform,
                    FailureCount = 1,
                    LastError = error,
                    Timestamp = DateTime.UtcNow
                },
                (k, existing) =>
                {
                    existing.FailureCount++;
                    existing.LastError = error;
                    existing.Timestamp = DateTime.UtcNow;
                    return existing;
                });

            UpdatePerformanceCounter("routing_failures_total", 1);
            UpdatePerformanceCounter($"failures_{sourcePlatform.ToLower()}_to_{targetPlatform.ToLower()}", 1);
            
            _logger.LogDebug("Recorded routing failure: {SourcePlatform} -> {TargetPlatform}, Error: {Error}",
                sourcePlatform, targetPlatform, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording routing failure");
        }
    }

    // Private helper methods

    private void UpdatePerformanceCounter(string counterName, long increment)
    {
        _performanceCounters.AddOrUpdate(counterName,
            new PerformanceCounter { Value = increment, LastUpdated = DateTime.UtcNow },
            (key, existing) =>
            {
                existing.Value += increment;
                existing.LastUpdated = DateTime.UtcNow;
                return existing;
            });
    }

    private Dictionary<string, PlatformStatistic> GeneratePlatformStats(
        List<RoutingMetric> routingMetrics, 
        List<PlatformLatencyMetric> latencyMetrics)
    {
        var platformStats = new Dictionary<string, PlatformStatistic>();
        
        // Group by platform and calculate statistics
        var platforms = routingMetrics.Select(m => m.SourcePlatform)
            .Concat(latencyMetrics.Select(m => m.TargetPlatform))
            .Distinct();

        foreach (var platform in platforms)
        {
            var platformRoutingMetrics = routingMetrics.Where(m => m.SourcePlatform == platform).ToList();
            var platformLatencyMetrics = latencyMetrics.Where(m => m.TargetPlatform == platform).ToList();
            
            var eventCount = platformRoutingMetrics.Sum(m => m.TotalEvents);
            var successCount = platformRoutingMetrics.Sum(m => m.SuccessfulRoutes);
            var totalRoutes = platformRoutingMetrics.Sum(m => m.TotalRoutes);
            
            var averageLatency = platformLatencyMetrics.Any()
                ? TimeSpan.FromMilliseconds(platformLatencyMetrics.Average(m => m.AverageLatency.TotalMilliseconds))
                : TimeSpan.Zero;

            platformStats[platform] = new PlatformStatistic
            {
                Platform = platform,
                EventCount = eventCount,
                SuccessRate = totalRoutes > 0 ? (double)successCount / totalRoutes : 1.0,
                AverageLatency = averageLatency,
                TotalRequests = platformLatencyMetrics.Sum(m => m.Count)
            };
        }
        
        return platformStats;
    }

    private Dictionary<string, int> GenerateCategoryBreakdown(List<ClassificationMetric> classificationMetrics)
    {
        return classificationMetrics
            .GroupBy(m => m.Category)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Count));
    }

    private Dictionary<string, int> GeneratePriorityBreakdown(List<ClassificationMetric> classificationMetrics)
    {
        return classificationMetrics
            .GroupBy(m => m.Priority.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Count));
    }

    private void CleanupOldMetrics(object? state)
    {
        try
        {
            var cutoff = DateTime.UtcNow - _metricsRetention;
            
            // Clean up routing metrics
            var oldRoutingKeys = _routingMetrics
                .Where(kvp => kvp.Value.Timestamp < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in oldRoutingKeys)
            {
                _routingMetrics.TryRemove(key, out _);
            }

            // Clean up classification metrics
            var oldClassificationKeys = _classificationMetrics
                .Where(kvp => kvp.Value.Timestamp < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in oldClassificationKeys)
            {
                _classificationMetrics.TryRemove(key, out _);
            }

            // Clean up latency metrics
            var oldLatencyKeys = _latencyMetrics
                .Where(kvp => kvp.Value.Timestamp < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in oldLatencyKeys)
            {
                _latencyMetrics.TryRemove(key, out _);
            }

            // Clean up failure metrics
            var oldFailureKeys = _failureMetrics
                .Where(kvp => kvp.Value.Timestamp < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in oldFailureKeys)
            {
                _failureMetrics.TryRemove(key, out _);
            }

            _logger.LogDebug("Cleaned up old metrics: {RoutingKeys} routing, {ClassificationKeys} classification, {LatencyKeys} latency, {FailureKeys} failure",
                oldRoutingKeys.Count, oldClassificationKeys.Count, oldLatencyKeys.Count, oldFailureKeys.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during metrics cleanup");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}

// Supporting classes for metrics
public class RoutingMetric
{
    public string SourcePlatform { get; set; } = "";
    public int TotalEvents { get; set; }
    public int TotalRoutes { get; set; }
    public int SuccessfulRoutes { get; set; }
    public int FailedRoutes { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ClassificationMetric
{
    public string Category { get; set; } = "";
    public EventPriority Priority { get; set; }
    public int Count { get; set; }
    public double TotalConfidence { get; set; }
    public double AverageConfidence { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public DateTime Timestamp { get; set; }
}

public class PlatformLatencyMetric
{
    public string TargetPlatform { get; set; } = "";
    public int Count { get; set; }
    public TimeSpan TotalLatency { get; set; }
    public TimeSpan AverageLatency { get; set; }
    public TimeSpan MinLatency { get; set; }
    public TimeSpan MaxLatency { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime Timestamp { get; set; }
}

public class FailureMetric
{
    public string SourcePlatform { get; set; } = "";
    public string TargetPlatform { get; set; } = "";
    public int FailureCount { get; set; }
    public string LastError { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class PerformanceCounter
{
    public long Value { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class EventRoutingStats
{
    public TimeSpan Period { get; set; }
    public DateTime GeneratedAt { get; set; }
    public long TotalEventsProcessed { get; set; }
    public long TotalRoutesExecuted { get; set; }
    public long SuccessfulRoutes { get; set; }
    public long FailedRoutes { get; set; }
    public double SuccessRate { get; set; }
    public long TotalClassifications { get; set; }
    public double AverageClassificationConfidence { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
    public Dictionary<string, PlatformStatistic> PlatformStats { get; set; } = new();
    public Dictionary<string, int> CategoryBreakdown { get; set; } = new();
    public Dictionary<string, int> PriorityBreakdown { get; set; } = new();
}

public class PlatformStatistic
{
    public string Platform { get; set; } = "";
    public long EventCount { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageLatency { get; set; }
    public long TotalRequests { get; set; }
}