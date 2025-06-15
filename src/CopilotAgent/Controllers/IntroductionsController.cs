using Microsoft.AspNetCore.Mvc;
using CopilotAgent.Services;
using Microsoft.Graph.Models;

namespace CopilotAgent.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IntroductionsController : ControllerBase
{
    private readonly ILogger<IntroductionsController> _logger;
    private readonly IIntroductionOrchestrationService _orchestrationService;
    private readonly IUserMappingService _userMappingService;

    public IntroductionsController(
        ILogger<IntroductionsController> logger,
        IIntroductionOrchestrationService orchestrationService,
        IUserMappingService userMappingService)
    {
        _logger = logger;
        _orchestrationService = orchestrationService;
        _userMappingService = userMappingService;
    }

    [HttpPost("orchestrate")]
    public async Task<IActionResult> OrchestratePlatformIntroduction([FromBody] IntroductionRequest request)
    {
        try
        {
            _logger.LogInformation("🎭 Reynolds API: Introduction orchestration request from {RequestingUser} for {TargetName}", 
                request.RequestingUserEmail, request.TargetName);

            var result = await _orchestrationService.OrchestratePlatformIntroductionAsync(
                request.RequestingUserEmail, 
                request.TargetName);

            if (result.Success)
            {
                return Ok(new { success = true, message = result.Message });
            }
            else if (result.RequiresUserInput)
            {
                return Ok(new 
                { 
                    success = false, 
                    requiresInput = true,
                    message = result.Message,
                    graphUser = new 
                    {
                        displayName = result.GraphUser?.DisplayName,
                        email = result.GraphUser?.Mail ?? result.GraphUser?.UserPrincipalName
                    },
                    githubMembers = result.GitHubMembers
                });
            }
            else
            {
                return BadRequest(new { success = false, message = result.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reynolds API orchestration failed");
            return StatusCode(500, new { success = false, message = "Reynolds coordination error occurred!" });
        }
    }

    [HttpPost("confirm-mapping")]
    public async Task<IActionResult> ConfirmUserMapping([FromBody] UserMappingConfirmation confirmation)
    {
        try
        {
            _logger.LogInformation("🎯 Reynolds API: Confirming user mapping {DisplayName} → {GitHubId}", 
                confirmation.DisplayName, confirmation.GitHubId);

            var mapping = new UserMapping
            {
                TeamsUserId = confirmation.TeamsUserId ?? "",
                Email = confirmation.Email,
                DisplayName = confirmation.DisplayName,
                GitHubId = confirmation.GitHubId,
                IsValidated = true,
                CreatedAt = DateTime.UtcNow,
                LastValidated = DateTime.UtcNow
            };

            var stored = await _userMappingService.StoreMappingAsync(mapping);
            
            if (stored)
            {
                // Now execute the introduction
                var result = await _orchestrationService.OrchestratePlatformIntroductionAsync(
                    confirmation.RequestingUserEmail, 
                    confirmation.DisplayName);

                return Ok(new 
                { 
                    success = result.Success, 
                    message = result.Success 
                        ? $"✅ Mapping confirmed and introduction executed! {result.Message}"
                        : $"⚠️ Mapping stored but introduction failed: {result.Message}"
                });
            }
            else
            {
                return BadRequest(new { success = false, message = "Reynolds couldn't store the mapping. Try again?" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reynolds mapping confirmation failed");
            return StatusCode(500, new { success = false, message = "Reynolds mapping error occurred!" });
        }
    }

    [HttpGet("test-connectivity")]
    public IActionResult TestConnectivity()
    {
        _logger.LogInformation("🎭 Reynolds API: Connectivity test requested");
        
        return Ok(new 
        { 
            success = true, 
            message = "Reynolds introduction orchestration service online! Maximum Effort™ ready.",
            timestamp = DateTime.UtcNow,
            version = "1.0.0-reynolds"
        });
    }

    [HttpGet("github-members")]
    public async Task<IActionResult> GetGitHubMembers()
    {
        try
        {
            _logger.LogInformation("📋 Reynolds API: GitHub members list requested");
            
            var members = await _orchestrationService.GetGitHubOrganizationMembersAsync();
            
            return Ok(new 
            { 
                success = true, 
                members = members,
                count = members.Count,
                message = $"Reynolds discovered {members.Count} GitHub organization members"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reynolds GitHub members lookup failed");
            return StatusCode(500, new { success = false, message = "Reynolds GitHub discovery error!" });
        }
    }

    [HttpPost("search-user")]
    public async Task<IActionResult> SearchUser([FromBody] UserSearchRequest request)
    {
        try
        {
            _logger.LogInformation("🔍 Reynolds API: User search requested for: {SearchQuery}", request.SearchQuery);
            
            var user = await _orchestrationService.SearchMicrosoftGraphUserAsync(request.SearchQuery);
            
            if (user != null)
            {
                return Ok(new 
                { 
                    success = true,
                    user = new 
                    {
                        displayName = user.DisplayName,
                        email = user.Mail ?? user.UserPrincipalName,
                        givenName = user.GivenName,
                        surname = user.Surname
                    },
                    message = $"Reynolds found: {user.DisplayName}"
                });
            }
            else
            {
                return NotFound(new 
                { 
                    success = false, 
                    message = $"Reynolds couldn't find anyone matching '{request.SearchQuery}' in Microsoft Graph"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reynolds user search failed");
            return StatusCode(500, new { success = false, message = "Reynolds user search error!" });
        }
    }
}

// Request/Response DTOs
public class IntroductionRequest
{
    public string RequestingUserId { get; set; } = "";
    public string RequestingUserEmail { get; set; } = "";
    public string TargetName { get; set; } = "";
    public string Context { get; set; } = "";
}

public class UserMappingConfirmation
{
    public string RequestingUserEmail { get; set; } = "";
    public string? TeamsUserId { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string GitHubId { get; set; } = "";
}

public class UserSearchRequest
{
    public string SearchQuery { get; set; } = "";
}