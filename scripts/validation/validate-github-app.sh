#!/bin/bash

# GitHub App Setup Validation Script
# This script validates the GitHub App configuration and connectivity

# Note: We don't use 'set -e' here because we want to continue validation
# even when some checks fail (like missing credentials in development)

echo "🔧 GitHub App Registration & Setup Validation"
echo "=============================================="

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Success/failure counters
SUCCESS_COUNT=0
TOTAL_TESTS=0

# Function to print status
print_status() {
    local status=$1
    local message=$2
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    if [ "$status" = "SUCCESS" ]; then
        echo -e "${GREEN}✅ $message${NC}"
        SUCCESS_COUNT=$((SUCCESS_COUNT + 1))
    elif [ "$status" = "WARNING" ]; then
        echo -e "${YELLOW}⚠️  $message${NC}"
    else
        echo -e "${RED}❌ $message${NC}"
    fi
}

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to check environment variable or configuration
check_credential() {
    local var_name=$1
    local value=""
    
    # Check environment variable first
    if [ -n "${!var_name}" ]; then
        value="${!var_name}"
    fi
    
    if [ -n "$value" ]; then
        print_status "SUCCESS" "$var_name is configured"
        return 0
    else
        print_status "WARNING" "$var_name is not configured (expected in production)"
        return 1
    fi
}

# Function to validate private key format
validate_private_key() {
    local key_value="${NGL_DEVOPS_PRIVATE_KEY}"
    
    if [ -z "$key_value" ]; then
        print_status "WARNING" "Private key not found (expected in production)"
        return 1
    fi
    
    # Check if it contains PEM headers
    if echo "$key_value" | grep -q "BEGIN.*PRIVATE KEY"; then
        print_status "SUCCESS" "Private key has valid PEM format"
        return 0
    else
        print_status "FAILURE" "Private key does not have valid PEM format"
        return 1
    fi
}

# Function to test API endpoint
test_endpoint() {
    local endpoint=$1
    local expected_status=$2
    local description=$3
    
    echo "  Testing: $endpoint"
    
    if command_exists curl; then
        # Try to reach the endpoint
        local response_code
        response_code=$(curl -s -o /dev/null -w "%{http_code}" -X GET "$endpoint" --connect-timeout 10 --max-time 30 2>/dev/null || echo "000")
        
        if [ "$response_code" = "$expected_status" ]; then
            print_status "SUCCESS" "$description (HTTP $response_code)"
        elif [ "$response_code" = "000" ]; then
            print_status "WARNING" "$description - Service not running or not accessible"
        else
            print_status "WARNING" "$description - Got HTTP $response_code, expected $expected_status"
        fi
    else
        print_status "WARNING" "curl not available, skipping endpoint test"
    fi
}

# Function to check .NET project
check_dotnet_project() {
    if command_exists dotnet; then
        echo "📦 Checking .NET Project..."
        
        if [ -f "src/CopilotAgent.sln" ]; then
            print_status "SUCCESS" ".NET solution file found"
            
            # Try to build the project
            if dotnet build src/CopilotAgent.sln --verbosity quiet >/dev/null 2>&1; then
                print_status "SUCCESS" "Project builds successfully"
            else
                print_status "FAILURE" "Project build failed"
            fi
            
            # Try to run tests
            if dotnet test src/CopilotAgent.Tests/CopilotAgent.Tests.csproj --verbosity quiet >/dev/null 2>&1; then
                print_status "SUCCESS" "All tests pass"
            else
                print_status "FAILURE" "Some tests failed"
            fi
        else
            print_status "FAILURE" ".NET solution file not found"
        fi
    else
        print_status "WARNING" ".NET SDK not installed, skipping project checks"
    fi
}

# Main validation steps
main() {
    echo ""
    echo "🔍 Step 1: Checking GitHub App Credentials"
    echo "----------------------------------------"
    
    check_credential "NGL_DEVOPS_APP_ID"
    check_credential "NGL_DEVOPS_INSTALLATION_ID"
    check_credential "NGL_DEVOPS_PRIVATE_KEY"
    
    if [ -n "$NGL_DEVOPS_PRIVATE_KEY" ]; then
        validate_private_key
    fi
    
    echo ""
    echo "🏗️ Step 2: Checking Project Structure"
    echo "------------------------------------"
    
    # Check project files
    if [ -f "src/CopilotAgent/Controllers/GitHubController.cs" ]; then
        print_status "SUCCESS" "GitHub controller found"
    else
        print_status "FAILURE" "GitHub controller not found"
    fi
    
    if [ -f "src/CopilotAgent/Services/GitHubAppAuthService.cs" ]; then
        print_status "SUCCESS" "GitHub authentication service found"
    else
        print_status "FAILURE" "GitHub authentication service not found"
    fi
    
    if [ -f "src/CopilotAgent/Services/SecurityAuditService.cs" ]; then
        print_status "SUCCESS" "Security audit service found"
    else
        print_status "FAILURE" "Security audit service not found"
    fi
    
    if [ -f "docs/github-copilot/github-app-setup.md" ]; then
        print_status "SUCCESS" "GitHub App setup documentation found"
    else
        print_status "FAILURE" "GitHub App setup documentation not found"
    fi
    
    echo ""
    echo "🧪 Step 3: Testing Project Build"
    echo "-------------------------------"
    check_dotnet_project
    
    echo ""
    echo "🌐 Step 4: Testing API Endpoints (if service is running)"
    echo "-------------------------------------------------------"
    
    # Try common localhost ports
    local base_urls=("http://localhost:5000" "http://localhost:5077" "https://localhost:7077")
    local found_service=false
    
    for base_url in "${base_urls[@]}"; do
        echo "  Checking: $base_url"
        if curl -s "$base_url/api/github/health" --connect-timeout 2 --max-time 5 >/dev/null 2>&1; then
            found_service=true
            echo "  Found service at: $base_url"
            
            test_endpoint "$base_url/api/github/health" "200" "Health endpoint"
            test_endpoint "$base_url/api/github/test" "200" "Connectivity test endpoint"
            test_endpoint "$base_url/api/github/installation-info" "200" "Installation info endpoint"
            break
        fi
    done
    
    if [ "$found_service" = false ]; then
        print_status "WARNING" "Service not running - start with 'dotnet run' to test endpoints"
    fi
    
    echo ""
    echo "📋 Step 5: Validating Configuration Requirements"
    echo "----------------------------------------------"
    
    # Check that we have the minimum required components
    local required_files=(
        "src/CopilotAgent/Controllers/GitHubController.cs"
        "src/CopilotAgent/Services/GitHubAppAuthService.cs"
        "src/CopilotAgent/Services/SecurityAuditService.cs"
        "src/Shared/Models/Models.cs"
        "docs/github-copilot/github-app-setup.md"
        "src/CopilotAgent.Tests/Unit/GitHubIntegrationTests.cs"
    )
    
    local missing_files=0
    for file in "${required_files[@]}"; do
        if [ -f "$file" ]; then
            print_status "SUCCESS" "Required file: $file"
        else
            print_status "FAILURE" "Missing required file: $file"
            missing_files=$((missing_files + 1))
        fi
    done
    
    echo ""
    echo "📋 Step 6: Validating GitHub Actions Integration"
    echo "---------------------------------------------"
    
    # Check for GitHub Actions workflows
    if [ -f ".github/workflows/copilot-setup-steps.yml" ]; then
        print_status "SUCCESS" "Copilot setup steps workflow found"
    else
        print_status "WARNING" "Copilot setup steps workflow not found"
    fi
    
    if [ -f ".github/workflows/deploy-azure-container.yml" ]; then
        print_status "SUCCESS" "Azure container deployment workflow found"
    else
        print_status "WARNING" "Azure container deployment workflow not found"
    fi
    
    if [ -f ".github/workflows/assign-copilot.yml" ]; then
        print_status "SUCCESS" "Copilot assignment workflow found"
        
        # Check if it uses actions/create-github-app-token
        if grep -q "actions/create-github-app-token" ".github/workflows/assign-copilot.yml"; then
            print_status "SUCCESS" "Workflow uses actions/create-github-app-token"
        else
            print_status "WARNING" "Workflow does not use GitHub App token generation"
        fi
    else
        print_status "WARNING" "Copilot assignment workflow not found"
    fi
    
    # Check for Docker configuration
    if [ -f "src/CopilotAgent/Dockerfile" ]; then
        print_status "SUCCESS" "Docker configuration found"
    else
        print_status "WARNING" "Docker configuration not found"
    fi
    
    # Check for Azure deployment scripts
    if [ -f "scripts/azure/deploy-container-instance.sh" ]; then
        print_status "SUCCESS" "Azure deployment script found"
    else
        print_status "WARNING" "Azure deployment script not found"
    fi
    
    # Check if we're running in GitHub Actions environment
    if [ -n "$GITHUB_ACTIONS" ]; then
        print_status "SUCCESS" "Running in GitHub Actions environment"
        
        if [ -n "$GITHUB_TOKEN" ]; then
            print_status "SUCCESS" "GitHub Actions token available"
        else
            print_status "WARNING" "GitHub Actions token not available"
        fi
    else
        print_status "INFO" "Not running in GitHub Actions (this is normal for local testing)"
    fi
    
    echo ""
    echo "📊 Validation Summary"
    echo "===================="
    echo "Total tests: $TOTAL_TESTS"
    echo "Passed: $SUCCESS_COUNT"
    echo "Failed/Warnings: $((TOTAL_TESTS - SUCCESS_COUNT))"
    
    local success_rate=$((SUCCESS_COUNT * 100 / TOTAL_TESTS))
    echo "Success rate: $success_rate%"
    
    if [ $success_rate -ge 90 ]; then
        echo -e "${GREEN}✅ GitHub App setup is ready for production!${NC}"
        exit 0
    elif [ $success_rate -ge 75 ]; then
        echo -e "${YELLOW}⚠️  GitHub App setup is mostly complete but needs attention.${NC}"
        exit 0
    else
        echo -e "${RED}❌ GitHub App setup requires significant work before deployment.${NC}"
        exit 1
    fi
}

# Help function
show_help() {
    echo "GitHub App Setup Validation Script"
    echo ""
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  -h, --help     Show this help message"
    echo "  -v, --verbose  Enable verbose output"
    echo ""
    echo "Environment Variables:"
    echo "  NGL_DEVOPS_APP_ID              GitHub App ID"
    echo "  NGL_DEVOPS_INSTALLATION_ID     GitHub App Installation ID"
    echo "  NGL_DEVOPS_PRIVATE_KEY         GitHub App Private Key (PEM format)"
    echo ""
    echo "This script validates:"
    echo "  ✓ GitHub App credentials configuration"
    echo "  ✓ Project structure and required files"
    echo "  ✓ .NET project build and tests"
    echo "  ✓ API endpoint accessibility (if running)"
    echo "  ✓ Documentation completeness"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -v|--verbose)
            set -x
            shift
            ;;
        *)
            echo "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Check if we're in the right directory
if [ ! -f "src/CopilotAgent.sln" ] && [ ! -f "../src/CopilotAgent.sln" ]; then
    echo -e "${RED}❌ Please run this script from the repository root directory${NC}"
    exit 1
fi

# Change to repo root if needed
if [ -f "../src/CopilotAgent.sln" ]; then
    cd ..
fi

# Run main validation
main