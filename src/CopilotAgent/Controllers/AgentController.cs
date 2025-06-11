using Microsoft.AspNetCore.Mvc;
using CopilotAgent.Agents;
using CopilotAgent.Services;
using Shared.Models;

namespace CopilotAgent.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpPost("process")]
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

    [HttpGet("health")]
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

    [HttpGet("health/status")]
    public ActionResult<object> HealthStatus()
    {
        try
        {
            var status = _healthMonitoringService.GetCurrentStatus();
            return Ok(new { status = status.ToString(), timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status");
            return StatusCode(503, new { status = "Unhealthy", error = ex.Message, timestamp = DateTime.UtcNow });
        }
    }

    [HttpGet("metrics")]
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
            return StatusCode(500, new { error = "Failed to retrieve metrics", message = ex.Message });
        }
    }

    [HttpPost("metrics/reset")]
    public ActionResult ResetMetrics()
    {
        try
        {
            _telemetryService.ResetMetrics();
            _logger.LogInformation("Metrics reset by user request");
            return Ok(new { message = "Metrics reset successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset metrics");
            return StatusCode(500, new { error = "Failed to reset metrics", message = ex.Message });
        }
    }

    [HttpGet("configuration")]
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
            return StatusCode(500, new { error = "Failed to retrieve configuration", message = ex.Message });
        }
    }

    [HttpPut("configuration")]
    public async Task<ActionResult> UpdateConfiguration([FromBody] AgentConfiguration configuration)
    {
        try
        {
            var isValid = await _configurationService.ValidateConfigurationAsync(configuration);
            if (!isValid)
            {
                return BadRequest(new { error = "Configuration validation failed" });
            }

            await _configurationService.UpdateConfigurationAsync(configuration);
            _logger.LogInformation("Configuration updated via API");
            
            return Ok(new { message = "Configuration updated successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configuration");
            return StatusCode(500, new { error = "Failed to update configuration", message = ex.Message });
        }
    }

    [HttpGet("capabilities")]
    public ActionResult<AgentCapabilities> GetCapabilities()
    {
        return Ok(new AgentCapabilities
        {
            SupportedIntents = new[]
            {
                "environment_management",
                "cli_execution", 
                "code_generation",
                "knowledge_retrieval"
            },
            SupportedCliTools = new[]
            {
                "pac",
                "m365"
            },
            SupportedOperations = new[]
            {
                "create_environment",
                "list_environments",
                "export_solution",
                "import_solution",
                "generate_blazor_component",
                "create_app_registration"
            }
        });
    }
}