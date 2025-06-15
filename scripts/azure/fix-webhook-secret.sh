#!/bin/bash

# Quick Fix for Production Webhook Secret Issue
# Maximum Effort™ surgical strike to resolve webhook authentication

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m' # No Color

# Configuration
CONTAINER_APP_NAME="github-copilot-bot"
RESOURCE_GROUP="copilot-powerplatform-deploy-rg"

echo -e "${PURPLE}🎭 Reynolds Webhook Secret Emergency Fix${NC}"
echo -e "${PURPLE}=======================================${NC}"
echo ""

# Function to print status with formatting
print_status() {
    local status=$1
    local message=$2
    case $status in
        "SUCCESS")
            echo -e "${GREEN}✅ $message${NC}"
            ;;
        "ERROR")
            echo -e "${RED}❌ $message${NC}"
            ;;
        "WARNING")
            echo -e "${YELLOW}⚠️  $message${NC}"
            ;;
        "INFO")
            echo -e "${BLUE}ℹ️  $message${NC}"
            ;;
        "PROGRESS")
            echo -e "${PURPLE}🔄 $message${NC}"
            ;;
    esac
}

# Check prerequisites
if ! command -v az &> /dev/null; then
    print_status "ERROR" "Azure CLI is not installed"
    exit 1
fi

if ! az account show &> /dev/null; then
    print_status "ERROR" "Azure CLI is not authenticated - run 'az login' first"
    exit 1
fi

print_status "SUCCESS" "Azure CLI is available and authenticated"

# Check if required environment variables are set
if [[ -z "${NGL_DEVOPS_WEBHOOK_SECRET}" ]]; then
    print_status "ERROR" "NGL_DEVOPS_WEBHOOK_SECRET environment variable is required"
    print_status "INFO" "Export this variable with your GitHub webhook secret before running this script"
    exit 1
fi

if [[ -z "${NGL_DEVOPS_BOT_PEM}" ]]; then
    print_status "ERROR" "NGL_DEVOPS_BOT_PEM environment variable is required"
    print_status "INFO" "Export this variable with your GitHub App private key before running this script"
    exit 1
fi

print_status "SUCCESS" "Required environment variables are set"

# Check if container app exists
print_status "PROGRESS" "Checking if container app exists..."
if ! az containerapp show --name "$CONTAINER_APP_NAME" --resource-group "$RESOURCE_GROUP" > /dev/null 2>&1; then
    print_status "ERROR" "Container app '$CONTAINER_APP_NAME' not found in resource group '$RESOURCE_GROUP'"
    exit 1
fi

print_status "SUCCESS" "Container app found"

# Get current container app status
CONTAINER_STATUS=$(az containerapp show --name "$CONTAINER_APP_NAME" --resource-group "$RESOURCE_GROUP" --query "properties.provisioningState" -o tsv)
print_status "INFO" "Current container app status: $CONTAINER_STATUS"

# Set secrets in container app
print_status "PROGRESS" "Setting webhook secret in container app..."
az containerapp secret set \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --secrets "ngl-devops-webhook-secret=$NGL_DEVOPS_WEBHOOK_SECRET" \
               "ngl-devops-private-key=$NGL_DEVOPS_BOT_PEM"

if [ $? -eq 0 ]; then
    print_status "SUCCESS" "Secrets configured successfully"
else
    print_status "ERROR" "Failed to configure secrets"
    exit 1
fi

# Update container app environment variables
print_status "PROGRESS" "Updating container app environment variables..."
az containerapp update \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --set-env-vars "NGL_DEVOPS_WEBHOOK_SECRET=secretref:ngl-devops-webhook-secret" \
                   "NGL_DEVOPS_BOT_PEM=secretref:ngl-devops-private-key"

if [ $? -eq 0 ]; then
    print_status "SUCCESS" "Environment variables updated successfully"
else
    print_status "ERROR" "Failed to update environment variables"
    exit 1
fi

# Wait for container app to restart
print_status "PROGRESS" "Waiting for container app to restart and become ready..."
sleep 30

# Check if container app is running
MAX_ATTEMPTS=10
ATTEMPT=1

while [ $ATTEMPT -le $MAX_ATTEMPTS ]; do
    print_status "PROGRESS" "Checking container status (attempt $ATTEMPT/$MAX_ATTEMPTS)..."
    
    STATUS=$(az containerapp show --name "$CONTAINER_APP_NAME" --resource-group "$RESOURCE_GROUP" --query "properties.provisioningState" -o tsv)
    
    if [[ "$STATUS" == "Succeeded" ]]; then
        print_status "SUCCESS" "Container app is running successfully"
        break
    elif [[ "$STATUS" == "Failed" ]]; then
        print_status "ERROR" "Container app deployment failed"
        exit 1
    else
        print_status "INFO" "Container status: $STATUS, waiting..."
        sleep 15
        ATTEMPT=$((ATTEMPT + 1))
    fi
done

if [ $ATTEMPT -gt $MAX_ATTEMPTS ]; then
    print_status "WARNING" "Container status check timed out, but configuration was applied"
fi

# Get container FQDN for testing
FQDN=$(az containerapp show --name "$CONTAINER_APP_NAME" --resource-group "$RESOURCE_GROUP" --query "properties.configuration.ingress.fqdn" -o tsv)
print_status "INFO" "Container FQDN: $FQDN"

# Test endpoints
print_status "PROGRESS" "Testing endpoints..."

# Test health endpoint
if curl -f "https://$FQDN/api/github/health" > /dev/null 2>&1; then
    print_status "SUCCESS" "Health endpoint is responding"
else
    print_status "WARNING" "Health endpoint is not responding yet"
fi

# Test webhook endpoint (should return 401 for unsigned requests)
WEBHOOK_STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "https://$FQDN/api/github/webhook" 2>/dev/null || echo "000")
if [[ "$WEBHOOK_STATUS" == "401" ]]; then
    print_status "SUCCESS" "Webhook endpoint is properly rejecting unsigned requests (401)"
elif [[ "$WEBHOOK_STATUS" == "400" ]]; then
    print_status "SUCCESS" "Webhook endpoint is properly configured (400)"
else
    print_status "WARNING" "Webhook endpoint returned unexpected status: $WEBHOOK_STATUS"
fi

echo ""
print_status "SUCCESS" "🎉 Webhook secret configuration completed!"
echo ""
print_status "INFO" "Next steps:"
echo "  1. Update GitHub App webhook URL to: https://$FQDN/api/github/webhook"
echo "  2. Test webhook delivery from GitHub repository settings"
echo "  3. Monitor logs: az containerapp logs show --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP --follow"
echo ""
print_status "SUCCESS" "Maximum Effort™ applied successfully!"