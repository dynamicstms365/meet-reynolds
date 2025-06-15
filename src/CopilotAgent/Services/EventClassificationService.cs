using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using CopilotAgent.Services;

namespace CopilotAgent.Services;

/// <summary>
/// Event Classification Service for Issue #73
/// Provides intelligent event categorization and priority assessment
/// Integrates with GitHub Models for advanced classification capabilities
/// </summary>
public interface IEventClassificationService
{
    Task<EventClassification> ClassifyEventAsync(PlatformEvent platformEvent);
    Task<EventPriority> DeterminePriorityAsync(PlatformEvent platformEvent);
    Task<string> DetermineCategoryAsync(PlatformEvent platformEvent);
    Task<List<string>> ExtractKeywordsAsync(PlatformEvent platformEvent);
    Task<double> CalculateUrgencyScoreAsync(PlatformEvent platformEvent);
}

public class EventClassificationService : IEventClassificationService
{
    private readonly ILogger<EventClassificationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IGitHubModelsService _modelsService;
    private readonly ISecurityAuditService _auditService;
    
    // Classification patterns and rules
    private readonly Dictionary<string, EventClassificationRule> _classificationRules;
    private readonly Dictionary<string, double> _urgencyWeights;

    public EventClassificationService(
        ILogger<EventClassificationService> logger,
        IConfiguration configuration,
        IGitHubModelsService modelsService,
        ISecurityAuditService auditService)
    {
        _logger = logger;
        _configuration = configuration;
        _modelsService = modelsService;
        _auditService = auditService;
        
        _classificationRules = InitializeClassificationRules();
        _urgencyWeights = InitializeUrgencyWeights();
    }

    public async Task<EventClassification> ClassifyEventAsync(PlatformEvent platformEvent)
    {
        try
        {
            _logger.LogInformation("🧠 Reynolds Event Classification analyzing: {EventType} from {Platform}", 
                platformEvent.EventType, platformEvent.SourcePlatform);

            var startTime = DateTime.UtcNow;

            // Step 1: Determine basic category using rules
            var category = await DetermineCategoryAsync(platformEvent);
            
            // Step 2: Calculate priority based on multiple factors
            var priority = await DeterminePriorityAsync(platformEvent);
            
            // Step 3: Extract key information
            var keywords = await ExtractKeywordsAsync(platformEvent);
            var urgencyScore = await CalculateUrgencyScoreAsync(platformEvent);
            
            // Step 4: Use GitHub Models for advanced analysis if available
            var enhancedAnalysis = await PerformEnhancedAnalysisAsync(platformEvent, category);

            var classification = new EventClassification
            {
                Category = enhancedAnalysis?.Category ?? category,
                Priority = enhancedAnalysis?.Priority ?? priority,
                Keywords = keywords,
                UrgencyScore = urgencyScore,
                Confidence = enhancedAnalysis?.Confidence ?? CalculateBaseConfidence(platformEvent, category),
                ProcessingTime = DateTime.UtcNow - startTime,
                ReasoningPath = GenerateReasoningPath(platformEvent, category, priority),
                ReynoldsInsight = GenerateReynoldsInsight(platformEvent, category, priority)
            };

            await _auditService.LogEventAsync(
                "Event_Classification_Completed",
                repository: platformEvent.Repository,
                action: "ClassifyEvent",
                result: "SUCCESS",
                details: new {
                    EventType = platformEvent.EventType,
                    SourcePlatform = platformEvent.SourcePlatform,
                    Category = classification.Category,
                    Priority = classification.Priority,
                    Confidence = classification.Confidence,
                    ProcessingTime = classification.ProcessingTime.TotalMilliseconds
                });

            _logger.LogInformation("✅ Event classified as {Category} with {Priority} priority (confidence: {Confidence:P1})", 
                classification.Category, classification.Priority, classification.Confidence);

            return classification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error classifying event: {EventType}", platformEvent.EventType);
            
            // Return fallback classification
            return new EventClassification
            {
                Category = "general",
                Priority = EventPriority.Medium,
                Keywords = new List<string>(),
                UrgencyScore = 0.5,
                Confidence = 0.3,
                ProcessingTime = TimeSpan.Zero,
                ReasoningPath = "Fallback classification due to processing error",
                ReynoldsInsight = "Even Reynolds has the occasional classification hiccup. Using safe defaults!"
            };
        }
    }

    public async Task<EventPriority> DeterminePriorityAsync(PlatformEvent platformEvent)
    {
        var priority = EventPriority.Low;
        var score = 0.0;

        // Platform-specific priority rules
        switch (platformEvent.SourcePlatform.ToLowerInvariant())
        {
            case "github":
                score += await CalculateGitHubPriorityScore(platformEvent);
                break;
            case "azure":
                score += await CalculateAzurePriorityScore(platformEvent);
                break;
            case "teams":
                score += await CalculateTeamsPriorityScore(platformEvent);
                break;
        }

        // Event type specific scoring
        score += await CalculateEventTypeScore(platformEvent);
        
        // Content analysis scoring
        score += await CalculateContentScore(platformEvent);

        // Time-based urgency
        score += CalculateTimeUrgency(platformEvent);

        // Convert score to priority
        priority = score switch
        {
            >= 0.8 => EventPriority.Critical,
            >= 0.6 => EventPriority.High,
            >= 0.4 => EventPriority.Medium,
            _ => EventPriority.Low
        };

        return priority;
    }

    public async Task<string> DetermineCategoryAsync(PlatformEvent platformEvent)
    {
        // Rule-based classification first
        foreach (var rule in _classificationRules.Values)
        {
            if (await rule.MatchesAsync(platformEvent))
            {
                return rule.Category;
            }
        }

        // Platform-specific default categorization
        return platformEvent.SourcePlatform.ToLowerInvariant() switch
        {
            "github" => CategorizeGitHubEvent(platformEvent),
            "azure" => CategorizeAzureEvent(platformEvent),
            "teams" => CategorizeTeamsEvent(platformEvent),
            _ => "general"
        };
    }

    public async Task<List<string>> ExtractKeywordsAsync(PlatformEvent platformEvent)
    {
        var keywords = new List<string>();

        // Extract from event type
        keywords.Add(platformEvent.EventType);
        if (!string.IsNullOrEmpty(platformEvent.Action))
        {
            keywords.Add(platformEvent.Action);
        }

        // Extract from content
        if (!string.IsNullOrEmpty(platformEvent.Content))
        {
            var contentKeywords = await ExtractContentKeywords(platformEvent.Content);
            keywords.AddRange(contentKeywords);
        }

        // Extract from metadata
        foreach (var key in platformEvent.Metadata.Keys)
        {
            keywords.Add(key);
        }

        // Repository-based keywords
        if (!string.IsNullOrEmpty(platformEvent.Repository))
        {
            keywords.Add("repository");
            if (platformEvent.Repository.Contains("powerplatform"))
                keywords.Add("powerplatform");
            if (platformEvent.Repository.Contains("copilot"))
                keywords.Add("copilot");
        }

        return keywords.Distinct().ToList();
    }

    public async Task<double> CalculateUrgencyScoreAsync(PlatformEvent platformEvent)
    {
        await Task.CompletedTask;
        
        double urgencyScore = 0.0;

        // Base urgency from event type
        if (_urgencyWeights.TryGetValue(platformEvent.EventType, out var baseUrgency))
        {
            urgencyScore += baseUrgency;
        }

        // Action-specific urgency
        urgencyScore += platformEvent.Action?.ToLowerInvariant() switch
        {
            "failed" => 0.8,
            "error" => 0.7,
            "critical" => 0.9,
            "opened" => 0.3,
            "closed" => 0.2,
            "created" => 0.3,
            _ => 0.1
        };

        // Content analysis for urgency keywords
        if (!string.IsNullOrEmpty(platformEvent.Content))
        {
            var urgentKeywords = new[] { "urgent", "critical", "emergency", "down", "failed", "error", "broken" };
            var content = platformEvent.Content.ToLowerInvariant();
            
            foreach (var keyword in urgentKeywords)
            {
                if (content.Contains(keyword))
                {
                    urgencyScore += 0.2;
                }
            }
        }

        // Time-based urgency (newer events might be more urgent)
        var age = DateTime.UtcNow - platformEvent.Timestamp;
        if (age < TimeSpan.FromMinutes(5))
        {
            urgencyScore += 0.1; // Fresh events get slight urgency boost
        }

        return Math.Min(urgencyScore, 1.0); // Cap at 1.0
    }

    // Private helper methods

    private Dictionary<string, EventClassificationRule> InitializeClassificationRules()
    {
        return new Dictionary<string, EventClassificationRule>
        {
            ["scope_creep"] = new EventClassificationRule
            {
                Category = "scope_creep",
                PlatformPatterns = new[] { "github" },
                EventTypePatterns = new[] { "pull_request" },
                ContentPatterns = new[] { "scope", "additional", "also", "furthermore", "while we're at it" },
                MetadataChecks = (metadata) => metadata.ContainsKey("lines_changed") && 
                    int.TryParse(metadata["lines_changed"].ToString(), out var lines) && lines > 500
            },
            ["infrastructure_alert"] = new EventClassificationRule
            {
                Category = "infrastructure_alert",
                PlatformPatterns = new[] { "azure" },
                EventTypePatterns = new[] { "container_instance_failed", "resource_health_degraded", "deployment_failed" },
                ContentPatterns = new[] { "failed", "error", "down", "unhealthy" },
                MetadataChecks = (metadata) => true
            },
            ["github_command"] = new EventClassificationRule
            {
                Category = "github_command",
                PlatformPatterns = new[] { "teams" },
                EventTypePatterns = new[] { "message" },
                ContentPatterns = new[] { "create issue", "create discussion", "github", "repo", "pull request" },
                MetadataChecks = (metadata) => true
            },
            ["coordination_needed"] = new EventClassificationRule
            {
                Category = "coordination_needed",
                PlatformPatterns = new[] { "github", "teams", "azure" },
                EventTypePatterns = new[] { "workflow_run", "deployment", "message" },
                ContentPatterns = new[] { "coordinate", "sync", "align", "dependencies", "blocking" },
                MetadataChecks = (metadata) => true
            },
            ["security_alert"] = new EventClassificationRule
            {
                Category = "security_alert",
                PlatformPatterns = new[] { "github", "azure" },
                EventTypePatterns = new[] { "security_advisory", "vulnerability", "security_alert" },
                ContentPatterns = new[] { "security", "vulnerability", "exploit", "breach", "unauthorized" },
                MetadataChecks = (metadata) => true
            }
        };
    }

    private Dictionary<string, double> InitializeUrgencyWeights()
    {
        return new Dictionary<string, double>
        {
            ["workflow_run"] = 0.6,
            ["pull_request"] = 0.4,
            ["issues"] = 0.5,
            ["discussion"] = 0.2,
            ["container_instance_failed"] = 0.9,
            ["resource_health_degraded"] = 0.7,
            ["deployment_completed"] = 0.3,
            ["deployment_failed"] = 0.8,
            ["security_advisory"] = 0.9,
            ["vulnerability"] = 0.8
        };
    }

    private async Task<double> CalculateGitHubPriorityScore(PlatformEvent platformEvent)
    {
        await Task.CompletedTask;
        
        double score = 0.0;

        score += platformEvent.EventType switch
        {
            "workflow_run" when platformEvent.Action == "failed" => 0.7,
            "pull_request" when platformEvent.Action == "opened" => 0.4,
            "issues" when platformEvent.Action == "opened" => 0.5,
            "security_advisory" => 0.9,
            _ => 0.2
        };

        // Repository importance
        if (platformEvent.Repository?.Contains("copilot-powerplatform") == true)
        {
            score += 0.2; // Main repository gets priority boost
        }

        return score;
    }

    private async Task<double> CalculateAzurePriorityScore(PlatformEvent platformEvent)
    {
        await Task.CompletedTask;
        
        return platformEvent.EventType switch
        {
            "container_instance_failed" => 0.9,
            "resource_health_critical" => 0.8,
            "resource_health_degraded" => 0.6,
            "deployment_failed" => 0.7,
            "deployment_completed" => 0.3,
            _ => 0.2
        };
    }

    private async Task<double> CalculateTeamsPriorityScore(PlatformEvent platformEvent)
    {
        await Task.CompletedTask;
        
        var content = platformEvent.Content?.ToLowerInvariant() ?? "";
        
        double score = 0.2; // Base score

        if (content.Contains("urgent") || content.Contains("critical"))
            score += 0.4;
        
        if (content.Contains("reynolds"))
            score += 0.1; // Direct mentions get slight boost
            
        if (content.Contains("help") || content.Contains("issue"))
            score += 0.3;

        return score;
    }

    private async Task<double> CalculateEventTypeScore(PlatformEvent platformEvent)
    {
        await Task.CompletedTask;
        
        // Base scoring by event type criticality
        return platformEvent.EventType switch
        {
            var type when type.Contains("failed") => 0.6,
            var type when type.Contains("error") => 0.5,
            var type when type.Contains("security") => 0.7,
            var type when type.Contains("workflow") => 0.4,
            _ => 0.1
        };
    }

    private async Task<double> CalculateContentScore(PlatformEvent platformEvent)
    {
        await Task.CompletedTask;
        
        if (string.IsNullOrEmpty(platformEvent.Content))
            return 0.0;

        var content = platformEvent.Content.ToLowerInvariant();
        double score = 0.0;

        // High priority keywords
        var criticalKeywords = new[] { "critical", "urgent", "emergency", "down", "outage" };
        var highKeywords = new[] { "failed", "error", "problem", "issue", "broken" };
        var mediumKeywords = new[] { "warning", "attention", "review", "check" };

        foreach (var keyword in criticalKeywords)
        {
            if (content.Contains(keyword)) score += 0.3;
        }

        foreach (var keyword in highKeywords)
        {
            if (content.Contains(keyword)) score += 0.2;
        }

        foreach (var keyword in mediumKeywords)
        {
            if (content.Contains(keyword)) score += 0.1;
        }

        return Math.Min(score, 0.5); // Cap content contribution
    }

    private double CalculateTimeUrgency(PlatformEvent platformEvent)
    {
        var age = DateTime.UtcNow - platformEvent.Timestamp;
        
        // Recent events get urgency boost
        return age.TotalMinutes switch
        {
            < 5 => 0.1,   // Very recent
            < 30 => 0.05, // Recent
            _ => 0.0      // Older events
        };
    }

    private string CategorizeGitHubEvent(PlatformEvent platformEvent)
    {
        return platformEvent.EventType switch
        {
            "pull_request" => "development",
            "issues" => "issue_management",
            "workflow_run" => "deployment",
            "discussion" => "collaboration",
            "security_advisory" => "security_alert",
            _ => "general"
        };
    }

    private string CategorizeAzureEvent(PlatformEvent platformEvent)
    {
        return platformEvent.EventType switch
        {
            var type when type.Contains("container") => "infrastructure",
            var type when type.Contains("resource_health") => "infrastructure_alert",
            var type when type.Contains("deployment") => "deployment",
            _ => "general"
        };
    }

    private string CategorizeTeamsEvent(PlatformEvent platformEvent)
    {
        var content = platformEvent.Content?.ToLowerInvariant() ?? "";
        
        if (content.Contains("github") || content.Contains("repo") || content.Contains("issue"))
            return "github_command";
        
        if (content.Contains("azure") || content.Contains("deploy"))
            return "resource_management";
            
        if (content.Contains("coordinate") || content.Contains("sync"))
            return "coordination_needed";
            
        return "general";
    }

    private async Task<List<string>> ExtractContentKeywords(string content)
    {
        await Task.CompletedTask;
        
        var keywords = new List<string>();
        
        // Simple keyword extraction - could be enhanced with NLP
        var words = content.ToLowerInvariant()
            .Split(new[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3) // Filter out short words
            .Where(w => !IsStopWord(w))
            .Take(10); // Limit to top 10 keywords

        keywords.AddRange(words);
        return keywords;
    }

    private bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string> { "the", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "this", "that", "from" };
        return stopWords.Contains(word);
    }

    private async Task<EnhancedClassification?> PerformEnhancedAnalysisAsync(PlatformEvent platformEvent, string baseCategory)
    {
        try
        {
            // Use GitHub Models for enhanced classification if available
            var modelRequest = new ModelRequest
            {
                TaskType = TaskType.IssueManagement,
                ComplexityLevel = ComplexityLevel.Medium,
                Content = JsonSerializer.Serialize(new {
                    EventType = platformEvent.EventType,
                    Platform = platformEvent.SourcePlatform,
                    Action = platformEvent.Action,
                    Content = platformEvent.Content,
                    BaseCategory = baseCategory
                }),
                Repository = platformEvent.Repository ?? "dynamicstms365/copilot-powerplatform",
                UserId = "reynolds-event-classifier"
            };

            var response = await _modelsService.RouteToSpecializedModelAsync(modelRequest);
            
            if (response.Success)
            {
                // Parse enhanced classification from model response
                return await ParseEnhancedClassification(response.Content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Enhanced classification unavailable: {Error}", ex.Message);
        }

        return null;
    }

    private async Task<EnhancedClassification?> ParseEnhancedClassification(string modelResponse)
    {
        await Task.CompletedTask;
        
        try
        {
            // Simple parsing - could be enhanced with structured output
            if (modelResponse.Contains("critical", StringComparison.OrdinalIgnoreCase))
            {
                return new EnhancedClassification
                {
                    Category = "enhanced_critical",
                    Priority = EventPriority.Critical,
                    Confidence = 0.85
                };
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    private double CalculateBaseConfidence(PlatformEvent platformEvent, string category)
    {
        // Base confidence calculation
        double confidence = 0.7; // Base confidence

        // Boost confidence for well-defined events
        if (_classificationRules.ContainsKey(category))
        {
            confidence += 0.2;
        }

        // Platform-specific confidence adjustments
        confidence += platformEvent.SourcePlatform switch
        {
            "GitHub" => 0.1, // GitHub events are well-structured
            "Azure" => 0.05,
            "Teams" => -0.05, // Teams content can be ambiguous
            _ => 0.0
        };

        return Math.Min(confidence, 1.0);
    }

    private string GenerateReasoningPath(PlatformEvent platformEvent, string category, EventPriority priority)
    {
        return $"Platform: {platformEvent.SourcePlatform} → Event: {platformEvent.EventType} → Action: {platformEvent.Action} → Category: {category} → Priority: {priority}";
    }

    private string GenerateReynoldsInsight(PlatformEvent platformEvent, string category, EventPriority priority)
    {
        return priority switch
        {
            EventPriority.Critical => "This needs my immediate attention. Maximum Effort™ mode activated!",
            EventPriority.High => "High priority event detected. Time for some Reynolds-style intervention.",
            EventPriority.Medium => "Moderate priority. I'll handle this with my usual supernatural efficiency.",
            EventPriority.Low => "Low priority, but still gets the Reynolds treatment. No detail too small!",
            _ => "Even Reynolds needs to classify the unclassifiable sometimes."
        };
    }
}

// Supporting classes for event classification
public class EventClassification
{
    public string Category { get; set; } = "";
    public EventPriority Priority { get; set; }
    public List<string> Keywords { get; set; } = new();
    public double UrgencyScore { get; set; }
    public double Confidence { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string ReasoningPath { get; set; } = "";
    public string ReynoldsInsight { get; set; } = "";
}

public class EventClassificationRule
{
    public string Category { get; set; } = "";
    public string[] PlatformPatterns { get; set; } = Array.Empty<string>();
    public string[] EventTypePatterns { get; set; } = Array.Empty<string>();
    public string[] ContentPatterns { get; set; } = Array.Empty<string>();
    public Func<Dictionary<string, object>, bool> MetadataChecks { get; set; } = _ => true;

    public async Task<bool> MatchesAsync(PlatformEvent platformEvent)
    {
        await Task.CompletedTask;
        
        // Check platform patterns
        if (PlatformPatterns.Any() && !PlatformPatterns.Contains(platformEvent.SourcePlatform.ToLowerInvariant()))
            return false;

        // Check event type patterns
        if (EventTypePatterns.Any() && !EventTypePatterns.Any(pattern => 
            platformEvent.EventType.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            return false;

        // Check content patterns
        if (ContentPatterns.Any() && !string.IsNullOrEmpty(platformEvent.Content))
        {
            var content = platformEvent.Content.ToLowerInvariant();
            if (!ContentPatterns.Any(pattern => content.Contains(pattern)))
                return false;
        }

        // Check metadata conditions
        if (!MetadataChecks(platformEvent.Metadata))
            return false;

        return true;
    }
}

public class EnhancedClassification
{
    public string Category { get; set; } = "";
    public EventPriority Priority { get; set; }
    public double Confidence { get; set; }
}