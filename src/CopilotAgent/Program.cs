using CopilotAgent.Agents;
using CopilotAgent.Services;
using CopilotAgent.Skills;
using CopilotAgent.Middleware;
using CopilotAgent.Startup;
using CopilotAgent.MCP;
using CopilotAgent.Bot;
using CopilotAgent.Configuration;
using CopilotAgent.Telemetry;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol;
using Serilog;
using Serilog.Events;
using System.Diagnostics;

// Reynolds: Maximum Effort™ structured logging with Seq integration
// Configure Serilog for supernatural visibility into Chris Taylor communication system
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Seq(
        serverUrl: System.Environment.GetEnvironmentVariable("SEQ_SERVER_URL") ?? "https://seq-logger.salmonisland-520555ec.eastus.azurecontainerapps.io",
        apiKey: System.Environment.GetEnvironmentVariable("SEQ_API_KEY"),
        restrictedToMinimumLevel: LogEventLevel.Debug)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Reynolds: Replace default logging with Serilog
builder.Host.UseSerilog();

try
{
    Log.Information("🎭 Reynolds: Starting Copilot Agent with Maximum Effort™ structured logging");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Reynolds: .NET 9.0 OpenAPI configuration with Maximum Effort™ MCP integration
builder.Services.AddReynoldsOpenApiWithMcpSupport();

// Reynolds: Application Insights Telemetry with Maximum Effort™ - Supernatural Visibility
var appInsightsConnectionString = System.Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("ApplicationInsights")
    ?? System.Environment.GetEnvironmentVariable("APPINSIGHTS_CONNECTION_STRING");

if (string.IsNullOrEmpty(appInsightsConnectionString))
{
    Log.Warning("🎭 Reynolds: Application Insights connection string not found - telemetry will be limited to SEQ only");
    Log.Information("🔧 To enable Application Insights, set APPLICATIONINSIGHTS_CONNECTION_STRING environment variable");
    Log.Information("🔧 Expected format: InstrumentationKey=your-key;IngestionEndpoint=https://region.in.applicationinsights.azure.com/");
}
else
{
    Log.Information("🎭 Reynolds: Configuring Application Insights with supernatural telemetry capabilities");
    Log.Information("🔧 Application Insights endpoint: {Endpoint}",
        appInsightsConnectionString.Contains("IngestionEndpoint=")
            ? appInsightsConnectionString.Split("IngestionEndpoint=")[1].Split(";")[0]
            : "Classic endpoint");
}

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = appInsightsConnectionString;
    options.EnableAdaptiveSampling = true;
    options.EnableQuickPulseMetricStream = true;
    options.EnableDebugLogger = false; // Use Serilog instead
    options.EnableHeartbeat = true;
    options.AddAutoCollectedMetricExtractor = true;
    options.EnableActiveTelemetryConfigurationSetup = true;
    options.EnableAuthenticationTrackingJavaScript = true;
    options.EnableDependencyTrackingTelemetryModule = true;
    options.EnableEventCounterCollectionModule = true;
    options.EnablePerformanceCounterCollectionModule = true;
    options.EnableAppServicesHeartbeatTelemetryModule = true;
    options.EnableAzureInstanceMetadataTelemetryModule = true;
    options.EnableDiagnosticsTelemetryModule = true;
});

// Reynolds: Configure Application Insights telemetry processors for enhanced tracking
builder.Services.ConfigureTelemetryModule<Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryModule>((module, o) =>
{
    module.AuthenticationApiKey = System.Environment.GetEnvironmentVariable("APPINSIGHTS_QUICKPULSE_API_KEY");
});

// Reynolds: Add custom telemetry initializers for enterprise context
builder.Services.AddSingleton<Microsoft.ApplicationInsights.Extensibility.ITelemetryInitializer, ReynoldsTelemetryInitializer>();

// Reynolds: Configure Application Insights for Maximum Effort™ telemetry
builder.Services.AddSingleton<ReynoldsTelemetryInitializer>();
builder.Services.ConfigureOptions<ReynoldsTelemetryConfiguration>();

Log.Information("🎭 Reynolds: Application Insights configured with supernatural telemetry tracking");

// Register Copilot Agent services
builder.Services.AddScoped<IPowerPlatformAgent, PowerPlatformAgent>();
builder.Services.AddScoped<IEnvironmentManager, EnvironmentManager>();
builder.Services.AddScoped<ICliExecutor, CliExecutor>();
builder.Services.AddScoped<ICodeGenerator, CodeGenerator>();
builder.Services.AddScoped<IKnowledgeRetriever, KnowledgeRetriever>();
builder.Services.AddScoped<IPacCliValidator, PacCliValidator>();
builder.Services.AddScoped<IM365CliValidator, M365CliValidator>();

// Register enhanced agent framework services
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IIntentRecognitionService, IntentRecognitionService>();
builder.Services.AddScoped<IRetryService, RetryService>();
builder.Services.AddSingleton<ITelemetryService, TelemetryService>();
builder.Services.AddScoped<IHealthMonitoringService, HealthMonitoringService>();

// Register CLI monitoring service
builder.Services.AddScoped<ICliMonitoringService, CliMonitoringService>();
// Register CLI services
builder.Services.AddScoped<IPacCliService, PacCliService>();
builder.Services.AddScoped<IM365CliService, M365CliService>();

// Register Reynolds startup diagnostics service for Maximum Effort™ configuration validation
builder.Services.AddScoped<IStartupDiagnosticsService, StartupDiagnosticsService>();

// Register GitHub integration services
builder.Services.AddHttpClient<IGitHubAppAuthService, GitHubAppAuthService>();
builder.Services.AddScoped<IGitHubAppAuthService, GitHubAppAuthService>();
builder.Services.AddScoped<ISecurityAuditService, SecurityAuditService>();
builder.Services.AddScoped<IGitHubWebhookValidator, GitHubWebhookValidator>();

// Register GitHub discussions and issues services
builder.Services.AddHttpClient<IGitHubDiscussionsService, GitHubDiscussionsService>();
builder.Services.AddHttpClient<IGitHubIssuesService, GitHubIssuesService>();
builder.Services.AddHttpClient<IGitHubSemanticSearchService, GitHubSemanticSearchService>();
builder.Services.AddHttpClient<IGitHubIssuePRSynchronizationService, GitHubIssuePRSynchronizationService>();

// Register GitHub workflow orchestrator
builder.Services.AddScoped<IGitHubWorkflowOrchestrator, GitHubWorkflowOrchestrator>();
// Register Octokit webhook processor as scoped to fix lifetime mismatch with IGitHubWorkflowOrchestrator
builder.Services.AddScoped<WebhookEventProcessor, OctokitWebhookEventProcessor>();

// Register Reynolds User Introduction and Cross-Platform Mapping Services (Issue #85 + #86)
builder.Services.AddMemoryCache(); // Required for caching services
builder.Services.AddScoped<IUserMappingService, UserMappingService>();

// Register Reynolds' enhanced introduction orchestration services - Maximum Effort™
builder.Services.AddScoped<IMicrosoftGraphUserService, MicrosoftGraphUserService>();
builder.Services.AddScoped<IGitHubOrganizationService, GitHubOrganizationService>();
builder.Services.AddScoped<IReynoldsTeamsChatService, ReynoldsTeamsChatService>();
builder.Services.AddScoped<IIntroductionOrchestrationService, IntroductionOrchestrationService>();

// Register Reynolds M365 CLI Service for enhanced communication delivery
builder.Services.AddScoped<IReynoldsM365CliService, ReynoldsM365CliService>();

// Legacy service compatibility - using our new services as implementations
// builder.Services.AddScoped<IGraphUserLookupService, GraphUserLookupService>();
// builder.Services.AddScoped<IGitHubOrgMemberService, GitHubOrgMemberService>();
// builder.Services.AddScoped<IIntroductionOrchestrator, IntroductionOrchestrator>();

// Register GitHub Models Integration Services (Issue #72)
builder.Services.AddHttpClient<IGitHubModelsService, GitHubModelsService>();
builder.Services.AddScoped<IGitHubModelsService, GitHubModelsService>();
builder.Services.AddScoped<IGitHubModelsOrchestrator, GitHubModelsOrchestrator>();

// Register Cross-Platform Event Routing Services (Issue #73)
builder.Services.AddScoped<ICrossPlatformEventRouter, CrossPlatformEventRouter>();
builder.Services.AddScoped<IEventClassificationService, EventClassificationService>();
// builder.Services.AddScoped<IAzureEventProcessor, AzureEventProcessor>(); // Temporarily disabled
builder.Services.AddScoped<IEventRoutingMetrics, EventRoutingMetrics>();
builder.Services.AddSingleton<EventRoutingMetrics>();

// Add HTTP context accessor BEFORE MCP registration (Issue #365 fix)
builder.Services.AddHttpContextAccessor();

// Reynolds: APIM-First MCP Architecture - Julia's conversion approach
// Custom MCP server kept for fallback/development, APIM handles production MCP conversion
builder.Services.AddReynoldsMcpServer();

// Initialize Reynolds MCP Server configuration (development/fallback mode)
builder.Services.AddScoped<ReynoldsMcpServerConfiguration>();

// Register Reynolds Teams integration services
if (ReynoldsTeamsConfigurationValidator.IsTeamsIntegrationEnabled(builder.Configuration))
{
    builder.Services.AddReynoldsTeamsServices(builder.Configuration);
}
else
{
    // Register stub implementation when Teams integration is disabled
    builder.Services.AddScoped<IReynoldsTeamsService, StubReynoldsTeamsService>();
}

var app = builder.Build();

// Reynolds: Execute startup diagnostics with Maximum Effort™ configuration validation
try
{
    using var scope = app.Services.CreateScope();
    var diagnosticsService = scope.ServiceProvider.GetRequiredService<IStartupDiagnosticsService>();
    
    // Log system information
    await diagnosticsService.LogSystemInformationAsync();
    
    // Validate all configurations
    var diagnosticsResult = await diagnosticsService.ValidateConfigurationAsync();
    
    if (diagnosticsResult.HasCriticalIssues)
    {
        Log.Warning("⚠️ Reynolds: Critical configuration issues detected - application may not function optimally");
        Log.Warning("🔧 Reynolds: Review the configuration recommendations above for Maximum Effort™ operation");
    }
    else
    {
        Log.Information("✅ Reynolds: All critical configurations validated - ready for supernatural coordination!");
    }
}
catch (Exception ex)
{
    Log.Warning(ex, "⚠️ Reynolds: Startup diagnostics encountered minor turbulence - continuing with caution");
}

// Configure HTTP request pipeline - Reynolds: .NET 9.0 OpenAPI with Maximum Effort™ APIM integration

// Reynolds: Add static files middleware for SwaggerUI assets - CRITICAL for container deployment
app.UseStaticFiles();

// Always enable OpenAPI and Swagger in container deployments for APIM integration
app.MapOpenApi("/api-docs/v1/openapi.json");

// Configure SwaggerUI for all environments - container apps need this for debugging
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/api-docs/v1/openapi.json", "Reynolds Communication & Orchestration API v1.0");
    c.RoutePrefix = "api-docs";
    c.DocumentTitle = $"Reynolds API Documentation - {app.Environment.EnvironmentName} (.NET 9.0)";
    c.EnableDeepLinking();
    c.EnableValidator();
    c.EnableTryItOutByDefault();
    
    // Reynolds: Skip custom CSS injection in container environments to prevent 404 errors
    if (app.Environment.IsDevelopment())
    {
        // Only inject custom CSS in development if file exists
        var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        var cssPath = Path.Combine(webRoot, "swagger-ui", "custom.css");
        if (File.Exists(cssPath))
        {
            c.InjectStylesheet("/swagger-ui/custom.css");
        }
    }
});

// Add root redirect to Swagger for container debugging
app.MapGet("/", () => Results.Redirect("/api-docs"))
   .WithName("RootRedirect")
   .WithSummary("Redirect to API documentation")
   .ExcludeFromDescription();

// Skip HTTPS redirection in production container environment
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

// Reynolds: Add supernatural telemetry middleware for Maximum Effort™ request tracking
app.UseMiddleware<ReynoldsTelemetryMiddleware>();
app.Logger.LogInformation("🎭 Reynolds: Supernatural telemetry middleware activated with Maximum Effort™");

// Reynolds MCP Server endpoints are automatically configured via AddReynoldsMcpServer() with WithHttpTransport()
// Default endpoints: /mcp/capabilities, /mcp/sse, /mcp/tools/{toolName}
// 17 Reynolds tools loaded with Maximum Effort™ precision
app.Logger.LogInformation("🎭 Reynolds MCP Server configured with HTTP transport - 17 tools available at /mcp/*");
app.Logger.LogInformation("🔧 MCP Endpoints: /mcp/capabilities, /mcp/sse, /mcp/tools/[toolName]");
app.Logger.LogInformation("⚡ Maximum Effort™ coordination protocols active");

// Add webhook logging middleware before webhook processing
app.UseWebhookLogging();

// Add signature validation failure logging middleware
app.UseSignatureValidationLogging();

// Map non-MCP controllers only (MCP endpoints handled by SDK)
app.MapControllers();

// Map Reynolds MCP Server HTTP endpoints with proper routing
app.MapMcp("/mcp");
app.Logger.LogInformation("🎭 Reynolds MCP Server HTTP endpoints mapped successfully at /mcp/*");

// Standard MCP Server - no custom initialization required
app.Logger.LogInformation("🎭 Reynolds MCP Server configured with standard SDK pattern");

// Configure Reynolds Teams integration
if (ReynoldsTeamsConfigurationValidator.IsTeamsIntegrationEnabled(builder.Configuration))
{
    app.UseReynoldsTeamsIntegration();
}

// Map GitHub webhook endpoint using Octokit.Webhooks.AspNetCore
// This replaces the custom webhook controller endpoint
// Enhanced webhook secret resolution with detailed logging
var webhookSecret = builder.Configuration["NGL_DEVOPS_WEBHOOK_SECRET"] ??
                   System.Environment.GetEnvironmentVariable("NGL_DEVOPS_WEBHOOK_SECRET");

app.Logger.LogInformation("🔍 Webhook secret configuration check:");
app.Logger.LogInformation("  - Configuration[NGL_DEVOPS_WEBHOOK_SECRET]: {HasConfigSecret}",
    !string.IsNullOrEmpty(builder.Configuration["NGL_DEVOPS_WEBHOOK_SECRET"]) ? "SET" : "NOT_SET");
app.Logger.LogInformation("  - Environment[NGL_DEVOPS_WEBHOOK_SECRET]: {HasEnvSecret}",
    !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("NGL_DEVOPS_WEBHOOK_SECRET")) ? "SET" : "NOT_SET");
app.Logger.LogInformation("  - Final webhook secret: {HasFinalSecret}",
    !string.IsNullOrEmpty(webhookSecret) ? "AVAILABLE" : "MISSING");

// Add error handling for webhook setup
if (string.IsNullOrEmpty(webhookSecret))
{
    app.Logger.LogError("❌ WEBHOOK_SECRET_NOT_CONFIGURED: No webhook secret found in configuration or environment");
    app.Logger.LogError("🔧 Expected sources: Configuration['NGL_DEVOPS_WEBHOOK_SECRET'] or Environment['NGL_DEVOPS_WEBHOOK_SECRET']");
    app.Logger.LogWarning("⚠️  Webhook endpoint will reject all requests with 401 Unauthorized");
    
    // Map a fallback endpoint that returns proper 401 when secret is missing
    app.MapPost("/api/github/webhook", async (HttpContext context) =>
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Webhook secret not configured");
    });
}
else
{
    try
    {
        app.MapGitHubWebhooks("/api/github/webhook", webhookSecret);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to configure GitHub webhooks - setting up fallback endpoint");
        
        // Fallback endpoint that returns proper error codes
        app.MapPost("/api/github/webhook", async (HttpContext context) =>
        {
            var signature = context.Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            context.Response.StatusCode = string.IsNullOrEmpty(signature) ? 400 : 401;
            await context.Response.WriteAsync("Webhook processing temporarily unavailable");
        });
    }
}

    Log.Information("🚀 Reynolds: Copilot Agent application starting with Maximum Effort™");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💥 Reynolds: Application terminated unexpectedly");
}
finally
{
    Log.Information("🎭 Reynolds: Shutting down with supernatural grace");
    await Log.CloseAndFlushAsync();
}

// Stub implementation for when Teams integration is disabled
public class StubReynoldsTeamsService : IReynoldsTeamsService
{
    private readonly ILogger<StubReynoldsTeamsService> _logger;

    public StubReynoldsTeamsService(ILogger<StubReynoldsTeamsService> logger)
    {
        _logger = logger;
    }

    public Task SendOrganizationalUpdateAsync(string userId, string update)
    {
        _logger.LogDebug("Teams integration disabled - would have sent organizational update to {UserId}: {Update}", userId, update);
        return Task.CompletedTask;
    }

    public Task NotifyAboutScopeCreepAsync(string userId, string prNumber, string repository)
    {
        _logger.LogDebug("Teams integration disabled - would have notified {UserId} about scope creep in PR {PrNumber} of {Repository}", userId, prNumber, repository);
        return Task.CompletedTask;
    }

    public Task CreateChatForCoordinationAsync(string userPrincipalName, string coordinationContext)
    {
        _logger.LogDebug("Teams integration disabled - would have created coordination chat for {UserPrincipalName}: {CoordinationContext}", userPrincipalName, coordinationContext);
        return Task.CompletedTask;
    }
}