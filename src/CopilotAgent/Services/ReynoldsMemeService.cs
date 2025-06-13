using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Shared.Models;

namespace CopilotAgent.Services;

public class ReynoldsMemeService : IReynoldsMemeService
{
    private readonly ILogger<ReynoldsMemeService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IReynoldsTeamsChatService _teamsChatService;
    private readonly Random _random;
    private readonly List<MemeItem> _memes;

    public ReynoldsMemeService(
        ILogger<ReynoldsMemeService> logger,
        IConfiguration configuration,
        IReynoldsTeamsChatService teamsChatService)
    {
        _logger = logger;
        _configuration = configuration;
        _teamsChatService = teamsChatService;
        _random = new Random();
        _memes = InitializeDefaultMemes();
    }

    public async Task<MemeItem?> GetRandomMemeAsync(string? category = null)
    {
        try
        {
            var availableMemes = string.IsNullOrEmpty(category) 
                ? _memes.ToArray()
                : _memes.Where(m => m.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToArray();

            if (availableMemes.Length == 0)
            {
                _logger.LogWarning("No memes available for category: {Category}", category ?? "all");
                return null;
            }

            var selectedMeme = availableMemes[_random.Next(availableMemes.Length)];
            _logger.LogInformation("Reynolds selected meme: {MemeId} - {MemeName}", selectedMeme.Id, selectedMeme.Name);
            
            await Task.CompletedTask; // For async consistency
            return selectedMeme;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting random meme");
            return null;
        }
    }

    public async Task<MemeItem[]> GetMemesByCategoryAsync(string category)
    {
        await Task.CompletedTask; // For async consistency
        return _memes.Where(m => m.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    public async Task<MemeItem[]> GetAllMemesAsync()
    {
        await Task.CompletedTask; // For async consistency
        return _memes.ToArray();
    }

    public async Task<bool> AddMemeAsync(MemeItem meme)
    {
        try
        {
            if (string.IsNullOrEmpty(meme.Id))
                meme.Id = Guid.NewGuid().ToString();

            _memes.Add(meme);
            _logger.LogInformation("Reynolds added new meme: {MemeId} - {MemeName}", meme.Id, meme.Name);
            
            await Task.CompletedTask; // For async consistency
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding meme: {MemeName}", meme.Name);
            return false;
        }
    }

    public async Task<bool> SendMemeToChannelAsync(string channelId, MemeItem meme)
    {
        try
        {
            var message = FormatMemeMessage(meme);
            
            // Note: This would need proper channel messaging implementation
            // For now, we'll log the intent and return success
            _logger.LogInformation("Reynolds sending meme {MemeId} to channel {ChannelId}", meme.Id, channelId);
            _logger.LogInformation("Meme message: {Message}", message);
            
            await Task.CompletedTask; // Placeholder for actual Teams channel messaging
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending meme {MemeId} to channel {ChannelId}", meme.Id, channelId);
            return false;
        }
    }

    private string FormatMemeMessage(MemeItem meme)
    {
        return $@"🎭 **Reynolds' Meme of the Moment**

{meme.Description}

{meme.Url}

*{GetReynoldsMemeFlavor()}*";
    }

    private string GetReynoldsMemeFlavor()
    {
        var flavors = new[]
        {
            "Maximum effort meme deployment. Just Reynolds.",
            "This meme has been Reynolds-approved for maximum organizational impact.",
            "Like my name situation, this meme is mysteriously effective.",
            "Bringing you quality memes with the same precision I bring to project management.",
            "This meme pairs well with scope creep detection and stakeholder coordination.",
            "A meme so good, even my deflection strategies are impressed."
        };
        
        return flavors[_random.Next(flavors.Length)];
    }

    private List<MemeItem> InitializeDefaultMemes()
    {
        return new List<MemeItem>
        {
            new MemeItem
            {
                Id = "reynolds-classic-1",
                Name = "Reynolds Project Management",
                Url = "https://i.imgur.com/deadpool-project-mgmt.gif",
                Description = "When the project scope changes for the 5th time this week",
                Category = "project-management",
                Tags = new[] { "scope-creep", "project-management", "reynolds" },
                CreatedAt = DateTime.UtcNow
            },
            new MemeItem
            {
                Id = "reynolds-classic-2", 
                Name = "Name Deflection",
                Url = "https://i.imgur.com/deadpool-deflection.gif",
                Description = "When someone asks about my name origin",
                Category = "personal",
                Tags = new[] { "name", "deflection", "mystery" },
                CreatedAt = DateTime.UtcNow
            },
            new MemeItem
            {
                Id = "reynolds-dev-1",
                Name = "Debugging Success",
                Url = "https://i.imgur.com/success-celebration.gif", 
                Description = "When the code finally works after 47 attempts",
                Category = "development",
                Tags = new[] { "debugging", "success", "development" },
                CreatedAt = DateTime.UtcNow
            },
            new MemeItem
            {
                Id = "reynolds-teams-1",
                Name = "Teams Coordination",
                Url = "https://i.imgur.com/coordination-master.gif",
                Description = "Orchestrating cross-team collaboration like a maestro",
                Category = "teamwork",
                Tags = new[] { "teams", "coordination", "leadership" },
                CreatedAt = DateTime.UtcNow
            },
            new MemeItem
            {
                Id = "reynolds-motivation-1",
                Name = "Maximum Effort",
                Url = "https://i.imgur.com/maximum-effort.gif",
                Description = "The Reynolds approach to everything",
                Category = "motivation",
                Tags = new[] { "motivation", "effort", "reynolds-way" },
                CreatedAt = DateTime.UtcNow
            }
        };
    }
}