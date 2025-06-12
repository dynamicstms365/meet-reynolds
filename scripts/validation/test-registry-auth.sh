#!/bin/bash

# Test Registry Authentication Script
# This script validates GitHub Container Registry authentication for Azure deployments

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🔐 Testing GitHub Container Registry Authentication${NC}"
echo "=================================================="

# Function to print status messages
print_status() {
    local status=$1
    local message=$2
    case $status in
        "SUCCESS")
            echo -e "${GREEN}✅ $message${NC}"
            ;;
        "WARNING")
            echo -e "${YELLOW}⚠️  $message${NC}"
            ;;
        "FAILURE")
            echo -e "${RED}❌ $message${NC}"
            ;;
        *)
            echo "$message"
            ;;
    esac
}

# Configuration
REGISTRY="ghcr.io"
REPO_OWNER="dynamicstms365"
REPO_NAME="copilot-powerplatform"
IMAGE_NAME="copilot-agent"
IMAGE_TAG="latest"
FULL_IMAGE_URL="$REGISTRY/$REPO_OWNER/$REPO_NAME/$IMAGE_NAME:$IMAGE_TAG"

echo "Testing image: $FULL_IMAGE_URL"
echo ""

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    print_status "FAILURE" "Docker is not installed or not in PATH"
    exit 1
fi

print_status "SUCCESS" "Docker is available"

# Check for GitHub token
if [ -z "$GITHUB_TOKEN" ]; then
    print_status "WARNING" "GITHUB_TOKEN environment variable is not set"
    echo "Please set GITHUB_TOKEN with a token that has package:read permissions"
    echo "Example: export GITHUB_TOKEN=ghp_xxxxxxxxxxxxxxxxxxxx"
    exit 1
fi

print_status "SUCCESS" "GITHUB_TOKEN is set"

# Test registry login
echo ""
echo "🔑 Testing registry login..."
if echo "$GITHUB_TOKEN" | docker login $REGISTRY -u "$GITHUB_USER" --password-stdin 2>/dev/null; then
    print_status "SUCCESS" "Successfully logged into $REGISTRY"
else
    # Try with different username variations
    echo "Trying with different usernames..."
    if echo "$GITHUB_TOKEN" | docker login $REGISTRY -u "$REPO_OWNER" --password-stdin 2>/dev/null; then
        print_status "SUCCESS" "Successfully logged into $REGISTRY as $REPO_OWNER"
    elif echo "$GITHUB_TOKEN" | docker login $REGISTRY -u "cege7480" --password-stdin 2>/dev/null; then
        print_status "SUCCESS" "Successfully logged into $REGISTRY as cege7480"
    else
        print_status "FAILURE" "Failed to login to $REGISTRY"
        echo "This indicates an authentication issue that will cause Azure Container Apps deployment to fail"
        exit 1
    fi
fi

# Test image pull
echo ""
echo "📦 Testing image pull..."
if docker pull "$FULL_IMAGE_URL" 2>/dev/null; then
    print_status "SUCCESS" "Successfully pulled image: $FULL_IMAGE_URL"
    
    # Get image details
    IMAGE_SIZE=$(docker images "$FULL_IMAGE_URL" --format "table {{.Size}}" | tail -n +2)
    IMAGE_ID=$(docker images "$FULL_IMAGE_URL" --format "table {{.ID}}" | tail -n +2)
    print_status "SUCCESS" "Image size: $IMAGE_SIZE, ID: $IMAGE_ID"
else
    print_status "FAILURE" "Failed to pull image: $FULL_IMAGE_URL"
    echo "Possible causes:"
    echo "1. Image does not exist or has not been pushed"
    echo "2. Authentication token lacks package:read permissions"
    echo "3. Repository is private and token doesn't have access"
    exit 1
fi

# Test Azure CLI if available
echo ""
echo "🔧 Testing Azure CLI integration..."
if command -v az &> /dev/null; then
    print_status "SUCCESS" "Azure CLI is available"
    
    # Check if logged into Azure
    if az account show &> /dev/null; then
        print_status "SUCCESS" "Logged into Azure CLI"
        AZURE_SUBSCRIPTION=$(az account show --query name -o tsv)
        print_status "SUCCESS" "Active subscription: $AZURE_SUBSCRIPTION"
    else
        print_status "WARNING" "Not logged into Azure CLI"
        echo "Run 'az login' to test Azure Container Apps deployment"
    fi
else
    print_status "WARNING" "Azure CLI not found"
    echo "Install Azure CLI to test full deployment workflow"
fi

# Summary
echo ""
echo "📋 Summary"
echo "=========="
print_status "SUCCESS" "Registry authentication test completed"
echo ""
echo "🔧 Recommended Actions:"
echo "1. Ensure GitHub Actions secrets are properly configured:"
echo "   - GITHUB_TOKEN with package:read permissions"
echo "   - AZURE_CREDENTIALS for Azure authentication"
echo "2. Verify the image is accessible in GitHub Packages"
echo "3. Test the deployment workflow with these validated credentials"
echo ""
echo "🔗 Next Steps:"
echo "- Run GitHub Actions workflow: deploy-azure-container.yml"
echo "- Monitor Azure Container Apps deployment logs"
echo "- Verify container app health endpoints after deployment"

# Cleanup
echo ""
echo "🧹 Cleaning up..."
docker logout $REGISTRY &> /dev/null || true
print_status "SUCCESS" "Logged out from registry"

echo ""
print_status "SUCCESS" "Registry authentication test completed successfully"