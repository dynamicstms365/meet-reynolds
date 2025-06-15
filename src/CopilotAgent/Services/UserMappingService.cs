using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CopilotAgent.Services;

public interface IUserMappingService
{
    Task<UserMapping?> GetMappingAsync(string teamsUserId, string? displayName = null);
    Task<UserMapping?> GetMappingByEmailAsync(string email);
    Task<bool> StoreMappingAsync(UserMapping mapping);
    Task<bool> RemoveMappingAsync(string teamsUserId);
    Task<List<UserMapping>> GetAllMappingsAsync();
    Task<bool> ValidateMappingAsync(UserMapping mapping);
}

public class UserMappingService : IUserMappingService
{
    private readonly ILogger<UserMappingService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, UserMapping> _persistentMappings;
    private readonly string _mappingFilePath;

    public UserMappingService(
        ILogger<UserMappingService> logger,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _logger = logger;
        _cache = cache;
        _configuration = configuration;
        _persistentMappings = new ConcurrentDictionary<string, UserMapping>();
        _mappingFilePath = configuration["UserMapping:FilePath"] ?? "user-mappings.json";
        
        _ = LoadPersistedMappingsAsync(); // Fire and forget initialization
    }

    public async Task<UserMapping?> GetMappingAsync(string teamsUserId, string? displayName = null)
    {
        try
        {
            _logger.LogInformation("🔍 Reynolds searching for user mapping: TeamsId={TeamsUserId}, DisplayName={DisplayName}", 
                teamsUserId, displayName);

            // Check memory cache first - Reynolds loves efficiency
            var cacheKey = $"user_mapping_{teamsUserId}";
            if (_cache.TryGetValue(cacheKey, out UserMapping? cachedMapping))
            {
                _logger.LogInformation("✅ Reynolds found cached mapping for {TeamsUserId} → {GitHubId}", 
                    teamsUserId, cachedMapping.GitHubId);
                return cachedMapping;
            }

            // Check persistent storage
            if (_persistentMappings.TryGetValue(teamsUserId, out var persistentMapping))
            {
                // Refresh cache
                _cache.Set(cacheKey, persistentMapping, TimeSpan.FromMinutes(30));
                
                _logger.LogInformation("✅ Reynolds found persistent mapping for {TeamsUserId} → {GitHubId}", 
                    teamsUserId, persistentMapping.GitHubId);
                return persistentMapping;
            }

            // If we have a display name, try fuzzy matching
            if (!string.IsNullOrEmpty(displayName))
            {
                var fuzzyMatch = await FindByDisplayNameAsync(displayName);
                if (fuzzyMatch != null)
                {
                    _logger.LogInformation("🎯 Reynolds found fuzzy match for '{DisplayName}' → {GitHubId}", 
                        displayName, fuzzyMatch.GitHubId);
                    return fuzzyMatch;
                }
            }

            _logger.LogWarning("❌ Reynolds found no mapping for TeamsId={TeamsUserId}", teamsUserId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user mapping for {TeamsUserId}", teamsUserId);
            return null;
        }
    }

    public async Task<UserMapping?> GetMappingByEmailAsync(string email)
    {
        try
        {
            _logger.LogInformation("🔍 Reynolds searching for mapping by email: {Email}", email);

            var mapping = _persistentMappings.Values.FirstOrDefault(m => 
                string.Equals(m.Email, email, StringComparison.OrdinalIgnoreCase));

            if (mapping != null)
            {
                _logger.LogInformation("✅ Reynolds found mapping by email {Email} → {GitHubId}", 
                    email, mapping.GitHubId);
            }

            await Task.CompletedTask; // For async consistency
            return mapping;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user mapping by email {Email}", email);
            return null;
        }
    }

    public async Task<bool> StoreMappingAsync(UserMapping mapping)
    {
        try
        {
            _logger.LogInformation("💾 Reynolds storing user mapping: {TeamsUserId} → {GitHubId}", 
                mapping.TeamsUserId, mapping.GitHubId);

            // Validate mapping before storing
            if (await ValidateMappingAsync(mapping))
            {
                // Store in memory cache
                var cacheKey = $"user_mapping_{mapping.TeamsUserId}";
                _cache.Set(cacheKey, mapping, TimeSpan.FromMinutes(30));

                // Store in persistent storage
                _persistentMappings.AddOrUpdate(mapping.TeamsUserId, mapping, (key, oldValue) => mapping);

                // Persist to file
                await PersistMappingsAsync();

                _logger.LogInformation("✅ Reynolds successfully stored mapping for {TeamsUserId}", mapping.TeamsUserId);
                return true;
            }
            else
            {
                _logger.LogWarning("❌ Reynolds rejected invalid mapping for {TeamsUserId}", mapping.TeamsUserId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing user mapping for {TeamsUserId}", mapping.TeamsUserId);
            return false;
        }
    }

    public async Task<bool> RemoveMappingAsync(string teamsUserId)
    {
        try
        {
            _logger.LogInformation("🗑️ Reynolds removing user mapping for {TeamsUserId}", teamsUserId);

            // Remove from cache
            var cacheKey = $"user_mapping_{teamsUserId}";
            _cache.Remove(cacheKey);

            // Remove from persistent storage
            _persistentMappings.TryRemove(teamsUserId, out _);

            // Persist changes
            await PersistMappingsAsync();

            _logger.LogInformation("✅ Reynolds successfully removed mapping for {TeamsUserId}", teamsUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user mapping for {TeamsUserId}", teamsUserId);
            return false;
        }
    }

    public async Task<List<UserMapping>> GetAllMappingsAsync()
    {
        await Task.CompletedTask; // For async consistency
        return _persistentMappings.Values.ToList();
    }

    public async Task<bool> ValidateMappingAsync(UserMapping mapping)
    {
        await Task.CompletedTask; // For async consistency

        // Basic validation - Reynolds demands quality
        if (string.IsNullOrEmpty(mapping.TeamsUserId) || 
            string.IsNullOrEmpty(mapping.GitHubId) ||
            string.IsNullOrEmpty(mapping.Email))
        {
            return false;
        }

        // Validate email format
        if (!IsValidEmail(mapping.Email))
        {
            return false;
        }

        // Validate GitHub ID format
        if (mapping.GitHubId.Contains(" ") || mapping.GitHubId.Length < 3)
        {
            return false;
        }

        return true;
    }

    private async Task<UserMapping?> FindByDisplayNameAsync(string displayName)
    {
        await Task.CompletedTask; // For async consistency

        // Try exact match first
        var exactMatch = _persistentMappings.Values.FirstOrDefault(m => 
            string.Equals(m.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null) return exactMatch;

        // Try partial matching - Reynolds is thorough
        var partialMatch = _persistentMappings.Values.FirstOrDefault(m => 
            m.DisplayName?.Contains(displayName, StringComparison.OrdinalIgnoreCase) == true ||
            displayName.Contains(m.DisplayName ?? "", StringComparison.OrdinalIgnoreCase));

        return partialMatch;
    }

    private async Task LoadPersistedMappingsAsync()
    {
        try
        {
            if (File.Exists(_mappingFilePath))
            {
                var json = await File.ReadAllTextAsync(_mappingFilePath);
                var mappings = JsonSerializer.Deserialize<List<UserMapping>>(json) ?? new List<UserMapping>();

                foreach (var mapping in mappings)
                {
                    _persistentMappings.TryAdd(mapping.TeamsUserId, mapping);
                }

                _logger.LogInformation("📥 Reynolds loaded {Count} user mappings from persistent storage", mappings.Count);
            }
            else
            {
                _logger.LogInformation("📝 Reynolds initialized empty user mapping storage");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading persisted user mappings");
        }
    }

    private async Task PersistMappingsAsync()
    {
        try
        {
            var mappings = _persistentMappings.Values.ToList();
            var json = JsonSerializer.Serialize(mappings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            await File.WriteAllTextAsync(_mappingFilePath, json);
            _logger.LogInformation("💾 Reynolds persisted {Count} user mappings", mappings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting user mappings");
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

// Data model for user mappings
public class UserMapping
{
    public string TeamsUserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string GitHubId { get; set; } = "";
    public string? GitHubEmail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastValidated { get; set; } = DateTime.UtcNow;
    public bool IsValidated { get; set; } = false;
    public Dictionary<string, object> Metadata { get; set; } = new();

    public override string ToString()
    {
        return $"{DisplayName} ({Email}) → {GitHubId}";
    }
}