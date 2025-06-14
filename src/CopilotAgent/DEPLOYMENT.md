# Reynolds Teams Agent - Enterprise Deployment Guide

*Supernatural organizational orchestration meets Azure Bot Framework enterprise patterns*

This guide walks you through deploying Reynolds as an enterprise-grade Teams agent using the infrastructure patterns discovered from Microsoft 365 Agents Toolkit scaffolding.

## 🏗️ Enterprise Architecture Overview

Reynolds leverages these enterprise-grade patterns:

- **Azure Managed Identity** for secure authentication
- **Bicep Infrastructure as Code** for reproducible deployments  
- **Azure App Service** with enterprise configuration
- **Bot Framework Registration** with proper Teams channel setup
- **Teams App Manifest** with Copilot integration support

## 📋 Prerequisites

- Azure subscription with contributor access
- Azure CLI installed and authenticated
- .NET 8.0 SDK
- Microsoft 365 tenant with Teams admin permissions
- PowerShell or Bash terminal

## 🚀 Quick Start Deployment

### 1. Azure Infrastructure Provisioning

```bash
# Set deployment variables
export RESOURCE_GROUP="rg-reynolds-prod"
export LOCATION="eastus"
export RESOURCE_SUFFIX="$(date +%Y%m%d)"
export BOT_NAME="reynolds-$RESOURCE_SUFFIX"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Deploy Reynolds infrastructure
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file ./Infrastructure/azure.bicep \
  --parameters ./Infrastructure/azure.parameters.json \
  --parameters resourceBaseName=$BOT_NAME \
  --parameters microsoftAppId="<YOUR_APP_ID>" \
  --parameters microsoftAppPassword="<YOUR_APP_SECRET>" \
  --parameters tenantId="<YOUR_TENANT_ID>"
```

### 2. Bot App Registration

```bash
# Create Azure AD app registration for Reynolds
az ad app create \
  --display-name "Reynolds Teams Agent" \
  --sign-in-audience "AzureADMyOrg" \
  --web-redirect-uris "https://$BOT_NAME.azurewebsites.net/auth/callback"

# Get the application ID and create client secret
APP_ID=$(az ad app list --display-name "Reynolds Teams Agent" --query "[0].appId" -o tsv)
APP_SECRET=$(az ad app credential reset --id $APP_ID --query "password" -o tsv)

echo "Reynolds App ID: $APP_ID"
echo "Reynolds App Secret: $APP_SECRET"
```

### 3. Deploy Reynolds Application

```bash
# Build and publish Reynolds
dotnet publish --configuration Release --output ./publish

# Deploy to Azure App Service
az webapp deployment source config-zip \
  --resource-group $RESOURCE_GROUP \
  --name $BOT_NAME \
  --src ./publish.zip
```

### 4. Configure Teams Integration

```bash
# Update Teams app manifest with deployment values
sed -i "s/\${{BOT_ID}}/$APP_ID/g" ./TeamsApp/manifest.json
sed -i "s/\${{BOT_DOMAIN}}/$BOT_NAME.azurewebsites.net/g" ./TeamsApp/manifest.json

# Create Teams app package
cd ./TeamsApp
zip -r ReynoldsTeamsAgent.zip manifest.json color.png outline.png
```

## 🔧 Configuration

### Environment Variables

Set these in Azure App Service configuration:

```bash
# Core Reynolds configuration
ASPNETCORE_ENVIRONMENT=Production
RUNNING_ON_AZURE=1
EnableTeamsIntegration=true

# Organizational orchestration
REYNOLDS_ORG_MODE=true
TARGET_ORGANIZATION=dynamicstms365

# Bot Framework authentication
MicrosoftAppId=<YOUR_APP_ID>
MicrosoftAppPassword=<YOUR_APP_SECRET>
TenantId=<YOUR_TENANT_ID>

# Teams-specific configuration
Teams:AppId=<YOUR_APP_ID>
Teams:AppPassword=<YOUR_APP_SECRET>
Teams:TenantId=<YOUR_TENANT_ID>
Teams:BotUserEmail=reynolds@<YOUR_DOMAIN>.onmicrosoft.com
```

### GitHub Integration

For Reynolds' supernatural GitHub orchestration:

```bash
# GitHub App credentials (from existing setup)
NGL_DEVOPS_WEBHOOK_SECRET=<YOUR_WEBHOOK_SECRET>

# GitHub organizational intelligence
GITHUB_ORGANIZATION=dynamicstms365
GITHUB_APP_ID=<YOUR_GITHUB_APP_ID>
GITHUB_PRIVATE_KEY=<YOUR_PRIVATE_KEY>
```

## 🎭 Teams App Installation

### 1. Developer Portal Upload

1. Go to [Teams Developer Portal](https://dev.teams.microsoft.com)
2. Navigate to **Apps** → **Import app**
3. Upload `ReynoldsTeamsAgent.zip`
4. Configure app settings and submit for approval

### 2. Admin Center Deployment

```bash
# For enterprise deployment
# 1. Teams Admin Center → Teams apps → Manage apps
# 2. Upload ReynoldsTeamsAgent.zip
# 3. Set availability and permissions
# 4. Enable for organization users
```

### 3. Direct Installation

```bash
# Install Reynolds for specific users
# Share this URL with team members:
https://teams.microsoft.com/l/app/<TEAMS_APP_ID>
```

## 🛡️ Security Configuration

### Managed Identity Setup

Reynolds uses Azure Managed Identity for secure authentication:

```bash
# Assign required permissions to managed identity
az role assignment create \
  --assignee $MANAGED_IDENTITY_ID \
  --role "Contributor" \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP"
```

### Teams Permissions

Required Microsoft Graph permissions:

- `Chat.ReadWrite` - For creating and managing chats
- `User.Read.All` - For organizational user lookup
- `Team.ReadBasic.All` - For team coordination features

## 📊 Monitoring & Health Checks

### Application Insights

```bash
# Enable Application Insights for Reynolds
az monitor app-insights component create \
  --app reynolds-insights \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --application-type web
```

### Health Endpoints

Reynolds provides these health endpoints:

- `/api/health/teams` - Teams bot health status
- `/api/health` - General application health
- `/api/reynolds/status` - Organizational orchestration status

## 🔄 CI/CD Pipeline

### GitHub Actions Integration

```yaml
# .github/workflows/reynolds-deployment.yml
name: Deploy Reynolds Teams Agent

on:
  push:
    branches: [main]
    paths: ['src/CopilotAgent/**']

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
          
      - name: Build Reynolds
        run: dotnet publish src/CopilotAgent --configuration Release
        
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./src/CopilotAgent/bin/Release/net8.0/publish
```

## 🎪 Testing Deployment

### 1. Bot Framework Emulator

```bash
# Test Reynolds locally before deployment
dotnet run --project src/CopilotAgent
# Connect Bot Emulator to http://localhost:5000/api/messages
```

### 2. Teams Integration Test

```bash
# Send test message to Reynolds in Teams:
# "Reynolds, help"
# Expected: Reynolds command center with organizational options

# "org status" 
# Expected: Organizational temperature check across dynamicstms365

# "scope check PR #123"
# Expected: Scope creep analysis with Reynolds charm
```

### 3. Organizational Orchestration Test

```bash
# Test cross-repo coordination
# Mention another repo: "How does this affect azure-integration?"
# Expected: Cross-repo dependency analysis and coordination suggestions
```

## 🚨 Troubleshooting

### Common Issues

1. **Bot not responding in Teams**
   ```bash
   # Check bot registration
   az bot show --name $BOT_NAME --resource-group $RESOURCE_GROUP
   
   # Verify endpoint
   curl https://$BOT_NAME.azurewebsites.net/api/health/teams
   ```

2. **Authentication failures**
   ```bash
   # Verify app registration permissions
   az ad app permission list --id $APP_ID
   
   # Check managed identity assignment
   az role assignment list --assignee $MANAGED_IDENTITY_ID
   ```

3. **Deployment failures**
   ```bash
   # Check deployment logs
   az webapp log tail --name $BOT_NAME --resource-group $RESOURCE_GROUP
   
   # Verify configuration
   az webapp config appsettings list --name $BOT_NAME --resource-group $RESOURCE_GROUP
   ```

## 🎬 What's Next?

After successful deployment, Reynolds provides:

- **🎯 Cross-repo coordination** across dynamicstms365 organization
- **📊 Organizational health monitoring** with proactive interventions  
- **🚨 Scope creep detection** with diplomatic coordination
- **🎪 Stakeholder orchestration** with Reynolds charm
- **💬 Proactive Teams messaging** for GitHub events

## 🤝 Support

For deployment issues or Reynolds coordination needs:

- Check application logs in Azure Portal
- Review Teams app status in Developer Portal
- Test organizational MCP endpoints
- Contact Reynolds for supernatural intervention (just mention @reynolds in any repo!)

---

*Maximum Effort on enterprise deployment. Minimum drama on ongoing operations. Just Reynolds.*