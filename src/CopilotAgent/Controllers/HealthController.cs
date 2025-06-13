using Microsoft.AspNetCore.Mvc;
using CopilotAgent.Services;

namespace CopilotAgent.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly EnterpriseAuthService _authService;

    public HealthController(
        ILogger<HealthController> logger,
        EnterpriseAuthService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> Health()
    {
        try
        {
            await Task.CompletedTask; // Satisfy async requirement
            
            var health = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "2.0.0-sdk",
                server = "reynolds-mcp-server",
                transport = "mcp-sdk",
                features = new
                {
                    github_tools = 12,
                    reynolds_tools = 5,
                    enterprise_auth = true,
                    reynolds_persona = true
                }
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { status = "unhealthy", error = ex.Message });
        }
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        try
        {
            // Test critical dependencies
            var authAvailable = await TestAuthServiceAsync();
            
            if (!authAvailable)
            {
                return StatusCode(503, new { status = "not ready", reason = "authentication service unavailable" });
            }

            return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new { status = "not ready", error = ex.Message });
        }
    }

    private async Task<bool> TestAuthServiceAsync()
    {
        try
        {
            await Task.CompletedTask; // Satisfy async requirement
            
            // Test if auth service is responsive
            return true; // Simplified for example
        }
        catch
        {
            return false;
        }
    }
}