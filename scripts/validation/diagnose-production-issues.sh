#!/bin/bash

# Production Issue Diagnostic Script
# Comprehensive diagnosis for webhook secret and MCP server routing issues
# Maximum Effort™ parallel diagnostics

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
EXPECTED_FQDN="github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io"

echo -e "${PURPLE}🎭 Reynolds Maximum Effort™ Production Diagnostic${NC}"
echo -e "${PURPLE}=================================================${NC}"
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

# Function to test HTTP endpoint
test_endpoint() {
    local url=$1
    local description=$2
    local expected_status=${3:-200}
    
    print_status "PROGRESS" "Testing $description: $url"
    
    local status_code=$(curl -s -o /dev/null -w "%{http_code}" "$url" 2>/dev/null || echo "000")
    
    if [[ "$status_code" == "$expected_status" ]]; then
        print_status "SUCCESS" "$description returned $status_code (expected $expected_status)"
        return 0
    else
        print_status "ERROR" "$description returned $status_code (expected $expected_status)"
        return 1
    fi
}

# Parallel Diagnostic Phase 1: Azure Infrastructure
echo -e "${BLUE}📋 Phase 1: Azure Container Apps Infrastructure${NC}"
echo ""

if command -v az &> /dev/null; then
    print_status "SUCCESS" "Azure CLI is available"
    
    if az account show &> /dev/null; then
        print_status "SUCCESS" "Azure CLI is authenticated"
        
        # Get container app status
        print_status "PROGRESS" "Checking container app status..."
        CONTAINER_STATUS=$(az containerapp show --name "$CONTAINER_APP_NAME" --resource-group "$RESOURCE_GROUP" --query "properties.provisioningState" -o tsv 2>/dev/null || echo "NOT_FOUND")
        
        if [[ "$CONTAINER_STATUS" == "Succeeded" ]]; then
            print_status "SUCCESS" "Container app is running successfully"
            
            # Get FQDN
            ACTUAL_FQDN=$(az containerapp show --name "$CONTAINER_APP_NAME" --resource-group "$RESOURCE_GROUP" --query "properties.configuration.ingress.fqdn" -o tsv 2>/dev/null || echo "UNKNOWN")
            print_status "INFO" "Container FQDN: $ACTUAL_FQDN"
            
            # Check environment variables
            print_status "PROGRESS" "Checking environment variable configuration..."
            ENV_VARS=$(az containerapp show --name "$CONTAINER_APP_NAME" --resource-group "$RESOURCE_GROUP" --query "properties.template.containers[0].env" -o json 2>/dev/null || echo "[]")
            
            if echo "$ENV_VARS" | grep -q "NGL_DEVOPS_WEBHOOK_SECRET"; then
                print_status "SUCCESS" "NGL_DEVOPS_WEBHOOK_SECRET environment variable is configured"
            else
                print_status "ERROR" "NGL_DEVOPS_WEBHOOK_SECRET environment variable is MISSING"
                print_status "WARNING" "This explains the WEBHOOK_SECRET_NOT_CONFIGURED error"
            fi
            
            if echo "$ENV_VARS" | grep -q "NGL_DEVOPS_PRIVATE_KEY"; then
                print_status "SUCCESS" "NGL_DEVOPS_PRIVATE_KEY environment variable is configured"
            else
                print_status "ERROR" "NGL_DEVOPS_PRIVATE_KEY environment variable is MISSING"
            fi
            
            if echo "$ENV_VARS" | grep -q "NGL_DEVOPS_APP_ID"; then
                print_status "SUCCESS" "NGL_DEVOPS_APP_ID environment variable is configured"
            else
                print_status "ERROR" "NGL_DEVOPS_APP_ID environment variable is MISSING"
            fi
            
        else
            print_status "ERROR" "Container app status: $CONTAINER_STATUS"
        fi
        
        # Check secrets
        print_status "PROGRESS" "Checking container app secrets..."
        SECRETS=$(az containerapp secret list --name "$CONTAINER_APP_NAME" --resource-group "$RESOURCE_GROUP" --query "[].name" -o tsv 2>/dev/null || echo "")
        
        if [[ "$SECRETS" == *"ngl-devops-webhook-secret"* ]]; then
            print_status "SUCCESS" "Webhook secret is stored in container app secrets"
        else
            print_status "ERROR" "Webhook secret is NOT stored in container app secrets"
        fi
        
        if [[ "$SECRETS" == *"ngl-devops-private-key"* ]]; then
            print_status "SUCCESS" "Private key is stored in container app secrets"
        else
            print_status "ERROR" "Private key is NOT stored in container app secrets"
        fi
        
    else
        print_status "ERROR" "Azure CLI is not authenticated"
        print_status "WARNING" "Run 'az login' to authenticate"
    fi
else
    print_status "ERROR" "Azure CLI is not installed"
    ACTUAL_FQDN="$EXPECTED_FQDN"
fi

echo ""

# Parallel Diagnostic Phase 2: Application Endpoints
echo -e "${BLUE}📋 Phase 2: Application Endpoint Testing${NC}"
echo ""

BASE_URL="https://${ACTUAL_FQDN:-$EXPECTED_FQDN}"

# Test health endpoint
test_endpoint "$BASE_URL/api/github/health" "Health endpoint"

# Test webhook endpoint (should return 401 without signature)
test_endpoint "$BASE_URL/api/github/webhook" "Webhook endpoint (unsigned)" "401"

# Test GitHub integration endpoints
test_endpoint "$BASE_URL/api/github/test" "GitHub connectivity test"
test_endpoint "$BASE_URL/api/github/installation-info" "GitHub installation info"

echo ""

# Parallel Diagnostic Phase 3: MCP Server Endpoints
echo -e "${BLUE}📋 Phase 3: MCP Server Endpoint Testing${NC}"
echo ""

# Test MCP capabilities endpoint
test_endpoint "$BASE_URL/mcp/capabilities" "MCP capabilities endpoint"

# Test MCP SSE endpoint
test_endpoint "$BASE_URL/mcp/sse" "MCP SSE endpoint"

echo ""

# Parallel Diagnostic Phase 4: Container Logs Analysis
echo -e "${BLUE}📋 Phase 4: Container Logs Analysis${NC}"
echo ""

if command -v az &> /dev/null && az account show &> /dev/null; then
    print_status "PROGRESS" "Retrieving recent container logs..."
    
    RECENT_LOGS=$(az containerapp logs show --name "$CONTAINER_APP_NAME" --resource-group "$RESOURCE_GROUP" --tail 50 2>/dev/null || echo "Could not retrieve logs")
    
    if [[ "$RECENT_LOGS" == *"WEBHOOK_SECRET_NOT_CONFIGURED"* ]]; then
        print_status "ERROR" "Found WEBHOOK_SECRET_NOT_CONFIGURED in logs"
    else
        print_status "SUCCESS" "No webhook secret configuration errors in recent logs"
    fi
    
    if [[ "$RECENT_LOGS" == *"Reynolds MCP Server"* ]]; then
        print_status "SUCCESS" "Reynolds MCP Server is initializing properly"
    else
        print_status "WARNING" "Reynolds MCP Server initialization not found in logs"
    fi
    
    if [[ "$RECENT_LOGS" == *"17 tools"* ]]; then
        print_status "SUCCESS" "MCP tools are being loaded correctly"
    else
        print_status "WARNING" "MCP tool loading not confirmed in logs"
    fi
fi

echo ""

# Maximum Effort™ Recommendations
echo -e "${PURPLE}🎯 Reynolds Maximum Effort™ Recommendations${NC}"
echo -e "${PURPLE}==============================================${NC}"
echo ""

print_status "INFO" "Priority 1: Fix Webhook Secret Configuration"
echo "   • Run: az containerapp secret set --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP --secrets \"ngl-devops-webhook-secret=\$WEBHOOK_SECRET\""
echo "   • Then: az containerapp update --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP --set-env-vars \"NGL_DEVOPS_WEBHOOK_SECRET=secretref:ngl-devops-webhook-secret\""
echo ""

print_status "INFO" "Priority 2: Validate MCP Endpoint Routing"
echo "   • Check: curl -f \"$BASE_URL/mcp/capabilities\""
echo "   • Verify: curl -f \"$BASE_URL/mcp/sse\""
echo ""

print_status "INFO" "Priority 3: Monitor Deployment Pipeline"
echo "   • Check GitHub Actions workflow: deploy-azure-container.yml"
echo "   • Verify GitHub secrets: NGL_DEVOPS_WEBHOOK_SECRET, NGL_DEVOPS_PRIVATE_KEY"
echo ""

print_status "INFO" "Priority 4: Test Webhook Delivery"
echo "   • Update GitHub App webhook URL to: $BASE_URL/api/github/webhook"
echo "   • Test delivery from GitHub webhook settings"
echo ""

echo -e "${GREEN}🚀 Diagnostic Complete - Maximum Effort™ Applied!${NC}"