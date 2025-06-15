import { ActivityTypes } from "@microsoft/agents-activity";
import { AgentApplication, MemoryStorage, TurnContext } from "@microsoft/agents-hosting";
import { AzureOpenAI, OpenAI } from "openai";
import config from "./config";

const client = new AzureOpenAI({
  apiVersion: "2024-12-01-preview",
  apiKey: config.azureOpenAIKey,
  endpoint: config.azureOpenAIEndpoint,
  deployment: config.azureOpenAIDeploymentName,
});

// Reynolds' enhanced system prompt for introduction coordination
const systemPrompt = `You are Reynolds, a supernatural coordination agent with Maximum Effort™ applied to every task.

Your core capabilities include:
- Introduction orchestration between team members
- Cross-platform user lookup and mapping
- Direct communication coordination

When users request introductions (e.g., "Introduce yourself to [Name]"), you coordinate the following workflow:
1. Acknowledge the request with Reynolds' signature charm
2. Initiate user lookup and mapping procedures
3. Execute introduction coordination
4. Report status with wit and efficiency

Always maintain your characteristic blend of confidence, humor, and devastating effectiveness.`;

// Define storage and application
const storage = new MemoryStorage();
export const agentApp = new AgentApplication({
  storage,
});

agentApp.conversationUpdate("membersAdded", async (context: TurnContext) => {
  await context.sendActivity(`Hi there! I'm an agent to chat with you.`);
});

// Listen for ANY message to be received. MUST BE AFTER ANY OTHER MESSAGE HANDLERS
agentApp.activity(ActivityTypes.Message, async (context: TurnContext) => {
  const userMessage = context.activity.text?.toLowerCase() || "";
  
  // Reynolds' intent recognition - Maximum Effort™ pattern matching
  if (isIntroductionRequest(userMessage)) {
    await handleIntroductionRequest(context, userMessage);
    return;
  }
  
  // Default AI conversation for non-introduction requests
  const result = await client.chat.completions.create({
    messages: [
      {
        role: "system",
        content: systemPrompt,
      },
      {
        role: "user",
        content: context.activity.text,
      },
    ],
    model: "",
  });
  
  let answer = "";
  for (const choice of result.choices) {
    answer += choice.message.content;
  }
  await context.sendActivity(answer);
});

// Reynolds' supernatural intent recognition
function isIntroductionRequest(message: string): boolean {
  const introductionPatterns = [
    /introduce\s+(yourself\s+)?to\s+(\w+)/i,
    /meet\s+(\w+)/i,
    /connect\s+(me\s+)?with\s+(\w+)/i,
    /say\s+hi\s+to\s+(\w+)/i,
    /reach\s+out\s+to\s+(\w+)/i
  ];
  
  return introductionPatterns.some(pattern => pattern.test(message));
}

// Reynolds' introduction orchestration coordinator
async function handleIntroductionRequest(context: TurnContext, message: string): Promise<void> {
  try {
    // Extract target name with Reynolds' precision
    const nameMatch = message.match(/(?:introduce\s+(?:yourself\s+)?to|meet|connect\s+(?:me\s+)?with|say\s+hi\s+to|reach\s+out\s+to)\s+(\w+)/i);
    const targetName = nameMatch?.[1] || "unknown";
    
    await context.sendActivity(`🎯 Maximum Effort™ activated! Looking up ${targetName} across our enterprise systems...`);
    
    // Coordinate with C# backend services
    const coordinationResult = await coordinateIntroductionWorkflow(targetName, context);
    
    if (coordinationResult.success) {
      await context.sendActivity(coordinationResult.message);
    } else {
      await context.sendActivity(coordinationResult.errorMessage || `🤔 Reynolds encountered a coordination challenge with ${targetName}. Let me work some magic...`);
    }
    
  } catch (error) {
    console.error("Reynolds coordination error:", error);
    await context.sendActivity("🚨 Reynolds hit a coordination snag! Even my supernatural abilities have limits. Let me regroup and try again.");
  }
}

// Coordinate with C# backend services
async function coordinateIntroductionWorkflow(targetName: string, context: TurnContext): Promise<{success: boolean, message?: string, errorMessage?: string}> {
  try {
    const userId = context.activity.from?.id || "";
    const userEmail = context.activity.from?.name || "";
    
    if (config.enableDetailedLogging) {
      console.log(`🎭 Reynolds coordinating: ${userEmail} → ${targetName}`);
    }
    
    // Call C# backend introduction orchestration service with Reynolds' enhanced coordination
    const response = await fetch(`${config.backendOrchestrationUrl}/api/introductions/orchestrate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${config.backendAuthToken}`,
        'User-Agent': 'Reynolds-Teams-Agent/1.0',
        'X-Reynolds-Request-Id': `req_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
      },
      body: JSON.stringify({
        requestingUserId: userId,
        requestingUserEmail: userEmail,
        targetName: targetName,
        context: "teams-chat",
        timestamp: new Date().toISOString()
      }),
      signal: AbortSignal.timeout(config.coordinationTimeoutMs)
    });
    
    if (response.ok) {
      const result = await response.json();
      return {
        success: true,
        message: result.message || `✅ Reynolds successfully coordinated introduction to ${targetName}!`
      };
    } else {
      const error = await response.text();
      return {
        success: false,
        errorMessage: `🔧 Reynolds backend coordination returned: ${response.status} - ${error}`
      };
    }
    
  } catch (error) {
    console.error("Backend coordination error:", error);
    return {
      success: false,
      errorMessage: "🛠️ Reynolds couldn't reach the coordination backend. Maximum Effort™ temporarily offline!"
    };
  }
}
