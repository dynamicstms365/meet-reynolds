using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Shared.Models;

namespace CopilotAgent.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IStakeholderVisibilityService _stakeholderService;
    private readonly IDashboardService _dashboardService;
    private readonly ISecurityAuditService _auditService;

    // In-memory storage for notifications - in production, use a proper database
    private readonly ConcurrentDictionary<string, StakeholderNotification> _notifications = new();
    private readonly ConcurrentQueue<string> _pendingNotifications = new();

    public NotificationService(
        ILogger<NotificationService> logger,
        IStakeholderVisibilityService stakeholderService,
        IDashboardService dashboardService,
        ISecurityAuditService auditService)
    {
        _logger = logger;
        _stakeholderService = stakeholderService;
        _dashboardService = dashboardService;
        _auditService = auditService;
    }

    public async Task<StakeholderNotification> CreateNotificationAsync(StakeholderNotification notification)
    {
        try
        {
            if (string.IsNullOrEmpty(notification.Id))
            {
                notification.Id = Guid.NewGuid().ToString();
            }

            notification.CreatedAt = DateTime.UtcNow;
            notification.Status = NotificationStatus.Pending;

            if (!_notifications.TryAdd(notification.Id, notification))
            {
                throw new InvalidOperationException($"Notification with ID {notification.Id} already exists");
            }

            _pendingNotifications.Enqueue(notification.Id);

            await _auditService.LogEventAsync(
                "Notification_Created",
                action: "CreateNotification",
                result: "SUCCESS",
                details: new { 
                    NotificationId = notification.Id, 
                    StakeholderId = notification.StakeholderId,
                    Type = notification.Type.ToString(),
                    Channel = notification.Channel.ToString()
                });

            _logger.LogInformation("Created notification {NotificationId} for stakeholder {StakeholderId}", 
                notification.Id, notification.StakeholderId);

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification for stakeholder {StakeholderId}", notification.StakeholderId);
            await _auditService.LogEventAsync(
                "Notification_Create_Failed",
                action: "CreateNotification",
                result: "FAILURE",
                details: new { StakeholderId = notification.StakeholderId, Error = ex.Message });
            throw;
        }
    }

    public async Task<bool> SendNotificationAsync(string notificationId)
    {
        try
        {
            if (!_notifications.TryGetValue(notificationId, out var notification))
            {
                _logger.LogWarning("Notification {NotificationId} not found", notificationId);
                return false;
            }

            if (notification.Status != NotificationStatus.Pending)
            {
                _logger.LogWarning("Notification {NotificationId} is not in pending status", notificationId);
                return false;
            }

            // Get stakeholder configuration
            var stakeholder = await _stakeholderService.GetStakeholderAsync(notification.StakeholderId);
            if (stakeholder == null)
            {
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = "Stakeholder not found";
                return false;
            }

            // Send notification based on channel
            var success = notification.Channel switch
            {
                NotificationChannel.Email => await SendEmailNotificationAsync(notification, stakeholder),
                NotificationChannel.Teams => await SendTeamsNotificationAsync(notification, stakeholder),
                NotificationChannel.Dashboard => await SendDashboardNotificationAsync(notification, stakeholder),
                NotificationChannel.Webhook => await SendWebhookNotificationAsync(notification, stakeholder),
                _ => false
            };

            if (success)
            {
                notification.Status = NotificationStatus.Sent;
                notification.SentAt = DateTime.UtcNow;

                await _auditService.LogEventAsync(
                    "Notification_Sent",
                    action: "SendNotification",
                    result: "SUCCESS",
                    details: new { 
                        NotificationId = notificationId,
                        StakeholderId = notification.StakeholderId,
                        Channel = notification.Channel.ToString()
                    });

                _logger.LogInformation("Sent notification {NotificationId} via {Channel}", 
                    notificationId, notification.Channel);
            }
            else
            {
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = "Failed to send notification";

                await _auditService.LogEventAsync(
                    "Notification_Send_Failed",
                    action: "SendNotification",
                    result: "FAILURE",
                    details: new { 
                        NotificationId = notificationId,
                        Channel = notification.Channel.ToString()
                    });
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification {NotificationId}", notificationId);
            
            if (_notifications.TryGetValue(notificationId, out var notification))
            {
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = ex.Message;
            }

            return false;
        }
    }

    public async Task<IEnumerable<StakeholderNotification>> GetPendingNotificationsAsync()
    {
        try
        {
            var pendingNotifications = _notifications.Values
                .Where(n => n.Status == NotificationStatus.Pending)
                .OrderBy(n => n.CreatedAt)
                .ToList();

            return await Task.FromResult(pendingNotifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending notifications");
            throw;
        }
    }

    public async Task<IEnumerable<StakeholderNotification>> GetNotificationsByStakeholderAsync(string stakeholderId, int limit = 50)
    {
        try
        {
            var notifications = _notifications.Values
                .Where(n => n.StakeholderId == stakeholderId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToList();

            return await Task.FromResult(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for stakeholder {StakeholderId}", stakeholderId);
            throw;
        }
    }

    public async Task ProcessScheduledNotificationsAsync()
    {
        try
        {
            var pendingNotifications = await GetPendingNotificationsAsync();
            var processedCount = 0;

            foreach (var notification in pendingNotifications.Take(50)) // Process in batches
            {
                try
                {
                    await SendNotificationAsync(notification.Id);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing notification {NotificationId}", notification.Id);
                }
            }

            if (processedCount > 0)
            {
                _logger.LogInformation("Processed {Count} scheduled notifications", processedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled notifications");
        }
    }

    public async Task<bool> NotifyStakeholdersAsync(string repository, NotificationType type, object data)
    {
        try
        {
            var stakeholders = await _stakeholderService.GetStakeholdersByRepositoryAsync(repository);
            var notificationsCreated = 0;

            foreach (var stakeholder in stakeholders)
            {
                // Check if stakeholder wants this type of notification
                if (!ShouldNotifyStakeholder(stakeholder, type))
                {
                    continue;
                }

                // Determine preferred channels
                var channels = stakeholder.UpdatePreferences.Channels;
                
                foreach (var channel in channels)
                {
                    var notification = new StakeholderNotification
                    {
                        StakeholderId = stakeholder.Id,
                        Subject = GenerateNotificationSubject(type, repository, data),
                        Content = await GenerateNotificationContentAsync(type, repository, data, stakeholder),
                        Type = type,
                        Channel = channel,
                        Metadata = new Dictionary<string, object>
                        {
                            { "Repository", repository },
                            { "DataType", data.GetType().Name }
                        }
                    };

                    await CreateNotificationAsync(notification);
                    notificationsCreated++;
                }
            }

            await _auditService.LogEventAsync(
                "Stakeholder_Notifications_Created",
                repository: repository,
                action: "NotifyStakeholders",
                result: "SUCCESS",
                details: new { 
                    Repository = repository,
                    Type = type.ToString(),
                    NotificationsCreated = notificationsCreated
                });

            _logger.LogInformation("Created {Count} notifications for repository {Repository}, type {Type}", 
                notificationsCreated, repository, type);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying stakeholders for repository {Repository}", repository);
            await _auditService.LogEventAsync(
                "Stakeholder_Notifications_Failed",
                repository: repository,
                action: "NotifyStakeholders",
                result: "FAILURE",
                details: new { Repository = repository, Error = ex.Message });
            return false;
        }
    }

    private bool ShouldNotifyStakeholder(StakeholderConfiguration stakeholder, NotificationType type)
    {
        return type switch
        {
            NotificationType.IssueUpdate => stakeholder.UpdatePreferences.IssueProgressUpdates,
            NotificationType.PullRequestUpdate => stakeholder.UpdatePreferences.PullRequestStatusUpdates,
            NotificationType.DiscussionUpdate => stakeholder.UpdatePreferences.DiscussionUpdates,
            NotificationType.SecurityAlert => stakeholder.UpdatePreferences.SecurityAlerts,
            NotificationType.PerformanceAlert => stakeholder.UpdatePreferences.PerformanceMetrics,
            NotificationType.ProjectSummary => true, // Always send project summaries
            _ => false
        };
    }

    private string GenerateNotificationSubject(NotificationType type, string repository, object data)
    {
        return type switch
        {
            NotificationType.IssueUpdate => $"Issue Update - {repository}",
            NotificationType.PullRequestUpdate => $"Pull Request Update - {repository}",
            NotificationType.DiscussionUpdate => $"Discussion Update - {repository}",
            NotificationType.SecurityAlert => $"Security Alert - {repository}",
            NotificationType.PerformanceAlert => $"Performance Alert - {repository}",
            NotificationType.ProjectSummary => $"Project Summary - {repository}",
            _ => $"Update - {repository}"
        };
    }

    private async Task<string> GenerateNotificationContentAsync(NotificationType type, string repository, object data, StakeholderConfiguration stakeholder)
    {
        var content = new StringBuilder();
        
        content.AppendLine($"Hello {stakeholder.Name},");
        content.AppendLine();

        switch (type)
        {
            case NotificationType.ProjectSummary:
                content.AppendLine($"Here's your project summary for {repository}:");
                content.AppendLine();
                
                // Generate a tailored summary based on data
                if (data is ProjectProgressSummary summary)
                {
                    content.AppendLine($"📊 **Project Metrics:**");
                    content.AppendLine($"- Open Issues: {summary.Issues.TotalOpen}");
                    content.AppendLine($"- Closed Issues: {summary.Issues.TotalClosed}");
                    content.AppendLine($"- Open Pull Requests: {summary.PullRequests.TotalOpen}");
                    content.AppendLine($"- Project Health: {summary.Metrics.ProjectHealth:F1}%");
                    content.AppendLine();
                    
                    if (summary.RecentActivities.Length > 0)
                    {
                        content.AppendLine("🔄 **Recent Activity:**");
                        foreach (var activity in summary.RecentActivities.Take(5))
                        {
                            content.AppendLine($"- {activity.Type}: {activity.Title}");
                        }
                    }
                }
                break;

            case NotificationType.IssueUpdate:
                content.AppendLine($"An issue has been updated in {repository}:");
                content.AppendLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
                break;

            case NotificationType.PullRequestUpdate:
                content.AppendLine($"A pull request has been updated in {repository}:");
                content.AppendLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
                break;

            default:
                content.AppendLine($"Update notification for {repository}:");
                content.AppendLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
                break;
        }

        content.AppendLine();
        content.AppendLine("Best regards,");
        content.AppendLine("Reynolds Copilot Agent");

        return await Task.FromResult(content.ToString());
    }

    private async Task<bool> SendEmailNotificationAsync(StakeholderNotification notification, StakeholderConfiguration stakeholder)
    {
        // Mock email sending - in production, integrate with actual email service
        _logger.LogInformation("Email notification sent to {Email}: {Subject}", 
            stakeholder.Email, notification.Subject);
        
        // Simulate async email sending
        await Task.Delay(100);
        return true;
    }

    private async Task<bool> SendTeamsNotificationAsync(StakeholderNotification notification, StakeholderConfiguration stakeholder)
    {
        // Mock Teams notification - in production, integrate with Teams API
        _logger.LogInformation("Teams notification sent for stakeholder {Name}: {Subject}", 
            stakeholder.Name, notification.Subject);
        
        await Task.Delay(100);
        return true;
    }

    private async Task<bool> SendDashboardNotificationAsync(StakeholderNotification notification, StakeholderConfiguration stakeholder)
    {
        // Dashboard notifications are stored and displayed in the UI
        _logger.LogInformation("Dashboard notification created for stakeholder {Name}: {Subject}", 
            stakeholder.Name, notification.Subject);
        
        await Task.Delay(50);
        return true;
    }

    private async Task<bool> SendWebhookNotificationAsync(StakeholderNotification notification, StakeholderConfiguration stakeholder)
    {
        // Mock webhook notification - in production, send HTTP POST to configured endpoint
        _logger.LogInformation("Webhook notification sent for stakeholder {Name}: {Subject}", 
            stakeholder.Name, notification.Subject);
        
        await Task.Delay(100);
        return true;
    }
}