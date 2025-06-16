using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace EnterpriseAuthenticationServer.Services;

/// <summary>
/// Enterprise authentication service demonstrating the FIX for Issue #365 and #520
/// Shows proper HttpContextAccessor usage with correct DI lifetime management
/// </summary>
public class EnterpriseAuthService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EnterpriseAuthService> _logger;

    public EnterpriseAuthService(IHttpContextAccessor httpContextAccessor, ILogger<EnterpriseAuthService> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// CRITICAL FIX: This method demonstrates proper HttpContext access within MCP server tools
    /// Previously failed due to DI lifetime mismatch (Singleton MCP vs Scoped HttpContextAccessor)
    /// </summary>
    public async Task ValidateAuthenticationAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var context = _httpContextAccessor.HttpContext ?? httpContext;
        
        if (context == null)
        {
            throw new InvalidOperationException("HttpContext is not available");
        }

        // Multi-method authentication support
        var isAuthenticated = await ValidateBearerTokenAsync(context, cancellationToken) ||
                             await ValidateApiKeyAsync(context, cancellationToken) ||
                             await ValidateGitHubTokenAsync(context, cancellationToken);

        if (!isAuthenticated)
        {
            throw new UnauthorizedAccessException("Authentication required for MCP access");
        }

        _logger.LogInformation("Enterprise authentication successful for user: {User}", 
            context.User?.Identity?.Name ?? "Anonymous");
    }

    private async Task<bool> ValidateBearerTokenAsync(HttpContext context, CancellationToken cancellationToken)
    {
        // Check for JWT Bearer token
        var authResult = await context.AuthenticateAsync();
        if (authResult.Succeeded && authResult.Principal?.Identity?.IsAuthenticated == true)
        {
            _logger.LogDebug("Bearer token authentication successful");
            return true;
        }
        return false;
    }

    private async Task<bool> ValidateApiKeyAsync(HttpContext context, CancellationToken cancellationToken)
    {
        // Check for API key in headers
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey) && !string.IsNullOrEmpty(apiKey))
        {
            // Validate API key against your enterprise system
            // This is a simplified example - implement proper validation
            if (apiKey.ToString().StartsWith("enterprise_"))
            {
                // Create claims principal for API key authentication
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, "ApiKeyUser"),
                    new Claim(ClaimTypes.AuthenticationMethod, "ApiKey")
                };
                var identity = new ClaimsIdentity(claims, "ApiKey");
                context.User = new ClaimsPrincipal(identity);
                
                _logger.LogDebug("API key authentication successful");
                return true;
            }
        }
        return false;
    }

    private async Task<bool> ValidateGitHubTokenAsync(HttpContext context, CancellationToken cancellationToken)
    {
        // Check for GitHub token
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var token = authHeader.ToString();
            if (token.StartsWith("token ") || token.StartsWith("ghp_"))
            {
                // Validate GitHub token - simplified example
                // In production, validate against GitHub API
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, "GitHubUser"),
                    new Claim(ClaimTypes.AuthenticationMethod, "GitHub")
                };
                var identity = new ClaimsIdentity(claims, "GitHub");
                context.User = new ClaimsPrincipal(identity);
                
                _logger.LogDebug("GitHub token authentication successful");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get current user context - demonstrates safe HttpContext access
    /// </summary>
    public string GetCurrentUser()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.User?.Identity?.Name ?? "Anonymous";
    }

    /// <summary>
    /// Check if current request is authenticated
    /// </summary>
    public bool IsAuthenticated()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.User?.Identity?.IsAuthenticated == true;
    }
}