using Shared.Models;
using System.Text.Json;

namespace CopilotAgent.Services;

public class OnboardingService : IOnboardingService
{
    private readonly ILogger<OnboardingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ITelemetryService _telemetryService;
    private readonly Dictionary<string, OnboardingProgress> _progressCache;

    public OnboardingService(
        ILogger<OnboardingService> logger,
        IConfiguration configuration,
        ITelemetryService telemetryService)
    {
        _logger = logger;
        _configuration = configuration;
        _telemetryService = telemetryService;
        _progressCache = new Dictionary<string, OnboardingProgress>();
    }

    public async Task<OnboardingStep[]> GetOnboardingStepsAsync()
    {
        await Task.CompletedTask;

        return new[]
        {
            new OnboardingStep
            {
                Id = "welcome",
                Title = "Welcome to Power Platform Development",
                Description = "Get started with your development environment setup",
                Commands = Array.Empty<string>(),
                Links = new[] 
                { 
                    "https://docs.microsoft.com/power-platform/",
                    "https://github.com/dynamicstms365/copilot-powerplatform"
                },
                Interactive = true,
                Order = 1
            },
            new OnboardingStep
            {
                Id = "setup-tools",
                Title = "Install Required Tools",
                Description = "Install Power Platform CLI and Microsoft 365 CLI tools",
                Commands = new[]
                {
                    "dotnet tool install --global Microsoft.PowerApps.CLI.Tool",
                    "npm install -g @pnp/cli-microsoft365"
                },
                Links = new[]
                {
                    "https://docs.microsoft.com/power-platform/developer/cli/introduction",
                    "https://pnp.github.io/cli-microsoft365/"
                },
                Interactive = true,
                Order = 2
            },
            new OnboardingStep
            {
                Id = "validate-installation",
                Title = "Validate Tool Installation",
                Description = "Verify that all required tools are properly installed",
                Commands = new[]
                {
                    "pac --version",
                    "m365 --version",
                    "dotnet --version"
                },
                Links = Array.Empty<string>(),
                Interactive = true,
                Order = 3
            },
            new OnboardingStep
            {
                Id = "explore-codebase",
                Title = "Explore the Codebase",
                Description = "Learn about the project structure and key components",
                Commands = new[]
                {
                    "ls -la",
                    "cat README.md",
                    "find . -name '*.cs' | head -10"
                },
                Links = new[]
                {
                    "./docs/README.md",
                    "./STRATEGIC_IMPLEMENTATION_PLAN.md"
                },
                Interactive = false,
                Order = 4
            },
            new OnboardingStep
            {
                Id = "build-and-test",
                Title = "Build and Test the Project",
                Description = "Run the build process and execute tests to ensure everything works",
                Commands = new[]
                {
                    "cd src && dotnet restore",
                    "cd src && dotnet build",
                    "cd src && dotnet test"
                },
                Links = Array.Empty<string>(),
                Interactive = true,
                Order = 5
            },
            new OnboardingStep
            {
                Id = "setup-environment",
                Title = "Configure Development Environment",
                Description = "Set up your local development environment with required configurations",
                Commands = new[]
                {
                    "cp .env.example .env",
                    "code .vscode/settings.json"
                },
                Links = new[]
                {
                    "./docs/github-copilot/dev-environment-comprehensive.md"
                },
                Interactive = true,
                Order = 6
            },
            new OnboardingStep
            {
                Id = "first-contribution",
                Title = "Ready for Development",
                Description = "You're all set! Start exploring the codebase and making contributions",
                Commands = Array.Empty<string>(),
                Links = new[]
                {
                    "./STRATEGIC_IMPLEMENTATION_PLAN.md",
                    "./docs/github-copilot/agent-development.md"
                },
                Interactive = false,
                Order = 7
            }
        };
    }

    public async Task<OnboardingProgress> StartOnboardingAsync(string userId, string codespaceId)
    {
        try
        {
            _logger.LogInformation("Starting onboarding for user: {UserId} in Codespace: {CodespaceId}", userId, codespaceId);

            var progress = new OnboardingProgress
            {
                UserId = userId,
                CodespaceId = codespaceId,
                CurrentStep = "welcome",
                StartedAt = DateTime.UtcNow,
                CompletedSteps = Array.Empty<string>(),
                StepData = new Dictionary<string, object>()
            };

            var cacheKey = $"{userId}:{codespaceId}";
            _progressCache[cacheKey] = progress;

            await Task.CompletedTask;
            
            _logger.LogInformation("Onboarding started for user: {UserId}", userId);
            return progress;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start onboarding for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<OnboardingProgress> UpdateProgressAsync(string userId, string stepId, Dictionary<string, object> stepData)
    {
        try
        {
            _logger.LogInformation("Updating onboarding progress for user: {UserId}, step: {StepId}", userId, stepId);

            var cacheKey = _progressCache.Keys.FirstOrDefault(k => k.StartsWith($"{userId}:"));
            if (cacheKey == null)
            {
                throw new InvalidOperationException($"No onboarding session found for user: {userId}");
            }

            var progress = _progressCache[cacheKey];
            
            // Mark current step as completed if not already completed
            if (!progress.CompletedSteps.Contains(progress.CurrentStep))
            {
                var completedSteps = new List<string>(progress.CompletedSteps) { progress.CurrentStep };
                progress.CompletedSteps = completedSteps.ToArray();
            }

            // Update step data
            foreach (var kvp in stepData)
            {
                progress.StepData[kvp.Key] = kvp.Value;
            }

            // Move to next step
            var steps = await GetOnboardingStepsAsync();
            var currentStepIndex = Array.FindIndex(steps, s => s.Id == stepId);
            
            if (currentStepIndex < steps.Length - 1)
            {
                progress.CurrentStep = steps[currentStepIndex + 1].Id;
            }
            else
            {
                progress.CompletedAt = DateTime.UtcNow;
                progress.CurrentStep = "completed";
            }

            _progressCache[cacheKey] = progress;

            await Task.CompletedTask;

            _logger.LogInformation("Onboarding progress updated for user: {UserId}", userId);
            return progress;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update onboarding progress for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> CompleteOnboardingAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Completing onboarding for user: {UserId}", userId);

            var cacheKey = _progressCache.Keys.FirstOrDefault(k => k.StartsWith($"{userId}:"));
            if (cacheKey == null)
            {
                return false;
            }

            var progress = _progressCache[cacheKey];
            progress.CompletedAt = DateTime.UtcNow;
            progress.CurrentStep = "completed";

            // Mark all steps as completed
            var steps = await GetOnboardingStepsAsync();
            progress.CompletedSteps = steps.Select(s => s.Id).ToArray();

            _progressCache[cacheKey] = progress;

            _logger.LogInformation("Onboarding completed for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete onboarding for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<string> GenerateWelcomeMessageAsync(string userId, string repositoryName)
    {
        await Task.CompletedTask;

        var welcomeMessage = $@"
🎉 **Welcome to {repositoryName} Development Environment!**

🚀 **Power Platform Copilot Agent**
This Codespace is configured with everything you need to develop with the Reynolds Copilot agent for Microsoft Power Platform.

📋 **Quick Start Guide:**
1. **Environment Setup** - Tools and dependencies are being installed automatically
2. **Explore Documentation** - Check out the comprehensive guides in `/docs`
3. **Build & Test** - Run `cd src && dotnet build && dotnet test` to verify setup
4. **Interactive Learning** - Use the onboarding agent to guide you through development

🛠️ **Available Tools:**
• Power Platform CLI (`pac`)
• Microsoft 365 CLI (`m365`)
• .NET 8 SDK
• Visual Studio Code with recommended extensions

💡 **Need Help?**
• Ask the Reynolds Copilot agent: ""@reynolds help me get started""
• Read the documentation: `./docs/README.md`
• Check the implementation plan: `./STRATEGIC_IMPLEMENTATION_PLAN.md`

✨ **Pro Tip:** This Codespace includes an interactive onboarding experience. 
   Type `reynolds onboard` to start your guided tour!

Happy coding! 🎭
";

        return welcomeMessage;
    }
}