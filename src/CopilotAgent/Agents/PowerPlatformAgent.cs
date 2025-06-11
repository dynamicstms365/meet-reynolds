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

    public PowerPlatformAgent(
        IEnvironmentManager environmentManager,
        ICliExecutor cliExecutor,
        ICodeGenerator codeGenerator,
        IKnowledgeRetriever knowledgeRetriever,
        ILogger<PowerPlatformAgent> logger,
        IIntentRecognitionService intentRecognitionService,
        IRetryService retryService,
        ITelemetryService telemetryService,
        IConfigurationService configurationService)
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
                     "• Knowledge retrieval (documentation, best practices)\n\n" +
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
}

