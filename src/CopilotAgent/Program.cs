using CopilotAgent.Agents;
using CopilotAgent.Services;
using CopilotAgent.Skills;
using CopilotAgent.Middleware;
using CopilotAgent.Startup;
using CopilotAgent.MCP;
using CopilotAgent.Bot;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol;

// Reynolds: Trigger streamlined deployment for MCP protocol testing
// CI Build Trigger: Test compilation fixes applied for CS7036/CS1998 errors
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Add MCP SDK services - Reynolds Enterprise Integration (Preview 0.2.0-preview.3)
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

// Register Reynolds support services
builder.Services.AddScoped<EnterpriseAuthService>();
builder.Services.AddScoped<ReynoldsPersonaService>();

// Add HTTP context accessor for enterprise authentication
builder.Services.AddHttpContextAccessor();

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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Skip HTTPS redirection in production container environment
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

// Configure MCP Server middleware - required for HTTP transport endpoints
app.UseMcp();

// Add webhook logging middleware before webhook processing
app.UseWebhookLogging();

// Add signature validation failure logging middleware
app.UseSignatureValidationLogging();

// Map non-MCP controllers only (MCP endpoints handled by SDK)
app.MapControllers();

// Configure Reynolds Teams integration
if (ReynoldsTeamsConfigurationValidator.IsTeamsIntegrationEnabled(builder.Configuration))
{
    app.UseReynoldsTeamsIntegration();
}

// Map GitHub webhook endpoint using Octokit.Webhooks.AspNetCore
// This replaces the custom webhook controller endpoint
var webhookSecret = builder.Configuration["NGL_DEVOPS_WEBHOOK_SECRET"] ??
                   System.Environment.GetEnvironmentVariable("NGL_DEVOPS_WEBHOOK_SECRET");

// Add error handling for webhook setup
if (string.IsNullOrEmpty(webhookSecret))
{
    app.Logger.LogWarning("Webhook secret not configured - webhook endpoint will reject all requests");
    
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

app.Run();

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