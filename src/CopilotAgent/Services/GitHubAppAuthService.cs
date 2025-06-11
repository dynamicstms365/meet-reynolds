using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Shared.Models;

namespace CopilotAgent.Services;

public interface IGitHubAppAuthService
{
    Task<GitHubAppAuthentication> GetInstallationTokenAsync();
    Task<GitHubConnectivityResult> TestConnectivityAsync();
}

public class GitHubAppAuthService : IGitHubAppAuthService
{
    private readonly ILogger<GitHubAppAuthService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private GitHubAppAuthentication? _cachedToken;

    public GitHubAppAuthService(
        ILogger<GitHubAppAuthService> logger,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<GitHubAppAuthentication> GetInstallationTokenAsync()
    {
        // Check if we have a valid cached token
        if (_cachedToken != null && _cachedToken.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
        {
            return _cachedToken;
        }

        try
        {
            var appId = _configuration["NGL_DEVOPS_APP_ID"] ?? 
                       System.Environment.GetEnvironmentVariable("NGL_DEVOPS_APP_ID");
            var installationId = _configuration["NGL_DEVOPS_INSTALLATION_ID"] ?? 
                               System.Environment.GetEnvironmentVariable("NGL_DEVOPS_INSTALLATION_ID");
            var privateKeyPem = _configuration["NGL_DEVOPS_PRIVATE_KEY"] ?? 
                              System.Environment.GetEnvironmentVariable("NGL_DEVOPS_PRIVATE_KEY");

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(installationId) || string.IsNullOrEmpty(privateKeyPem))
            {
                throw new InvalidOperationException("GitHub App credentials not configured. Required: NGL_DEVOPS_APP_ID, NGL_DEVOPS_INSTALLATION_ID, NGL_DEVOPS_PRIVATE_KEY");
            }

            // Generate JWT token for GitHub App authentication
            var jwtToken = GenerateJwtToken(appId, privateKeyPem);

            // Exchange JWT for installation access token
            var installationToken = await GetInstallationAccessTokenAsync(installationId, jwtToken);

            _cachedToken = installationToken;
            _logger.LogInformation("Successfully obtained GitHub App installation token");
            
            return installationToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain GitHub App installation token");
            throw;
        }
    }

    public async Task<GitHubConnectivityResult> TestConnectivityAsync()
    {
        try
        {
            var auth = await GetInstallationTokenAsync();
            
            // Test API call to verify connectivity
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

            var response = await _httpClient.GetAsync("https://api.github.com/installation/repositories");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var repositories = ParseRepositories(content);
                
                return new GitHubConnectivityResult
                {
                    Success = true,
                    InstallationId = _configuration["NGL_DEVOPS_INSTALLATION_ID"],
                    Repositories = repositories,
                    Permissions = auth.Permissions,
                    TokenExpiresAt = auth.ExpiresAt
                };
            }
            else
            {
                return new GitHubConnectivityResult
                {
                    Success = false,
                    Error = $"GitHub API call failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub connectivity test failed");
            return new GitHubConnectivityResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private string GenerateJwtToken(string appId, string privateKeyPem)
    {
        var key = GetRSAKeyFromPem(privateKeyPem);
        var securityKey = new RsaSecurityKey(key);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var now = DateTimeOffset.UtcNow;
        var payload = new[]
        {
            new Claim("iat", now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("exp", now.AddMinutes(10).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("iss", appId)
        };

        var token = new JwtSecurityToken(
            claims: payload,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<GitHubAppAuthentication> GetInstallationAccessTokenAsync(string installationId, string jwtToken)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

        var url = $"https://api.github.com/app/installations/{installationId}/access_tokens";
        var response = await _httpClient.PostAsync(url, new StringContent("{}", Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to get installation access token: {response.StatusCode} - {errorContent}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(content);

        var token = tokenData.GetProperty("token").GetString() ?? throw new InvalidOperationException("Token not found in response");
        var expiresAt = tokenData.GetProperty("expires_at").GetDateTime();
        
        var permissions = new List<string>();
        if (tokenData.TryGetProperty("permissions", out var permsElement))
        {
            foreach (var perm in permsElement.EnumerateObject())
            {
                permissions.Add($"{perm.Name}:{perm.Value.GetString()}");
            }
        }

        return new GitHubAppAuthentication
        {
            Token = token,
            ExpiresAt = expiresAt,
            Permissions = permissions.ToArray()
        };
    }

    private static RSA GetRSAKeyFromPem(string privateKeyPem)
    {
        var key = RSA.Create();
        
        // Remove headers and whitespace
        var keyData = privateKeyPem
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
            .Replace("-----END RSA PRIVATE KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim();

        var keyBytes = Convert.FromBase64String(keyData);

        try
        {
            // Try PKCS#8 format first
            key.ImportPkcs8PrivateKey(keyBytes, out _);
        }
        catch
        {
            try
            {
                // Fall back to PKCS#1 format
                key.ImportRSAPrivateKey(keyBytes, out _);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse private key. Ensure it's in PKCS#8 or PKCS#1 format.", ex);
            }
        }

        return key;
    }

    private static string[] ParseRepositories(string jsonContent)
    {
        try
        {
            var doc = JsonSerializer.Deserialize<JsonElement>(jsonContent);
            var repositories = new List<string>();
            
            if (doc.TryGetProperty("repositories", out var reposArray))
            {
                foreach (var repo in reposArray.EnumerateArray())
                {
                    if (repo.TryGetProperty("full_name", out var nameElement))
                    {
                        repositories.Add(nameElement.GetString() ?? "");
                    }
                }
            }
            
            return repositories.ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}