using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using CopilotAgent.Services;

namespace CopilotAgent.MCP;

/// <summary>
/// Reynolds Enterprise MCP Server Configuration
/// Leverages the official MCP SDK 0.2.0-preview.3 with automatic tool discovery
/// </summary>
public static class ReynoldsMcpServerExtensions
{
    /// <summary>
    /// Configure Reynolds MCP Server with supernatural intelligence and Maximum Effort™ coordination
    /// </summary>
    public static IServiceCollection AddReynoldsMcpServer(this IServiceCollection services)
    {
        // Add Reynolds-specific services
        services.AddSingleton<ReynoldsPersonaService>();
        services.AddSingleton<EnterpriseAuthService>();
        
        // Add MCP Server with automatic tool discovery from assembly
        services.AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly(typeof(ReynoldsMcpServerExtensions).Assembly);

        // Add Reynolds server configuration
        services.AddSingleton<ReynoldsMcpServerConfiguration>();
        
        return services;
    }
}

/// <summary>
/// Reynolds MCP Server Configuration and Enhancement Service
/// Provides Reynolds persona enhancement and enterprise authentication
/// </summary>
public class ReynoldsMcpServerConfiguration
{
    private readonly ILogger<ReynoldsMcpServerConfiguration> _logger;
    private readonly EnterpriseAuthService _authService;
    private readonly ReynoldsPersonaService _personaService;

    public ReynoldsMcpServerConfiguration(
        ILogger<ReynoldsMcpServerConfiguration> logger,
        EnterpriseAuthService authService,
        ReynoldsPersonaService personaService)
    {
        _logger = logger;
        _authService = authService;
        _personaService = personaService;
    }

    /// <summary>
    /// Initialize Reynolds MCP Server with enterprise capabilities
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogInformation("🎭 Reynolds MCP Server initializing with supernatural intelligence...");
        
        // Initialize enterprise authentication
        await _authService.InitializeAsync();
        
        // Initialize Reynolds persona service
        await _personaService.InitializeAsync();
        
        _logger.LogInformation("🚀 Reynolds MCP Server ready - 17 tools loaded with Maximum Effort™ precision");
        _logger.LogInformation("📊 Tool Discovery: Automatic assembly scanning enabled");
        _logger.LogInformation("🔧 Architecture: Official MCP SDK 0.2.0-preview.3");
        _logger.LogInformation("🎯 Organization: {TargetOrganization}", "dynamicstms365");
        
        await LogToolInventoryAsync();
    }

    /// <summary>
    /// Validate enterprise authentication for tool execution
    /// </summary>
    public async Task<bool> ValidateAuthenticationAsync()
    {
        return await _authService.ValidateCurrentContextAsync();
    }

    /// <summary>
    /// Apply Reynolds persona enhancement to tool responses
    /// </summary>
    public async Task<object> EnhanceResponseAsync(object result, string toolName)
    {
        return await _personaService.EnhanceResponseAsync(result, toolName);
    }

    /// <summary>
    /// Log comprehensive tool inventory for operational visibility
    /// </summary>
    private async Task LogToolInventoryAsync()
    {
        await Task.CompletedTask; // Satisfy async requirement
        
        _logger.LogInformation("🔧 Reynolds Tool Inventory:");
        
        // GitHub Tools (11)
        _logger.LogInformation("📂 GitHub Integration Tools (11):");
        _logger.LogInformation("   • AddComment - Comment creation with engagement metrics");
        _logger.LogInformation("   • CreateDiscussion - Discussion creation with category management");
        _logger.LogInformation("   • CreateIssue - Issue creation with label and assignee support");
        _logger.LogInformation("   • GetDiscussion - Discussion retrieval with comprehensive context");
        _logger.LogInformation("   • GetIssue - Issue retrieval with project management insights");
        _logger.LogInformation("   • OrganizationDiscussions - Organization-wide discussion analysis");
        _logger.LogInformation("   • OrganizationIssues - Organization-wide issue analysis");
        _logger.LogInformation("   • PromptAction - AI-powered prompt processing");
        _logger.LogInformation("   • SearchDiscussions - Discussion search with semantic relevance");
        _logger.LogInformation("   • SearchIssues - Issue search with priority assessment");
        _logger.LogInformation("   • UpdateContent - Content update validation");
        
        // Reynolds Organizational Tools (6)
        _logger.LogInformation("🏢 Reynolds Organizational Intelligence Tools (6):");
        _logger.LogInformation("   • SemanticSearch - GitHub semantic search with Reynolds persona");
        _logger.LogInformation("   • AnalyzeOrgProjects - Organizational project analysis");
        _logger.LogInformation("   • CrossRepoOrchestration - Multi-repository coordination");
        _logger.LogInformation("   • StrategicStakeholderCoordination - Stakeholder coordination");
        _logger.LogInformation("   • OrgProjectHealth - Health assessment with benchmarking");
        _logger.LogInformation("   • OrgDependencyIntelligence - Dependency mapping with risk analysis");
        
        _logger.LogInformation("🎭 All tools enhanced with Reynolds supernatural intelligence");
        _logger.LogInformation("⚡ Maximum Effort™ coordination protocols active");
    }

    /// <summary>
    /// Get server information for MCP client initialization
    /// </summary>
    public McpServerInfo GetServerInfo()
    {
        return new McpServerInfo
        {
            Name = "reynolds-mcp-server",
            Version = "2.0.0-sdk",
            Description = "Reynolds Enterprise MCP Server - Official SDK Implementation with supernatural intelligence and Maximum Effort™ coordination"
        };
    }

    /// <summary>
    /// Get server capabilities for MCP client initialization
    /// </summary>
    public McpServerCapabilities GetServerCapabilities()
    {
        return new McpServerCapabilities
        {
            Tools = new McpToolsCapability
            {
                ListChanged = true
            },
            Resources = new McpResourcesCapability
            {
                Subscribe = true,
                ListChanged = true
            },
            Prompts = new McpPromptsCapability
            {
                ListChanged = true
            },
            Logging = new McpLoggingCapability()
        };
    }
}

/// <summary>
/// MCP Server Information for client initialization
/// </summary>
public class McpServerInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
}

/// <summary>
/// MCP Server Capabilities for client initialization
/// </summary>
public class McpServerCapabilities
{
    public McpToolsCapability Tools { get; set; } = new();
    public McpResourcesCapability Resources { get; set; } = new();
    public McpPromptsCapability Prompts { get; set; } = new();
    public McpLoggingCapability Logging { get; set; } = new();
}

/// <summary>
/// MCP Tools Capability Definition
/// </summary>
public class McpToolsCapability
{
    public bool ListChanged { get; set; } = true;
}

/// <summary>
/// MCP Resources Capability Definition
/// </summary>
public class McpResourcesCapability
{
    public bool Subscribe { get; set; } = true;
    public bool ListChanged { get; set; } = true;
}

/// <summary>
/// MCP Prompts Capability Definition
/// </summary>
public class McpPromptsCapability
{
    public bool ListChanged { get; set; } = true;
}

/// <summary>
/// MCP Logging Capability Definition
/// </summary>
public class McpLoggingCapability
{
    // Logging capability configuration
}