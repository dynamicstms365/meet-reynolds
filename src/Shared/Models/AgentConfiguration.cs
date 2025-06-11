namespace Shared.Models;

public class AgentConfiguration
{
    public IntentRecognitionConfig IntentRecognition { get; set; } = new();
    public RetryPolicyConfig RetryPolicy { get; set; } = new();
    public TelemetryConfig Telemetry { get; set; } = new();
    public HealthCheckConfig HealthCheck { get; set; } = new();
    public ProcessingConfig Processing { get; set; } = new();
}

public class IntentRecognitionConfig
{
    public double ConfidenceThreshold { get; set; } = 0.5; // Lowered from 0.7 for better recognition
    public int MaxAnalysisTimeMs { get; set; } = 1000;
    public Dictionary<string, double> IntentWeights { get; set; } = new()
    {
        ["environment"] = 1.0,
        ["pac"] = 1.0,
        ["m365"] = 1.0,
        ["create"] = 0.8,
        ["generate"] = 0.8,
        ["help"] = 0.6
    };
}

public class RetryPolicyConfig
{
    public int MaxRetries { get; set; } = 3;
    public int BaseDelayMs { get; set; } = 1000;
    public double BackoffMultiplier { get; set; } = 2.0;
    public int MaxDelayMs { get; set; } = 30000;
    public string[] RetriableExceptions { get; set; } = { "TimeoutException", "HttpRequestException", "SocketException" };
}

public class TelemetryConfig
{
    public bool EnableDetailedLogging { get; set; } = true;
    public bool EnablePerformanceMetrics { get; set; } = true;
    public bool EnableUserAnalytics { get; set; } = false;
    public int MetricsRetentionDays { get; set; } = 30;
}

public class HealthCheckConfig
{
    public int HealthCheckIntervalMs { get; set; } = 30000;
    public int UnhealthyThresholdMs { get; set; } = 5000;
    public bool EnableDependencyChecks { get; set; } = true;
}

public class ProcessingConfig
{
    public int RequestTimeoutMs { get; set; } = 30000;
    public int MaxConcurrentRequests { get; set; } = 10;
    public double AccuracyTarget { get; set; } = 0.9;
}

public class AgentMetrics
{
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double AverageProcessingTimeMs { get; set; }
    public double AccuracyRate { get; set; }
    public Dictionary<IntentType, long> IntentCounts { get; set; } = new();
    public List<string> RecentErrors { get; set; } = new();
    public AgentStatus Status { get; set; } = AgentStatus.Healthy;
}

public enum AgentStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Offline
}

public class AgentHealthReport
{
    public AgentStatus OverallStatus { get; set; }
    public Dictionary<string, bool> DependencyHealth { get; set; } = new();
    public AgentMetrics Metrics { get; set; } = new();
    public DateTime ReportGeneratedAt { get; set; } = DateTime.UtcNow;
    public List<string> HealthIssues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}