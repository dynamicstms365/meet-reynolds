using CopilotAgent.Agents;
using CopilotAgent.Services;
using CopilotAgent.Skills;

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

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();