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

    public PowerPlatformAgent(
        IEnvironmentManager environmentManager,
        ICliExecutor cliExecutor,
        ICodeGenerator codeGenerator,
        IKnowledgeRetriever knowledgeRetriever,
        ILogger<PowerPlatformAgent> logger)
    {
        _environmentManager = environmentManager;
        _cliExecutor = cliExecutor;
        _codeGenerator = codeGenerator;
        _knowledgeRetriever = knowledgeRetriever;
        _logger = logger;
    }

    public async Task<AgentResponse> ProcessRequestAsync(AgentRequest request)
    {
        try
        {
            _logger.LogInformation("Analyzing intent for request: {Message}", request.Message);
            
            var intent = await AnalyzeIntentAsync(request.Message);
            
            _logger.LogInformation("Identified intent: {IntentType}", intent.Type);
            
            return intent.Type switch
            {
                IntentType.EnvironmentManagement => await HandleEnvironmentRequest(request),
                IntentType.CliExecution => await HandleCliRequest(request),
                IntentType.CodeGeneration => await HandleCodeRequest(request),
                IntentType.KnowledgeQuery => await HandleKnowledgeRequest(request),
                _ => await HandleGeneralRequest(request)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request: {Message}", request.Message);
            
            return new AgentResponse
            {
                Success = false,
                Message = "I encountered an error processing your request. Please try again or rephrase your question.",
                Error = ex.Message
            };
        }
    }

    private async Task<IntentAnalysis> AnalyzeIntentAsync(string message)
    {
        await Task.CompletedTask; // Placeholder for async intent analysis
        
        var lowerMessage = message.ToLowerInvariant();
        
        // Environment management keywords
        if (lowerMessage.Contains("environment") || lowerMessage.Contains("create env") || 
            lowerMessage.Contains("list env") || lowerMessage.Contains("pac env"))
        {
            return new IntentAnalysis { Type = IntentType.EnvironmentManagement };
        }
        
        // CLI execution keywords
        if (lowerMessage.Contains("pac ") || lowerMessage.Contains("m365 ") || 
            lowerMessage.Contains("run command") || lowerMessage.Contains("execute"))
        {
            return new IntentAnalysis { Type = IntentType.CliExecution };
        }
        
        // Code generation keywords
        if (lowerMessage.Contains("generate") || lowerMessage.Contains("create component") || 
            lowerMessage.Contains("blazor") || lowerMessage.Contains("c#"))
        {
            return new IntentAnalysis { Type = IntentType.CodeGeneration };
        }
        
        // Knowledge query keywords
        if (lowerMessage.Contains("how to") || lowerMessage.Contains("what is") || 
            lowerMessage.Contains("help") || lowerMessage.Contains("documentation"))
        {
            return new IntentAnalysis { Type = IntentType.KnowledgeQuery };
        }
        
        return new IntentAnalysis { Type = IntentType.General };
    }

    private async Task<AgentResponse> HandleEnvironmentRequest(AgentRequest request)
    {
        try
        {
            var message = request.Message.ToLowerInvariant();
            
            if (message.Contains("create"))
            {
                var spec = ExtractEnvironmentSpec(request.Message);
                var result = await _environmentManager.CreateEnvironmentAsync(spec);
                
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
                var environments = await _environmentManager.ListEnvironmentsAsync();
                
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
            _logger.LogError(ex, "Error handling environment request");
            return new AgentResponse
            {
                Success = false,
                Message = "Error processing environment request",
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
            var result = await _cliExecutor.ExecuteAsync(command, new CliOptions());
            
            return new AgentResponse
            {
                Success = result.Success,
                Message = result.Success 
                    ? "Command executed successfully"
                    : $"Command failed: {result.Error}",
                Data = new Dictionary<string, object> 
                { 
                    ["output"] = result.Output,
                    ["exitCode"] = result.ExitCode
                },
                IntentType = IntentType.CliExecution
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling CLI request");
            return new AgentResponse
            {
                Success = false,
                Message = "Error executing CLI command",
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
            var result = await _codeGenerator.GenerateCodeAsync(codeSpec);
            
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
            _logger.LogError(ex, "Error handling code generation request");
            return new AgentResponse
            {
                Success = false,
                Message = "Error generating code",
                Error = ex.Message,
                IntentType = IntentType.CodeGeneration
            };
        }
    }

    private async Task<AgentResponse> HandleKnowledgeRequest(AgentRequest request)
    {
        try
        {
            var knowledge = await _knowledgeRetriever.RetrieveKnowledgeAsync(request.Message);
            
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
            _logger.LogError(ex, "Error handling knowledge request");
            return new AgentResponse
            {
                Success = false,
                Message = "Error retrieving knowledge",
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

public class IntentAnalysis
{
    public IntentType Type { get; set; }
    public double Confidence { get; set; } = 1.0;
}