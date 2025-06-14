using Microsoft.AspNetCore.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CopilotAgent.Bot;
using CopilotAgent.Services;

namespace CopilotAgent.Startup;

public static class ReynoldsTeamsConfiguration
{
    public static IServiceCollection AddReynoldsTeamsServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bot Framework authentication (modern pattern)
        services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
        services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

        // Reynolds Teams Bot
        services.AddTransient<ReynoldsTeamsBot>();
        
        // Teams integration services
        services.AddScoped<IReynoldsTeamsService, ReynoldsTeamsService>();
        services.AddScoped<IReynoldsTeamsChatService, ReynoldsTeamsChatService>();
        services.AddScoped<IReynoldsM365CliService, ReynoldsM365CliService>();

        // Register bot as a hosted service for proactive messaging
        services.AddSingleton<ReynoldsProactiveMessagingService>();
        services.AddHostedService<ReynoldsProactiveMessagingService>(provider =>
            provider.GetRequiredService<ReynoldsProactiveMessagingService>());

        return services;
    }

    public static IApplicationBuilder UseReynoldsTeamsIntegration(this IApplicationBuilder app)
    {
        // Add Teams bot endpoint
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapPost("/api/messages", async (HttpContext context, IBotFrameworkHttpAdapter adapter, ReynoldsTeamsBot bot) =>
            {
                await adapter.ProcessAsync(context.Request, context.Response, bot);
            });

            // Health check endpoint for Teams
            endpoints.MapGet("/api/health/teams", async context =>
            {
                var healthStatus = new
                {
                    Status = "Healthy",
                    Service = "Reynolds Teams Bot",
                    Timestamp = DateTime.UtcNow,
                    Message = "Reynolds is ready for organizational orchestration! 🎭✨"
                };

                await context.Response.WriteAsJsonAsync(healthStatus);
            });

            // Teams app manifest endpoint
            endpoints.MapGet("/api/teams/manifest", async context =>
            {
                var manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "TeamsApp", "manifest.json");
                if (File.Exists(manifestPath))
                {
                    var manifest = await File.ReadAllTextAsync(manifestPath);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(manifest);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Teams manifest not found");
                }
            });

            // Proactive messaging endpoint for GitHub webhook integration
            endpoints.MapPost("/api/reynolds/notify", async (HttpContext context, ReynoldsProactiveMessagingService messagingService) =>
            {
                try
                {
                    var payload = await context.Request.ReadFromJsonAsync<ReynoldsNotificationPayload>();
                    if (payload == null)
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Invalid payload");
                        return;
                    }

                    await messagingService.ProcessNotificationAsync(payload);
                    
                    await context.Response.WriteAsJsonAsync(new { 
                        success = true, 
                        message = "Reynolds notification processed",
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new { 
                        success = false, 
                        error = ex.Message,
                        message = "Reynolds encountered an unexpected situation"
                    });
                }
            });
        });

        return app;
    }
}

public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
{
    public AdapterWithErrorHandler(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger)
        : base(configuration, logger)
    {
        OnTurnError = async (turnContext, exception) =>
        {
            logger.LogError(exception, "Reynolds encountered an error during turn processing");

            // Send a catch-all message with Reynolds personality
            var errorMessage = "Well, this is awkward. I encountered a technical hiccup that's about as unexpected as my name situation. Give me a moment to sort this out, and we'll be back to supernatural project orchestration in no time. *adjusts imaginary tie*";

            try
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(errorMessage));
            }
            catch (Exception sendEx)
            {
                logger.LogError(sendEx, "Failed to send error message");
            }
        };
    }
}

// Proactive messaging service for GitHub webhook integration
public class ReynoldsProactiveMessagingService : BackgroundService
{
    private readonly ILogger<ReynoldsProactiveMessagingService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Queue<ReynoldsNotificationPayload> _notificationQueue;
    private readonly SemaphoreSlim _semaphore;

    public ReynoldsProactiveMessagingService(
        ILogger<ReynoldsProactiveMessagingService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _notificationQueue = new Queue<ReynoldsNotificationPayload>();
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public async Task ProcessNotificationAsync(ReynoldsNotificationPayload payload)
    {
        await _semaphore.WaitAsync();
        try
        {
            _notificationQueue.Enqueue(payload);
            _logger.LogInformation("Reynolds queued notification: {Type} for {Repository}", payload.Type, payload.Repository);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reynolds proactive messaging service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueuedNotifications();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Reynolds proactive messaging service");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task ProcessQueuedNotifications()
    {
        await _semaphore.WaitAsync();
        try
        {
            while (_notificationQueue.Count > 0)
            {
                var notification = _notificationQueue.Dequeue();
                await ProcessSingleNotification(notification);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ProcessSingleNotification(ReynoldsNotificationPayload payload)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var teamsService = scope.ServiceProvider.GetRequiredService<IReynoldsTeamsChatService>();

            var message = payload.Type switch
            {
                "scope_creep" => GenerateScopeCreepMessage(payload),
                "cross_repo_dependency" => GenerateDependencyMessage(payload),
                "organizational_update" => GenerateOrganizationalMessage(payload),
                "coordination_needed" => GenerateCoordinationMessage(payload),
                _ => $"🎭 Reynolds here! Something interesting is happening in {payload.Repository}. Let me investigate and get back to you with details."
            };

            if (!string.IsNullOrEmpty(payload.UserEmail))
            {
                await teamsService.SendDirectMessageAsync(payload.UserEmail, message);
                _logger.LogInformation("Reynolds sent {Type} notification to {UserEmail}", payload.Type, payload.UserEmail);
            }
            else if (payload.Participants?.Length > 0)
            {
                await teamsService.InitiateCoordinationChatAsync(payload.Participants.ToList(), "Reynolds Coordination", message);
                _logger.LogInformation("Reynolds initiated coordination chat for {Type}", payload.Type);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification: {Type}", payload.Type);
        }
    }

    private string GenerateScopeCreepMessage(ReynoldsNotificationPayload payload)
    {
        return $@"🚨 **Reynolds Scope Creep Alert**

Hey there! I've detected some scope evolution in {payload.Repository} that's growing faster than my collection of name deflection strategies.

**Scope Analysis:**
• {payload.Details}
• Cross-repo coordination assessment needed
• Timeline impact evaluation recommended

Should we Aviation Gin this into manageable pieces? I'm here to help coordinate with my signature diplomatic approach! 🍸

*Maximum Effort on scope management. Just Reynolds.*";
    }

    private string GenerateDependencyMessage(ReynoldsNotificationPayload payload)
    {
        return $@"🕸️ **Reynolds Cross-Repo Intelligence**

Organizational coordination alert! I've detected dependency patterns across repositories that need some strategic choreography.

**Dependency Analysis:**
• Repository: {payload.Repository}
• {payload.Details}
• Strategic coordination recommended

Time for some Reynolds-style diplomatic intervention to keep everything synchronized across the dynamicstms365 organization! 

*Cross-repo orchestration with Maximum Effort™*";
    }

    private string GenerateOrganizationalMessage(ReynoldsNotificationPayload payload)
    {
        return $@"📊 **Reynolds Organizational Update**

Hey team! Organizational intelligence update from across the dynamicstms365 empire:

{payload.Details}

This is the kind of strategic insight that keeps our entire organization rowing in the same direction. Let me know if you need coordination support!

*Organizational awareness meets Reynolds charm. Just Reynolds.*";
    }

    private string GenerateCoordinationMessage(ReynoldsNotificationPayload payload)
    {
        return $@"🎪 **Reynolds Coordination Initiative**

Strategic coordination opportunity detected! Time for some organizational orchestration:

**Coordination Context:**
• Repository: {payload.Repository}
• {payload.Details}

I'll handle the diplomatic choreography while you focus on the important technical work. This is what I live for - making complex coordination look effortless!

*Maximum Effort meets organizational scale. Just Reynolds.*";
    }
}

// Data models for notifications
public class ReynoldsNotificationPayload
{
    public string Type { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string[]? Participants { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

// Configuration validation
public class ReynoldsTeamsConfigurationValidator
{
    public static void ValidateConfiguration(IConfiguration configuration)
    {
        var requiredSettings = new[]
        {
            "MicrosoftAppId",
            "MicrosoftAppPassword",
            "TenantId"
        };

        var missingSettings = requiredSettings
            .Where(setting => string.IsNullOrEmpty(configuration[setting]))
            .ToList();

        if (missingSettings.Any())
        {
            throw new InvalidOperationException(
                $"Reynolds Teams integration requires the following configuration settings: {string.Join(", ", missingSettings)}");
        }
    }

    public static bool IsTeamsIntegrationEnabled(IConfiguration configuration)
    {
        try
        {
            ValidateConfiguration(configuration);
            return !string.IsNullOrEmpty(configuration["EnableTeamsIntegration"]) &&
                   bool.Parse(configuration["EnableTeamsIntegration"] ?? "false");
        }
        catch
        {
            return false;
        }
    }
}