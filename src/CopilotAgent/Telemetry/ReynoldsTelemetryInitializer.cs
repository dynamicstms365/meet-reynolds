using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using System.Reflection;

namespace CopilotAgent.Telemetry;

/// <summary>
/// Reynolds Telemetry Initializer - Maximum Effort™ Application Insights Context Enhancement
/// Adds supernatural visibility and enterprise context to all telemetry data
/// </summary>
public class ReynoldsTelemetryInitializer : ITelemetryInitializer
{
    private readonly ILogger<ReynoldsTelemetryInitializer> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _applicationVersion;
    private readonly string _deploymentEnvironment;

    public ReynoldsTelemetryInitializer(
        ILogger<ReynoldsTelemetryInitializer> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        
        // Reynolds: Get application version with Maximum Effort™
        _applicationVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion 
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() 
            ?? "1.0.0-reynolds";

        // Reynolds: Detect deployment environment
        _deploymentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
            ?? Environment.GetEnvironmentVariable("ENVIRONMENT") 
            ?? "Production";
    }

    public void Initialize(ITelemetry telemetry)
    {
        try
        {
            // Reynolds: Add supernatural application context
            telemetry.Context.GlobalProperties["Reynolds.ApplicationName"] = "CopilotAgent.Enterprise";
            telemetry.Context.GlobalProperties["Reynolds.Version"] = _applicationVersion;
            telemetry.Context.GlobalProperties["Reynolds.Environment"] = _deploymentEnvironment;
            telemetry.Context.GlobalProperties["Reynolds.CoordinationMode"] = "MaximumEffort";
            telemetry.Context.GlobalProperties["Reynolds.MachineName"] = Environment.MachineName;
            telemetry.Context.GlobalProperties["Reynolds.ProcessId"] = Environment.ProcessId.ToString();

            // Reynolds: Add enterprise deployment context
            var containerAppName = Environment.GetEnvironmentVariable("CONTAINER_APP_NAME");
            var azureResourceGroup = Environment.GetEnvironmentVariable("AZURE_RESOURCE_GROUP");
            var deploymentTimestamp = Environment.GetEnvironmentVariable("DEPLOYMENT_TIMESTAMP");

            if (!string.IsNullOrEmpty(containerAppName))
            {
                telemetry.Context.GlobalProperties["Reynolds.ContainerApp"] = containerAppName;
            }

            if (!string.IsNullOrEmpty(azureResourceGroup))
            {
                telemetry.Context.GlobalProperties["Reynolds.ResourceGroup"] = azureResourceGroup;
            }

            if (!string.IsNullOrEmpty(deploymentTimestamp))
            {
                telemetry.Context.GlobalProperties["Reynolds.DeploymentTimestamp"] = deploymentTimestamp;
            }

            // Reynolds: Add HTTP context if available (for web requests)
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                // Add request context
                telemetry.Context.GlobalProperties["Reynolds.RequestPath"] = httpContext.Request.Path;
                telemetry.Context.GlobalProperties["Reynolds.RequestMethod"] = httpContext.Request.Method;
                
                // Add user context if authenticated
                if (httpContext.User?.Identity?.IsAuthenticated == true)
                {
                    var userName = httpContext.User.Identity.Name ?? "Unknown";
                    telemetry.Context.GlobalProperties["Reynolds.AuthenticatedUser"] = userName;
                }

                // Add GitHub webhook context if present
                if (httpContext.Request.Headers.ContainsKey("X-GitHub-Event"))
                {
                    telemetry.Context.GlobalProperties["Reynolds.GitHubEvent"] = httpContext.Request.Headers["X-GitHub-Event"].ToString();
                }

                if (httpContext.Request.Headers.ContainsKey("X-GitHub-Delivery"))
                {
                    telemetry.Context.GlobalProperties["Reynolds.GitHubDelivery"] = httpContext.Request.Headers["X-GitHub-Delivery"].ToString();
                }

                // Add MCP context if present
                if (httpContext.Request.Headers.ContainsKey("X-MCP-Operation"))
                {
                    telemetry.Context.GlobalProperties["Reynolds.MCPOperation"] = httpContext.Request.Headers["X-MCP-Operation"].ToString();
                }

                // Add Teams context if present
                if (httpContext.Request.Headers.ContainsKey("X-Teams-Context"))
                {
                    telemetry.Context.GlobalProperties["Reynolds.TeamsContext"] = httpContext.Request.Headers["X-Teams-Context"].ToString();
                }
            }

            // Reynolds: Add custom performance metrics
            telemetry.Context.GlobalProperties["Reynolds.MemoryUsageMB"] = (GC.GetTotalMemory(false) / 1024 / 1024).ToString();
            telemetry.Context.GlobalProperties["Reynolds.ThreadCount"] = Environment.ProcessorCount.ToString();
            telemetry.Context.GlobalProperties["Reynolds.WorkingSetMB"] = (Environment.WorkingSet / 1024 / 1024).ToString();

            // Reynolds: Add coordination tracking
            telemetry.Context.GlobalProperties["Reynolds.CoordinationTimestamp"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            telemetry.Context.GlobalProperties["Reynolds.CorrelationId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        }
        catch (Exception ex)
        {
            // Reynolds: Never let telemetry initialization break the application
            _logger.LogWarning(ex, "🎭 Reynolds: Telemetry initialization encountered minor turbulence - continuing with Maximum Effort™");
        }
    }
}