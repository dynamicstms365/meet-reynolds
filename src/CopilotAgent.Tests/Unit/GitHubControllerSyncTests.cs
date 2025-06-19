using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using CopilotAgent.Controllers;
using CopilotAgent.Services;
using Shared.Models;

namespace CopilotAgent.Tests.Unit;

[TestFixture]
public class GitHubControllerSyncTests
{
    private Mock<IGitHubAppAuthService> _mockAuthService;
    private Mock<IGitHubDiscussionsService> _mockDiscussionsService;
    private Mock<IGitHubIssuesService> _mockIssuesService;
    private Mock<IGitHubWebhookValidator> _mockWebhookValidator;
    private Mock<ILogger<GitHubController>> _mockLogger;
    private GitHubController _controller;

    [SetUp]
    public void Setup()
    {
        _mockAuthService = new Mock<IGitHubAppAuthService>();
        _mockDiscussionsService = new Mock<IGitHubDiscussionsService>();
        _mockIssuesService = new Mock<IGitHubIssuesService>();
        _mockWebhookValidator = new Mock<IGitHubWebhookValidator>();
        _mockLogger = new Mock<ILogger<GitHubController>>();

        _controller = new GitHubController(
            _mockLogger.Object,
            _mockAuthService.Object,
            _mockIssuesService.Object,
            _mockDiscussionsService.Object,
            _mockWebhookValidator.Object);
    }

    [Test]
    public async Task TestConnectivity_ReturnsValidResponse()
    {
        // Arrange
        _mockAuthService.Setup(s => s.TestConnectivityAsync())
            .ReturnsAsync(new GitHubConnectivityResult { Success = true });

        // Act
        var result = await _controller.TestConnectivity();

        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null);
    }

    [Test]
    public async Task GetInstallationInfo_ReturnsValidResponse()
    {
        // Arrange
        _mockAuthService.Setup(s => s.GetInstallationTokenAsync())
            .ReturnsAsync(new GitHubAppAuthentication 
            { 
                Token = "test-token", 
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Permissions = new[] { "read" }
            });

        // Act
        var result = await _controller.GetInstallationInfo();

        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null);
    }

    [Test]
    public async Task ValidateWebhook_ReturnsValidResponse()
    {
        // Arrange
        var payload = new { test = "data" };

        // Act
        var result = await _controller.ValidateWebhook(payload);

        // Assert
        // This test validates the webhook validation flow
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetRateLimit_ReturnsValidResponse()
    {
        // Act
        var result = await _controller.GetRateLimit();

        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null);
    }
}