#!/bin/bash

# Force Azure Container Apps Configuration Update
# This script forces configuration of environment variables even if secrets exist

set -e

echo "🎭 Reynolds FORCE Configuration Update - Maximum Effort™"
echo "🔧 Applying environment variables to Azure Container Apps..."

RESOURCE_GROUP="copilot-powerplatform-deploy-rg"
CONTAINER_APP_NAME="github-copilot-bot"

# Force configuration update with current environment variables
echo "⚡ Forcing container app environment variable update..."

az containerapp update \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --set-env-vars \
    NGL_DEVOPS_APP_ID="${NGL_DEVOPS_APP_ID:-}" \
    NGL_DEVOPS_INSTALLATION_ID="${NGL_DEVOPS_INSTALLATION_ID:-}" \
    NGL_DEVOPS_PRIVATE_KEY="secretref:ngl-devops-private-key" \
    NGL_DEVOPS_WEBHOOK_SECRET="secretref:ngl-devops-webhook-secret" \
    COPILOT_VERSION="1.17.29" \
  --cpu 1.0 \
  --memory 2.0Gi \
  --min-replicas 1 \
  --max-replicas 3

echo "✅ Configuration update completed!"
echo "🔍 Checking container app status..."

# Wait for update to complete
sleep 30

echo "🧪 Testing environment variables in container..."
echo "📋 Check container logs to verify environment variables are now set correctly"