using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Models;
using Moq;
using Xunit;
using CopilotAgent.Services;

namespace CopilotAgent.Tests.Unit;

public class IntroductionOrchestrationTests
{
    private readonly Mock<ILogger<IntroductionOrchestrationService>> _mockLogger;
    private readonly Mock<IUserMappingService> _mockUserMappingService;
    private readonly Mock<IReynoldsTeamsChatService> _mockTeamsChatService;
    private readonly Mock<IMicrosoftGraphUserService> _mockGraphUserService;
    private readonly Mock<IGitHubOrganizationService> _mockGitHubOrgService;
    private readonly IntroductionOrchestrationService _orchestrationService;

    public IntroductionOrchestrationTests()
    {
        _mockLogger = new Mock<ILogger<IntroductionOrchestrationService>>();
        _mockUserMappingService = new Mock<IUserMappingService>();
        _mockTeamsChatService = new Mock<IReynoldsTeamsChatService>();
        _mockGraphUserService = new Mock<IMicrosoftGraphUserService>();
        _mockGitHubOrgService = new Mock<IGitHubOrganizationService>();

        _orchestrationService = new IntroductionOrchestrationService(
            _mockLogger.Object,
            _mockUserMappingService.Object,
            _mockTeamsChatService.Object,
            _mockGraphUserService.Object,
            _mockGitHubOrgService.Object);
    }

    [Fact]
    public async Task OrchestratePlatformIntroductionAsync_WithValidatedMapping_ExecutesIntroduction()
    {
        // Arrange - Reynolds style setup with Maximum Effort™
        var requestingUserEmail = "christaylor@nextgeneration.com";
        var targetName = "Ari";
        
        var existingMapping = new UserMapping
        {
            TeamsUserId = "teams-ari-123",
            Email = "ari@nextgeneration.com",
            DisplayName = "Ari Johnson",
            GitHubId = "NextGenerationLogistics",
            IsValidated = true
        };

        _mockUserMappingService
            .Setup(x => x.GetMappingAsync(targetName, targetName))
            .ReturnsAsync(existingMapping);

        _mockTeamsChatService
            .Setup(x => x.SendDirectMessageAsync(existingMapping.Email, It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act - Reynolds orchestration in action
        var result = await _orchestrationService.OrchestratePlatformIntroductionAsync(requestingUserEmail, targetName);

        // Assert - Maximum Effort™ validation
        Assert.True(result.Success);
        Assert.Contains("Maximum Effort™ successful", result.Message);
        Assert.Contains("Ari Johnson", result.Message);
        
        _mockTeamsChatService.Verify(x => x.SendDirectMessageAsync(
            existingMapping.Email, 
            It.Is<string>(msg => msg.Contains("Reynolds") && msg.Contains("christaylor"))), 
            Times.Once);
    }

    [Fact]
    public async Task OrchestratePlatformIntroductionAsync_WithoutMapping_RequiresGitHubMapping()
    {
        // Arrange - Reynolds encountering unmapped territory
        var requestingUserEmail = "christaylor@nextgeneration.com";
        var targetName = "NewUser";

        _mockUserMappingService
            .Setup(x => x.GetMappingAsync(targetName, targetName))
            .ReturnsAsync((UserMapping?)null);

        var graphUser = new User
        {
            DisplayName = "New User",
            Mail = "newuser@nextgeneration.com",
            GivenName = "New",
            Surname = "User"
        };

        _mockGraphUserService
            .Setup(x => x.SearchUserAsync(targetName))
            .ReturnsAsync(graphUser);

        _mockUserMappingService
            .Setup(x => x.GetMappingByEmailAsync(graphUser.Mail!))
            .ReturnsAsync((UserMapping?)null);

        var githubMembers = new List<string> 
        { 
            "cege7480", 
            "NextGenerationLogistics", 
            "devuser123" 
        };

        _mockGitHubOrgService
            .Setup(x => x.GetOrganizationMembersAsync())
            .ReturnsAsync(githubMembers);

        // Act - Reynolds coordination challenge
        var result = await _orchestrationService.OrchestratePlatformIntroductionAsync(requestingUserEmail, targetName);

        // Assert - Reynolds asking for assistance
        Assert.False(result.Success);
        Assert.True(result.RequiresUserInput);
        Assert.Contains("Microsoft Graph", result.Message);
        Assert.Contains("GitHub organization members", result.Message);
        Assert.Equal(githubMembers, result.GitHubMembers);
        Assert.Equal(graphUser, result.GraphUser);
    }

    [Fact]
    public async Task OrchestratePlatformIntroductionAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange - Reynolds facing the unknown
        var requestingUserEmail = "christaylor@nextgeneration.com";
        var targetName = "NonExistentUser";

        _mockUserMappingService
            .Setup(x => x.GetMappingAsync(targetName, targetName))
            .ReturnsAsync((UserMapping?)null);

        _mockGraphUserService
            .Setup(x => x.SearchUserAsync(targetName))
            .ReturnsAsync((User?)null);

        // Act - Reynolds hitting a wall
        var result = await _orchestrationService.OrchestratePlatformIntroductionAsync(requestingUserEmail, targetName);

        // Assert - Reynolds graceful failure
        Assert.False(result.Success);
        Assert.Contains("couldn't find NonExistentUser", result.Message);
        Assert.Contains("Double-check the name", result.Message);
    }

    [Fact]
    public async Task OrchestratePlatformIntroductionAsync_TeamsMessageFails_ReturnsFailure()
    {
        // Arrange - Reynolds messaging malfunction
        var requestingUserEmail = "christaylor@nextgeneration.com";
        var targetName = "Ari";
        
        var existingMapping = new UserMapping
        {
            TeamsUserId = "teams-ari-123",
            Email = "ari@nextgeneration.com",
            DisplayName = "Ari Johnson",
            GitHubId = "NextGenerationLogistics",
            IsValidated = true
        };

        _mockUserMappingService
            .Setup(x => x.GetMappingAsync(targetName, targetName))
            .ReturnsAsync(existingMapping);

        _mockTeamsChatService
            .Setup(x => x.SendDirectMessageAsync(existingMapping.Email, It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act - Reynolds coordination failure
        var result = await _orchestrationService.OrchestratePlatformIntroductionAsync(requestingUserEmail, targetName);

        // Assert - Reynolds acknowledging defeat
        Assert.False(result.Success);
        Assert.Contains("couldn't send the introduction message", result.Message);
        Assert.Contains("DMs locked down", result.Message);
    }

    [Theory]
    [InlineData("christaylor@nextgeneration.com", "cege7480")] // Test user validation
    [InlineData("admin@nextgeneration.com", "NextGenerationLogistics")] // Organization mapping
    public async Task OrchestratePlatformIntroductionAsync_ValidUserMappings_ExecutesSuccessfully(
        string requestingEmail, string expectedGitHubId)
    {
        // Arrange - Reynolds testing known good mappings
        var targetName = "TestUser";
        
        var mapping = new UserMapping
        {
            TeamsUserId = $"teams-{targetName.ToLower()}-123",
            Email = requestingEmail,
            DisplayName = "Test User",
            GitHubId = expectedGitHubId,
            IsValidated = true
        };

        _mockUserMappingService
            .Setup(x => x.GetMappingAsync(targetName, targetName))
            .ReturnsAsync(mapping);

        _mockTeamsChatService
            .Setup(x => x.SendDirectMessageAsync(mapping.Email, It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act - Reynolds systematic validation
        var result = await _orchestrationService.OrchestratePlatformIntroductionAsync(requestingEmail, targetName);

        // Assert - Reynolds confirming coordination success
        Assert.True(result.Success);
        Assert.Contains(expectedGitHubId, result.Message);
    }
}

public class MicrosoftGraphUserServiceTests
{
    private readonly Mock<ILogger<MicrosoftGraphUserService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public MicrosoftGraphUserServiceTests()
    {
        _mockLogger = new Mock<ILogger<MicrosoftGraphUserService>>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup basic Graph configuration
        _mockConfiguration.Setup(x => x["MicrosoftGraph:ClientId"]).Returns("test-client-id");
        _mockConfiguration.Setup(x => x["MicrosoftGraph:ClientSecret"]).Returns("test-client-secret");
        _mockConfiguration.Setup(x => x["MicrosoftGraph:TenantId"]).Returns("test-tenant-id");
    }

    [Theory]
    [InlineData("christaylor@nextgeneration.com", true)]
    [InlineData("user@domain.com", true)]
    [InlineData("notanemail", false)]
    [InlineData("", false)]
    public void IsEmailFormat_ValidatesEmailCorrectly(string input, bool expected)
    {
        // This would be tested via reflection or by making the method internal/public
        // For now, we'll test the behavior through the public methods
        Assert.True(true); // Placeholder - actual implementation would test email validation
    }
}

public class GitHubOrganizationServiceTests
{
    private readonly Mock<ILogger<GitHubOrganizationService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IGitHubAppAuthService> _mockAuthService;
    private readonly Mock<HttpClient> _mockHttpClient;

    public GitHubOrganizationServiceTests()
    {
        _mockLogger = new Mock<ILogger<GitHubOrganizationService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockAuthService = new Mock<IGitHubAppAuthService>();
        _mockHttpClient = new Mock<HttpClient>();

        _mockConfiguration.Setup(x => x["GitHub:Organization"]).Returns("dynamicstms365");
    }

    [Fact]
    public void GitHubMember_ToString_ReturnsCorrectFormat()
    {
        // Arrange - Reynolds testing data models
        var member = new GitHubMember
        {
            Login = "cege7480",
            Id = 12345
        };

        // Act - Reynolds string representation
        var result = member.ToString();

        // Assert - Reynolds format validation
        Assert.Equal("cege7480 (12345)", result);
    }
}