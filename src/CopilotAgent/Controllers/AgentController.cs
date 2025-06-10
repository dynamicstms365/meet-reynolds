using Microsoft.AspNetCore.Mvc;
using CopilotAgent.Agents;
using Shared.Models;

namespace CopilotAgent.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IPowerPlatformAgent _agent;
    private readonly ILogger<AgentController> _logger;

    public AgentController(IPowerPlatformAgent agent, ILogger<AgentController> logger)
    {
        _agent = agent;
        _logger = logger;
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
    public ActionResult<string> Health()
    {
        return Ok("Power Platform Copilot Agent is running");
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