#!/bin/bash

# Reynolds' Maximum Effort™ Container Deployment Script
# Fixes the webhook secret and GitHub App authentication issues

set -e

echo "🎭 Reynolds Container Deployment with Maximum Effort™"
echo "=================================================="

# Check prerequisites
if [ ! -f "app.pem" ]; then
    echo "❌ ERROR: app.pem not found. This file contains your GitHub App private key."
    exit 1
fi

if [ ! -f "container-deployment.yaml" ]; then
    echo "❌ ERROR: container-deployment.yaml not found."
    exit 1
fi

# Set environment variables for deployment
echo "🔐 Setting up authentication environment variables..."

# Read the private key content and export it
export GITHUB_APP_PRIVATE_KEY=$(cat app.pem)

# Resource group and container details
RESOURCE_GROUP=${RESOURCE_GROUP:-"copilot-powerplatform-rg"}
LOCATION=${LOCATION:-"eastus"}
CONTAINER_NAME="github-copilot-bot"

echo "📦 Resource Group: $RESOURCE_GROUP"
echo "🌍 Location: $LOCATION"
echo "🐳 Container: $CONTAINER_NAME"

# Create resource group if it doesn't exist
echo "🏗️  Ensuring resource group exists..."
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output table

# Deploy the container with environment variables
echo "🚀 Deploying container with fixed configuration..."
az container create \
    --resource-group "$RESOURCE_GROUP" \
    --file container-deployment.yaml \
    --secure-environment-variables GITHUB_APP_PRIVATE_KEY="$GITHUB_APP_PRIVATE_KEY" \
    --output table

echo ""
echo "✅ Deployment completed!"
echo ""
echo "🔍 Container logs will show:"
echo "   - No more WEBHOOK_SECRET_NOT_CONFIGURED errors"
echo "   - No more GitHub App credentials not configured errors"
echo ""
echo "📋 Next steps:"
echo "1. Update your GitHub webhook secret to: 96328478de1391f4633f28221ef6d62d8fa42b57cea159ff65360e88015507dd"
echo "2. Monitor container logs with: az container logs --resource-group $RESOURCE_GROUP --name $CONTAINER_NAME --follow"
echo ""
echo "🎭 Reynolds says: Sequential deployment is dead - this parallel fix deployment is ALIVE!"