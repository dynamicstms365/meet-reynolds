using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using CopilotAgent.Services;
using Shared.Models;

namespace CopilotAgent.Tests.Unit;

[TestFixture]
public class CodespaceManagementServiceTests
{
    private Mock<ILogger<CodespaceManagementService>> _mockLogger = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<HttpClient> _mockHttpClient = null!;
    private Mock<ITelemetryService> _mockTelemetryService = null!;
    private Mock<IConfigurationService> _mockConfigurationService = null!;
    private CodespaceManagementService _codespaceService = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<CodespaceManagementService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockHttpClient = new Mock<HttpClient>();
        _mockTelemetryService = new Mock<ITelemetryService>();
        _mockConfigurationService = new Mock<IConfigurationService>();

        _codespaceService = new CodespaceManagementService(
            _mockLogger.Object,
            _mockConfiguration.Object,
            new HttpClient(), // Use real HttpClient for simplicity
            _mockTelemetryService.Object,
            _mockConfigurationService.Object);
    }

    [Test]
    public async Task CreateCodespaceAsync_ShouldReturnSuccess_WhenSpecIsValid()
    {
        // Arrange
        var spec = new CodespaceSpec
        {
            RepositoryName = "test-repo",
            Branch = "main",
            AutomaticOnboarding = true
        };

        // Act
        var result = await _codespaceService.CreateCodespaceAsync(spec);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.CodespaceId, Is.Not.Null.And.Not.Empty);
        Assert.That(result.WebUrl, Is.Not.Null.And.Not.Empty);
        Assert.That(result.State, Is.EqualTo("Available"));
    }

    [Test]
    public async Task GetCodespaceStatusAsync_ShouldReturnStatus_WhenCodespaceExists()
    {
        // Arrange
        var codespaceId = "test-codespace-id";

        // Act
        var result = await _codespaceService.GetCodespaceStatusAsync(codespaceId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.CodespaceId, Is.EqualTo(codespaceId));
        Assert.That(result.State, Is.EqualTo("Available"));
    }

    [Test]
    public async Task ListCodespacesAsync_ShouldReturnCodespaces_WhenRepositoryExists()
    {
        // Arrange
        var repository = "test-repo";

        // Act
        var result = await _codespaceService.ListCodespacesAsync(repository);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.GreaterThan(0));
    }

    [Test]
    public async Task DeleteCodespaceAsync_ShouldReturnTrue_WhenCodespaceExists()
    {
        // Arrange
        var codespaceId = "test-codespace-id";

        // Act
        var result = await _codespaceService.DeleteCodespaceAsync(codespaceId);

        // Assert
        Assert.That(result, Is.True);
    }
}

[TestFixture]
public class OnboardingServiceTests
{
    private Mock<ILogger<OnboardingService>> _mockLogger = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<ITelemetryService> _mockTelemetryService = null!;
    private OnboardingService _onboardingService = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<OnboardingService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockTelemetryService = new Mock<ITelemetryService>();

        _onboardingService = new OnboardingService(
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockTelemetryService.Object);
    }

    [Test]
    public async Task GetOnboardingStepsAsync_ShouldReturnOrderedSteps()
    {
        // Act
        var steps = await _onboardingService.GetOnboardingStepsAsync();

        // Assert
        Assert.That(steps, Is.Not.Null);
        Assert.That(steps.Length, Is.GreaterThan(0));
        Assert.That(steps.First().Id, Is.EqualTo("welcome"));
        Assert.That(steps.Last().Id, Is.EqualTo("first-contribution"));
        
        // Verify ordering
        for (int i = 1; i < steps.Length; i++)
        {
            Assert.That(steps[i].Order, Is.GreaterThan(steps[i - 1].Order));
        }
    }

    [Test]
    public async Task StartOnboardingAsync_ShouldCreateProgressRecord()
    {
        // Arrange
        var userId = "test-user";
        var codespaceId = "test-codespace";

        // Act
        var progress = await _onboardingService.StartOnboardingAsync(userId, codespaceId);

        // Assert
        Assert.That(progress, Is.Not.Null);
        Assert.That(progress.UserId, Is.EqualTo(userId));
        Assert.That(progress.CodespaceId, Is.EqualTo(codespaceId));
        Assert.That(progress.CurrentStep, Is.EqualTo("welcome"));
        Assert.That(progress.CompletedSteps, Is.Empty);
    }

    [Test]
    public async Task UpdateProgressAsync_ShouldAdvanceToNextStep()
    {
        // Arrange
        var userId = "test-user";
        var codespaceId = "test-codespace";
        
        // Start onboarding first
        await _onboardingService.StartOnboardingAsync(userId, codespaceId);

        // Act
        var updatedProgress = await _onboardingService.UpdateProgressAsync(
            userId, "welcome", new Dictionary<string, object>());

        // Assert
        Assert.That(updatedProgress, Is.Not.Null);
        Assert.That(updatedProgress.CompletedSteps, Contains.Item("welcome"));
        Assert.That(updatedProgress.CurrentStep, Is.EqualTo("setup-tools"));
    }

    [Test]
    public async Task GenerateWelcomeMessageAsync_ShouldReturnFormattedMessage()
    {
        // Arrange
        var userId = "test-user";
        var repositoryName = "test-repo";

        // Act
        var message = await _onboardingService.GenerateWelcomeMessageAsync(userId, repositoryName);

        // Assert
        Assert.That(message, Is.Not.Null.And.Not.Empty);
        Assert.That(message, Contains.Substring(repositoryName));
        Assert.That(message, Contains.Substring("Welcome"));
        Assert.That(message, Contains.Substring("Power Platform"));
    }

    [Test]
    public async Task CompleteOnboardingAsync_ShouldMarkAllStepsComplete()
    {
        // Arrange
        var userId = "test-user";
        var codespaceId = "test-codespace";
        
        // Start onboarding first
        await _onboardingService.StartOnboardingAsync(userId, codespaceId);

        // Act
        var result = await _onboardingService.CompleteOnboardingAsync(userId);

        // Assert
        Assert.That(result, Is.True);
    }
}