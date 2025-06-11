using Shared.Models;
using System.Collections.Concurrent;

namespace CopilotAgent.Services;

public interface ITelemetryService
{
    void RecordRequestProcessed(string requestId, IntentType intentType, bool success, double processingTimeMs);
    void RecordIntentRecognition(IntentType intentType, double confidence);
    void RecordError(string operation, Exception exception);
    void RecordCustomMetric(string metricName, double value, Dictionary<string, object>? properties = null);
    AgentMetrics GetMetrics();
    void ResetMetrics();
}

public class TelemetryService : ITelemetryService
{
    private readonly ILogger<TelemetryService> _logger;
    private readonly IConfigurationService _configurationService;
    
    // Thread-safe collections for metrics
    private readonly ConcurrentDictionary<IntentType, long> _intentCounts = new();
    private readonly ConcurrentQueue<RequestMetric> _recentRequests = new();
    private readonly ConcurrentQueue<string> _recentErrors = new();
    private readonly object _metricsLock = new();
    
    private long _totalRequests;
    private long _successfulRequests;
    private long _failedRequests;
    private double _totalProcessingTime;
    private DateTime _metricsStartTime;

    public TelemetryService(ILogger<TelemetryService> logger, IConfigurationService configurationService)
    {
        _logger = logger;
        _configurationService = configurationService;
        _metricsStartTime = DateTime.UtcNow;
    }

    public void RecordRequestProcessed(string requestId, IntentType intentType, bool success, double processingTimeMs)
    {
        try
        {
            var config = _configurationService.GetConfiguration();
            
            if (!config.Telemetry.EnablePerformanceMetrics)
                return;

            // Update counters
            Interlocked.Increment(ref _totalRequests);
            
            if (success)
            {
                Interlocked.Increment(ref _successfulRequests);
            }
            else
            {
                Interlocked.Increment(ref _failedRequests);
            }

            // Update processing time
            lock (_metricsLock)
            {
                _totalProcessingTime += processingTimeMs;
            }

            // Update intent counts
            _intentCounts.AddOrUpdate(intentType, 1, (key, current) => current + 1);

            // Store recent request for trend analysis
            var requestMetric = new RequestMetric
            {
                RequestId = requestId,
                IntentType = intentType,
                Success = success,
                ProcessingTimeMs = processingTimeMs,
                Timestamp = DateTime.UtcNow
            };

            _recentRequests.Enqueue(requestMetric);

            // Keep only recent requests (last 1000)
            while (_recentRequests.Count > 1000)
            {
                _recentRequests.TryDequeue(out _);
            }

            if (config.Telemetry.EnableDetailedLogging)
            {
                _logger.LogDebug("Request processed: {RequestId}, Intent: {Intent}, Success: {Success}, Time: {ProcessingTime}ms",
                    requestId, intentType, success, processingTimeMs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record request metrics");
        }
    }

    public void RecordIntentRecognition(IntentType intentType, double confidence)
    {
        try
        {
            var config = _configurationService.GetConfiguration();
            
            if (config.Telemetry.EnableDetailedLogging)
            {
                _logger.LogDebug("Intent recognized: {Intent} with confidence {Confidence:P2}", intentType, confidence);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record intent recognition metrics");
        }
    }

    public void RecordError(string operation, Exception exception)
    {
        try
        {
            var errorMessage = $"{operation}: {exception.GetType().Name} - {exception.Message}";
            _recentErrors.Enqueue(errorMessage);

            // Keep only recent errors (last 100)
            while (_recentErrors.Count > 100)
            {
                _recentErrors.TryDequeue(out _);
            }

            _logger.LogError(exception, "Operation failed: {Operation}", operation);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record error metrics");
        }
    }

    public void RecordCustomMetric(string metricName, double value, Dictionary<string, object>? properties = null)
    {
        try
        {
            var config = _configurationService.GetConfiguration();
            
            if (config.Telemetry.EnableDetailedLogging)
            {
                var propertiesStr = properties != null 
                    ? string.Join(", ", properties.Select(p => $"{p.Key}={p.Value}"))
                    : "none";
                
                _logger.LogInformation("Custom metric: {MetricName} = {Value}, Properties: {Properties}",
                    metricName, value, propertiesStr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record custom metric: {MetricName}", metricName);
        }
    }

    public AgentMetrics GetMetrics()
    {
        try
        {
            var totalRequests = Interlocked.Read(ref _totalRequests);
            var successfulRequests = Interlocked.Read(ref _successfulRequests);
            var failedRequests = Interlocked.Read(ref _failedRequests);

            double averageProcessingTime;
            lock (_metricsLock)
            {
                averageProcessingTime = totalRequests > 0 ? _totalProcessingTime / totalRequests : 0;
            }

            var accuracyRate = totalRequests > 0 ? (double)successfulRequests / totalRequests : 0.0;

            // Determine agent status based on metrics
            var status = DetermineAgentStatus(accuracyRate, averageProcessingTime);

            return new AgentMetrics
            {
                LastUpdated = DateTime.UtcNow,
                TotalRequests = totalRequests,
                SuccessfulRequests = successfulRequests,
                FailedRequests = failedRequests,
                AverageProcessingTimeMs = averageProcessingTime,
                AccuracyRate = accuracyRate,
                IntentCounts = new Dictionary<IntentType, long>(_intentCounts),
                RecentErrors = _recentErrors.ToList(),
                Status = status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate metrics");
            return new AgentMetrics { Status = AgentStatus.Unhealthy };
        }
    }

    public void ResetMetrics()
    {
        try
        {
            Interlocked.Exchange(ref _totalRequests, 0);
            Interlocked.Exchange(ref _successfulRequests, 0);
            Interlocked.Exchange(ref _failedRequests, 0);

            lock (_metricsLock)
            {
                _totalProcessingTime = 0;
            }

            _intentCounts.Clear();
            
            while (_recentRequests.TryDequeue(out _)) { }
            while (_recentErrors.TryDequeue(out _)) { }

            _metricsStartTime = DateTime.UtcNow;
            
            _logger.LogInformation("Metrics reset successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset metrics");
        }
    }

    private AgentStatus DetermineAgentStatus(double accuracyRate, double averageProcessingTime)
    {
        var config = _configurationService.GetConfiguration();
        
        // Check if agent is meeting accuracy targets
        if (accuracyRate < config.Processing.AccuracyTarget * 0.8) // 80% of target
        {
            return AgentStatus.Unhealthy;
        }
        
        if (accuracyRate < config.Processing.AccuracyTarget * 0.9) // 90% of target
        {
            return AgentStatus.Degraded;
        }

        // Check processing time
        if (averageProcessingTime > config.HealthCheck.UnhealthyThresholdMs)
        {
            return AgentStatus.Degraded;
        }

        return AgentStatus.Healthy;
    }
}

public class RequestMetric
{
    public string RequestId { get; set; } = string.Empty;
    public IntentType IntentType { get; set; }
    public bool Success { get; set; }
    public double ProcessingTimeMs { get; set; }
    public DateTime Timestamp { get; set; }
}