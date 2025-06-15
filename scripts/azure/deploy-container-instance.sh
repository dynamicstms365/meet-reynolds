#!/bin/bash

# Azure Container Instance Deployment Script for GitHub App Service
# This script deploys the Copilot Agent as a container instance for webhook processing

set -e

# Configuration
RESOURCE_GROUP="copilot-powerplatform-deploy-rg"
CONTAINER_NAME="github-copilot-bot"
LOCATION="eastus"
IMAGE_NAME="copilot-agent:latest"
DNS_LABEL="copilot-github-app"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🚀 Deploying GitHub App Service to Azure Container Instance${NC}"
echo "=================================================="

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}❌ Azure CLI is not installed. Please install it first.${NC}"
    exit 1
fi

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    echo -e "${YELLOW}⚠️  Not logged in to Azure. Please run 'az login' first.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Azure CLI is available and logged in${NC}"

# Check for required environment variables
if [[ -z "${NGL_DEVOPS_APP_ID}" ]]; then
    echo -e "${RED}❌ NGL_DEVOPS_APP_ID environment variable is required${NC}"
    exit 1
fi

if [[ -z "${NGL_DEVOPS_PRIVATE_KEY}" ]]; then
    echo -e "${RED}❌ NGL_DEVOPS_PRIVATE_KEY environment variable is required${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Required environment variables are set${NC}"

# Create resource group if it doesn't exist
echo -e "${YELLOW}📦 Checking/creating resource group: ${RESOURCE_GROUP}${NC}"
az group create --name "${RESOURCE_GROUP}" --location "${LOCATION}" --output none

# Build and push Docker image (assumes Azure Container Registry)
echo -e "${YELLOW}🔨 Building Docker image${NC}"
docker build -t "${IMAGE_NAME}" -f src/CopilotAgent/Dockerfile .

# Get Azure Container Registry name (assumes it exists)
ACR_NAME=$(az acr list --resource-group "${RESOURCE_GROUP}" --query "[0].name" -o tsv 2>/dev/null || echo "")

if [[ -n "${ACR_NAME}" ]]; then
    echo -e "${YELLOW}📤 Pushing image to Azure Container Registry: ${ACR_NAME}${NC}"
    az acr build --registry "${ACR_NAME}" --image "${IMAGE_NAME}" .
    FULL_IMAGE_NAME="${ACR_NAME}.azurecr.io/${IMAGE_NAME}"
else
    echo -e "${YELLOW}⚠️  No Azure Container Registry found. Using local image.${NC}"
    FULL_IMAGE_NAME="${IMAGE_NAME}"
fi

# Deploy container instance
echo -e "${YELLOW}🚀 Deploying container instance: ${CONTAINER_NAME}${NC}"
az container create \
    --resource-group "${RESOURCE_GROUP}" \
    --name "${CONTAINER_NAME}" \
    --image "${FULL_IMAGE_NAME}" \
    --dns-name-label "${DNS_LABEL}" \
    --ports 80 443 \
    --environment-variables \
        "NGL_DEVOPS_APP_ID=${NGL_DEVOPS_APP_ID}" \
        "ASPNETCORE_ENVIRONMENT=Production" \
        "ASPNETCORE_URLS=http://+:80" \
    --secure-environment-variables \
        "NGL_DEVOPS_PRIVATE_KEY=${NGL_DEVOPS_PRIVATE_KEY}" \
        "NGL_DEVOPS_WEBHOOK_SECRET=${NGL_DEVOPS_WEBHOOK_SECRET}" \
    --cpu 1 \
    --memory 1.5 \
    --restart-policy Always \
    --output table

# Get the public IP and FQDN
echo -e "${GREEN}📡 Retrieving deployment information${NC}"
FQDN=$(az container show --resource-group "${RESOURCE_GROUP}" --name "${CONTAINER_NAME}" --query "ipAddress.fqdn" -o tsv)
IP=$(az container show --resource-group "${RESOURCE_GROUP}" --name "${CONTAINER_NAME}" --query "ipAddress.ip" -o tsv)

echo ""
echo -e "${GREEN}🎉 Deployment successful!${NC}"
echo "=================================================="
echo -e "${GREEN}📍 Container FQDN: ${FQDN}${NC}"
echo -e "${GREEN}📍 Container IP: ${IP}${NC}"
echo -e "${GREEN}🔗 Webhook URL: https://${FQDN}/api/github/webhook${NC}"
echo -e "${GREEN}🔗 Health Check: https://${FQDN}/api/github/health${NC}"
echo -e "${GREEN}🔗 Test Endpoint: https://${FQDN}/api/github/test${NC}"
echo ""
echo -e "${YELLOW}⚠️  Next Steps:${NC}"
echo "1. Update your GitHub App webhook URL to: https://${FQDN}/api/github/webhook"
echo "2. Test the webhook delivery from GitHub"
echo "3. Monitor container logs: az container logs --resource-group ${RESOURCE_GROUP} --name ${CONTAINER_NAME}"
echo ""
echo -e "${GREEN}✅ GitHub App Service is now running in Azure Container Instance${NC}"