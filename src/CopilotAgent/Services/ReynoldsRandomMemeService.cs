using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CopilotAgent.Services;
using CopilotAgent.Bot;
using Shared.Models;

namespace CopilotAgent.Startup;

// Random Meme Scheduler Service for Reynolds
public class ReynoldsRandomMemeService : BackgroundService
{
    private readonly ILogger<ReynoldsRandomMemeService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly Random _random;
    private DateTime _nextMemeTime;

    public ReynoldsRandomMemeService(
        ILogger<ReynoldsRandomMemeService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _random = new Random();
        _nextMemeTime = CalculateNextMemeTime();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reynolds random meme service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var settings = GetMemeScheduleSettings();
                
                if (settings.EnableRandomMemes && DateTime.UtcNow >= _nextMemeTime)
                {
                    await SendRandomMemeToChannels(settings);
                    _nextMemeTime = CalculateNextMemeTime();
                }

                // Check every minute for meme opportunities
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Reynolds random meme service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task SendRandomMemeToChannels(MemeScheduleSettings settings)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var memeService = scope.ServiceProvider.GetRequiredService<IReynoldsMemeService>();
            var teamsService = scope.ServiceProvider.GetRequiredService<IReynoldsTeamsService>();

            // Select category based on settings or random
            string? category = null;
            if (settings.Categories.Length > 0)
            {
                category = settings.Categories[_random.Next(settings.Categories.Length)];
            }

            var meme = await memeService.GetRandomMemeAsync(category);
            if (meme == null)
            {
                _logger.LogWarning("Reynolds random meme service: No memes available for category {Category}", category ?? "all");
                return;
            }

            // Send to configured channels (for now, just log the intent)
            foreach (var channelId in settings.TargetChannels)
            {
                _logger.LogInformation("Reynolds sending random meme {MemeId} to channel {ChannelId}", meme.Id, channelId);
                
                // Note: This would need proper channel messaging implementation
                // For now, we'll use the existing proactive messaging infrastructure
                await memeService.SendMemeToChannelAsync(channelId, meme);
            }

            _logger.LogInformation("Reynolds random meme delivery completed: {MemeName} ({Category})", meme.Name, meme.Category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending random meme");
        }
    }

    private MemeScheduleSettings GetMemeScheduleSettings()
    {
        // Get settings from configuration
        var enableRandomMemes = _configuration.GetValue<bool>("Reynolds:Memes:EnableRandom", false);
        var minIntervalHours = _configuration.GetValue<int>("Reynolds:Memes:MinIntervalHours", 2);
        var maxIntervalHours = _configuration.GetValue<int>("Reynolds:Memes:MaxIntervalHours", 6);

        var targetChannels = _configuration.GetSection("Reynolds:Memes:TargetChannels")
            .Get<string[]>() ?? Array.Empty<string>();

        var categories = _configuration.GetSection("Reynolds:Memes:Categories")
            .Get<string[]>() ?? new[] { "general", "project-management", "development", "motivation" };

        return new MemeScheduleSettings
        {
            EnableRandomMemes = enableRandomMemes,
            MinInterval = TimeSpan.FromHours(minIntervalHours),
            MaxInterval = TimeSpan.FromHours(maxIntervalHours),
            TargetChannels = targetChannels,
            Categories = categories
        };
    }

    private DateTime CalculateNextMemeTime()
    {
        var settings = GetMemeScheduleSettings();
        
        if (!settings.EnableRandomMemes)
        {
            return DateTime.MaxValue; // Never send if disabled
        }

        var intervalMinutes = (int)settings.MinInterval.TotalMinutes +
                             _random.Next((int)(settings.MaxInterval.TotalMinutes - settings.MinInterval.TotalMinutes));

        var nextTime = DateTime.UtcNow.AddMinutes(intervalMinutes);
        
        _logger.LogInformation("Reynolds next random meme scheduled for: {NextMemeTime} UTC", nextTime);
        return nextTime;
    }

    public async Task SendMemeNowAsync(string? category = null, string? channelId = null)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var memeService = scope.ServiceProvider.GetRequiredService<IReynoldsMemeService>();

            var meme = await memeService.GetRandomMemeAsync(category);
            if (meme == null)
            {
                _logger.LogWarning("Reynolds on-demand meme: No memes available for category {Category}", category ?? "all");
                return;
            }

            if (!string.IsNullOrEmpty(channelId))
            {
                await memeService.SendMemeToChannelAsync(channelId, meme);
                _logger.LogInformation("Reynolds sent on-demand meme {MemeId} to channel {ChannelId}", meme.Id, channelId);
            }
            else
            {
                var settings = GetMemeScheduleSettings();
                foreach (var targetChannel in settings.TargetChannels)
                {
                    await memeService.SendMemeToChannelAsync(targetChannel, meme);
                }
                _logger.LogInformation("Reynolds sent on-demand meme {MemeId} to all configured channels", meme.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending on-demand meme");
        }
    }
}