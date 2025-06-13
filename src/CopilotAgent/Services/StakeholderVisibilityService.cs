using System.Collections.Concurrent;
using Shared.Models;

namespace CopilotAgent.Services;

public class StakeholderVisibilityService : IStakeholderVisibilityService
{
    private readonly ILogger<StakeholderVisibilityService> _logger;
    private readonly ISecurityAuditService _auditService;
    
    // In-memory storage for this implementation - in production, use a proper database
    private readonly ConcurrentDictionary<string, StakeholderConfiguration> _stakeholders = new();

    public StakeholderVisibilityService(
        ILogger<StakeholderVisibilityService> logger,
        ISecurityAuditService auditService)
    {
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<StakeholderConfiguration> CreateStakeholderAsync(StakeholderConfiguration stakeholder)
    {
        try
        {
            if (string.IsNullOrEmpty(stakeholder.Id))
            {
                stakeholder.Id = Guid.NewGuid().ToString();
            }

            stakeholder.CreatedAt = DateTime.UtcNow;
            stakeholder.UpdatedAt = DateTime.UtcNow;

            if (!_stakeholders.TryAdd(stakeholder.Id, stakeholder))
            {
                throw new InvalidOperationException($"Stakeholder with ID {stakeholder.Id} already exists");
            }

            await _auditService.LogEventAsync(
                "Stakeholder_Created",
                action: "CreateStakeholder",
                result: "SUCCESS",
                details: new { StakeholderId = stakeholder.Id, Name = stakeholder.Name });

            _logger.LogInformation("Created stakeholder {StakeholderId} ({Name})", stakeholder.Id, stakeholder.Name);
            return stakeholder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stakeholder {Name}", stakeholder.Name);
            await _auditService.LogEventAsync(
                "Stakeholder_Create_Failed",
                action: "CreateStakeholder",
                result: "FAILURE",
                details: new { Name = stakeholder.Name, Error = ex.Message });
            throw;
        }
    }

    public async Task<StakeholderConfiguration> UpdateStakeholderAsync(string stakeholderId, StakeholderConfiguration stakeholder)
    {
        try
        {
            if (!_stakeholders.TryGetValue(stakeholderId, out var existing))
            {
                throw new InvalidOperationException($"Stakeholder with ID {stakeholderId} not found");
            }

            // Preserve creation metadata
            stakeholder.Id = stakeholderId;
            stakeholder.CreatedAt = existing.CreatedAt;
            stakeholder.UpdatedAt = DateTime.UtcNow;

            _stakeholders[stakeholderId] = stakeholder;

            await _auditService.LogEventAsync(
                "Stakeholder_Updated",
                action: "UpdateStakeholder",
                result: "SUCCESS",
                details: new { StakeholderId = stakeholderId, Name = stakeholder.Name });

            _logger.LogInformation("Updated stakeholder {StakeholderId} ({Name})", stakeholderId, stakeholder.Name);
            return stakeholder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stakeholder {StakeholderId}", stakeholderId);
            await _auditService.LogEventAsync(
                "Stakeholder_Update_Failed",
                action: "UpdateStakeholder",
                result: "FAILURE",
                details: new { StakeholderId = stakeholderId, Error = ex.Message });
            throw;
        }
    }

    public async Task<bool> DeleteStakeholderAsync(string stakeholderId)
    {
        try
        {
            var removed = _stakeholders.TryRemove(stakeholderId, out var stakeholder);

            if (removed)
            {
                await _auditService.LogEventAsync(
                    "Stakeholder_Deleted",
                    action: "DeleteStakeholder",
                    result: "SUCCESS",
                    details: new { StakeholderId = stakeholderId, Name = stakeholder?.Name });

                _logger.LogInformation("Deleted stakeholder {StakeholderId}", stakeholderId);
            }
            else
            {
                _logger.LogWarning("Attempted to delete non-existent stakeholder {StakeholderId}", stakeholderId);
            }

            return removed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting stakeholder {StakeholderId}", stakeholderId);
            await _auditService.LogEventAsync(
                "Stakeholder_Delete_Failed",
                action: "DeleteStakeholder",
                result: "FAILURE",
                details: new { StakeholderId = stakeholderId, Error = ex.Message });
            throw;
        }
    }

    public async Task<StakeholderConfiguration?> GetStakeholderAsync(string stakeholderId)
    {
        try
        {
            _stakeholders.TryGetValue(stakeholderId, out var stakeholder);
            return await Task.FromResult(stakeholder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stakeholder {StakeholderId}", stakeholderId);
            throw;
        }
    }

    public async Task<IEnumerable<StakeholderConfiguration>> GetAllStakeholdersAsync()
    {
        try
        {
            return await Task.FromResult(_stakeholders.Values.Where(s => s.IsActive).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all stakeholders");
            throw;
        }
    }

    public async Task<IEnumerable<StakeholderConfiguration>> GetStakeholdersByRepositoryAsync(string repository)
    {
        try
        {
            var stakeholders = _stakeholders.Values
                .Where(s => s.IsActive && s.Repositories.Contains(repository))
                .ToList();

            return await Task.FromResult(stakeholders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stakeholders for repository {Repository}", repository);
            throw;
        }
    }
}