using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using CopilotAgent.Services;
using Shared.Models;
using System.Net;
using Moq.Protected;

namespace CopilotAgent.Tests.Unit;

[TestFixture]
public class GitHubIntegrationTests
{
    private Mock<ILogger<GitHubAppAuthService>> _mockLogger;
    private Mock<IConfiguration> _mockConfiguration;
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private HttpClient _mockHttpClient;
    private Mock<ISecurityAuditService> _mockAuditService;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<GitHubAppAuthService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockHttpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockAuditService = new Mock<ISecurityAuditService>();
    }

    [TearDown]
    public void TearDown()
    {
        _mockHttpClient?.Dispose();
    }

    [Test]
    public void GitHubAppAuthService_ShouldThrowException_WhenCredentialsNotConfigured()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["NGL_DEVOPS_APP_ID"]).Returns((string?)null);
        _mockConfiguration.Setup(x => x["NGL_DEVOPS_INSTALLATION_ID"]).Returns((string?)null);
        _mockConfiguration.Setup(x => x["NGL_DEVOPS_PRIVATE_KEY"]).Returns((string?)null);

        var service = new GitHubAppAuthService(_mockLogger.Object, _mockConfiguration.Object, _mockHttpClient);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.GetInstallationTokenAsync());
        Assert.That(ex.Message, Contains.Substring("GitHub App credentials not configured"));
    }

    [Test]
    public void SecurityAuditService_ShouldLogEvent_WithCorrectFormat()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SecurityAuditService>>();
        var service = new SecurityAuditService(mockLogger.Object);

        // Act
        var task = service.LogEventAsync("Test_Event", "user1", "repo1", "action1", "success", new { data = "test" });

        // Assert
        Assert.DoesNotThrowAsync(async () => await task);
        
        // Verify that the logger was called with the correct information
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SECURITY_AUDIT")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SecurityAuditService_ShouldLogWebhookEvent_WithPayloadDetails()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SecurityAuditService>>();
        var service = new SecurityAuditService(mockLogger.Object);
        
        var payload = new GitHubWebhookPayload
        {
            Action = "opened",
            Event = "pull_request",
            Repository = new GitHubRepository { FullName = "test/repo" },
            Sender = new GitHubUser { Login = "testuser" },
            Installation = new GitHubInstallation { Id = 12345 }
        };

        // Act
        await service.LogWebhookEventAsync(payload, "SUCCESS");

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GitHub_Webhook_Received")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SecurityAuditService_ShouldLogAuthenticationEvent_WithResult()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SecurityAuditService>>();
        var service = new SecurityAuditService(mockLogger.Object);

        // Act
        await service.LogAuthenticationEventAsync("SUCCESS");

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GitHub_App_Authentication")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void GitHubWebhookPayload_ShouldBeSerializable()
    {
        // Arrange
        var payload = new GitHubWebhookPayload
        {
            Action = "opened",
            Event = "pull_request",
            Repository = new GitHubRepository 
            { 
                Id = 123,
                Name = "test-repo",
                FullName = "org/test-repo",
                Description = "Test repository",
                Private = false
            },
            Installation = new GitHubInstallation 
            { 
                Id = 456,
                NodeId = "node123",
                Account = "org"
            },
            Sender = new GitHubUser 
            { 
                Id = 789,
                Login = "testuser",
                Type = "User"
            }
        };

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<GitHubWebhookPayload>(json);
            
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Action, Is.EqualTo(payload.Action));
            Assert.That(deserialized.Repository?.FullName, Is.EqualTo(payload.Repository.FullName));
        });
    }

    [Test]
    public void GitHubConnectivityResult_ShouldIndicateSuccess_WhenValid()
    {
        // Arrange & Act
        var result = new GitHubConnectivityResult
        {
            Success = true,
            InstallationId = "12345",
            Repositories = new[] { "org/repo1", "org/repo2" },
            Permissions = new[] { "contents:read", "issues:write" },
            TokenExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Repositories.Length, Is.EqualTo(2));
        Assert.That(result.Permissions.Length, Is.EqualTo(2));
        Assert.That(result.TokenExpiresAt, Is.GreaterThan(DateTime.UtcNow));
    }

    [Test]
    public void GitHubConnectivityResult_ShouldIndicateFailure_WhenError()
    {
        // Arrange & Act
        var result = new GitHubConnectivityResult
        {
            Success = false,
            Error = "Authentication failed"
        };

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Authentication failed"));
        Assert.That(result.Repositories, Is.Empty);
    }
}