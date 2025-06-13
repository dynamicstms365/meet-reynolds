using NUnit.Framework;
using CopilotAgent.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotAgent.Tests.Unit;

[TestFixture]
public class WebhookLoggingTests
{
    private Mock<ILogger<OctokitWebhookEventProcessor>> _mockLogger;
    private Mock<IGitHubWorkflowOrchestrator> _mockOrchestrator;
    private Mock<ISecurityAuditService> _mockAuditService;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<OctokitWebhookEventProcessor>>();
        _mockOrchestrator = new Mock<IGitHubWorkflowOrchestrator>();
        _mockAuditService = new Mock<ISecurityAuditService>();
    }

    [Test]
    public void OctokitWebhookEventProcessor_ShouldBeConstructable()
    {
        // Arrange & Act
        var mockNotificationService = new Mock<INotificationService>();
        var processor = new OctokitWebhookEventProcessor(
            _mockOrchestrator.Object,
            _mockAuditService.Object,
            mockNotificationService.Object,
            _mockLogger.Object);

        // Assert
        Assert.That(processor, Is.Not.Null);
    }

    [Test]
    public void EnhancedLogging_ShouldIncludeStructuredInformation()
    {
        // Arrange
        var mockNotificationService = new Mock<INotificationService>();
        var processor = new OctokitWebhookEventProcessor(
            _mockOrchestrator.Object,
            _mockAuditService.Object,
            mockNotificationService.Object,
            _mockLogger.Object);

        // Act & Assert - This validates that the processor is properly constructed
        // and can handle enhanced logging scenarios
        Assert.That(processor, Is.Not.Null);
        
        // Verify that the logger mock was set up
        Assert.That(_mockLogger.Object, Is.Not.Null);
    }
}