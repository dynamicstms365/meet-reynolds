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
        _logger.LogDebug("🎭 Reynolds: Attempting to obtain GitHub App installation token with Maximum Effort™");
        
        // Check if we have a valid cached token
        if (_cachedToken != null && _cachedToken.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
        {
            _logger.LogDebug("🎭 Reynolds: Using cached GitHub token (expires at {ExpiresAt})", _cachedToken.ExpiresAt);
            return _cachedToken;
        }

        if (_cachedToken != null)
        {
            _logger.LogInformation("🎭 Reynolds: Cached token expired at {ExpiresAt}, obtaining fresh token", _cachedToken.ExpiresAt);
        }

        try
        {
            // Check if we're running in GitHub Actions with a pre-generated token
            var githubToken = System.Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            var isGitHubActions = IsGitHubActionsEnvironment();
            
            _logger.LogDebug("🎭 Reynolds: Environment analysis - GitHub Actions: {IsGitHubActions}, GITHUB_TOKEN present: {HasToken}",
                isGitHubActions, !string.IsNullOrEmpty(githubToken));
            
            if (!string.IsNullOrEmpty(githubToken) && isGitHubActions)
            {
                _logger.LogInformation("🎭 Reynolds: Using GitHub Actions generated token with supernatural efficiency");
                return await CreateTokenFromGitHubActions(githubToken);
            }

            // Fall back to manual JWT generation
            _logger.LogInformation("🎭 Reynolds: Generating GitHub App JWT token manually");
            return await GenerateInstallationTokenManually();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Reynolds: Failed to obtain GitHub App installation token - supernatural coordination compromised");
            _logger.LogError("🔧 Reynolds: Verify NGL_DEVOPS_APP_ID, NGL_DEVOPS_PRIVATE_KEY, and NGL_DEVOPS_INSTALLATION_ID configuration");
            throw;
        }
    }

    private async Task<GitHubAppAuthentication> CreateTokenFromGitHubActions(string githubToken)
    {
        // When using GitHub Actions, the token is already scoped to the installation
        // Test the token to get installation information
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", githubToken);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

        var response = await _httpClient.GetAsync("https://api.github.com/installation/repositories");
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GitHub Actions token validation failed: {response.StatusCode}");
        }

        // GitHub Actions tokens typically expire after the job completion
        // Set a reasonable expiration time for caching
        var expiresAt = DateTime.UtcNow.AddHours(1);

        return new GitHubAppAuthentication
        {
            Token = githubToken,
            ExpiresAt = expiresAt,
            Permissions = new[] { "github-actions-scoped" }
        };
    }

    private async Task<GitHubAppAuthentication> GenerateInstallationTokenManually()
    {
        _logger.LogDebug("🎭 Reynolds: Starting manual GitHub App token generation with Maximum Effort™");
        
        var appId = _configuration["NGL_DEVOPS_APP_ID"] ??
                   System.Environment.GetEnvironmentVariable("NGL_DEVOPS_APP_ID");
        var installationId = _configuration["NGL_DEVOPS_INSTALLATION_ID"] ??
                           System.Environment.GetEnvironmentVariable("NGL_DEVOPS_INSTALLATION_ID");
        var privateKeyPem = _configuration["NGL_DEVOPS_PRIVATE_KEY"] ??
                          System.Environment.GetEnvironmentVariable("NGL_DEVOPS_PRIVATE_KEY");

        _logger.LogDebug("🔧 Reynolds: Configuration analysis - AppId: {HasAppId}, InstallationId: {HasInstallationId}, PrivateKey: {HasPrivateKey}",
            !string.IsNullOrEmpty(appId) ? "PRESENT" : "MISSING",
            !string.IsNullOrEmpty(installationId) ? "PRESENT" : "MISSING",
            !string.IsNullOrEmpty(privateKeyPem) ? "PRESENT" : "MISSING");

        if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(privateKeyPem))
        {
            _logger.LogError("💥 Reynolds: Critical GitHub App credentials missing!");
            _logger.LogError("🔧 Required configuration:");
            _logger.LogError("   - NGL_DEVOPS_APP_ID: {Status}", string.IsNullOrEmpty(appId) ? "MISSING" : "PRESENT");
            _logger.LogError("   - NGL_DEVOPS_PRIVATE_KEY: {Status}", string.IsNullOrEmpty(privateKeyPem) ? "MISSING" : "PRESENT");
            _logger.LogError("   - NGL_DEVOPS_INSTALLATION_ID: {Status} (optional - will auto-resolve)", string.IsNullOrEmpty(installationId) ? "MISSING" : "PRESENT");
            
            throw new InvalidOperationException("GitHub App credentials not configured. Required: NGL_DEVOPS_APP_ID, NGL_DEVOPS_PRIVATE_KEY (and optionally NGL_DEVOPS_INSTALLATION_ID)");
        }

        _logger.LogInformation("🎭 Reynolds: GitHub App credentials validated - App ID: {AppId}", appId);

        // If installation ID is not provided, try to resolve it automatically
        if (string.IsNullOrEmpty(installationId))
        {
            _logger.LogInformation("🔍 Reynolds: Installation ID not provided - attempting supernatural auto-resolution");
            installationId = await ResolveInstallationIdAsync(appId, privateKeyPem);
            _logger.LogInformation("✨ Reynolds: Successfully resolved installation ID: {InstallationId}", installationId);
        }
        else
        {
            _logger.LogInformation("🎯 Reynolds: Using provided installation ID: {InstallationId}", installationId);
        }

        try
        {
            // Generate JWT token for GitHub App authentication
            _logger.LogDebug("🔐 Reynolds: Generating JWT token for GitHub App authentication");
            var jwtToken = GenerateJwtToken(appId, privateKeyPem);
            _logger.LogDebug("✅ Reynolds: JWT token generated successfully");

            // Exchange JWT for installation access token
            _logger.LogDebug("🔄 Reynolds: Exchanging JWT for installation access token");
            var installationToken = await GetInstallationAccessTokenAsync(installationId, jwtToken);
            
            _cachedToken = installationToken;
            _logger.LogInformation("🎭 Reynolds: Successfully obtained GitHub App installation token for App ID {AppId}", appId);
            _logger.LogInformation("⏰ Reynolds: Token expires at {ExpiresAt} (valid for {ValidFor})",
                installationToken.ExpiresAt,
                installationToken.ExpiresAt - DateTime.UtcNow);
            _logger.LogDebug("🔑 Reynolds: Token permissions: {Permissions}",
                string.Join(", ", installationToken.Permissions));
            
            return installationToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Reynolds: Failed during manual token generation process");
            _logger.LogError("🔧 Reynolds: Check GitHub App configuration and network connectivity");
            throw;
        }
    }

    private async Task<string> ResolveInstallationIdAsync(string appId, string privateKeyPem)
    {
        // Generate JWT to call the GitHub App API
        var jwtToken = GenerateJwtToken(appId, privateKeyPem);
        
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CopilotAgent/1.0");

        var response = await _httpClient.GetAsync("https://api.github.com/app/installations");
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to resolve installation ID: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var installations = JsonSerializer.Deserialize<JsonElement>(content);

        if (installations.ValueKind == JsonValueKind.Array && installations.GetArrayLength() > 0)
        {
            var firstInstallation = installations[0];
            if (firstInstallation.TryGetProperty("id", out var idElement))
            {
                var resolvedId = idElement.GetInt64().ToString();
                _logger.LogInformation("Automatically resolved installation ID: {InstallationId}", resolvedId);
                return resolvedId;
            }
        }

        throw new InvalidOperationException("No GitHub App installations found. Ensure the app is installed on the organization.");
    }

    private static bool IsGitHubActionsEnvironment()
    {
        return !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
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