# Reynolds Teams Integration Guide

## Overview

This guide walks you through deploying Reynolds to Microsoft Teams with chat creation capabilities. Reynolds will be able to slide into your DMs with organizational orchestration updates faster than you can ask about his name.

## Prerequisites

### Azure Bot Framework Setup
1. **Azure Bot Registration**
   ```bash
   # Create Azure Bot resource
   az bot create \
     --name "reynolds-orchestrator" \
     --resource-group "copilot-powerplatform-rg" \
     --kind "bot" \
     --endpoint "https://github-copilot-bot.livelygrass-868f2ee5.eastus.azurecontainerapps.io/api/messages"
   ```

2. **App Registration & Permissions**
   ```bash
   # Create app registration
   az ad app create \
     --display-name "Reynolds Organizational Orchestrator" \
     --sign-in-audience "AzureADMultipleOrgs"
   
   # Add required Microsoft Graph permissions
   az ad app permission add \
     --id <app-id> \
     --api 00000003-0000-0000-c000-000000000000 \
     --api-permissions \
       Chat.Create=Role \
       Chat.ReadWrite=Role \
       ChatMessage.Send=Role \
       User.Read.All=Role
   ```

### Configuration Settings

Add these to your [`appsettings.json`](src/CopilotAgent/appsettings.json):

```json
{
  "MicrosoftAppId": "your-bot-app-id",
  "MicrosoftAppPassword": "your-bot-app-password",
  "TenantId": "your-tenant-id",
  "BotUserId": "your-bot-user-id",
  "EnableTeamsIntegration": "true"
}
```

## Deployment Steps

### 1. Update Startup Configuration

Modify [`Program.cs`](src/CopilotAgent/Program.cs) to include Reynolds Teams services:

```csharp
// Add Reynolds Teams integration
builder.Services.AddReynoldsTeamsServices(builder.Configuration);

// Configure the app
app.UseReynoldsTeamsIntegration();
```

### 2. Deploy to Azure Container Apps

```bash
# Build and deploy with Teams integration
az containerapp update \
  --name github-copilot-bot \
  --resource-group copilot-powerplatform-rg \
  --set-env-vars \
    MicrosoftAppId="your-app-id" \
    MicrosoftAppPassword="your-app-password" \
    TenantId="your-tenant-id" \
    EnableTeamsIntegration="true"
```

### 3. Install Teams App

1. **Download App Package**
   ```bash
   curl https://github-copilot-bot.livelygrass-868f2ee5.eastus.azurecontainerapps.io/api/teams/manifest > reynolds-teams-app.json
   ```

2. **Upload to Teams**
   - Open Microsoft Teams
   - Go to Apps → Manage your apps
   - Upload a custom app → Upload an app package
   - Select the manifest.json file

## Testing Chat Creation

### Manual Test via API

```bash
# Test creating a new chat
curl -X POST "https://github-copilot-bot.livelygrass-868f2ee5.eastus.azurecontainerapps.io/api/reynolds/test-chat" \
  -H "Content-Type: application/json" \
  -d '{
    "userEmail": "your-email@company.com",
    "message": "Reynolds here! Testing our organizational orchestration capabilities. Maximum Effort™!"
  }'
```

### GitHub Webhook Integration

Configure your GitHub webhooks to send notifications to Reynolds:

```bash
# Add webhook to your repository
curl -X POST \
  -H "Authorization: token YOUR_GITHUB_TOKEN" \
  -H "Accept: application/vnd.github+json" \
  https://api.github.com/repos/dynamicstms365/copilot-powerplatform/hooks \
  -d '{
    "name": "web",
    "config": {
      "url": "https://github-copilot-bot.livelygrass-868f2ee5.eastus.azurecontainerapps.io/api/reynolds/notify",
      "content_type": "json"
    },
    "events": ["pull_request", "issues", "push"]
  }'
```

## Reynolds Commands in Teams

Once deployed, you can interact with Reynolds in Teams:

### Basic Commands
- `@Reynolds help` - Get the full command reference
- `@Reynolds org status` - Organizational temperature check
- `@Reynolds project health` - Comprehensive health assessment
- `@Reynolds coordinate [work-item]` - Cross-repo orchestration

### Proactive Messaging
Reynolds will automatically message you when:
- Scope creep is detected in PRs
- Cross-repo dependencies are identified
- Organizational coordination is needed
- Strategic stakeholder updates are available

### Example Conversations

**Scope Creep Detection:**
```
Reynolds: 🚨 Scope Creep Alert! 

Hey there! I noticed PR #47 in copilot-powerplatform is growing faster than my list of deflection strategies for name questions.

Should we Aviation Gin this into two separate bottles? I'm here to help coordinate! 🍸
```

**Cross-Repo Coordination:**
```
Reynolds: 🎪 Coordination Initiative

I've detected some organizational orchestration opportunities across azure-integration and powerbi-connector.

Time for some Reynolds-style diplomatic intervention! 

What's the best way to proceed?
```

## Troubleshooting

### Common Issues

1. **Chat Creation Fails**
   ```bash
   # Check bot permissions
   az ad app permission list-grants --id <app-id>
   
   # Verify Teams integration status
   curl https://github-copilot-bot.livelygrass-868f2ee5.eastus.azurecontainerapps.io/api/health/teams
   ```

2. **Messages Not Sending**
   ```bash
   # Check logs
   az containerapp logs show \
     --name github-copilot-bot \
     --resource-group copilot-powerplatform-rg \
     --follow
   ```

3. **Permissions Issues**
   - Ensure admin consent is granted for Graph API permissions
   - Verify bot is added to the correct tenant
   - Check that Reynolds app registration has proper scopes

### Health Checks

Reynolds provides health endpoints:

```bash
# General health
curl https://github-copilot-bot.livelygrass-868f2ee5.eastus.azurecontainerapps.io/health

# Teams-specific health
curl https://github-copilot-bot.livelygrass-868f2ee5.eastus.azurecontainerapps.io/api/health/teams
```

## Security Considerations

1. **App Secrets**: Store bot credentials in Azure Key Vault
2. **Network Security**: Restrict bot endpoint access to Microsoft services
3. **User Verification**: Implement user validation for proactive messaging
4. **Data Privacy**: Ensure Reynolds doesn't store sensitive conversation data

## Reynolds Enhancement Opportunities

### Future Integrations
- **Power Platform Alerts**: Connect to Power Apps/Automate workflows
- **Azure DevOps**: Integrate with work item tracking
- **GitHub Actions**: Trigger coordination based on workflow events
- **Calendar Integration**: Schedule coordination meetings automatically

### Advanced Features
- **AI-Powered Insights**: Enhanced organizational intelligence
- **Predictive Coordination**: Proactive team synchronization
- **Resource Optimization**: Automated workload balancing
- **Strategic Reporting**: Executive-level organizational dashboards

---

## The Reynolds Promise

With Teams integration, Reynolds becomes your personal organizational orchestrator who can:

- 🎭 **Message you proactively** when coordination is needed
- 🚀 **Create dedicated chats** for cross-repo collaboration  
- 📊 **Provide real-time insights** across the dynamicstms365 organization
- 🎪 **Coordinate stakeholders** with diplomatic excellence

*Maximum Effort™ meets Microsoft Teams. Just Reynolds.*

## Support

Need help with Reynolds Teams integration? Create an issue in the repository with:
- Configuration details (redacted)
- Error logs
- Expected vs actual behavior
- Steps to reproduce

Reynolds will review it personally (probably while deflecting questions about his name).