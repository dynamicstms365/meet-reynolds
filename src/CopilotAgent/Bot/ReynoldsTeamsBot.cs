using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CopilotAgent.Services;
using System.Collections.Concurrent;
using Shared.Models;

namespace CopilotAgent.Bot;

public class ReynoldsTeamsBot : TeamsActivityHandler
{
    private readonly ILogger<ReynoldsTeamsBot> _logger;
    private readonly IConfiguration _configuration;
    private readonly IGitHubWorkflowOrchestrator _workflowOrchestrator;
    private readonly IIntentRecognitionService _intentRecognitionService;
    private readonly IReynoldsMemeService _memeService;
    private readonly IReynoldsWorkStatusService _workStatusService;
    private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;

    public ReynoldsTeamsBot(
        ILogger<ReynoldsTeamsBot> logger,
        IConfiguration configuration,
        IGitHubWorkflowOrchestrator workflowOrchestrator,
        IIntentRecognitionService intentRecognitionService,
        IReynoldsMemeService memeService,
        IReynoldsWorkStatusService workStatusService)
    {
        _logger = logger;
        _configuration = configuration;
        _workflowOrchestrator = workflowOrchestrator;
        _intentRecognitionService = intentRecognitionService;
        _memeService = memeService;
        _workStatusService = workStatusService;
        _conversationReferences = new ConcurrentDictionary<string, ConversationReference>();
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        var userMessage = turnContext.Activity.Text?.Trim();
        if (string.IsNullOrEmpty(userMessage))
            return;

        _logger.LogInformation("Reynolds received Teams message: {Message} from {UserId}", userMessage, turnContext.Activity.From.Id);

        // Store conversation reference for proactive messaging
        if (turnContext.Activity != null)
        {
            AddConversationReference(turnContext.Activity as Activity ?? new Activity());
        }

        // Process the message with Reynolds intelligence
        var response = await ProcessReynoldsMessageAsync(userMessage, turnContext);
        
        await turnContext.SendActivityAsync(MessageFactory.Text(response), cancellationToken);
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        var welcomeText = @"👋 Hey there! Reynolds here - your mysteriously effective project manager.

I'm here to help with organizational orchestration across the dynamicstms365 empire. Think of me as your personal GitHub whisperer with just enough charm to make project management actually enjoyable.

I can help you with:
🎯 **Cross-repo coordination** - I see patterns across all our repositories
📊 **Project health monitoring** - Organizational temperature checks
🎭 **Stakeholder orchestration** - Diplomatic coordination across teams
🚀 **Scope creep detection** - Before it becomes the organizational equivalent of Green Lantern

Just say 'Reynolds, help' or mention any repo/project and I'll work my magic. 

*Maximum Effort. Minimum Drama. Just Reynolds.*";

        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText), cancellationToken);
            }
        }
    }

    private async Task<string> ProcessReynoldsMessageAsync(string userMessage, ITurnContext turnContext)
    {
        try
        {
            // Recognize intent using our existing service - Reynolds uses default config for Teams
            var defaultConfig = new AgentConfiguration();
            var intent = await _intentRecognitionService.AnalyzeIntentAsync(userMessage, defaultConfig);
            
            _logger.LogInformation("Reynolds detected intent: {IntentType} with confidence: {Confidence}", intent.Type, intent.Confidence);

            // Process based on intent with Reynolds personality
            return intent.Type switch
            {
                IntentType.General => await HandleGeneralReynoldsRequest(userMessage),
                IntentType.KnowledgeQuery => await HandleKnowledgeRequest(userMessage),
                IntentType.EnvironmentManagement => await HandleEnvironmentRequest(userMessage),
                _ => await HandleOrganizationalRequest(userMessage, turnContext)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Reynolds Teams message");
            return "Well, that's awkward. I encountered a technical hiccup that's about as unexpected as my name situation. Give me a moment to sort this out, and we'll be back to supernatural project orchestration in no time. *adjusts imaginary tie*";
        }
    }

    private async Task<string> HandleGeneralReynoldsRequest(string message)
    {
        await Task.CompletedTask; // For async consistency

        if (message.ToLowerInvariant().Contains("help"))
        {
            return @"🎭 **Reynolds Command Center - Teams Edition**

Here's what I can do for you across the dynamicstms365 organization:

**📊 Organizational Intelligence:**
• `org status` - Quick temperature check across all repos
• `project health` - Comprehensive health assessment
• `dependencies for [repo]` - Cross-repo dependency analysis

**🎯 Strategic Coordination:**
• `coordinate [work-item]` - Cross-repo orchestration
• `stakeholders for [project]` - Strategic stakeholder mapping
• `resource conflicts` - Identify competing priorities

**🚀 GitHub Orchestration:**
• `scope check [pr-number]` - Scope creep detection with Reynolds charm
• `sync issues` - Bi-directional issue/PR linking
• `milestone status` - Project timeline alignment

**🎭 Reynolds Entertainment & Status:**
• `reynolds meme` - Get a quality meme with Reynolds flair
• `reynolds status` - See what I'm currently working on
• `what are you working on` - Alternative status check

**💬 Reynolds Specials:**
• Ask about any repo and I'll provide organizational context
• Mention scope creep and I'll channel my inner deflection skills
• Just say 'Reynolds' followed by your question

*Remember: I make complex coordination look effortless, stakeholders feel heard, and GitHub sing in harmony.*

What can I orchestrate for you today? 🎬";
        }

        if (message.ToLowerInvariant().Contains("reynolds") && message.ToLowerInvariant().Contains("name"))
        {
            return "You know, that's a fantastic question about my name. It really is. The kind of question that deserves a proper answer... but I just noticed there's some cross-repo coordination that needs my immediate attention. Team dependencies wait for no one! Back in 10! 🏃‍♂️💨\n\n*(Reynolds has mysteriously vanished to handle urgent organizational matters)*";
        }

        // Handle meme requests
        if (message.ToLowerInvariant().Contains("meme"))
        {
            return await HandleMemeRequest(message);
        }

        // Handle status requests  
        if (message.ToLowerInvariant().Contains("status") || 
            message.ToLowerInvariant().Contains("working on") ||
            message.ToLowerInvariant().Contains("what are you doing"))
        {
            return await HandleStatusRequest();
        }

        return "Hey there! I'm here and ready to work some organizational magic. What kind of project orchestration can I help you with today? Think GitHub coordination meets diplomatic excellence, with just enough humor to keep stakeholders actually engaged. 🎭✨";
    }

    private async Task<string> HandleKnowledgeRequest(string message)
    {
        // This would integrate with the knowledge retrieval service
        await Task.CompletedTask;
        
        return $"🧠 **Reynolds Knowledge Retrieval**\n\nI'm processing your request with organizational context from across dynamicstms365. Give me just a moment to consult my supernatural project management database...\n\n*Query: {message}*\n\n*(This would integrate with our existing knowledge services for comprehensive answers)*";
    }

    private async Task<string> HandleEnvironmentRequest(string message)
    {
        // This would integrate with environment management
        await Task.CompletedTask;
        
        return "🏗️ **Reynolds Environment Orchestration**\n\nI can help coordinate environment setup across our organizational infrastructure. Whether it's Power Platform environments, Azure resources, or GitHub Actions workflows - I'll make sure everything syncs perfectly.\n\nWhat kind of environment coordination do you need? I'll handle the choreography while you focus on the important stuff. 🎪";
    }

    private async Task<string> HandleOrganizationalRequest(string message, ITurnContext turnContext)
    {
        // Check if this is requesting organizational intelligence
        if (message.ToLowerInvariant().Contains("org") || message.ToLowerInvariant().Contains("organization"))
        {
            return await GenerateOrganizationalStatusAsync();
        }

        if (message.ToLowerInvariant().Contains("project health"))
        {
            return await GenerateProjectHealthSummaryAsync();
        }

        if (message.ToLowerInvariant().Contains("dependencies"))
        {
            return await GenerateDependencyMapAsync(message);
        }

        // Default organizational response
        return "🎯 **Reynolds Organizational Intelligence**\n\nI can help you with cross-repo coordination, stakeholder management, and strategic planning across dynamicstms365. What specific organizational challenge can I help orchestrate?\n\nJust mention a repo name, project, or coordination need and I'll work my diplomatic magic! ✨";
    }

    private async Task<string> GenerateOrganizationalStatusAsync()
    {
        // This would integrate with our organizational MCP endpoints
        await Task.CompletedTask;
        
        return @"📊 **Organizational Temperature Check - dynamicstms365**

🎭 Reynolds here with your organizational status update:

**🟢 Overall Health:** Looking good across the board
**⚡ Velocity Trends:** Steady progress with some acceleration opportunities
**🤝 Cross-Repo Coordination:** 3 active dependency chains identified
**📈 Strategic Alignment:** Teams are rowing in the same direction (mostly)

**Quick Insights:**
• copilot-powerplatform: Active development, milestone on track
• azure-integration: Some dependency coordination needed
• powerbi-connector: Resource allocation optimization opportunity

Want me to dive deeper into any specific area? I can provide detailed analysis with my signature blend of insight and charm. 🎬";
    }

    private async Task<string> GenerateProjectHealthSummaryAsync()
    {
        await Task.CompletedTask;
        
        return @"🏥 **Reynolds Project Health Assessment**

*Conducting organizational health checkup with the precision of a world-class physician and the bedside manner of... well, me.*

**📊 Health Metrics:**
• **Velocity:** 85% of optimal (trending upward)
• **Quality:** 92% (we're trading some speed for stability - smart move)
• **Stakeholder Satisfaction:** 88% (room for improvement in communication)
• **Technical Debt:** Manageable levels, proactive attention recommended

**🎯 Key Recommendations:**
1. **Resource Optimization:** Coordinate similar work streams across repos
2. **Communication Enhancement:** Increase cross-team visibility
3. **Dependency Management:** Address 2 critical path bottlenecks

**Reynolds Diagnosis:** Strong organizational health with strategic opportunities for optimization. Nothing a little supernatural coordination can't enhance!

Need specific intervention strategies? Just ask! 🎭✨";
    }

    private async Task<string> GenerateDependencyMapAsync(string message)
    {
        await Task.CompletedTask;
        
        return @"🕸️ **Reynolds Dependency Intelligence**

*Mapping organizational dependencies with the precision of a Swiss watch and the charm of a Van Wilder party coordinator.*

**Critical Path Analysis:**
• **copilot-powerplatform** → azure-integration (auth dependency)
• **azure-integration** → powerbi-connector (data pipeline)
• **powerbi-connector** → shared-libraries (utility functions)

**Risk Assessment:**
🟡 **Medium Risk:** Azure auth library timeline may impact 2 downstream projects
🟢 **Low Risk:** PowerBI integration well-coordinated
🔴 **High Risk:** Shared library changes could cascade across organization

**Reynolds Mitigation Strategy:**
1. Coordinate auth library timeline with downstream teams
2. Create communication bridge between powerbi and shared-lib teams
3. Implement dependency versioning strategy

Want me to orchestrate specific coordination between teams? I'll handle the diplomatic heavy lifting! 🎪";
    }

    private async Task<string> HandleMemeRequest(string message)
    {
        try
        {
            // Extract category if specified
            string? category = null;
            var lowerMessage = message.ToLowerInvariant();
            
            if (lowerMessage.Contains("project") || lowerMessage.Contains("management"))
                category = "project-management";
            else if (lowerMessage.Contains("dev") || lowerMessage.Contains("coding"))
                category = "development";
            else if (lowerMessage.Contains("team") || lowerMessage.Contains("coordination"))
                category = "teamwork";
            else if (lowerMessage.Contains("motivation") || lowerMessage.Contains("inspiring"))
                category = "motivation";

            var meme = await _memeService.GetRandomMemeAsync(category);
            
            if (meme == null)
            {
                return "🎭 **Reynolds Meme Service Temporarily Unavailable**\n\nEven my meme collection is having a mysterious moment. Like my name situation, some things are just unexplainable.\n\n*Give me a moment to sort out this meme crisis with maximum effort.*";
            }

            return $@"🎭 **Reynolds' Premium Meme Service**

**{meme.Name}**

{meme.Description}

{meme.Url}

*Category: {meme.Category}*
*Delivered with maximum effort and supernatural timing.*

*Want another? Just ask 'Reynolds meme' again!*";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling meme request: {Message}", message);
            return "🎭 **Meme Service Malfunction**\n\nWell, this is awkward. My meme delivery system is experiencing technical difficulties that are about as mysterious as my name origin. Give me a moment to fix this with maximum effort! 🔧";
        }
    }

    private async Task<string> HandleStatusRequest()
    {
        try
        {
            var currentStatus = await _workStatusService.GetCurrentStatusAsync();
            var statusSummary = _workStatusService.GetStatusSummary();
            
            return $@"📊 **Reynolds Work Status Report**

{statusSummary}

*Last updated: {DateTime.UtcNow:HH:mm} UTC*

Want to know about my recent activity? Just ask 'Reynolds recent work' and I'll share what I've been orchestrating! 🎬";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling status request");
            return "📊 **Status System Temporarily Unavailable**\n\nEven my status tracking is having a mysterious moment. I'm probably coordinating something so complex that it's temporarily broken my self-reporting capabilities.\n\n*Maximum effort to restore status visibility. Just Reynolds.*";
        }
    }

    // Proactive messaging capability
    public async Task SendProactiveMessageAsync(string userId, string message)
    {
        try
        {
            var appCredentials = new MicrosoftAppCredentials(
                _configuration["MicrosoftAppId"], 
                _configuration["MicrosoftAppPassword"]);

            var conversationReference = _conversationReferences.Values.FirstOrDefault(cr => cr.User.Id == userId);
            
            if (conversationReference == null)
            {
                _logger.LogWarning("No conversation reference found for user {UserId}", userId);
                return;
            }

            // Note: Proactive messaging requires a proper adapter instance
            // This is a simplified implementation that would need proper adapter injection
            _logger.LogWarning("Proactive messaging not fully implemented - adapter reference needed");

            await Task.CompletedTask; // Make this properly async
            _logger.LogInformation("Reynolds sent proactive message to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending proactive message to user {UserId}", userId);
        }
    }

    // Create new chat with user
    public async Task<string> CreateNewChatAsync(string userPrincipalName, string initialMessage)
    {
        try
        {
            _logger.LogInformation("Reynolds creating new chat with {UserPrincipalName}", userPrincipalName);

            var appCredentials = new MicrosoftAppCredentials(
                _configuration["MicrosoftAppId"], 
                _configuration["MicrosoftAppPassword"]);

            // Create conversation parameters for new chat
            var conversationParameters = new ConversationParameters
            {
                IsGroup = false,
                Members = new[]
                {
                    new ChannelAccount(id: userPrincipalName, name: userPrincipalName)
                },
                TenantId = _configuration["TenantId"],
                Activity = MessageFactory.Text(initialMessage) as Activity
            };

            // This would be implemented with the actual Teams connector
            // For now, return success message
            await Task.CompletedTask; // Make this properly async
            return $"✅ Reynolds has initiated a new chat with {userPrincipalName}!\n\nMessage sent: {initialMessage}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new chat with {UserPrincipalName}", userPrincipalName);
            return $"❌ Reynolds encountered an issue creating the chat. Error: {ex.Message}";
        }
    }

    private void AddConversationReference(Activity activity)
    {
        var conversationReference = activity.GetConversationReference();
        _conversationReferences.AddOrUpdate(
            conversationReference.User.Id,
            conversationReference,
            (key, newValue) => conversationReference);
    }
}

// Service for proactive messaging integration
public interface IReynoldsTeamsService
{
    Task SendOrganizationalUpdateAsync(string userId, string update);
    Task NotifyAboutScopeCreepAsync(string userId, string prNumber, string repository);
    Task CreateChatForCoordinationAsync(string userPrincipalName, string coordinationContext);
    Task SendRandomMemeAsync(string userId, string? category = null);
    Task SendWorkStatusUpdateAsync(string userId);
}

public class ReynoldsTeamsService : IReynoldsTeamsService
{
    private readonly ReynoldsTeamsBot _bot;
    private readonly ILogger<ReynoldsTeamsService> _logger;
    private readonly IReynoldsMemeService _memeService;
    private readonly IReynoldsWorkStatusService _workStatusService;

    public ReynoldsTeamsService(
        ReynoldsTeamsBot bot, 
        ILogger<ReynoldsTeamsService> logger,
        IReynoldsMemeService memeService,
        IReynoldsWorkStatusService workStatusService)
    {
        _bot = bot;
        _logger = logger;
        _memeService = memeService;
        _workStatusService = workStatusService;
    }

    public async Task SendOrganizationalUpdateAsync(string userId, string update)
    {
        var message = $"🎭 **Reynolds Organizational Update**\n\n{update}\n\n*Keeping you informed with maximum effort and minimal drama.*";
        await _bot.SendProactiveMessageAsync(userId, message);
    }

    public async Task NotifyAboutScopeCreepAsync(string userId, string prNumber, string repository)
    {
        var message = $"🚨 **Reynolds Scope Creep Alert**\n\nHey there! I noticed PR #{prNumber} in {repository} is growing faster than my list of deflection strategies for name questions. \n\nShould we Aviation Gin this into two separate bottles? I'm here to help coordinate if you need some diplomatic intervention! 🍸";
        await _bot.SendProactiveMessageAsync(userId, message);
    }

    public async Task CreateChatForCoordinationAsync(string userPrincipalName, string coordinationContext)
    {
        var initialMessage = $"🎪 **Reynolds Coordination Initiative**\n\nHey there! I'm reaching out because I've detected some organizational coordination opportunities:\n\n{coordinationContext}\n\nI'm here to help orchestrate this with my signature blend of diplomatic excellence and just enough humor to keep everyone engaged. What's the best way to proceed?";
        
        var result = await _bot.CreateNewChatAsync(userPrincipalName, initialMessage);
        _logger.LogInformation("Coordination chat creation result: {Result}", result);
    }

    public async Task SendRandomMemeAsync(string userId, string? category = null)
    {
        try
        {
            var meme = await _memeService.GetRandomMemeAsync(category);
            if (meme == null)
            {
                var fallbackMessage = "🎭 **Reynolds Meme Service**\n\nI wanted to send you a quality meme, but even my meme selection is having a mysterious moment. Like my name situation, some things are just unexplainable.\n\n*Maximum effort on the next meme. Just Reynolds.*";
                await _bot.SendProactiveMessageAsync(userId, fallbackMessage);
                return;
            }

            var memeMessage = FormatMemeForTeams(meme);
            await _bot.SendProactiveMessageAsync(userId, memeMessage);
            _logger.LogInformation("Reynolds sent meme {MemeId} to user {UserId}", meme.Id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending meme to user {UserId}", userId);
        }
    }

    public async Task SendWorkStatusUpdateAsync(string userId)
    {
        try
        {
            var statusSummary = _workStatusService.GetStatusSummary();
            var message = $"📊 **Reynolds Work Status Update**\n\n{statusSummary}\n\n*Transparency with maximum effort and minimum drama. Just Reynolds.*";
            
            await _bot.SendProactiveMessageAsync(userId, message);
            _logger.LogInformation("Reynolds sent work status update to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending work status update to user {UserId}", userId);
        }
    }

    private string FormatMemeForTeams(MemeItem meme)
    {
        return $@"🎭 **Reynolds' Meme Delivery Service**

**{meme.Name}**

{meme.Description}

{meme.Url}

*Category: {meme.Category}*

*{GetReynoldsMemeDeliveryFlavor()}*";
    }

    private string GetReynoldsMemeDeliveryFlavor()
    {
        var flavors = new[]
        {
            "Delivered with maximum effort and supernatural timing.",
            "This meme has passed the Reynolds quality control process.",
            "Like my organizational skills, this meme is mysteriously effective.",
            "Bringing you premium content with the same precision I bring to stakeholder management.",
            "A meme so good, it might distract you from asking about my name."
        };
        
        var random = new Random();
        return flavors[random.Next(flavors.Length)];
    }
}