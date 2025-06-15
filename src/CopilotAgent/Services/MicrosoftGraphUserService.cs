using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Identity;

namespace CopilotAgent.Services;

public interface IMicrosoftGraphUserService
{
    Task<User?> SearchUserAsync(string searchQuery);
    Task<IEnumerable<User>> SearchUsersAsync(string searchQuery, int maxResults = 10);
    Task<User?> GetUserByEmailAsync(string email);
}

public class MicrosoftGraphUserService : IMicrosoftGraphUserService
{
    private readonly ILogger<MicrosoftGraphUserService> _logger;
    private readonly IConfiguration _configuration;
    private readonly GraphServiceClient _graphClient;

    public MicrosoftGraphUserService(
        ILogger<MicrosoftGraphUserService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _graphClient = CreateGraphClient();
    }

    public async Task<User?> SearchUserAsync(string searchQuery)
    {
        try
        {
            _logger.LogInformation("🔍 Reynolds searching Microsoft Graph for: {SearchQuery}", searchQuery);

            // Try direct email lookup first - most precise
            if (IsEmailFormat(searchQuery))
            {
                var userByEmail = await GetUserByEmailAsync(searchQuery);
                if (userByEmail != null)
                {
                    _logger.LogInformation("✅ Reynolds found user by email: {DisplayName}", userByEmail.DisplayName);
                    return userByEmail;
                }
            }

            // Search by display name using Graph search
            var searchResults = await SearchUsersAsync(searchQuery, 5);
            var bestMatch = FindBestMatch(searchResults, searchQuery);

            if (bestMatch != null)
            {
                _logger.LogInformation("✅ Reynolds found best match: {DisplayName} ({Email})", 
                    bestMatch.DisplayName, bestMatch.Mail ?? bestMatch.UserPrincipalName);
            }
            else
            {
                _logger.LogWarning("❌ Reynolds found no matches for: {SearchQuery}", searchQuery);
            }

            return bestMatch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reynolds Graph search failed for: {SearchQuery}", searchQuery);
            return null;
        }
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string searchQuery, int maxResults = 10)
    {
        try
        {
            _logger.LogInformation("🔍 Reynolds executing Graph users search: {SearchQuery}, max: {MaxResults}", 
                searchQuery, maxResults);

            // Use Graph API search with multiple strategies
            var searchStrategies = new[]
            {
                $"displayName:{searchQuery}",
                $"givenName:{searchQuery}",
                $"surname:{searchQuery}",
                $"mail:{searchQuery}",
                $"userPrincipalName:{searchQuery}"
            };

            var allResults = new List<User>();

            foreach (var strategy in searchStrategies)
            {
                try
                {
                    var users = await _graphClient.Users
                        .GetAsync(requestConfiguration =>
                        {
                            requestConfiguration.QueryParameters.Search = $"\"{strategy}\"";
                            requestConfiguration.QueryParameters.Top = maxResults;
                            requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName", "givenName", "surname" };
                            requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                        });

                    if (users?.Value != null)
                    {
                        allResults.AddRange(users.Value);
                    }
                }
                catch (Exception strategyEx)
                {
                    _logger.LogDebug("Search strategy '{Strategy}' failed: {Error}", strategy, strategyEx.Message);
                }
            }

            // Remove duplicates and take best matches
            var uniqueResults = allResults
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .Take(maxResults)
                .ToList();

            _logger.LogInformation("🎯 Reynolds found {Count} unique matches for: {SearchQuery}", 
                uniqueResults.Count, searchQuery);

            return uniqueResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reynolds bulk Graph search failed for: {SearchQuery}", searchQuery);
            return Array.Empty<User>();
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        try
        {
            _logger.LogInformation("📧 Reynolds looking up user by email: {Email}", email);

            var user = await _graphClient.Users[email].GetAsync();
            
            if (user != null)
            {
                _logger.LogInformation("✅ Reynolds found user: {DisplayName}", user.DisplayName);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Email lookup failed for {Email}: {Error}", email, ex.Message);
            return null;
        }
    }

    private GraphServiceClient CreateGraphClient()
    {
        try
        {
            var clientId = _configuration["MicrosoftGraph:ClientId"];
            var clientSecret = _configuration["MicrosoftGraph:ClientSecret"];
            var tenantId = _configuration["MicrosoftGraph:TenantId"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId))
            {
                throw new InvalidOperationException("Microsoft Graph configuration missing required settings");
            }

            var options = new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            };

            var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);
            
            return new GraphServiceClient(clientSecretCredential);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Microsoft Graph client");
            throw;
        }
    }

    private static bool IsEmailFormat(string input)
    {
        return !string.IsNullOrEmpty(input) && input.Contains("@") && input.Contains(".");
    }

    private User? FindBestMatch(IEnumerable<User> users, string searchQuery)
    {
        if (!users.Any()) return null;

        var query = searchQuery.ToLowerInvariant();

        // Scoring strategy for best match - Reynolds style precision
        return users.OrderByDescending(user =>
        {
            var score = 0;
            var displayName = user.DisplayName?.ToLowerInvariant() ?? "";
            var email = user.Mail?.ToLowerInvariant() ?? user.UserPrincipalName?.ToLowerInvariant() ?? "";
            var givenName = user.GivenName?.ToLowerInvariant() ?? "";
            var surname = user.Surname?.ToLowerInvariant() ?? "";

            // Exact matches get highest scores
            if (displayName == query) score += 100;
            if (givenName == query) score += 90;
            if (surname == query) score += 90;
            if (email.StartsWith(query)) score += 80;

            // Partial matches
            if (displayName.Contains(query)) score += 50;
            if (givenName.Contains(query)) score += 40;
            if (surname.Contains(query)) score += 40;
            if (email.Contains(query)) score += 30;

            return score;
        }).First();
    }
}