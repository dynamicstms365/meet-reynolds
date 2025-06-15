using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CopilotAgent.Services;

public interface IGitHubOrganizationService
{
    Task<List<string>> GetOrganizationMembersAsync();
    Task<List<GitHubMember>> GetDetailedOrganizationMembersAsync();
    Task<GitHubMember?> FindMemberByUsernameAsync(string username);
}

public class GitHubOrganizationService : IGitHubOrganizationService
{
    private readonly ILogger<GitHubOrganizationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IGitHubAppAuthService _authService;
    private readonly HttpClient _httpClient;

    public GitHubOrganizationService(
        ILogger<GitHubOrganizationService> logger,
        IConfiguration configuration,
        IGitHubAppAuthService authService,
        HttpClient httpClient)
    {
        _logger = logger;
        _configuration = configuration;
        _authService = authService;
        _httpClient = httpClient;
    }

    public async Task<List<string>> GetOrganizationMembersAsync()
    {
        try
        {
            var detailedMembers = await GetDetailedOrganizationMembersAsync();
            return detailedMembers.Select(m => m.Login).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reynolds failed to get organization member usernames");
            return new List<string>();
        }
    }

    public async Task<List<GitHubMember>> GetDetailedOrganizationMembersAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Reynolds gathering GitHub organization members...");

            var auth = await _authService.GetInstallationTokenAsync();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var organization = _configuration["GitHub:Organization"] ?? "dynamicstms365";
            var members = new List<GitHubMember>();
            var page = 1;
            const int perPage = 100;

            while (true)
            {
                var response = await _httpClient.GetAsync(
                    $"https://api.github.com/orgs/{organization}/members?per_page={perPage}&page={page}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get organization members: {StatusCode}", response.StatusCode);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync();
                var pageMembers = JsonSerializer.Deserialize<List<GitHubMember>>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                }) ?? new List<GitHubMember>();

                if (!pageMembers.Any())
                    break;

                members.AddRange(pageMembers);
                
                // If we got less than a full page, we're done
                if (pageMembers.Count < perPage)
                    break;

                page++;
            }

            _logger.LogInformation("✅ Reynolds discovered {Count} GitHub organization members", members.Count);
            return members;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reynolds GitHub organization discovery failed");
            return new List<GitHubMember>();
        }
    }

    public async Task<GitHubMember?> FindMemberByUsernameAsync(string username)
    {
        try
        {
            _logger.LogInformation("🎯 Reynolds searching for GitHub member: {Username}", username);

            var members = await GetDetailedOrganizationMembersAsync();
            var member = members.FirstOrDefault(m => 
                string.Equals(m.Login, username, StringComparison.OrdinalIgnoreCase));

            if (member != null)
            {
                _logger.LogInformation("✅ Reynolds found GitHub member: {Login} (ID: {Id})", member.Login, member.Id);
            }
            else
            {
                _logger.LogWarning("❌ Reynolds couldn't find GitHub member: {Username}", username);
            }

            return member;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding GitHub member: {Username}", username);
            return null;
        }
    }
}

// GitHub member data model
public class GitHubMember
{
    public string Login { get; set; } = "";
    public int Id { get; set; }
    public string NodeId { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string GravatarId { get; set; } = "";
    public string Url { get; set; } = "";
    public string HtmlUrl { get; set; } = "";
    public string FollowersUrl { get; set; } = "";
    public string FollowingUrl { get; set; } = "";
    public string GistsUrl { get; set; } = "";
    public string StarredUrl { get; set; } = "";
    public string SubscriptionsUrl { get; set; } = "";
    public string OrganizationsUrl { get; set; } = "";
    public string ReposUrl { get; set; } = "";
    public string EventsUrl { get; set; } = "";
    public string ReceivedEventsUrl { get; set; } = "";
    public string Type { get; set; } = "";
    public bool SiteAdmin { get; set; }

    public override string ToString()
    {
        return $"{Login} ({Id})";
    }
}