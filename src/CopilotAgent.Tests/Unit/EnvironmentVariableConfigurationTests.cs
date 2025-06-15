using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using Moq;
using CopilotAgent.Services;
using Microsoft.Extensions.Logging;

namespace CopilotAgent.Tests.Unit;

/// <summary>
/// Tests to validate environment variable configuration for webhook authentication
/// These tests ensure the correct environment variable names are used consistently
/// </summary>
[TestFixture]
public class EnvironmentVariableConfigurationTests
{
    private Mock<ILogger<GitHubAppAuthService>> _mockLogger;
    private Mock<IConfiguration> _mockConfiguration;
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private HttpClient _mockHttpClient;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<GitHubAppAuthService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockHttpClient = new HttpClient(_mockHttpMessageHandler.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _mockHttpClient?.Dispose();
    }

    [Test]
    public void GitHubAppAuthService_ShouldUse_NGL_DEVOPS_PRIVATE_KEY_EnvironmentVariable()
    {
        // Arrange - Set up configuration to simulate NGL_DEVOPS_PRIVATE_KEY being available
        _mockConfiguration.Setup(x => x["NGL_DEVOPS_APP_ID"]).Returns("test-app-id");
        _mockConfiguration.Setup(x => x["NGL_DEVOPS_PRIVATE_KEY"]).Returns("test-private-key");
        _mockConfiguration.Setup(x => x["NGL_DEVOPS_INSTALLATION_ID"]).Returns("test-installation-id");

        var service = new GitHubAppAuthService(_mockLogger.Object, _mockConfiguration.Object, _mockHttpClient);

        // Act & Assert - The service should access the correct environment variable
        try
        {
            _ = service.GetInstallationTokenAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // The exception should NOT be about missing credentials configuration
            Assert.That(ex.Message, Does.Not.Contain("GitHub App credentials not configured"));
        }

        // Verify that the correct configuration keys were accessed
        _mockConfiguration.Verify(x => x["NGL_DEVOPS_PRIVATE_KEY"], Times.AtLeastOnce);
        _mockConfiguration.Verify(x => x["NGL_DEVOPS_APP_ID"], Times.AtLeastOnce);
    }

    [Test]
    public void GitHubAppAuthService_ShouldFail_WhenUsingOldEnvironmentVariableName()
    {
        // Arrange - Simulate the old incorrect environment variable name being set
        _mockConfiguration.Setup(x => x["NGL_DEVOPS_APP_ID"]).Returns("test-app-id");
        _mockConfiguration.Setup(x => x["NGL_DEVOPS_BOT_PEM"]).Returns("test-private-key"); // Wrong variable name
        _mockConfiguration.Setup(x => x["NGL_DEVOPS_PRIVATE_KEY"]).Returns((string?)null); // Correct variable name not set

        var service = new GitHubAppAuthService(_mockLogger.Object, _mockConfiguration.Object, _mockHttpClient);

        // Act & Assert - Service should fail because it looks for NGL_DEVOPS_PRIVATE_KEY, not NGL_DEVOPS_BOT_PEM
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () 
            => await service.GetInstallationTokenAsync());
        
        Assert.That(ex.Message, Contains.Substring("GitHub App credentials not configured"));
        Assert.That(ex.Message, Contains.Substring("NGL_DEVOPS_PRIVATE_KEY"));
    }
}