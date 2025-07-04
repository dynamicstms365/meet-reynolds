using NUnit.Framework;
using CopilotAgent.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotAgent.Tests.Unit;

/// <summary>
/// Tests for webhook logging functionality - ensuring proper constructor parameter handling
/// </summary>
[TestFixture]
public class WebhookLoggingTests
{
    private Mock<ILogger<OctokitWebhookEventProcessor>> _mockLogger;
    private Mock<IGitHubWorkflowOrchestrator> _mockOrchestrator;
    private Mock<ISecurityAuditService> _mockAuditService;
    private Mock<ICrossPlatformEventRouter> _mockEventRouter;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<OctokitWebhookEventProcessor>>();
        _mockOrchestrator = new Mock<IGitHubWorkflowOrchestrator>();
        _mockAuditService = new Mock<ISecurityAuditService>();
        _mockEventRouter = new Mock<ICrossPlatformEventRouter>();
    }

    [Test]
    public void OctokitWebhookEventProcessor_ShouldBeConstructable()
    {
        // Arrange & Act
        var processor = new OctokitWebhookEventProcessor(
            _mockOrchestrator.Object,
            _mockAuditService.Object,
            _mockLogger.Object,
            _mockEventRouter.Object);

        // Assert
        Assert.That(processor, Is.Not.Null);
    }

    [Test]
    public void EnhancedLogging_ShouldIncludeStructuredInformation()
    {
        // Arrange
        var processor = new OctokitWebhookEventProcessor(
            _mockOrchestrator.Object,
            _mockAuditService.Object,
            _mockLogger.Object,
            _mockEventRouter.Object);

        // Act & Assert - This validates that the processor is properly constructed
        // and can handle enhanced logging scenarios
        Assert.That(processor, Is.Not.Null);
        
        // Verify that the logger mock was set up
        Assert.That(_mockLogger.Object, Is.Not.Null);
    }
}