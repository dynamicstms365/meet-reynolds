using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using CopilotAgent.Services;

namespace CopilotAgent.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StakeholderDashboardController : ControllerBase
{
    private readonly ILogger<StakeholderDashboardController> _logger;
    private readonly IStakeholderVisibilityService _stakeholderService;
    private readonly IDashboardService _dashboardService;
    private readonly INotificationService _notificationService;

    public StakeholderDashboardController(
        ILogger<StakeholderDashboardController> logger,
        IStakeholderVisibilityService stakeholderService,
        IDashboardService dashboardService,
        INotificationService notificationService)
    {
        _logger = logger;
        _stakeholderService = stakeholderService;
        _dashboardService = dashboardService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Get all stakeholders
    /// </summary>
    [HttpGet("stakeholders")]
    public async Task<ActionResult<IEnumerable<StakeholderConfiguration>>> GetStakeholders()
    {
        try
        {
            var stakeholders = await _stakeholderService.GetAllStakeholdersAsync();
            return Ok(stakeholders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stakeholders");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific stakeholder by ID
    /// </summary>
    [HttpGet("stakeholders/{stakeholderId}")]
    public async Task<ActionResult<StakeholderConfiguration>> GetStakeholder(string stakeholderId)
    {
        try
        {
            var stakeholder = await _stakeholderService.GetStakeholderAsync(stakeholderId);
            if (stakeholder == null)
            {
                return NotFound($"Stakeholder {stakeholderId} not found");
            }

            return Ok(stakeholder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stakeholder {StakeholderId}", stakeholderId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new stakeholder
    /// </summary>
    [HttpPost("stakeholders")]
    public async Task<ActionResult<StakeholderConfiguration>> CreateStakeholder([FromBody] StakeholderConfiguration stakeholder)
    {
        try
        {
            var createdStakeholder = await _stakeholderService.CreateStakeholderAsync(stakeholder);
            return CreatedAtAction(nameof(GetStakeholder), new { stakeholderId = createdStakeholder.Id }, createdStakeholder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stakeholder");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing stakeholder
    /// </summary>
    [HttpPut("stakeholders/{stakeholderId}")]
    public async Task<ActionResult<StakeholderConfiguration>> UpdateStakeholder(string stakeholderId, [FromBody] StakeholderConfiguration stakeholder)
    {
        try
        {
            var updatedStakeholder = await _stakeholderService.UpdateStakeholderAsync(stakeholderId, stakeholder);
            return Ok(updatedStakeholder);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stakeholder {StakeholderId}", stakeholderId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a stakeholder
    /// </summary>
    [HttpDelete("stakeholders/{stakeholderId}")]
    public async Task<IActionResult> DeleteStakeholder(string stakeholderId)
    {
        try
        {
            var deleted = await _stakeholderService.DeleteStakeholderAsync(stakeholderId);
            if (!deleted)
            {
                return NotFound($"Stakeholder {stakeholderId} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting stakeholder {StakeholderId}", stakeholderId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get project dashboard for a specific repository
    /// </summary>
    [HttpGet("dashboard/project/{repository}")]
    public async Task<ActionResult<ProjectProgressSummary>> GetProjectDashboard(string repository, [FromQuery] string? stakeholderId = null)
    {
        try
        {
            var summary = await _dashboardService.GenerateProjectSummaryAsync(repository, stakeholderId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating project dashboard for repository {Repository}", repository);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get organization dashboard
    /// </summary>
    [HttpGet("dashboard/organization/{organization}")]
    public async Task<ActionResult<ProjectProgressSummary>> GetOrganizationDashboard(string organization, [FromQuery] string? stakeholderId = null)
    {
        try
        {
            var summary = await _dashboardService.GenerateOrganizationSummaryAsync(organization, stakeholderId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating organization dashboard for {Organization}", organization);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get dashboard configuration for a stakeholder
    /// </summary>
    [HttpGet("stakeholders/{stakeholderId}/dashboard-config")]
    public async Task<ActionResult<DashboardConfiguration>> GetDashboardConfiguration(string stakeholderId)
    {
        try
        {
            var config = await _dashboardService.GetDashboardConfigurationAsync(stakeholderId);
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard configuration for stakeholder {StakeholderId}", stakeholderId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update dashboard configuration for a stakeholder
    /// </summary>
    [HttpPut("stakeholders/{stakeholderId}/dashboard-config")]
    public async Task<ActionResult<DashboardConfiguration>> UpdateDashboardConfiguration(string stakeholderId, [FromBody] DashboardConfiguration config)
    {
        try
        {
            var updatedConfig = await _dashboardService.UpdateDashboardConfigurationAsync(stakeholderId, config);
            return Ok(updatedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dashboard configuration for stakeholder {StakeholderId}", stakeholderId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get widget data for a specific stakeholder and widget
    /// </summary>
    [HttpGet("stakeholders/{stakeholderId}/widgets/{widgetId}/data")]
    public async Task<ActionResult<Dictionary<string, object>>> GetWidgetData(string stakeholderId, string widgetId)
    {
        try
        {
            var data = await _dashboardService.GetWidgetDataAsync(stakeholderId, widgetId);
            return Ok(data);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving widget data for stakeholder {StakeholderId}, widget {WidgetId}", stakeholderId, widgetId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get notifications for a specific stakeholder
    /// </summary>
    [HttpGet("stakeholders/{stakeholderId}/notifications")]
    public async Task<ActionResult<IEnumerable<StakeholderNotification>>> GetStakeholderNotifications(string stakeholderId, [FromQuery] int limit = 50)
    {
        try
        {
            var notifications = await _notificationService.GetNotificationsByStakeholderAsync(stakeholderId, limit);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for stakeholder {StakeholderId}", stakeholderId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a manual notification for stakeholders
    /// </summary>
    [HttpPost("notifications")]
    public async Task<ActionResult<StakeholderNotification>> CreateNotification([FromBody] StakeholderNotification notification)
    {
        try
        {
            var createdNotification = await _notificationService.CreateNotificationAsync(notification);
            return Ok(createdNotification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Trigger manual notification to all stakeholders for a repository
    /// </summary>
    [HttpPost("notifications/broadcast")]
    public async Task<ActionResult> BroadcastNotification([FromBody] BroadcastNotificationRequest request)
    {
        try
        {
            var success = await _notificationService.NotifyStakeholdersAsync(request.Repository, request.Type, request.Data);
            if (success)
            {
                return Ok(new { message = "Notifications sent successfully" });
            }
            
            return StatusCode(500, "Failed to send notifications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting notification");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Process pending notifications manually
    /// </summary>
    [HttpPost("notifications/process")]
    public async Task<ActionResult> ProcessNotifications()
    {
        try
        {
            await _notificationService.ProcessScheduledNotificationsAsync();
            return Ok(new { message = "Notifications processed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notifications");
            return StatusCode(500, "Internal server error");
        }
    }
}

public class BroadcastNotificationRequest
{
    public string Repository { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public object Data { get; set; } = new();
}