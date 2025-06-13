#!/bin/bash

# Reynolds Teams Integration Deployment Script
# Because even Reynolds needs proper deployment to slide into DMs

set -e

echo "🎭 Reynolds Teams Deployment Starting..."
echo "Maximum Effort™ on getting this bot operational!"

# Configuration
RESOURCE_GROUP="copilot-powerplatform-rg"
CONTAINER_APP_NAME="github-copilot-bot"
ACR_NAME="copilotplatformacr"
IMAGE_TAG="reynolds-teams-latest"

# Check if we're in the right directory
if [ ! -f "src/CopilotAgent/CopilotAgent.csproj" ]; then
    echo "❌ Please run this script from the project root directory"
    exit 1
fi

# Build the container with Teams integration
echo "📦 Building Reynolds with Teams capabilities..."
docker build -t $ACR_NAME.azurecr.io/copilot-agent:$IMAGE_TAG \
    --build-arg ENABLE_TEAMS_INTEGRATION=true \
    -f src/CopilotAgent/Dockerfile .

# Push to ACR
echo "🚀 Pushing Reynolds to Azure Container Registry..."
az acr login --name $ACR_NAME
docker push $ACR_NAME.azurecr.io/copilot-agent:$IMAGE_TAG

# Update container app with Teams integration
echo "🔄 Updating Container App with Reynolds Teams integration..."
az containerapp update \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --image $ACR_NAME.azurecr.io/copilot-agent:$IMAGE_TAG \
    --set-env-vars \
        EnableTeamsIntegration=true \
        ASPNETCORE_ENVIRONMENT=Production \
        REYNOLDS_ORG_MODE=true \
        TARGET_ORGANIZATION=dynamicstms365

# Get the updated URL
echo "🎯 Getting Reynolds endpoint URL..."
FQDN=$(az containerapp show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query "properties.configuration.ingress.fqdn" \
    -o tsv)

if [ -z "$FQDN" ]; then
    echo "❌ Failed to get container app FQDN"
    exit 1
fi

REYNOLDS_URL="https://$FQDN"
echo "✅ Reynolds deployed at: $REYNOLDS_URL"

# Test Reynolds health
echo "🏥 Testing Reynolds health..."
sleep 30  # Give it time to start up

if curl -f "$REYNOLDS_URL/health" > /dev/null 2>&1; then
    echo "✅ Reynolds is healthy and ready for organizational orchestration!"
    
    # Test Teams status
    if curl -f "$REYNOLDS_URL/api/health/teams" > /dev/null 2>&1; then
        echo "🎭 Reynolds Teams integration is operational!"
    else
        echo "⚠️  Reynolds is running but Teams integration needs configuration"
    fi
else
    echo "⚠️  Reynolds deployed but health check failed. Checking logs..."
    az containerapp logs show \
        --name $CONTAINER_APP_NAME \
        --resource-group $RESOURCE_GROUP \
        --tail 20
fi

echo ""
echo "🎪 Reynolds Teams Integration Endpoints:"
echo "   Status: $REYNOLDS_URL/api/reynolds/status"
echo "   Test Chat: $REYNOLDS_URL/api/reynolds/test-chat"
echo "   Teams Health: $REYNOLDS_URL/api/health/teams"
echo "   Teams Manifest: $REYNOLDS_URL/api/teams/manifest"
echo ""
echo "📋 Next Steps:"
echo "1. Configure Teams bot credentials in Azure Key Vault"
echo "2. Upload Teams app manifest to Microsoft Teams admin center"
echo "3. Test chat creation via the test endpoints"
echo ""
echo "Maximum Effort deployment complete! 🎭✨"