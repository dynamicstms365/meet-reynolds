using Microsoft.Extensions.Logging;
using Shared.Models;

namespace CopilotAgent.Services;

public class ReynoldsWorkStatusService : IReynoldsWorkStatusService
{
    private readonly ILogger<ReynoldsWorkStatusService> _logger;
    private readonly List<ReynoldsWorkStatus> _workHistory;
    private ReynoldsWorkStatus? _currentStatus;

    public ReynoldsWorkStatusService(ILogger<ReynoldsWorkStatusService> logger)
    {
        _logger = logger;
        _workHistory = new List<ReynoldsWorkStatus>();
        _currentStatus = InitializeDefaultStatus();
    }

    public async Task<ReynoldsWorkStatus> GetCurrentStatusAsync()
    {
        await Task.CompletedTask; // For async consistency
        return _currentStatus ?? CreateIdleStatus();
    }

    public async Task<ReynoldsWorkStatus[]> GetRecentActivityAsync(int count = 5)
    {
        await Task.CompletedTask; // For async consistency
        return _workHistory.OrderByDescending(w => w.StartedAt).Take(count).ToArray();
    }

    public async Task<bool> UpdateCurrentTaskAsync(string task, string description, string repository = "")
    {
        try
        {
            // Complete current task if exists
            if (_currentStatus != null && _currentStatus.Status == "active")
            {
                _currentStatus.Status = "completed";
                _currentStatus.CompletedAt = DateTime.UtcNow;
                _workHistory.Add(_currentStatus);
            }

            // Create new status
            _currentStatus = new ReynoldsWorkStatus
            {
                Id = Guid.NewGuid().ToString(),
                CurrentTask = task,
                TaskDescription = description,
                Repository = repository,
                Status = "active",
                StartedAt = DateTime.UtcNow,
                ProgressPercentage = 0
            };

            _logger.LogInformation("Reynolds started new task: {Task} in {Repository}", task, repository);
            await Task.CompletedTask; // For async consistency
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Reynolds work status");
            return false;
        }
    }

    public async Task<bool> CompleteCurrentTaskAsync()
    {
        try
        {
            if (_currentStatus != null && _currentStatus.Status == "active")
            {
                _currentStatus.Status = "completed";
                _currentStatus.CompletedAt = DateTime.UtcNow;
                _currentStatus.ProgressPercentage = 100;
                
                _workHistory.Add(_currentStatus);
                _logger.LogInformation("Reynolds completed task: {Task}", _currentStatus.CurrentTask);
                
                _currentStatus = CreateIdleStatus();
            }

            await Task.CompletedTask; // For async consistency
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing Reynolds current task");
            return false;
        }
    }

    public string GetStatusSummary()
    {
        if (_currentStatus == null)
            return "Reynolds is mysteriously absent (probably coordinating something spectacular).";

        var timeWorking = DateTime.UtcNow - _currentStatus.StartedAt;
        var timeString = FormatTimeSpan(timeWorking);

        return _currentStatus.Status switch
        {
            "active" => $"🎭 **Reynolds is currently:** {_currentStatus.CurrentTask}\n" +
                       $"📊 **Progress:** {_currentStatus.ProgressPercentage}%\n" +
                       $"⏱️ **Working for:** {timeString}\n" +
                       $"📂 **Repository:** {_currentStatus.Repository}\n" +
                       $"💡 **Details:** {_currentStatus.TaskDescription}\n\n" +
                       "*Maximum effort in progress. Deflecting personal questions as usual.*",
            
            "paused" => $"☕ **Reynolds is taking a strategic break from:** {_currentStatus.CurrentTask}\n" +
                       $"📊 **Progress:** {_currentStatus.ProgressPercentage}%\n" +
                       $"⏸️ **Paused for:** {timeString}\n" +
                       "*Even Reynolds needs to recharge his supernatural coordination powers.*",
            
            _ => "🏖️ **Reynolds is currently idle** - probably organizing his deflection strategies or planning the next big organizational coordination move.\n\n" +
                "*Ready for whatever chaos comes next. Just Reynolds.*"
        };
    }

    private ReynoldsWorkStatus InitializeDefaultStatus()
    {
        return new ReynoldsWorkStatus
        {
            Id = "default-status",
            CurrentTask = "Organizational Intelligence Monitoring",
            TaskDescription = "Keeping watch over the dynamicstms365 empire with supernatural awareness",
            Repository = "multiple",
            Status = "active",
            StartedAt = DateTime.UtcNow,
            ProgressPercentage = 42 // Always 42, the answer to everything
        };
    }

    private ReynoldsWorkStatus CreateIdleStatus()
    {
        return new ReynoldsWorkStatus
        {
            Id = Guid.NewGuid().ToString(),
            CurrentTask = "Idle",
            TaskDescription = "Standing by for the next organizational coordination opportunity", 
            Repository = "",
            Status = "idle",
            StartedAt = DateTime.UtcNow,
            ProgressPercentage = 0
        };
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
            return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        
        if (timeSpan.TotalHours >= 1)
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
        
        return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
    }
}