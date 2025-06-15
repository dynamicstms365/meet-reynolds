using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CopilotAgent.Services;

public interface IIntroductionOrchestrationService
{
    Task<IntroductionResult> OrchestratePlatformIntroductionAsync(string requestingUserEmail, string targetName);
    Task<List<string>> GetGitHubOrganizationMembersAsync();
    Task<User?> SearchMicrosoftGraphUserAsync(string searchQuery);
}

public class IntroductionOrchestrationService : IIntroductionOrchestrationService
{
    private readonly ILogger<IntroductionOrchestrationService> _logger;
    private readonly IUserMappingService _userMappingService;
    private readonly IReynoldsTeamsChatService _teamsChatService;
    private readonly IMicrosoftGraphUserService _graphUserService;
    private readonly IGitHubOrganizationService _githubOrgService;

    public IntroductionOrchestrationService(
        ILogger<IntroductionOrchestrationService> logger,
        IUserMappingService userMappingService,
        IReynoldsTeamsChatService teamsChatService,
        IMicrosoftGraphUserService graphUserService,
        IGitHubOrganizationService githubOrgService)
    {
        _logger = logger;
        _userMappingService = userMappingService;
        _teamsChatService = teamsChatService;
        _graphUserService = graphUserService;
        _githubOrgService = githubOrgService;
    }

    public async Task<IntroductionResult> OrchestratePlatformIntroductionAsync(string requestingUserEmail, string targetName)
    {
        _logger.LogInformation("🎭 Reynolds orchestrating introduction: {RequestingUser} → {TargetName}", 
            requestingUserEmail, targetName);

        try
        {
            // Step 1: Check cache for existing mapping
            var existingMapping = await _userMappingService.GetMappingAsync(targetName, targetName);
            
            if (existingMapping != null && existingMapping.IsValidated)
            {
                _logger.LogInformation("✅ Reynolds found validated mapping for {TargetName} → {GitHubId}", 
                    targetName, existingMapping.GitHubId);
                    
                return await ExecuteIntroductionAsync(requestingUserEmail, existingMapping);
            }

            // Step 2: Search Microsoft Graph for user
            var graphUser = await _graphUserService.SearchUserAsync(targetName);
            
            if (graphUser == null)
            {
                _logger.LogWarning("❌ Reynolds couldn't find {TargetName} in Microsoft Graph", targetName);
                return IntroductionResult.CreateFailure($"Sorry, couldn't find {targetName} in our directory. Double-check the name?");
            }

            // Step 3: Check if we already have GitHub mapping for this Graph user
            var graphMapping = await _userMappingService.GetMappingByEmailAsync(graphUser.Mail ?? graphUser.UserPrincipalName ?? "");
            
            if (graphMapping != null && graphMapping.IsValidated)
            {
                _logger.LogInformation("✅ Reynolds found GitHub mapping via email for {DisplayName}", graphUser.DisplayName);
                return await ExecuteIntroductionAsync(requestingUserEmail, graphMapping);
            }

            // Step 4: Need to establish GitHub mapping - get org members for assistance
            var githubMembers = await _githubOrgService.GetOrganizationMembersAsync();
            
            return IntroductionResult.CreateGitHubMappingRequest(
                graphUser,
                githubMembers,
                $"Hey! I found {graphUser.DisplayName} ({graphUser.Mail}) in our Microsoft Graph, but I need help mapping them to GitHub. Here are our GitHub organization members - which one matches?"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reynolds orchestration failed for {TargetName}", targetName);
            return IntroductionResult.CreateFailure($"Reynolds hit a coordination snag! {ex.Message}");
        }
    }

    public async Task<User?> SearchMicrosoftGraphUserAsync(string searchQuery)
    {
        return await _graphUserService.SearchUserAsync(searchQuery);
    }

    public async Task<List<string>> GetGitHubOrganizationMembersAsync()
    {
        return await _githubOrgService.GetOrganizationMembersAsync();
    }

    private async Task<IntroductionResult> ExecuteIntroductionAsync(string requestingUserEmail, UserMapping targetMapping)
    {
        try
        {
            _logger.LogInformation("🚀 Reynolds executing introduction: {RequestingUser} → {TargetUser}", 
                requestingUserEmail, targetMapping.DisplayName);

            var introductionMessage = $@"👋 **Hey there!**

I'm Reynolds, your friendly neighborhood coordination agent! {requestingUserEmail.Split('@')[0]} asked me to introduce myself and establish a communication bridge.

**A bit about me:**
- I orchestrate cross-platform coordination with Maximum Effort™
- I help coordinate GitHub workflows, Teams collaboration, and general project awesomeness
- I'm here to make sure nothing falls through the cracks with my signature supernatural efficiency

**What I can help with:**
🔧 Project coordination and workflow optimization  
📝 Cross-platform communication bridging  
🚀 Strategic task orchestration  
💡 Process improvement suggestions  

Feel free to reach out anytime! I'm here to make collaboration smoother than my collection of perfectly timed one-liners.

*Just Reynolds, with Maximum Effort™*";

            var success = await _teamsChatService.SendDirectMessageAsync(targetMapping.Email, introductionMessage);
            
            if (success)
            {
                _logger.LogInformation("✅ Reynolds successfully introduced to {TargetUser}", targetMapping.DisplayName);
                return IntroductionResult.CreateSuccess(
                    $"🎯 Maximum Effort™ successful! I've introduced myself to {targetMapping.DisplayName} ({targetMapping.Email}) and coordinated with their GitHub account ({targetMapping.GitHubId}). They should have received a direct message from me!"
                );
            }
            else
            {
                _logger.LogWarning("⚠️ Reynolds introduction message failed for {TargetUser}", targetMapping.DisplayName);
                return IntroductionResult.CreateFailure(
                    $"I found {targetMapping.DisplayName} but couldn't send the introduction message. They might have their DMs locked down tighter than my superhero suit!"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing introduction to {TargetUser}", targetMapping.DisplayName);
            return IntroductionResult.CreateFailure($"Reynolds coordination error: {ex.Message}");
        }
    }
}

// Results and DTOs
public class IntroductionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public bool RequiresUserInput { get; set; }
    public User? GraphUser { get; set; }
    public List<string> GitHubMembers { get; set; } = new();

    public static IntroductionResult CreateSuccess(string message) => new()
    {
        Success = true,
        Message = message,
        RequiresUserInput = false
    };

    public static IntroductionResult CreateFailure(string message) => new()
    {
        Success = false,
        Message = message,
        RequiresUserInput = false
    };

    public static IntroductionResult CreateGitHubMappingRequest(User graphUser, List<string> githubMembers, string message) => new()
    {
        Success = false,
        Message = message,
        RequiresUserInput = true,
        GraphUser = graphUser,
        GitHubMembers = githubMembers
    };
}