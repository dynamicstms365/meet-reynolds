using Shared.Models;
using System.Text.RegularExpressions;

namespace CopilotAgent.Services;

public interface IIntentRecognitionService
{
    Task<IntentAnalysis> AnalyzeIntentAsync(string message, AgentConfiguration config);
}

public class IntentRecognitionService : IIntentRecognitionService
{
    private readonly ILogger<IntentRecognitionService> _logger;

    // Enhanced pattern matching for better intent recognition
    private readonly Dictionary<IntentType, List<IntentPattern>> _intentPatterns = new()
    {
        [IntentType.EnvironmentManagement] = new()
        {
            new IntentPattern { Pattern = @"\b(create|make|setup|provision).*(env|environment)", Weight = 1.0 },
            new IntentPattern { Pattern = @"\b(list|show|get).*(env|environment)", Weight = 1.0 },
            new IntentPattern { Pattern = @"\bpac\s+env\b", Weight = 1.0 },
            new IntentPattern { Pattern = @"\benvironment\b", Weight = 0.8 },
            new IntentPattern { Pattern = @"\bdataverse\b", Weight = 0.7 },
            new IntentPattern { Pattern = @"\bcreate.*environment", Weight = 0.9 }
        },
        [IntentType.CliExecution] = new()
        {
            new IntentPattern { Pattern = @"\bpac\s+", Weight = 1.0 },
            new IntentPattern { Pattern = @"\bm365\s+", Weight = 1.0 },
            new IntentPattern { Pattern = @"\b(run|execute).*(pac|m365|command)", Weight = 1.0 },
            new IntentPattern { Pattern = @"\b(run|execute|command)\b", Weight = 0.8 },
            new IntentPattern { Pattern = @"\b(cli|command\s+line)\b", Weight = 0.7 }
        },
        [IntentType.CodeGeneration] = new()
        {
            new IntentPattern { Pattern = @"\b(generate|create|make).*(code|component|class|blazor)", Weight = 1.0 },
            new IntentPattern { Pattern = @"\b(blazor|razor).*(component|page)", Weight = 1.0 },
            new IntentPattern { Pattern = @"\bc#.*(class|method|interface)", Weight = 0.9 },
            new IntentPattern { Pattern = @"\b(scaffold|template)\b", Weight = 0.8 },
            new IntentPattern { Pattern = @"\bgenerate\b", Weight = 0.7 }
        },
        [IntentType.KnowledgeQuery] = new()
        {
            new IntentPattern { Pattern = @"\b(how\s+to|what\s+is|explain|help\s+with)", Weight = 1.0 },
            new IntentPattern { Pattern = @"\b(documentation|docs|guide)\b", Weight = 0.9 },
            new IntentPattern { Pattern = @"\b(help|assist|support)\b", Weight = 0.7 },
            new IntentPattern { Pattern = @"\?\s*$", Weight = 0.8 }
        }
    };

    public IntentRecognitionService(ILogger<IntentRecognitionService> logger)
    {
        _logger = logger;
    }

    public async Task<IntentAnalysis> AnalyzeIntentAsync(string message, AgentConfiguration config)
    {
        await Task.CompletedTask; // Placeholder for async operations like ML model calls

        try
        {
            var startTime = DateTime.UtcNow;
            var lowerMessage = message.ToLowerInvariant();
            var results = new Dictionary<IntentType, double>();

            // Calculate confidence scores for each intent type
            foreach (var intentType in _intentPatterns.Keys)
            {
                var confidence = CalculateIntentConfidence(lowerMessage, intentType, config);
                if (confidence > 0)
                {
                    results[intentType] = confidence;
                }
            }

            // Find the best match
            var bestMatch = results.OrderByDescending(r => r.Value).FirstOrDefault();
            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var analysis = new IntentAnalysis
            {
                Type = bestMatch.Value >= config.IntentRecognition.ConfidenceThreshold 
                    ? bestMatch.Key 
                    : IntentType.General,
                Confidence = bestMatch.Value,
                ProcessingTimeMs = processingTime,
                AlternativeIntents = results
                    .Where(r => r.Key != bestMatch.Key)
                    .OrderByDescending(r => r.Value)
                    .Take(2)
                    .ToDictionary(r => r.Key, r => r.Value)
            };

            _logger.LogDebug("Intent analysis completed: {Intent} (confidence: {Confidence:P2}, processing: {ProcessingTime}ms)",
                analysis.Type, analysis.Confidence, analysis.ProcessingTimeMs);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing intent for message: {Message}", message);
            return new IntentAnalysis 
            { 
                Type = IntentType.General, 
                Confidence = 0.0,
                Error = ex.Message
            };
        }
    }

    private double CalculateIntentConfidence(string message, IntentType intentType, AgentConfiguration config)
    {
        if (!_intentPatterns.TryGetValue(intentType, out var patterns))
            return 0.0;

        double totalScore = 0.0;
        double maxPossibleScore = 0.0;

        foreach (var pattern in patterns)
        {
            maxPossibleScore += pattern.Weight;
            
            var regex = new Regex(pattern.Pattern, RegexOptions.IgnoreCase);
            var matches = regex.Matches(message);
            
            if (matches.Count > 0)
            {
                // Boost score for multiple matches but with diminishing returns
                var matchScore = pattern.Weight * (1.0 + (matches.Count - 1) * 0.1);
                totalScore += Math.Min(matchScore, pattern.Weight * 1.5);
            }
        }

        // Normalize score and apply any intent-specific weights from configuration
        var normalizedScore = maxPossibleScore > 0 ? totalScore / maxPossibleScore : 0.0;
        
        // Apply configuration weights if available
        var intentName = intentType.ToString().ToLowerInvariant();
        if (config.IntentRecognition.IntentWeights.TryGetValue(intentName, out var weight))
        {
            normalizedScore *= weight;
        }

        return Math.Min(normalizedScore, 1.0);
    }
}

public class IntentPattern
{
    public string Pattern { get; set; } = string.Empty;
    public double Weight { get; set; } = 1.0;
}

// Enhanced IntentAnalysis with more details
public class IntentAnalysis
{
    public IntentType Type { get; set; }
    public double Confidence { get; set; } = 1.0;
    public double ProcessingTimeMs { get; set; }
    public Dictionary<IntentType, double> AlternativeIntents { get; set; } = new();
    public string? Error { get; set; }
}