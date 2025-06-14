using Microsoft.AspNetCore.Mvc;
using CopilotAgent.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace CopilotAgent.Controllers;

[ApiController]
[Route("api/reynolds")]
public class ReynoldsTestController : ControllerBase
{
    private readonly ILogger<ReynoldsTestController> _logger;
    private readonly IReynoldsTeamsChatService _chatService;
    private readonly IReynoldsM365CliService _cliService;

    public ReynoldsTestController(
        ILogger<ReynoldsTestController> logger,
        IReynoldsTeamsChatService chatService,
        IReynoldsM365CliService cliService)
    {
        _logger = logger;
        _chatService = chatService;
        _cliService = cliService;
    }

    [HttpPost("test-chat")]
    public async Task<IActionResult> TestChatCreation([FromBody] TestChatRequest request)
    {
        try
        {
            _logger.LogInformation("Reynolds testing chat creation for {UserEmail}", request.UserEmail);

            var message = string.IsNullOrEmpty(request.Message) 
                ? "🎭 **Reynolds Test Message**\n\nHey there! This is a test of Reynolds' supernatural chat creation capabilities. If you're seeing this, it means I can slide into your Teams DMs faster than you can ask about my name!\n\n*Maximum Effort on organizational coordination. Just Reynolds.*"
                : request.Message;

            string result;
            if (request.UseM365Cli)
            {
                _logger.LogInformation("Using M365 CLI for chat creation");
                result = await _cliService.CreateTeamsChatViaCli(request.UserEmail, message);
            }
            else
            {
                _logger.LogInformation("Using Graph API for chat creation");
                result = await _chatService.CreateNewChatAsync(request.UserEmail, message);
            }

            return Ok(new TestChatResponse
            {
                Success = result.StartsWith("✅"),
                Result = result,
                UserEmail = request.UserEmail,
                Method = request.UseM365Cli ? "M365 CLI" : "Graph API",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing chat creation for {UserEmail}", request.UserEmail);
            
            return StatusCode(500, new TestChatResponse
            {
                Success = false,
                Result = $"Reynolds encountered an unexpected situation: {ex.Message}",
                UserEmail = request.UserEmail,
                Method = request.UseM365Cli ? "M365 CLI" : "Graph API",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPost("test-message")]
    public async Task<IActionResult> TestDirectMessage([FromBody] TestMessageRequest request)
    {
        try
        {
            _logger.LogInformation("Reynolds testing direct message to {UserEmail}", request.UserEmail);

            var success = await _chatService.SendDirectMessageAsync(request.UserEmail, request.Message);

            return Ok(new
            {
                Success = success,
                UserEmail = request.UserEmail,
                Message = request.Message,
                Timestamp = DateTime.UtcNow,
                Result = success 
                    ? "✅ Reynolds successfully sent the message!" 
                    : "❌ Reynolds encountered an issue sending the message."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing direct message for {UserEmail}", request.UserEmail);
            
            return StatusCode(500, new
            {
                Success = false,
                UserEmail = request.UserEmail,
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPost("test-coordination")]
    public async Task<IActionResult> TestCoordinationChat([FromBody] TestCoordinationRequest request)
    {
        try
        {
            _logger.LogInformation("Reynolds testing coordination chat with {ParticipantCount} participants", request.Participants.Length);

            var context = string.IsNullOrEmpty(request.CoordinationContext)
                ? "Testing Reynolds' coordination capabilities across organizational boundaries. This is a demonstration of cross-repo orchestration with diplomatic excellence!"
                : request.CoordinationContext;

            var result = await _chatService.InitiateCoordinationChatAsync(request.Participants.ToList(), context, "🎪 **Reynolds Coordination Initiative**\n\nStrategic coordination opportunity detected! Time for some organizational orchestration.");

            return Ok(new
            {
                Success = result.StartsWith("✅"),
                Result = result,
                Participants = request.Participants,
                CoordinationContext = context,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing coordination chat");
            
            return StatusCode(500, new
            {
                Success = false,
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPost("demo-scope-creep")]
    public async Task<IActionResult> DemoScopeCreepNotification([FromBody] ScopeCreepDemoRequest request)
    {
        try
        {
            _logger.LogInformation("Reynolds demonstrating scope creep notification for PR #{PrNumber}", request.PrNumber);

            var success = await _chatService.SendProactiveCoordinationMessageAsync(
                request.UserEmail, 
                request.PrNumber, 
                request.Repository);

            return Ok(new
            {
                Success = success,
                UserEmail = request.UserEmail,
                PrNumber = request.PrNumber,
                Repository = request.Repository,
                Message = "Reynolds scope creep alert sent!",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending scope creep demo");
            
            return StatusCode(500, new
            {
                Success = false,
                Error = ex.Message,
                Message = "Reynolds encountered an issue with scope creep notification",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("status")]
    public IActionResult GetReynoldsStatus()
    {
        return Ok(new
        {
            Status = "Reynolds is operational and ready for organizational orchestration! 🎭",
            Capabilities = new[]
            {
                "Chat Creation (Graph API & M365 CLI)",
                "Direct Messaging", 
                "Coordination Chat Initiation",
                "Proactive Scope Creep Alerts",
                "Cross-Repo Dependency Intelligence",
                "Organizational Health Monitoring"
            },
            Personality = new
            {
                Name = "Just Reynolds",
                Style = "Ryan Reynolds energy meets Pete Buttigieg competence",
                Motto = "Maximum Effort™ • Minimal Drama • Just Reynolds",
                NameMystery = "The question that deserves a proper answer... back in 10!"
            },
            Endpoints = new
            {
                TestChat = "/api/reynolds/test-chat",
                TestMessage = "/api/reynolds/test-message", 
                TestCoordination = "/api/reynolds/test-coordination",
                DemoScopeCreep = "/api/reynolds/demo-scope-creep",
                Status = "/api/reynolds/status"
            },
            Timestamp = DateTime.UtcNow
        });
    }
}

// Request/Response models
public class TestChatRequest
{
    [Required]
    [EmailAddress]
    public string UserEmail { get; set; } = string.Empty;
    
    public string? Message { get; set; }
    
    public bool UseM365Cli { get; set; } = false;
}

public class TestChatResponse
{
    public bool Success { get; set; }
    public string Result { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class TestMessageRequest
{
    [Required]
    [EmailAddress]
    public string UserEmail { get; set; } = string.Empty;
    
    [Required]
    public string Message { get; set; } = string.Empty;
}

public class TestCoordinationRequest
{
    [Required]
    [MinLength(2)]
    public string[] Participants { get; set; } = Array.Empty<string>();
    
    public string? CoordinationContext { get; set; }
}

public class ScopeCreepDemoRequest
{
    [Required]
    [EmailAddress]
    public string UserEmail { get; set; } = string.Empty;
    
    [Required]
    public string PrNumber { get; set; } = string.Empty;
    
    [Required]
    public string Repository { get; set; } = string.Empty;
}