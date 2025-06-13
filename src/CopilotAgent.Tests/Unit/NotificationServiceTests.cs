using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using CopilotAgent.Services;
using Shared.Models;

namespace CopilotAgent.Tests.Unit;

[TestFixture]
public class NotificationServiceTests
{
    private Mock<ILogger<NotificationService>> _mockLogger = null!;
    private Mock<IStakeholderVisibilityService> _mockStakeholderService = null!;
    private Mock<IDashboardService> _mockDashboardService = null!;
    private Mock<ISecurityAuditService> _mockAuditService = null!;
    private NotificationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<NotificationService>>();
        _mockStakeholderService = new Mock<IStakeholderVisibilityService>();
        _mockDashboardService = new Mock<IDashboardService>();
        _mockAuditService = new Mock<ISecurityAuditService>();
        
        _service = new NotificationService(
            _mockLogger.Object,
            _mockStakeholderService.Object,
            _mockDashboardService.Object,
            _mockAuditService.Object);
    }

    [Test]
    public async Task CreateNotificationAsync_ShouldCreateNotification_WhenValidInput()
    {
        // Arrange
        var notification = new StakeholderNotification
        {
            StakeholderId = "stakeholder-1",
            Subject = "Test Notification",
            Content = "Test content",
            Type = NotificationType.IssueUpdate,
            Channel = NotificationChannel.Email
        };

        // Act
        var result = await _service.CreateNotificationAsync(notification);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Not.Empty);
        Assert.That(result.StakeholderId, Is.EqualTo("stakeholder-1"));
        Assert.That(result.Subject, Is.EqualTo("Test Notification"));
        Assert.That(result.Status, Is.EqualTo(NotificationStatus.Pending));
        Assert.That(result.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));

        // Verify audit log was called
        _mockAuditService.Verify(x => x.LogEventAsync(
            "Notification_Created",
            null, null, "CreateNotification", "SUCCESS",
            It.IsAny<object>()), Times.Once);
    }

    [Test]
    public async Task SendNotificationAsync_ShouldSendEmailNotification_WhenValidEmail()
    {
        // Arrange
        var stakeholder = new StakeholderConfiguration
        {
            Id = "stakeholder-1",
            Name = "Test Stakeholder",
            Email = "test@example.com"
        };

        var notification = new StakeholderNotification
        {
            StakeholderId = "stakeholder-1",
            Subject = "Test Notification",
            Content = "Test content",
            Type = NotificationType.IssueUpdate,
            Channel = NotificationChannel.Email,
            Status = NotificationStatus.Pending
        };

        var created = await _service.CreateNotificationAsync(notification);

        _mockStakeholderService.Setup(x => x.GetStakeholderAsync("stakeholder-1"))
            .ReturnsAsync(stakeholder);

        // Act
        var result = await _service.SendNotificationAsync(created.Id);

        // Assert
        Assert.That(result, Is.True);

        var notifications = await _service.GetNotificationsByStakeholderAsync("stakeholder-1");
        var sentNotification = notifications.First();
        
        Assert.That(sentNotification.Status, Is.EqualTo(NotificationStatus.Sent));
        Assert.That(sentNotification.SentAt, Is.Not.Null);

        _mockAuditService.Verify(x => x.LogEventAsync(
            "Notification_Sent",
            null, null, "SendNotification", "SUCCESS",
            It.IsAny<object>()), Times.Once);
    }

    [Test]
    public async Task SendNotificationAsync_ShouldFail_WhenStakeholderNotFound()
    {
        // Arrange
        var notification = new StakeholderNotification
        {
            StakeholderId = "non-existent-stakeholder",
            Subject = "Test Notification",
            Type = NotificationType.IssueUpdate,
            Channel = NotificationChannel.Email,
            Status = NotificationStatus.Pending
        };

        var created = await _service.CreateNotificationAsync(notification);

        _mockStakeholderService.Setup(x => x.GetStakeholderAsync("non-existent-stakeholder"))
            .ReturnsAsync((StakeholderConfiguration?)null);

        // Act
        var result = await _service.SendNotificationAsync(created.Id);

        // Assert
        Assert.That(result, Is.False);

        var notifications = await _service.GetNotificationsByStakeholderAsync("non-existent-stakeholder");
        var failedNotification = notifications.First();
        
        Assert.That(failedNotification.Status, Is.EqualTo(NotificationStatus.Failed));
        Assert.That(failedNotification.ErrorMessage, Is.EqualTo("Stakeholder not found"));
    }

    [Test]
    public async Task GetPendingNotificationsAsync_ShouldReturnPendingNotifications()
    {
        // Arrange
        var notification1 = new StakeholderNotification
        {
            StakeholderId = "stakeholder-1",
            Subject = "Notification 1",
            Type = NotificationType.IssueUpdate,
            Channel = NotificationChannel.Email
        };

        var notification2 = new StakeholderNotification
        {
            StakeholderId = "stakeholder-2",
            Subject = "Notification 2",
            Type = NotificationType.PullRequestUpdate,
            Channel = NotificationChannel.Teams
        };

        await _service.CreateNotificationAsync(notification1);
        await _service.CreateNotificationAsync(notification2);

        // Act
        var result = await _service.GetPendingNotificationsAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(n => n.Status == NotificationStatus.Pending), Is.True);
    }

    [Test]
    public async Task NotifyStakeholdersAsync_ShouldCreateNotifications_ForRelevantStakeholders()
    {
        // Arrange
        var stakeholder1 = new StakeholderConfiguration
        {
            Id = "stakeholder-1",
            Name = "Stakeholder 1",
            Email = "test1@example.com",
            Repositories = new[] { "owner/repo1" },
            UpdatePreferences = new StakeholderUpdatePreferences
            {
                IssueProgressUpdates = true,
                Channels = new[] { NotificationChannel.Email }
            }
        };

        var stakeholder2 = new StakeholderConfiguration
        {
            Id = "stakeholder-2",
            Name = "Stakeholder 2",
            Email = "test2@example.com",
            Repositories = new[] { "owner/repo1" },
            UpdatePreferences = new StakeholderUpdatePreferences
            {
                IssueProgressUpdates = false, // Should not receive notification
                Channels = new[] { NotificationChannel.Email }
            }
        };

        var stakeholder3 = new StakeholderConfiguration
        {
            Id = "stakeholder-3",
            Name = "Stakeholder 3",
            Email = "test3@example.com",
            Repositories = new[] { "owner/repo2" }, // Different repository
            UpdatePreferences = new StakeholderUpdatePreferences
            {
                IssueProgressUpdates = true,
                Channels = new[] { NotificationChannel.Email }
            }
        };

        _mockStakeholderService.Setup(x => x.GetStakeholdersByRepositoryAsync("owner/repo1"))
            .ReturnsAsync(new[] { stakeholder1, stakeholder2 });

        var issueData = new { IssueNumber = 123, Title = "Test Issue" };

        // Act
        var result = await _service.NotifyStakeholdersAsync("owner/repo1", NotificationType.IssueUpdate, issueData);

        // Assert
        Assert.That(result, Is.True);

        // Verify only stakeholder1 gets notification (stakeholder2 has IssueProgressUpdates = false)
        var pendingNotifications = await _service.GetPendingNotificationsAsync();
        Assert.That(pendingNotifications.Count(), Is.EqualTo(1));
        Assert.That(pendingNotifications.First().StakeholderId, Is.EqualTo("stakeholder-1"));

        _mockAuditService.Verify(x => x.LogEventAsync(
            "Stakeholder_Notifications_Created",
            null, "owner/repo1", "NotifyStakeholders", "SUCCESS",
            It.IsAny<object>()), Times.Once);
    }

    [Test]
    public async Task ProcessScheduledNotificationsAsync_ShouldProcessPendingNotifications()
    {
        // Arrange
        var stakeholder = new StakeholderConfiguration
        {
            Id = "stakeholder-1",
            Name = "Test Stakeholder",
            Email = "test@example.com"
        };

        var notification1 = new StakeholderNotification
        {
            StakeholderId = "stakeholder-1",
            Subject = "Notification 1",
            Type = NotificationType.IssueUpdate,
            Channel = NotificationChannel.Email
        };

        var notification2 = new StakeholderNotification
        {
            StakeholderId = "stakeholder-1",
            Subject = "Notification 2",
            Type = NotificationType.PullRequestUpdate,
            Channel = NotificationChannel.Email
        };

        await _service.CreateNotificationAsync(notification1);
        await _service.CreateNotificationAsync(notification2);

        _mockStakeholderService.Setup(x => x.GetStakeholderAsync("stakeholder-1"))
            .ReturnsAsync(stakeholder);

        // Act
        await _service.ProcessScheduledNotificationsAsync();

        // Assert
        var notifications = await _service.GetNotificationsByStakeholderAsync("stakeholder-1");
        Assert.That(notifications.All(n => n.Status == NotificationStatus.Sent), Is.True);
    }

    [Test]
    [TestCase(NotificationChannel.Email)]
    [TestCase(NotificationChannel.Teams)]
    [TestCase(NotificationChannel.Dashboard)]
    [TestCase(NotificationChannel.Webhook)]
    public async Task SendNotificationAsync_ShouldHandleAllChannelTypes(NotificationChannel channel)
    {
        // Arrange
        var stakeholder = new StakeholderConfiguration
        {
            Id = "stakeholder-1",
            Name = "Test Stakeholder",
            Email = "test@example.com"
        };

        var notification = new StakeholderNotification
        {
            StakeholderId = "stakeholder-1",
            Subject = "Test Notification",
            Content = "Test content",
            Type = NotificationType.ProjectSummary,
            Channel = channel,
            Status = NotificationStatus.Pending
        };

        var created = await _service.CreateNotificationAsync(notification);

        _mockStakeholderService.Setup(x => x.GetStakeholderAsync("stakeholder-1"))
            .ReturnsAsync(stakeholder);

        // Act
        var result = await _service.SendNotificationAsync(created.Id);

        // Assert
        Assert.That(result, Is.True);

        var notifications = await _service.GetNotificationsByStakeholderAsync("stakeholder-1");
        var sentNotification = notifications.First();
        
        Assert.That(sentNotification.Status, Is.EqualTo(NotificationStatus.Sent));
        Assert.That(sentNotification.Channel, Is.EqualTo(channel));
    }
}