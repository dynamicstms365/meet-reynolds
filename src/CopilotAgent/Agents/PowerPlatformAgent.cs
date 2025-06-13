using Shared.Models;
using CopilotAgent.Services;
using CopilotAgent.Skills;

namespace CopilotAgent.Agents;

public interface IPowerPlatformAgent
{
    Task<AgentResponse> ProcessRequestAsync(AgentRequest request);
}

public class PowerPlatformAgent : IPowerPlatformAgent
{
    private readonly IEnvironmentManager _environmentManager;
    private readonly ICliExecutor _cliExecutor;
    private readonly ICodeGenerator _codeGenerator;
    private readonly IKnowledgeRetriever _knowledgeRetriever;
    private readonly ILogger<PowerPlatformAgent> _logger;
    private readonly IIntentRecognitionService _intentRecognitionService;
    private readonly IRetryService _retryService;
    private readonly ITelemetryService _telemetryService;
    private readonly IConfigurationService _configurationService;
    private readonly ICodespaceManagementService _codespaceManagementService;
    private readonly IOnboardingService _onboardingService;

    public PowerPlatformAgent(
        IEnvironmentManager environmentManager,
        ICliExecutor cliExecutor,
        ICodeGenerator codeGenerator,
        IKnowledgeRetriever knowledgeRetriever,
        ILogger<PowerPlatformAgent> logger,
        IIntentRecognitionService intentRecognitionService,
        IRetryService retryService,
        ITelemetryService telemetryService,
        IConfigurationService configurationService,
        ICodespaceManagementService codespaceManagementService,
        IOnboardingService onboardingService)
    {
        _environmentManager = environmentManager;
        _cliExecutor = cliExecutor;
        _codeGenerator = codeGenerator;
        _knowledgeRetriever = knowledgeRetriever;
        _logger = logger;
        _intentRecognitionService = intentRecognitionService;
        _retryService = retryService;
        _telemetryService = telemetryService;
        _configurationService = configurationService;
        _codespaceManagementService = codespaceManagementService;
        _onboardingService = onboardingService;
    }

    public async Task<AgentResponse> ProcessRequestAsync(AgentRequest request)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;
        var config = _configurationService.GetConfiguration();
        
        try
        {
            _logger.LogInformation("Processing request {RequestId}: {Message}", requestId, request.Message);
            
            // Use enhanced intent recognition with retry mechanism
            var intent = await _retryService.ExecuteWithRetryAsync(async () =>
                await _intentRecognitionService.AnalyzeIntentAsync(request.Message, config));
            
            _logger.LogInformation("Request {RequestId} - Identified intent: {IntentType} (confidence: {Confidence:P2})", 
                requestId, intent.Type, intent.Confidence);

            // Record intent recognition telemetry
            _telemetryService.RecordIntentRecognition(intent.Type, intent.Confidence);

            // Process request with timeout
            var processingTask = intent.Type switch
            {
                IntentType.EnvironmentManagement => HandleEnvironmentRequest(request),
                IntentType.CliExecution => HandleCliRequest(request),
                IntentType.CodeGeneration => HandleCodeRequest(request),
                IntentType.KnowledgeQuery => HandleKnowledgeRequest(request),
                IntentType.CodespaceManagement => HandleCodespaceRequest(request),
                _ => HandleGeneralRequest(request)
            };

            var response = await processingTask.WaitAsync(TimeSpan.FromMilliseconds(config.Processing.RequestTimeoutMs));
            
            // Enhance response with metadata
            response.Data ??= new Dictionary<string, object>();
            response.Data["requestId"] = requestId;
            response.Data["processingTimeMs"] = (DateTime.UtcNow - startTime).TotalMilliseconds;
            response.Data["intentConfidence"] = intent.Confidence;
            response.IntentType = intent.Type;

            // Record telemetry
            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _telemetryService.RecordRequestProcessed(requestId, intent.Type, response.Success, processingTime);

            _logger.LogInformation("Request {RequestId} processed successfully in {ProcessingTime}ms", 
                requestId, processingTime);

            return response;
        }
        catch (TimeoutException)
        {
            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _telemetryService.RecordError("RequestProcessing", new TimeoutException("Request processing timeout"));
            _telemetryService.RecordRequestProcessed(requestId, IntentType.General, false, processingTime);
            
            _logger.LogWarning("Request {RequestId} timed out after {ProcessingTime}ms", requestId, processingTime);
            
            return new AgentResponse
            {
                Success = false,
                Message = "Request processing timed out. Please try again or simplify your request.",
                Error = "Request timeout",
                Data = new Dictionary<string, object> 
                { 
                    ["requestId"] = requestId,
                    ["processingTimeMs"] = processingTime
                }
            };
        }
        catch (Exception ex)
        {
            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _telemetryService.RecordError("RequestProcessing", ex);
            _telemetryService.RecordRequestProcessed(requestId, IntentType.General, false, processingTime);
            
            _logger.LogError(ex, "Request {RequestId} failed after {ProcessingTime}ms", requestId, processingTime);
            
            return new AgentResponse
            {
                Success = false,
                Message = "I encountered an error processing your request. Please try again or rephrase your question.",
                Error = ex.Message,
                Data = new Dictionary<string, object> 
                { 
                    ["requestId"] = requestId,
                    ["processingTimeMs"] = processingTime
                }
            };
        }
    }


    private async Task<AgentResponse> HandleCodespaceRequest(AgentRequest request)
    {
        try
        {
            var message = request.Message.ToLowerInvariant();
            
            if (message.Contains("create") || message.Contains("setup"))
            {
                var spec = ExtractCodespaceSpec(request.Message, request.Context);
                
                // Use retry mechanism for Codespace creation
                var result = await _retryService.ExecuteWithRetryAsync(async () =>
                    await _codespaceManagementService.CreateCodespaceAsync(spec));
                
                // Start onboarding if automatic onboarding is enabled
                if (result.Success && spec.AutomaticOnboarding)
                {
                    var userId = request.UserId ?? "unknown";
                    var onboardingProgress = await _onboardingService.StartOnboardingAsync(userId, result.CodespaceId!);
                    
                    var welcomeMessage = await _onboardingService.GenerateWelcomeMessageAsync(userId, spec.RepositoryName);
                    
                    return new AgentResponse
                    {
                        Success = true,
                        Message = $"🎉 Codespace created successfully!\n\n{welcomeMessage}",
                        Data = new Dictionary<string, object> 
                        { 
                            ["codespaceResult"] = result,
                            ["onboardingProgress"] = onboardingProgress,
                            ["webUrl"] = result.WebUrl!
                        },
                        IntentType = IntentType.CodespaceManagement
                    };
                }
                
                return new AgentResponse
                {
                    Success = result.Success,
                    Message = result.Success 
                        ? $"Codespace created successfully! Access it at: {result.WebUrl}"
                        : $"Failed to create Codespace: {result.Error}",
                    Data = new Dictionary<string, object> { ["codespaceResult"] = result },
                    IntentType = IntentType.CodespaceManagement
                };
            }
            
            if (message.Contains("onboard") || message.Contains("welcome"))
            {
                var userId = request.UserId ?? "unknown";
                var codespaceId = ExtractCodespaceId(request.Context);
                
                if (string.IsNullOrEmpty(codespaceId))
                {
                    // Generate welcome message without specific Codespace
                    var welcomeMessage = await _onboardingService.GenerateWelcomeMessageAsync(userId, "Power Platform");
                    return new AgentResponse
                    {
                        Success = true,
                        Message = welcomeMessage,
                        IntentType = IntentType.CodespaceManagement
                    };
                }
                
                var onboardingProgress = await _onboardingService.StartOnboardingAsync(userId, codespaceId);
                var steps = await _onboardingService.GetOnboardingStepsAsync();
                
                return new AgentResponse
                {
                    Success = true,
                    Message = "🚀 Starting your interactive onboarding experience!\n\nI'll guide you through setting up your development environment step by step.",
                    Data = new Dictionary<string, object>
                    {
                        ["onboardingProgress"] = onboardingProgress,
                        ["onboardingSteps"] = steps,
                        ["currentStep"] = (object?)(steps.FirstOrDefault(s => s.Id == onboardingProgress.CurrentStep) ?? steps.FirstOrDefault()) ?? "none"
                    },
                    IntentType = IntentType.CodespaceManagement
                };
            }
            
            if (message.Contains("status") || message.Contains("list"))
            {
                var repository = ExtractRepositoryName(request.Message, request.Context);
                var codespaces = await _codespaceManagementService.ListCodespacesAsync(repository);
                
                return new AgentResponse
                {
                    Success = true,
                    Message = $"Found {codespaces.Length} Codespace(s) for repository '{repository}'",
                    Data = new Dictionary<string, object> { ["codespaces"] = codespaces },
                    IntentType = IntentType.CodespaceManagement
                };
            }
            
            if (message.Contains("help"))
            {
                return new AgentResponse
                {
                    Success = true,
                    Message = "🎭 **Reynolds Copilot Codespace Management**\n\n" +
                             "I can help you with:\n" +
                             "• **Create Codespace**: \"Create a new Codespace for this repository\"\n" +
                             "• **Start Onboarding**: \"Help me get started\" or \"onboard me\"\n" +
                             "• **List Codespaces**: \"Show my Codespaces\" or \"list codespaces\"\n" +
                             "• **Interactive Guidance**: Step-by-step development environment setup\n\n" +
                             "✨ **Pro Tips:**\n" +
                             "- New Codespaces include automatic onboarding\n" +
                             "- Interactive cards guide you through setup\n" +
                             "- All Power Platform tools are pre-configured\n\n" +
                             "What would you like to do?",
                    IntentType = IntentType.CodespaceManagement
                };
            }
            
            return new AgentResponse
            {
                Success = false,
                Message = "I didn't understand the Codespace operation you want to perform. Try asking me to 'create a Codespace', 'help me get started', or 'list codespaces'.",
                IntentType = IntentType.CodespaceManagement
            };
        }
        catch (Exception ex)
        {
            _telemetryService.RecordError("HandleCodespaceRequest", ex);
            _logger.LogError(ex, "Error handling Codespace request");
            return new AgentResponse
            {
                Success = false,
                Message = "Error processing Codespace request. Please try again.",
                Error = ex.Message,
                IntentType = IntentType.CodespaceManagement
            };
        }
    }

    private async Task<AgentResponse> HandleEnvironmentRequest(AgentRequest request)
    {
        try
        {
            var message = request.Message.ToLowerInvariant();
            
            if (message.Contains("create"))
            {
                var spec = ExtractEnvironmentSpec(request.Message);
                
                // Use retry mechanism for environment creation
                var result = await _retryService.ExecuteWithRetryAsync(async () =>
                    await _environmentManager.CreateEnvironmentAsync(spec));
                
                return new AgentResponse
                {
                    Success = result.Success,
                    Message = result.Success 
                        ? $"Successfully created environment '{spec.Name}' with ID: {result.EnvironmentId}"
                        : $"Failed to create environment: {result.Error}",
                    Data = new Dictionary<string, object> { ["environmentResult"] = result },
                    IntentType = IntentType.EnvironmentManagement
                };
            }
            
            if (message.Contains("list"))
            {
                var environments = await _retryService.ExecuteWithRetryAsync(async () =>
                    await _environmentManager.ListEnvironmentsAsync());
                
                return new AgentResponse
                {
                    Success = true,
                    Message = $"Found {environments.Count} environments",
                    Data = new Dictionary<string, object> { ["environments"] = environments },
                    IntentType = IntentType.EnvironmentManagement
                };
            }
            
            return new AgentResponse
            {
                Success = false,
                Message = "I didn't understand the environment operation you want to perform. Please specify 'create' or 'list'.",
                IntentType = IntentType.EnvironmentManagement
            };
        }
        catch (Exception ex)
        {
            _telemetryService.RecordError("HandleEnvironmentRequest", ex);
            _logger.LogError(ex, "Error handling environment request");
            return new AgentResponse
            {
                Success = false,
                Message = "Error processing environment request. Please try again.",
                Error = ex.Message,
                IntentType = IntentType.EnvironmentManagement
            };
        }
    }

    private async Task<AgentResponse> HandleCliRequest(AgentRequest request)
    {
        try
        {
            var command = ExtractCliCommand(request.Message);
            
            // Use retry mechanism for CLI execution
            var result = await _retryService.ExecuteWithRetryAsync(async () =>
                await _cliExecutor.ExecuteAsync(command, new CliOptions()));
            
            return new AgentResponse
            {
                Success = result.Success,
                Message = result.Success 
                    ? "Command executed successfully"
                    : $"Command failed: {result.Error}",
                Data = new Dictionary<string, object> 
                { 
                    ["output"] = result.Output,
                    ["exitCode"] = result.ExitCode,
                    ["command"] = command
                },
                IntentType = IntentType.CliExecution
            };
        }
        catch (Exception ex)
        {
            _telemetryService.RecordError("HandleCliRequest", ex);
            _logger.LogError(ex, "Error handling CLI request");
            return new AgentResponse
            {
                Success = false,
                Message = "Error executing CLI command. Please verify the command syntax and try again.",
                Error = ex.Message,
                IntentType = IntentType.CliExecution
            };
        }
    }

    private async Task<AgentResponse> HandleCodeRequest(AgentRequest request)
    {
        try
        {
            var codeSpec = ExtractCodeSpec(request.Message);
            
            // Use retry mechanism for code generation
            var result = await _retryService.ExecuteWithRetryAsync(async () =>
                await _codeGenerator.GenerateCodeAsync(codeSpec));
            
            return new AgentResponse
            {
                Success = result.Success,
                Message = result.Success 
                    ? "Code generated successfully"
                    : $"Code generation failed: {result.Error}",
                Data = new Dictionary<string, object> { ["generatedCode"] = result },
                IntentType = IntentType.CodeGeneration
            };
        }
        catch (Exception ex)
        {
            _telemetryService.RecordError("HandleCodeRequest", ex);
            _logger.LogError(ex, "Error handling code generation request");
            return new AgentResponse
            {
                Success = false,
                Message = "Error generating code. Please review your request and try again.",
                Error = ex.Message,
                IntentType = IntentType.CodeGeneration
            };
        }
    }

    private async Task<AgentResponse> HandleKnowledgeRequest(AgentRequest request)
    {
        try
        {
            // Use retry mechanism for knowledge retrieval
            var knowledge = await _retryService.ExecuteWithRetryAsync(async () =>
                await _knowledgeRetriever.RetrieveKnowledgeAsync(request.Message));
            
            return new AgentResponse
            {
                Success = true,
                Message = knowledge.Answer,
                Data = new Dictionary<string, object> 
                { 
                    ["sources"] = knowledge.Sources,
                    ["confidence"] = knowledge.Confidence
                },
                IntentType = IntentType.KnowledgeQuery
            };
        }
        catch (Exception ex)
        {
            _telemetryService.RecordError("HandleKnowledgeRequest", ex);
            _logger.LogError(ex, "Error handling knowledge request");
            return new AgentResponse
            {
                Success = false,
                Message = "I'm unable to retrieve that information right now. Please try rephrasing your question.",
                Error = ex.Message,
                IntentType = IntentType.KnowledgeQuery
            };
        }
    }

    private async Task<AgentResponse> HandleGeneralRequest(AgentRequest request)
    {
        await Task.CompletedTask; // Placeholder for async operation
        
        return new AgentResponse
        {
            Success = true,
            Message = "I'm a specialized Power Platform Copilot agent. I can help you with:\n" +
                     "• Environment management (create, list, configure)\n" +
                     "• CLI command execution (pac, m365)\n" +
                     "• Code generation (C#, Blazor components)\n" +
                     "• Knowledge retrieval (documentation, best practices)\n" +
                     "• **Codespace management (create, onboard, setup)**\n\n" +
                     "🚀 **New!** Try saying:\n" +
                     "- \"Create a Codespace for me\"\n" +
                     "- \"Help me get started\"\n" +
                     "- \"Start onboarding\"\n\n" +
                     "How can I assist you with Power Platform development?",
            IntentType = IntentType.General
        };
    }

    private EnvironmentSpec ExtractEnvironmentSpec(string message)
    {
        // Simple extraction logic - in production, use more sophisticated NLP
        var spec = new EnvironmentSpec
        {
            Type = "Sandbox",
            Region = "UnitedStates"
        };

        // Extract name from message
        var words = message.Split(' ');
        for (int i = 0; i < words.Length - 1; i++)
        {
            if (words[i].ToLowerInvariant() == "environment" && i + 1 < words.Length)
            {
                spec.Name = words[i + 1].Trim('"', '\'');
                break;
            }
        }

        if (string.IsNullOrEmpty(spec.Name))
        {
            spec.Name = $"CopilotEnv-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        }

        return spec;
    }

    private string ExtractCliCommand(string message)
    {
        // Extract CLI command from natural language
        var lowerMessage = message.ToLowerInvariant();
        
        if (lowerMessage.Contains("pac "))
        {
            var pacIndex = lowerMessage.IndexOf("pac ");
            return message.Substring(pacIndex);
        }
        
        if (lowerMessage.Contains("m365 "))
        {
            var m365Index = lowerMessage.IndexOf("m365 ");
            return message.Substring(m365Index);
        }
        
        return message; // Fallback
    }

    private CodeSpec ExtractCodeSpec(string message)
    {
        return new CodeSpec
        {
            Type = message.ToLowerInvariant().Contains("blazor") ? "BlazorComponent" : "CSharpClass",
            Name = ExtractComponentName(message),
            Requirements = message
        };
    }

    private string ExtractComponentName(string message)
    {
        // Simple name extraction
        var words = message.Split(' ');
        foreach (var word in words)
        {
            if (char.IsUpper(word[0]) && word.Length > 3)
            {
                return word;
            }
        }
        
        return "GeneratedComponent";
    }

    private CodespaceSpec ExtractCodespaceSpec(string message, Dictionary<string, object> context)
    {
        var spec = new CodespaceSpec
        {
            RepositoryName = ExtractRepositoryName(message, context),
            Branch = "main",
            Machine = "standardLinux32gb",
            IdleTimeoutMinutes = 30,
            AutomaticOnboarding = true
        };

        // Extract specific machine type if mentioned
        if (message.ToLowerInvariant().Contains("large"))
        {
            spec.Machine = "largePlus";
        }
        else if (message.ToLowerInvariant().Contains("premium"))
        {
            spec.Machine = "premiumLinux";
        }

        // Extract branch if specified
        var words = message.Split(' ');
        for (int i = 0; i < words.Length - 1; i++)
        {
            if (words[i].ToLowerInvariant() == "branch" && i + 1 < words.Length)
            {
                spec.Branch = words[i + 1].Trim('"', '\'');
                break;
            }
        }

        return spec;
    }

    private string ExtractRepositoryName(string message, Dictionary<string, object> context)
    {
        // Try to get from context first
        if (context.TryGetValue("repository", out var repoObj) && repoObj is string repo)
        {
            return repo;
        }

        // Extract from message
        var words = message.Split(' ');
        for (int i = 0; i < words.Length - 1; i++)
        {
            if (words[i].ToLowerInvariant() == "repository" && i + 1 < words.Length)
            {
                return words[i + 1].Trim('"', '\'');
            }
        }

        // Default fallback
        return "copilot-powerplatform";
    }

    private string ExtractCodespaceId(Dictionary<string, object> context)
    {
        if (context.TryGetValue("codespaceId", out var codespaceIdObj) && codespaceIdObj is string codespaceId)
        {
            return codespaceId;
        }

        return string.Empty;
    }
}

