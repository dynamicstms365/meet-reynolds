using Shared.Models;

namespace CopilotAgent.Services;

public interface IHealthMonitoringService
{
    Task<AgentHealthReport> GenerateHealthReportAsync();
    Task<bool> CheckDependencyHealthAsync(string dependencyName);
    AgentStatus GetCurrentStatus();
}

public class HealthMonitoringService : IHealthMonitoringService
{
    private readonly ILogger<HealthMonitoringService> _logger;
    private readonly ITelemetryService _telemetryService;
    private readonly IConfigurationService _configurationService;
    private readonly IServiceProvider _serviceProvider;

    public HealthMonitoringService(
        ILogger<HealthMonitoringService> logger,
        ITelemetryService telemetryService,
        IConfigurationService configurationService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _telemetryService = telemetryService;
        _configurationService = configurationService;
        _serviceProvider = serviceProvider;
    }

    public async Task<AgentHealthReport> GenerateHealthReportAsync()
    {
        try
        {
            var metrics = _telemetryService.GetMetrics();
            var config = _configurationService.GetConfiguration();
            
            var healthReport = new AgentHealthReport
            {
                Metrics = metrics,
                ReportGeneratedAt = DateTime.UtcNow
            };

            // Check dependency health if enabled
            if (config.HealthCheck.EnableDependencyChecks)
            {
                healthReport.DependencyHealth = await CheckAllDependenciesAsync();
            }

            // Analyze overall health
            var healthAnalysis = AnalyzeHealth(metrics, healthReport.DependencyHealth, config);
            healthReport.OverallStatus = healthAnalysis.Status;
            healthReport.HealthIssues = healthAnalysis.Issues;
            healthReport.Recommendations = healthAnalysis.Recommendations;

            _logger.LogDebug("Health report generated: {Status}", healthReport.OverallStatus);

            return healthReport;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate health report");
            return new AgentHealthReport
            {
                OverallStatus = AgentStatus.Unhealthy,
                HealthIssues = new List<string> { $"Health check failed: {ex.Message}" },
                ReportGeneratedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<bool> CheckDependencyHealthAsync(string dependencyName)
    {
        try
        {
            return dependencyName.ToLowerInvariant() switch
            {
                "environmentmanager" => await CheckServiceHealthAsync<IEnvironmentManager>(),
                "cliexecutor" => await CheckServiceHealthAsync<ICliExecutor>(),
                "codegenerator" => await CheckServiceHealthAsync<ICodeGenerator>(),
                "knowledgeretriever" => await CheckServiceHealthAsync<IKnowledgeRetriever>(),
                "configuration" => CheckConfigurationHealth(),
                _ => true // Unknown dependencies are considered healthy
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dependency health check failed for {Dependency}", dependencyName);
            return false;
        }
    }

    public AgentStatus GetCurrentStatus()
    {
        try
        {
            var metrics = _telemetryService.GetMetrics();
            return metrics.Status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current agent status");
            return AgentStatus.Unhealthy;
        }
    }

    private async Task<Dictionary<string, bool>> CheckAllDependenciesAsync()
    {
        var dependencies = new[]
        {
            "EnvironmentManager",
            "CliExecutor", 
            "CodeGenerator",
            "KnowledgeRetriever",
            "Configuration"
        };

        var results = new Dictionary<string, bool>();

        foreach (var dependency in dependencies)
        {
            try
            {
                results[dependency] = await CheckDependencyHealthAsync(dependency);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Health check failed for dependency: {Dependency}", dependency);
                results[dependency] = false;
            }
        }

        return results;
    }

    private async Task<bool> CheckServiceHealthAsync<T>() where T : class
    {
        try
        {
            var service = _serviceProvider.GetService<T>();
            if (service == null)
            {
                _logger.LogWarning("Service {ServiceType} is not registered", typeof(T).Name);
                return false;
            }

            // For now, just check if the service can be resolved
            // In the future, we could add health check methods to service interfaces
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for service {ServiceType}", typeof(T).Name);
            return false;
        }
    }

    private bool CheckConfigurationHealth()
    {
        try
        {
            var config = _configurationService.GetConfiguration();
            return config != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Configuration health check failed");
            return false;
        }
    }

    private HealthAnalysis AnalyzeHealth(AgentMetrics metrics, Dictionary<string, bool> dependencyHealth, AgentConfiguration config)
    {
        var issues = new List<string>();
        var recommendations = new List<string>();
        var status = AgentStatus.Healthy;

        // Check accuracy rate
        if (metrics.AccuracyRate < config.Processing.AccuracyTarget)
        {
            issues.Add($"Accuracy rate ({metrics.AccuracyRate:P2}) is below target ({config.Processing.AccuracyTarget:P2})");
            recommendations.Add("Review intent recognition patterns and training data");
            
            if (metrics.AccuracyRate < config.Processing.AccuracyTarget * 0.8)
            {
                status = AgentStatus.Unhealthy;
            }
            else if (status == AgentStatus.Healthy)
            {
                status = AgentStatus.Degraded;
            }
        }

        // Check processing time
        if (metrics.AverageProcessingTimeMs > config.HealthCheck.UnhealthyThresholdMs)
        {
            issues.Add($"Average processing time ({metrics.AverageProcessingTimeMs:F0}ms) exceeds threshold ({config.HealthCheck.UnhealthyThresholdMs}ms)");
            recommendations.Add("Optimize request processing and consider scaling resources");
            
            if (status == AgentStatus.Healthy)
            {
                status = AgentStatus.Degraded;
            }
        }

        // Check error rate
        var errorRate = metrics.TotalRequests > 0 ? (double)metrics.FailedRequests / metrics.TotalRequests : 0;
        if (errorRate > 0.05) // 5% error rate threshold
        {
            issues.Add($"Error rate ({errorRate:P2}) is high");
            recommendations.Add("Review recent errors and implement fixes");
            
            if (errorRate > 0.1) // 10% error rate is unhealthy
            {
                status = AgentStatus.Unhealthy;
            }
            else if (status == AgentStatus.Healthy)
            {
                status = AgentStatus.Degraded;
            }
        }

        // Check dependencies
        var unhealthyDependencies = dependencyHealth.Where(d => !d.Value).ToList();
        if (unhealthyDependencies.Any())
        {
            issues.Add($"Unhealthy dependencies: {string.Join(", ", unhealthyDependencies.Select(d => d.Key))}");
            recommendations.Add("Check service configurations and network connectivity");
            status = AgentStatus.Unhealthy;
        }

        return new HealthAnalysis
        {
            Status = status,
            Issues = issues,
            Recommendations = recommendations
        };
    }
}

public class HealthAnalysis
{
    public AgentStatus Status { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}