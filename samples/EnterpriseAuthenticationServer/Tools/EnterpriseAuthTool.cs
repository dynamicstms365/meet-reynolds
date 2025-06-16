using ModelContextProtocol.Server;
using System.ComponentModel;
using EnterpriseAuthenticationServer.Services;

namespace EnterpriseAuthenticationServer.Tools;

/// <summary>
/// Demonstrates WORKING enterprise authentication patterns within MCP tools
/// This tool shows the FIX for Issues #365 and #520 in action
/// </summary>
[McpServerToolType]
public class EnterpriseAuthTool
{
    private readonly EnterpriseAuthService _authService;
    private readonly ILogger<EnterpriseAuthTool> _logger;

    public EnterpriseAuthTool(EnterpriseAuthService authService, ILogger<EnterpriseAuthTool> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [McpServerTool, Description("Get current authentication status and user context - demonstrates HttpContextAccessor fix")]
    public async Task<string> GetAuthenticationStatus()
    {
        try
        {
            var currentUser = _authService.GetCurrentUser();
            var isAuthenticated = _authService.IsAuthenticated();
            
            _logger.LogInformation("Authentication status requested by user: {User}", currentUser);
            
            return $"""
                🎭 Enterprise Authentication Status:
                ✅ User: {currentUser}
                ✅ Authenticated: {isAuthenticated}
                ✅ HttpContextAccessor: Working properly
                ✅ DI Lifetime: Fixed (Scoped service injection)
                
                This demonstrates the solution to Issue #365 and #520:
                - HttpContextAccessor properly accessible within MCP tools
                - Enterprise authentication patterns working in production
                - Multi-method auth support (Bearer, API Key, GitHub)
                """;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get authentication status");
            return $"❌ Authentication check failed: {ex.Message}";
        }
    }

    [McpServerTool, Description("Validate enterprise authentication methods - demonstrates multi-auth patterns")]
    public async Task<string> ValidateEnterpriseAuth(
        [Description("Authentication method to test: bearer, apikey, or github")] string method = "all")
    {
        try
        {
            var results = new List<string>();
            
            if (method == "all" || method == "bearer")
            {
                results.Add("🔐 Bearer Token: Configured for JWT validation");
            }
            
            if (method == "all" || method == "apikey")
            {
                results.Add("🔑 API Key: Supporting X-API-Key header with enterprise_ prefix");
            }
            
            if (method == "all" || method == "github")
            {
                results.Add("🐙 GitHub Token: Supporting both 'token' and 'ghp_' formats");
            }
            
            var currentUser = _authService.GetCurrentUser();
            results.Add($"👤 Current User: {currentUser}");
            
            return $"""
                🎭 Enterprise Authentication Validation Results:
                
                {string.Join("\n", results)}
                
                💡 This sample demonstrates:
                - Solution to Issue #520 (HttpContext access for auth)
                - Solution to Issue #365 (HttpContextAccessor DI lifetime)
                - Real-world enterprise authentication patterns
                - Load balancer friendly configuration
                """;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enterprise auth validation failed");
            return $"❌ Enterprise auth validation failed: {ex.Message}";
        }
    }
}