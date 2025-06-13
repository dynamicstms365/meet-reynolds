using CopilotAgent.Agents;
using CopilotAgent.Services;
using CopilotAgent.Skills;
using CopilotAgent.Middleware;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;

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

// Register GitHub workflow orchestrator
builder.Services.AddScoped<IGitHubWorkflowOrchestrator, GitHubWorkflowOrchestrator>();

// Register stakeholder visibility services
builder.Services.AddScoped<IStakeholderVisibilityService, StakeholderVisibilityService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Register Octokit webhook processor (replaces custom webhook controller)
builder.Services.AddSingleton<WebhookEventProcessor, OctokitWebhookEventProcessor>();

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

// Add webhook logging middleware before webhook processing
app.UseWebhookLogging();

// Add signature validation failure logging middleware
app.UseSignatureValidationLogging();

app.MapControllers();

// Map GitHub webhook endpoint using Octokit.Webhooks.AspNetCore
// This replaces the custom webhook controller endpoint
var webhookSecret = builder.Configuration["NGL_DEVOPS_WEBHOOK_SECRET"] ??
                   System.Environment.GetEnvironmentVariable("NGL_DEVOPS_WEBHOOK_SECRET");

app.MapGitHubWebhooks("/api/github/webhook", webhookSecret);

app.Run();