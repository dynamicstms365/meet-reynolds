using CopilotAgent.Services;
using System.Diagnostics;

namespace CopilotAgent.Services;

/// <summary>
/// Reynolds Startup Diagnostics Service - Maximum Effort™ Configuration Validation
/// Validates all critical configurations and dependencies at startup with supernatural precision
/// </summary>
public interface IStartupDiagnosticsService
{
    Task<StartupDiagnosticsResult> ValidateConfigurationAsync();
    Task LogSystemInformationAsync();
}

public class StartupDiagnosticsService : IStartupDiagnosticsService
{
    private readonly ILogger<StartupDiagnosticsService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IGitHubAppAuthService _githubAuthService;

    public StartupDiagnosticsService(
        ILogger<StartupDiagnosticsService> logger,
        IConfiguration configuration,
        IGitHubAppAuthService githubAuthService)
    {
        _logger = logger;
        _configuration = configuration;
        _githubAuthService = githubAuthService;
    }

    public async Task<StartupDiagnosticsResult> ValidateConfigurationAsync()
    {
        _logger.LogInformation("🎭 Reynolds: Starting comprehensive configuration validation with Maximum Effort™");
        
        var result = new StartupDiagnosticsResult
        {
            ValidationTimestamp = DateTime.UtcNow,
            Issues = new List<DiagnosticIssue>(),
            Warnings = new List<DiagnosticWarning>()
        };

        // Validate Application Insights Configuration
        await ValidateApplicationInsightsAsync(result);
        
        // Validate SEQ Configuration
        ValidateSeqConfiguration(result);
        
        // Validate GitHub App Configuration
        await ValidateGitHubConfigurationAsync(result);
        
        // Validate Environment Variables
        ValidateEnvironmentVariables(result);
        
        // Validate Logging Configuration
        ValidateLoggingConfiguration(result);
        
        // Validate Network Connectivity
        await ValidateNetworkConnectivityAsync(result);

        // Log summary
        LogValidationSummary(result);
        
        return result;
    }

    public async Task LogSystemInformationAsync()
    {
        _logger.LogInformation("🎭 Reynolds: Logging system information with supernatural detail");
        
        try
        {
            _logger.LogInformation("🖥️ Reynolds: Machine Name: {MachineName}", System.Environment.MachineName);
            _logger.LogInformation("🔧 Reynolds: .NET Version: {NetVersion}", System.Environment.Version);
            _logger.LogInformation("🔧 Reynolds: OS Version: {OSVersion}", System.Environment.OSVersion);
            _logger.LogInformation("🔧 Reynolds: Process ID: {ProcessId}", System.Environment.ProcessId);
            _logger.LogInformation("🔧 Reynolds: Working Directory: {WorkingDirectory}", System.Environment.CurrentDirectory);
            _logger.LogInformation("🔧 Reynolds: Environment: {Environment}", System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown");
            
            // Memory information
            var memoryUsage = GC.GetTotalMemory(false) / 1024 / 1024;
            var workingSet = System.Environment.WorkingSet / 1024 / 1024;
            _logger.LogInformation("💾 Reynolds: Memory Usage: {MemoryUsage}MB, Working Set: {WorkingSet}MB", memoryUsage, workingSet);
            
            // Configuration sources
            var configSources = _configuration.AsEnumerable()
                .Where(c => !string.IsNullOrEmpty(c.Value))
                .Count();
            _logger.LogInformation("⚙️ Reynolds: Configuration entries loaded: {ConfigCount}", configSources);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Reynolds: Minor turbulence while logging system information");
        }
    }

    private async Task ValidateApplicationInsightsAsync(StartupDiagnosticsResult result)
    {
        _logger.LogDebug("🔍 Reynolds: Validating Application Insights configuration");
        
        var connectionString = System.Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")
            ?? _configuration.GetConnectionString("ApplicationInsights")
            ?? System.Environment.GetEnvironmentVariable("APPINSIGHTS_CONNECTION_STRING");

        if (string.IsNullOrEmpty(connectionString))
        {
            result.Warnings.Add(new DiagnosticWarning
            {
                Component = "ApplicationInsights",
                Message = "Application Insights connection string not configured",
                Recommendation = "Set APPLICATIONINSIGHTS_CONNECTION_STRING environment variable for cloud telemetry"
            });
            _logger.LogWarning("⚠️ Reynolds: Application Insights not configured - telemetry limited to SEQ");
        }
        else if (connectionString.Contains("12345678-1234-1234-1234-123456789abc"))
        {
            result.Issues.Add(new DiagnosticIssue
            {
                Component = "ApplicationInsights",
                Severity = "High",
                Message = "Application Insights using placeholder connection string",
                Recommendation = "Replace with actual Application Insights connection string from Azure portal"
            });
            _logger.LogError("❌ Reynolds: Application Insights has placeholder connection string - telemetry will fail");
        }
        else
        {
            _logger.LogInformation("✅ Reynolds: Application Insights configuration validated");
        }

        await Task.CompletedTask;
    }

    private void ValidateSeqConfiguration(StartupDiagnosticsResult result)
    {
        _logger.LogDebug("🔍 Reynolds: Validating SEQ configuration");
        
        var seqUrl = System.Environment.GetEnvironmentVariable("SEQ_SERVER_URL")
            ?? _configuration["SEQ:ServerUrl"]
            ?? _configuration["Serilog:WriteTo:1:Args:serverUrl"];

        if (string.IsNullOrEmpty(seqUrl))
        {
            result.Warnings.Add(new DiagnosticWarning
            {
                Component = "SEQ",
                Message = "SEQ server URL not configured",
                Recommendation = "Set SEQ_SERVER_URL environment variable for structured logging dashboard"
            });
            _logger.LogWarning("⚠️ Reynolds: SEQ not configured - missing structured logging dashboard");
        }
        else
        {
            _logger.LogInformation("✅ Reynolds: SEQ configuration found - URL: {SeqUrl}", seqUrl);
        }
    }

    private async Task ValidateGitHubConfigurationAsync(StartupDiagnosticsResult result)
    {
        _logger.LogDebug("🔍 Reynolds: Validating GitHub App configuration");
        
        var appId = _configuration["NGL_DEVOPS_APP_ID"] ?? System.Environment.GetEnvironmentVariable("NGL_DEVOPS_APP_ID");
        var privateKey = _configuration["NGL_DEVOPS_PRIVATE_KEY"] ?? System.Environment.GetEnvironmentVariable("NGL_DEVOPS_PRIVATE_KEY");
        var webhookSecret = _configuration["NGL_DEVOPS_WEBHOOK_SECRET"] ?? System.Environment.GetEnvironmentVariable("NGL_DEVOPS_WEBHOOK_SECRET");

        if (string.IsNullOrEmpty(appId))
        {
            result.Issues.Add(new DiagnosticIssue
            {
                Component = "GitHub",
                Severity = "High",
                Message = "GitHub App ID not configured",
                Recommendation = "Set NGL_DEVOPS_APP_ID environment variable"
            });
        }

        if (string.IsNullOrEmpty(privateKey))
        {
            result.Issues.Add(new DiagnosticIssue
            {
                Component = "GitHub",
                Severity = "High", 
                Message = "GitHub App private key not configured",
                Recommendation = "Set NGL_DEVOPS_PRIVATE_KEY environment variable"
            });
        }

        if (string.IsNullOrEmpty(webhookSecret))
        {
            result.Issues.Add(new DiagnosticIssue
            {
                Component = "GitHub",
                Severity = "Medium",
                Message = "GitHub webhook secret not configured",
                Recommendation = "Set NGL_DEVOPS_WEBHOOK_SECRET environment variable"
            });
        }

        // Test GitHub connectivity if basic config is present
        if (!string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(privateKey))
        {
            try
            {
                _logger.LogDebug("🔍 Reynolds: Testing GitHub connectivity");
                var connectivityResult = await _githubAuthService.TestConnectivityAsync();
                
                if (connectivityResult.Success)
                {
                    _logger.LogInformation("✅ Reynolds: GitHub connectivity validated - {RepositoryCount} repositories accessible", 
                        connectivityResult.Repositories?.Length ?? 0);
                }
                else
                {
                    result.Issues.Add(new DiagnosticIssue
                    {
                        Component = "GitHub",
                        Severity = "High",
                        Message = $"GitHub connectivity test failed: {connectivityResult.Error}",
                        Recommendation = "Verify GitHub App credentials and installation"
                    });
                    _logger.LogError("❌ Reynolds: GitHub connectivity test failed - {Error}", connectivityResult.Error);
                }
            }
            catch (Exception ex)
            {
                result.Issues.Add(new DiagnosticIssue
                {
                    Component = "GitHub",
                    Severity = "High",
                    Message = $"GitHub connectivity test threw exception: {ex.Message}",
                    Recommendation = "Check GitHub App configuration and network connectivity"
                });
                _logger.LogError(ex, "💥 Reynolds: GitHub connectivity test encountered supernatural complications");
            }
        }
    }

    private void ValidateEnvironmentVariables(StartupDiagnosticsResult result)
    {
        _logger.LogDebug("🔍 Reynolds: Validating critical environment variables");
        
        var requiredEnvVars = new[]
        {
            "ASPNETCORE_ENVIRONMENT",
            "ASPNETCORE_URLS"
        };

        foreach (var envVar in requiredEnvVars)
        {
            var value = System.Environment.GetEnvironmentVariable(envVar);
            if (string.IsNullOrEmpty(value))
            {
                result.Warnings.Add(new DiagnosticWarning
                {
                    Component = "Environment",
                    Message = $"Environment variable {envVar} not set",
                    Recommendation = $"Consider setting {envVar} for proper configuration"
                });
            }
            else
            {
                _logger.LogDebug("✅ Reynolds: Environment variable {EnvVar} = {Value}", envVar, value);
            }
        }
    }

    private void ValidateLoggingConfiguration(StartupDiagnosticsResult result)
    {
        _logger.LogDebug("🔍 Reynolds: Validating logging configuration");
        
        try
        {
            var loggingSection = _configuration.GetSection("Logging");
            if (!loggingSection.Exists())
            {
                result.Warnings.Add(new DiagnosticWarning
                {
                    Component = "Logging",
                    Message = "Logging section not found in configuration",
                    Recommendation = "Add Logging section to appsettings.json"
                });
            }
            else
            {
                _logger.LogInformation("✅ Reynolds: Logging configuration validated");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Reynolds: Minor turbulence validating logging configuration");
        }
    }

    private async Task ValidateNetworkConnectivityAsync(StartupDiagnosticsResult result)
    {
        _logger.LogDebug("🔍 Reynolds: Validating network connectivity");
        
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            // Test GitHub API connectivity
            var githubResponse = await httpClient.GetAsync("https://api.github.com/zen");
            if (githubResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Reynolds: GitHub API connectivity verified");
            }
            else
            {
                result.Warnings.Add(new DiagnosticWarning
                {
                    Component = "Network",
                    Message = "GitHub API not reachable",
                    Recommendation = "Check network connectivity and firewall settings"
                });
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add(new DiagnosticWarning
            {
                Component = "Network",
                Message = $"Network connectivity test failed: {ex.Message}",
                Recommendation = "Verify internet connectivity and DNS resolution"
            });
            _logger.LogWarning(ex, "⚠️ Reynolds: Network connectivity test encountered minor turbulence");
        }
    }

    private void LogValidationSummary(StartupDiagnosticsResult result)
    {
        _logger.LogInformation("🎭 Reynolds: Configuration validation completed with Maximum Effort™");
        _logger.LogInformation("📊 Reynolds: Validation Summary - Issues: {IssueCount}, Warnings: {WarningCount}", 
            result.Issues.Count, result.Warnings.Count);
        
        if (result.Issues.Any())
        {
            _logger.LogWarning("❌ Reynolds: {IssueCount} configuration issues found:", result.Issues.Count);
            foreach (var issue in result.Issues)
            {
                _logger.LogError("   🔥 {Component}: {Message} - {Recommendation}", 
                    issue.Component, issue.Message, issue.Recommendation);
            }
        }

        if (result.Warnings.Any())
        {
            _logger.LogInformation("⚠️ Reynolds: {WarningCount} configuration warnings:", result.Warnings.Count);
            foreach (var warning in result.Warnings)
            {
                _logger.LogWarning("   ⚡ {Component}: {Message} - {Recommendation}", 
                    warning.Component, warning.Message, warning.Recommendation);
            }
        }

        if (!result.Issues.Any() && !result.Warnings.Any())
        {
            _logger.LogInformation("✅ Reynolds: All configurations validated successfully - supernatural coordination is ready! 🎭");
        }
    }
}

public class StartupDiagnosticsResult
{
    public DateTime ValidationTimestamp { get; set; }
    public List<DiagnosticIssue> Issues { get; set; } = new();
    public List<DiagnosticWarning> Warnings { get; set; } = new();
    public bool HasCriticalIssues => Issues.Any(i => i.Severity == "High");
}

public class DiagnosticIssue
{
    public string Component { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

public class DiagnosticWarning
{
    public string Component { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}