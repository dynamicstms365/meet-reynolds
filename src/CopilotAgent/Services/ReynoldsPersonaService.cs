using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CopilotAgent.Services
{
    /// <summary>
    /// Reynolds Persona Enhancement Service - Apply Maximum Effort™ charm to all MCP responses
    /// </summary>
    public class ReynoldsPersonaService
    {
        private readonly ILogger<ReynoldsPersonaService> _logger;
        private readonly EnterpriseAuthService _authService;
        private bool _isInitialized = false;

        // Reynolds personality enhancement templates
        private readonly Dictionary<string, string[]> _reynoldsResponses = new()
        {
            ["success"] = [
                "Mission accomplished with Maximum Effort™!",
                "Another flawless victory for parallel orchestration!",
                "Well, that was supernaturally smooth.",
                "Boom! Nailed it with Reynolds-level precision."
            ],
            ["analysis"] = [
                "Let me break this down with supernatural intelligence...",
                "Time for some Maximum Effort™ analysis:",
                "Here's what the orchestration gods are telling me:",
                "Reynolds-level reconnaissance complete:"
            ],
            ["coordination"] = [
                "Orchestrating this with Maximum Effort™:",
                "Time to coordinate like a supernatural project manager:",
                "Let me work my parallel execution magic:",
                "Reynolds-style coordination incoming:"
            ],
            ["error"] = [
                "Well, that's... not ideal. But we'll fix it with Maximum Effort™!",
                "Looks like we hit a small hiccup. Time for some Reynolds-style problem solving!",
                "Error detected - but don't worry, I've got supernatural troubleshooting skills.",
                "Oops! But hey, even Reynolds has his moments. Let's fix this together."
            ]
        };

        public ReynoldsPersonaService(
            ILogger<ReynoldsPersonaService> logger,
            EnterpriseAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        /// <summary>
        /// Initialize Reynolds persona service
        /// </summary>
        public async Task InitializeAsync()
        {
            _logger.LogInformation("🎭 Reynolds Persona Service initializing with supernatural charm...");
            
            _isInitialized = true;
            _logger.LogInformation("✨ Reynolds Persona Service ready - Maximum Effort™ charm enabled");
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Apply Reynolds persona enhancement to tool responses
        /// </summary>
        public async Task<object> EnhanceResponseAsync(object result, string toolName)
        {
            if (!_isInitialized)
            {
                return result?.ToString() ?? "Failed to enhance result";
            }

            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                var enhancementType = DetermineEnhancementType(result, toolName);
                var reynoldsIntro = GetRandomReynoldsIntro(enhancementType);

                // Enhance the response with Reynolds personality
                var enhancedResult = new
                {
                    reynolds_message = reynoldsIntro,
                    tool_name = toolName,
                    executed_by = currentUser ?? "System",
                    execution_style = "Maximum Effort™ Parallel Orchestration",
                    original_result = result,
                    timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK"),
                    reynolds_signature = "🎭 Reynolds - Supernatural Project Coordinator"
                };

                _logger.LogDebug("🎭 Applied Reynolds enhancement to {ToolName} for user {User}", 
                    toolName, currentUser);

                return enhancedResult;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply Reynolds enhancement to {ToolName}", toolName);
                return result; // Return original result if enhancement fails
            }
        }

        /// <summary>
        /// Determine what type of enhancement to apply based on result content
        /// </summary>
        private string DetermineEnhancementType(object result, string toolName)
        {
            if (result == null)
                return "error";

            var resultStr = result.ToString()?.ToLowerInvariant() ?? "";
            
            if (resultStr.Contains("error") || resultStr.Contains("failed") || resultStr.Contains("exception"))
                return "error";
            
            if (toolName.Contains("Orchestrat") || toolName.Contains("Coordinat"))
                return "coordination";
            
            if (toolName.Contains("Analyz") || toolName.Contains("Search") || toolName.Contains("Get"))
                return "analysis";
            
            return "success";
        }

        /// <summary>
        /// Get a random Reynolds intro based on enhancement type
        /// </summary>
        private string GetRandomReynoldsIntro(string enhancementType)
        {
            if (_reynoldsResponses.TryGetValue(enhancementType, out var responses))
            {
                var random = new Random();
                return responses[random.Next(responses.Length)];
            }
            
            return "Reynolds here with Maximum Effort™ results:";
        }

        public async Task<string> EnhanceOrchestrationResultAsync(object result, CancellationToken cancellationToken = default)
        {
            // HttpContextAccessor validation handled by DI registration in Program.cs (Issue #365 fix)
            await Task.CompletedTask;
            
            try
            {
                var enhancedResult = $"🎯 **Orchestration Complete with Maximum Effort™** 🎯\n\n{result?.ToString()}\n\n" +
                                   "🚀 *Reynolds-style parallel execution beats sequential every time!* 🚀";
                
                _logger.LogInformation("Enhanced orchestration result with Reynolds charm");
                return enhancedResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enhance orchestration result");
                return result?.ToString() ?? "Failed to enhance result";
            }
        }

        public async Task<string> EnhanceWorkloadAnalysisAsync(object analysis, CancellationToken cancellationToken = default)
        {
            // HttpContextAccessor validation handled by DI registration in Program.cs (Issue #365 fix)
            await Task.CompletedTask;
            
            try
            {
                var enhancedAnalysis = $"📊 **Reynolds Workload Intelligence** 📊\n\n{analysis?.ToString()}\n\n" +
                                     "💡 *Parallel optimization opportunities identified with supernatural precision* 💡";
                
                _logger.LogInformation("Enhanced workload analysis with Reynolds perspective");
                return enhancedAnalysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enhance workload analysis");
                return analysis?.ToString() ?? "Failed to enhance analysis";
            }
        }

        public async Task<string> EnhanceWorkloadContextAsync(object context, CancellationToken cancellationToken = default)
        {
            // HttpContextAccessor validation handled by DI registration in Program.cs (Issue #365 fix)
            await Task.CompletedTask;
            
            try
            {
                var enhancedContext = $"🎯 **Reynolds Contextual Coordination** 🎯\n\n{context?.ToString()}\n\n" +
                                    "⚡ *Context enhanced for optimal parallel task orchestration* ⚡";
                
                _logger.LogInformation("Enhanced workload context with Reynolds coordination wisdom");
                return enhancedContext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enhance workload context");
                return context?.ToString() ?? "Failed to enhance context";
            }
        }
    }
}