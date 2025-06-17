using Microsoft.AspNetCore.Mvc;
using CopilotAgent.Agents;
using CopilotAgent.Services;
using Shared.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;
using CopilotAgent.Models;

namespace CopilotAgent.Controllers;

/// <summary>
/// Core Agent Controller for Power Platform operations and agent orchestration
/// Designed for Azure APIM MCP integration with comprehensive OpenAPI documentation
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Agent")]
public class AgentController : ControllerBase
{
    private readonly IPowerPlatformAgent _agent;
    private readonly ILogger<AgentController> _logger;
    private readonly IHealthMonitoringService _healthMonitoringService;
    private readonly ITelemetryService _telemetryService;
    private readonly IConfigurationService _configurationService;

    public AgentController(
        IPowerPlatformAgent agent, 
        ILogger<AgentController> logger,
        IHealthMonitoringService healthMonitoringService,
        ITelemetryService telemetryService,
        IConfigurationService configurationService)
    {
        _agent = agent;
        _logger = logger;
        _healthMonitoringService = healthMonitoringService;
        _telemetryService = telemetryService;
        _configurationService = configurationService;
    }

    /// <summary>
    /// Process an agent request for Power Platform operations
    /// </summary>
    /// <param name="request">Agent request containing message and operation details</param>
    /// <returns>Agent response with operation results</returns>
    /// <response code="200">Request processed successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Request processing failed</response>
    [HttpPost("process")]
    [ProducesResponseType(typeof(AgentResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<AgentResponse>> ProcessRequest([FromBody] AgentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing agent request: {Message}", request.Message);
            
            var response = await _agent.ProcessRequestAsync(request);
            
            _logger.LogInformation("Agent request processed successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing agent request");
            return StatusCode(500, new AgentResponse
            {
                Success = false,
                Message = "An error occurred processing your request",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get comprehensive agent health report
    /// </summary>
    /// <returns>Detailed health status of all agent components</returns>
    /// <response code="200">Health report generated successfully</response>
    /// <response code="503">Agent is unhealthy or offline</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(AgentHealthReport), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(AgentHealthReport), (int)HttpStatusCode.ServiceUnavailable)]
    public async Task<ActionResult<AgentHealthReport>> Health()
    {
        try
        {
            var healthReport = await _healthMonitoringService.GenerateHealthReportAsync();
            
            var statusCode = healthReport.OverallStatus switch
            {
                AgentStatus.Healthy => 200,
                AgentStatus.Degraded => 200, // Still responding but with warnings
                AgentStatus.Unhealthy => 503,
                AgentStatus.Offline => 503,
                _ => 503
            };

            return StatusCode(statusCode, healthReport);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, new AgentHealthReport
            {
                OverallStatus = AgentStatus.Unhealthy,
                HealthIssues = new List<string> { $"Health check failed: {ex.Message}" },
                ReportGeneratedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get current agent status (lightweight health check)
    /// </summary>
    /// <returns>Current agent status and timestamp</returns>
    /// <response code="200">Status retrieved successfully</response>
    /// <response code="503">Agent status check failed</response>
    [HttpGet("health/status")]
    [ProducesResponseType(typeof(AgentStatusResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
    public ActionResult<AgentStatusResponse> HealthStatus()
    {
        try
        {
            var status = _healthMonitoringService.GetCurrentStatus();
            return Ok(new AgentStatusResponse 
            { 
                Status = status.ToString(), 
                Timestamp = DateTime.UtcNow,
                Message = $"Agent is {status.ToString().ToLower()}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status");
            return StatusCode(503, new ErrorResponse 
            { 
                Error = "StatusCheckFailed",
                Message = ex.Message,
                Details = new Dictionary<string, object> { ["Timestamp"] = DateTime.UtcNow }
            });
        }
    }

    /// <summary>
    /// Get agent performance and operational metrics
    /// </summary>
    /// <returns>Comprehensive agent metrics and performance data</returns>
    /// <response code="200">Metrics retrieved successfully</response>
    /// <response code="500">Metrics retrieval failed</response>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(AgentMetrics), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public ActionResult<AgentMetrics> GetMetrics()
    {
        try
        {
            var metrics = _telemetryService.GetMetrics();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics");
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "MetricsRetrievalFailed", 
                Message = "Failed to retrieve agent metrics",
                Details = new Dictionary<string, object> { ["Exception"] = ex.Message }
            });
        }
    }

    /// <summary>
    /// Reset agent metrics and performance counters
    /// </summary>
    /// <returns>Metrics reset confirmation</returns>
    /// <response code="200">Metrics reset successfully</response>
    /// <response code="500">Metrics reset failed</response>
    [HttpPost("metrics/reset")]
    [ProducesResponseType(typeof(MetricsResetResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public ActionResult<MetricsResetResponse> ResetMetrics()
    {
        try
        {
            _telemetryService.ResetMetrics();
            _logger.LogInformation("Metrics reset by user request");
            return Ok(new MetricsResetResponse 
            { 
                Message = "Metrics reset successfully", 
                Timestamp = DateTime.UtcNow,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset metrics");
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "MetricsResetFailed", 
                Message = "Failed to reset agent metrics",
                Details = new Dictionary<string, object> { ["Exception"] = ex.Message }
            });
        }
    }

    /// <summary>
    /// Get current agent configuration
    /// </summary>
    /// <returns>Current agent configuration settings</returns>
    /// <response code="200">Configuration retrieved successfully</response>
    /// <response code="500">Configuration retrieval failed</response>
    [HttpGet("configuration")]
    [ProducesResponseType(typeof(AgentConfiguration), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public ActionResult<AgentConfiguration> GetConfiguration()
    {
        try
        {
            var config = _configurationService.GetConfiguration();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration");
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "ConfigurationRetrievalFailed", 
                Message = "Failed to retrieve agent configuration",
                Details = new Dictionary<string, object> { ["Exception"] = ex.Message }
            });
        }
    }

    /// <summary>
    /// Update agent configuration settings
    /// </summary>
    /// <param name="configuration">New configuration settings to apply</param>
    /// <returns>Configuration update confirmation</returns>
    /// <response code="200">Configuration updated successfully</response>
    /// <response code="400">Invalid configuration parameters</response>
    /// <response code="500">Configuration update failed</response>
    [HttpPut("configuration")]
    [ProducesResponseType(typeof(ConfigurationUpdateResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<ConfigurationUpdateResponse>> UpdateConfiguration([FromBody] AgentConfiguration configuration)
    {
        try
        {
            var isValid = await _configurationService.ValidateConfigurationAsync(configuration);
            if (!isValid)
            {
                return BadRequest(new ErrorResponse 
                { 
                    Error = "ConfigurationValidationFailed", 
                    Message = "Configuration validation failed - invalid parameters provided" 
                });
            }

            await _configurationService.UpdateConfigurationAsync(configuration);
            _logger.LogInformation("Configuration updated via API");
            
            return Ok(new ConfigurationUpdateResponse 
            { 
                Message = "Configuration updated successfully", 
                Timestamp = DateTime.UtcNow,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configuration");
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "ConfigurationUpdateFailed", 
                Message = "Failed to update agent configuration",
                Details = new Dictionary<string, object> { ["Exception"] = ex.Message }
            });
        }
    }

    /// <summary>
    /// Get agent capabilities and supported operations
    /// </summary>
    /// <returns>Comprehensive list of agent capabilities and supported operations</returns>
    /// <response code="200">Capabilities retrieved successfully</response>
    [HttpGet("capabilities")]
    [ProducesResponseType(typeof(AgentCapabilities), (int)HttpStatusCode.OK)]
    public ActionResult<AgentCapabilities> GetCapabilities()
    {
        return Ok(new Shared.Models.AgentCapabilities
        {
            SupportedIntents = new[]
            {
                "environment_management",
                "cli_execution", 
                "code_generation",
                "knowledge_retrieval",
                "power_platform_operations",
                "workflow_orchestration"
            },
            SupportedCliTools = new[]
            {
                "pac",
                "m365",
                "azure-cli",
                "powershell"
            },
            SupportedOperations = new[]
            {
                "create_environment",
                "list_environments",
                "export_solution",
                "import_solution",
                "generate_blazor_component",
                "create_app_registration",
                "manage_connectors",
                "deploy_flows",
                "create_power_apps"
            }
        });
    }
}

// Supporting response models for OpenAPI documentation
public class AgentStatusResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class MetricsResetResponse
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
}

public class ConfigurationUpdateResponse
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
}

public class AgentFeatures
{
    public bool HealthMonitoring { get; set; }
    public bool MetricsCollection { get; set; }
    public bool ConfigurationManagement { get; set; }
    public bool CLIIntegration { get; set; }
    public bool PowerPlatformIntegration { get; set; }
    public bool ReynoldsPersona { get; set; }
}

// Enhanced AgentCapabilities with additional properties
public partial class AgentCapabilities
{
    public AgentFeatures Features { get; set; } = new();
    public string Version { get; set; } = string.Empty;
    public string ReynoldsCoordination { get; set; } = string.Empty;
}
