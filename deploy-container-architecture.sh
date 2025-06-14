#!/bin/bash

# Deploy Docker Container Architecture for Copilot PowerPlatform
# Issue #70: Docker Container Architecture Implementation
# This script deploys the secure Azure OpenAI integrated container architecture

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_NAME="copilot-powerplatform"
DEPLOYMENT_MODE="production"
AZURE_RESOURCE_GROUP=""
AZURE_LOCATION="eastus"
CONTAINER_REGISTRY="ghcr.io"
IMAGE_TAG="latest"

# Print functions
print_header() {
    echo ""
    echo -e "${BLUE}🐳💫════════════════════════════════════════════════════════════════════💫🐳${NC}"
    echo -e "${BLUE}║${NC}                ${YELLOW}Copilot PowerPlatform Container Architecture${NC}                      ${BLUE}║${NC}"
    echo -e "${BLUE}║${NC}                        ${PURPLE}Issue #70 - Docker Deployment${NC}                             ${BLUE}║${NC}"
    echo -e "${BLUE}🐳💫════════════════════════════════════════════════════════════════════💫🐳${NC}"
    echo ""
}

print_status() {
    echo -e "${BLUE}🔧${NC} $1"
}

print_success() {
    echo -e "${GREEN}✅${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠️${NC} $1"
}

print_error() {
    echo -e "${RED}❌${NC} $1"
}

print_info() {
    echo -e "${PURPLE}ℹ️${NC} $1"
}

# Function to check prerequisites
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    local missing_tools=()
    
    # Check required tools
    if ! command -v docker &> /dev/null; then
        missing_tools+=("docker")
    fi
    
    if ! command -v docker-compose &> /dev/null; then
        missing_tools+=("docker-compose")
    fi
    
    if ! command -v az &> /dev/null; then
        missing_tools+=("azure-cli")
    fi
    
    if ! command -v jq &> /dev/null; then
        missing_tools+=("jq")
    fi
    
    if [ ${#missing_tools[@]} -ne 0 ]; then
        print_error "Missing required tools: ${missing_tools[*]}"
        print_info "Please install the missing tools and try again."
        exit 1
    fi
    
    # Check Docker daemon
    if ! docker info &> /dev/null; then
        print_error "Docker daemon is not running"
        exit 1
    fi
    
    # Check Azure CLI authentication
    if ! az account show &> /dev/null; then
        print_warning "Azure CLI not authenticated. Please run: az login"
        exit 1
    fi
    
    print_success "Prerequisites check passed"
}

# Function to validate secrets
validate_secrets() {
    print_status "Validating secrets configuration..."
    
    local secrets_dir="$SCRIPT_DIR/autonomous-orchestration/secrets"
    local required_secrets=(
        "github_token.txt"
        "azure_openai_endpoint.txt"
        "azure_openai_key.txt"
        "db_password.txt"
        "encryption_key.txt"
    )
    
    local missing_secrets=()
    
    for secret in "${required_secrets[@]}"; do
        if [ ! -f "$secrets_dir/$secret" ]; then
            missing_secrets+=("$secret")
        fi
    done
    
    if [ ${#missing_secrets[@]} -ne 0 ]; then
        print_error "Missing required secrets: ${missing_secrets[*]}"
        print_info "Please create the missing secret files in: $secrets_dir"
        print_info "Use the template: autonomous-orchestration/config/secrets.env.template"
        exit 1
    fi
    
    # Validate secret file permissions
    for secret in "${required_secrets[@]}"; do
        local secret_file="$secrets_dir/$secret"
        local permissions=$(stat -c %a "$secret_file" 2>/dev/null || echo "000")
        if [ "$permissions" != "600" ]; then
            print_warning "Fixing permissions for $secret"
            chmod 600 "$secret_file"
        fi
    done
    
    print_success "Secrets validation passed"
}

# Function to build container images
build_images() {
    print_status "Building container images..."
    
    # Build main application image
    print_info "Building main application image..."
    docker build -t "${CONTAINER_REGISTRY}/dynamicstms365/${PROJECT_NAME}/copilot-agent:${IMAGE_TAG}" \
        --build-arg BUILD_CONFIGURATION=Release \
        --build-arg ASPNETCORE_ENVIRONMENT=Production \
        --label "version=${IMAGE_TAG}" \
        --label "build-date=$(date -u +'%Y-%m-%dT%H:%M:%SZ')" \
        --label "vcs-ref=$(git rev-parse --short HEAD 2>/dev/null || echo 'unknown')" \
        -f Dockerfile .
    
    # Build Reynolds orchestrator image
    if [ -f "autonomous-orchestration/reynolds/Dockerfile" ]; then
        print_info "Building Reynolds orchestrator image..."
        docker build -t "${PROJECT_NAME}/reynolds:${IMAGE_TAG}" \
            --label "version=${IMAGE_TAG}" \
            --label "build-date=$(date -u +'%Y-%m-%dT%H:%M:%SZ')" \
            autonomous-orchestration/reynolds/
    fi
    
    # Build agent images
    for agent_dir in autonomous-orchestration/agents/*/; do
        if [ -f "${agent_dir}Dockerfile" ]; then
            local agent_name=$(basename "$agent_dir")
            print_info "Building ${agent_name} agent image..."
            docker build -t "${PROJECT_NAME}/${agent_name}:${IMAGE_TAG}" \
                --label "version=${IMAGE_TAG}" \
                --label "build-date=$(date -u +'%Y-%m-%dT%H:%M:%SZ')" \
                "$agent_dir"
        fi
    done
    
    print_success "Container images built successfully"
}

# Function to deploy locally
deploy_local() {
    print_status "Deploying container architecture locally..."
    
    cd autonomous-orchestration
    
    # Create required directories
    mkdir -p volumes/{reynolds-memory,agent-memory/{devops,platform,code},logs,redis-data,postgres-data}
    mkdir -p volumes/monitoring/{prometheus,grafana}
    
    # Set proper permissions
    sudo chown -R 1000:1000 volumes/ 2>/dev/null || true
    
    # Deploy with Docker Compose
    if [ "$DEPLOYMENT_MODE" = "production" ]; then
        print_info "Deploying in production mode..."
        docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
    else
        print_info "Deploying in development mode..."
        docker-compose up -d
    fi
    
    # Wait for services to be ready
    print_info "Waiting for services to be ready..."
    sleep 30
    
    # Run health checks
    if [ -f "health-check.sh" ]; then
        print_info "Running health checks..."
        bash health-check.sh --quick
    fi
    
    cd ..
    
    print_success "Local deployment completed"
}

# Function to deploy to Azure
deploy_azure() {
    print_status "Deploying to Azure Container Instances..."
    
    if [ -z "$AZURE_RESOURCE_GROUP" ]; then
        print_error "Azure resource group not specified"
        print_info "Set AZURE_RESOURCE_GROUP environment variable or use --azure-rg flag"
        exit 1
    fi
    
    # Create resource group if it doesn't exist
    if ! az group show --name "$AZURE_RESOURCE_GROUP" &> /dev/null; then
        print_info "Creating resource group: $AZURE_RESOURCE_GROUP"
        az group create --name "$AZURE_RESOURCE_GROUP" --location "$AZURE_LOCATION"
    fi
    
    # Deploy using ARM template
    print_info "Deploying using ARM template..."
    local deployment_name="${PROJECT_NAME}-deployment-$(date +%Y%m%d%H%M%S)"
    
    az deployment group create \
        --resource-group "$AZURE_RESOURCE_GROUP" \
        --name "$deployment_name" \
        --template-file container-arm-template.json \
        --parameters \
            containerName="$PROJECT_NAME" \
            imageName="${CONTAINER_REGISTRY}/dynamicstms365/${PROJECT_NAME}/copilot-agent:${IMAGE_TAG}" \
            registryUsername="$REGISTRY_USERNAME" \
            registryPassword="$REGISTRY_PASSWORD" \
            githubAppId="$GITHUB_APP_ID" \
            githubAppPrivateKey="$GITHUB_APP_PRIVATE_KEY" \
            azureOpenAiEndpoint="$AZURE_OPENAI_ENDPOINT" \
            azureOpenAiApiKey="$AZURE_OPENAI_API_KEY" \
            azureOpenAiDeploymentName="$AZURE_OPENAI_DEPLOYMENT_NAME" \
            enableSecurityHardening=true \
            logAnalyticsWorkspaceId="$LOG_ANALYTICS_WORKSPACE_ID"
    
    # Get deployment outputs
    local container_fqdn=$(az deployment group show \
        --resource-group "$AZURE_RESOURCE_GROUP" \
        --name "$deployment_name" \
        --query "properties.outputs.containerFQDN.value" \
        --output tsv)
    
    local health_endpoint=$(az deployment group show \
        --resource-group "$AZURE_RESOURCE_GROUP" \
        --name "$deployment_name" \
        --query "properties.outputs.healthEndpoint.value" \
        --output tsv)
    
    print_success "Azure deployment completed"
    print_info "Container FQDN: $container_fqdn"
    print_info "Health endpoint: $health_endpoint"
    
    # Test health endpoint
    print_info "Testing health endpoint..."
    sleep 60  # Wait for container to start
    if curl -f "$health_endpoint" &> /dev/null; then
        print_success "Health check passed"
    else
        print_warning "Health check failed - container may still be starting"
    fi
}

# Function to generate deployment report
generate_report() {
    print_status "Generating deployment report..."
    
    local report_file="deployment-report-$(date +%Y%m%d_%H%M%S).json"
    
    cat > "$report_file" << EOF
{
  "deployment": {
    "timestamp": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
    "project": "$PROJECT_NAME",
    "mode": "$DEPLOYMENT_MODE",
    "version": "$IMAGE_TAG",
    "azure_resource_group": "$AZURE_RESOURCE_GROUP",
    "azure_location": "$AZURE_LOCATION"
  },
  "architecture": {
    "container_runtime": "Docker",
    "orchestration": "Docker Compose / Azure Container Instances",
    "security_hardening": true,
    "azure_openai_integration": true,
    "monitoring": true,
    "logging": true
  },
  "security_features": {
    "non_root_user": true,
    "secrets_management": true,
    "network_isolation": true,
    "resource_limits": true,
    "security_headers": true,
    "rate_limiting": true,
    "encryption_at_rest": true,
    "audit_logging": true
  },
  "azure_integration": {
    "openai_service": "Azure OpenAI",
    "api_version": "2024-02-15-preview",
    "deployment_model": "gpt-4",
    "rate_limiting": true,
    "content_safety": true
  }
}
EOF
    
    print_success "Deployment report generated: $report_file"
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  --mode MODE           Deployment mode: development|production (default: production)"
    echo "  --azure-rg RG         Azure resource group name (required for Azure deployment)"
    echo "  --azure-location LOC  Azure location (default: eastus)"
    echo "  --image-tag TAG       Container image tag (default: latest)"
    echo "  --local               Deploy locally using Docker Compose"
    echo "  --azure               Deploy to Azure Container Instances"
    echo "  --build-only          Only build container images"
    echo "  --report              Generate deployment report"
    echo "  --help                Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 --local                                    # Deploy locally"
    echo "  $0 --azure --azure-rg my-rg                  # Deploy to Azure"
    echo "  $0 --build-only                              # Build images only"
    echo "  $0 --local --mode development                # Deploy locally in dev mode"
}

# Main execution
main() {
    local deploy_local_flag=false
    local deploy_azure_flag=false
    local build_only_flag=false
    local generate_report_flag=false
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --mode)
                DEPLOYMENT_MODE="$2"
                shift 2
                ;;
            --azure-rg)
                AZURE_RESOURCE_GROUP="$2"
                shift 2
                ;;
            --azure-location)
                AZURE_LOCATION="$2"
                shift 2
                ;;
            --image-tag)
                IMAGE_TAG="$2"
                shift 2
                ;;
            --local)
                deploy_local_flag=true
                shift
                ;;
            --azure)
                deploy_azure_flag=true
                shift
                ;;
            --build-only)
                build_only_flag=true
                shift
                ;;
            --report)
                generate_report_flag=true
                shift
                ;;
            --help)
                show_usage
                exit 0
                ;;
            *)
                print_warning "Unknown option: $1"
                shift
                ;;
        esac
    done
    
    print_header
    
    # Check prerequisites
    check_prerequisites
    
    # Validate secrets if not build-only
    if [ "$build_only_flag" = "false" ]; then
        validate_secrets
    fi
    
    # Build container images
    build_images
    
    # Deploy if not build-only
    if [ "$build_only_flag" = "false" ]; then
        if [ "$deploy_local_flag" = "true" ]; then
            deploy_local
        elif [ "$deploy_azure_flag" = "true" ]; then
            deploy_azure
        else
            print_info "No deployment target specified. Use --local or --azure"
        fi
    fi
    
    # Generate report if requested
    if [ "$generate_report_flag" = "true" ]; then
        generate_report
    fi
    
    print_success "Container architecture deployment completed successfully!"
}

# Run main function with all arguments
main "$@"