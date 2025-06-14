#!/bin/bash

# Reynolds Autonomous Container Orchestration System - Enhanced Startup Script
# This script initializes and starts the Reynolds Orchestrator with all agent pools

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
COMPOSE_FILE="docker-compose.yml"
COMPOSE_OVERRIDE="docker-compose.override.yml"
COMPOSE_PROD="docker-compose.prod.yml"
PROJECT_NAME="reynolds-orchestration"
HEALTH_CHECK_TIMEOUT=300
HEALTH_CHECK_INTERVAL=5

# Function to print colored output
print_status() {
    echo -e "${BLUE}🎭 Reynolds Orchestrator:${NC} $1"
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

print_debug() {
    if [ "$DEBUG" = "true" ]; then
        echo -e "${CYAN}🔍${NC} $1"
    fi
}

# Function to print Reynolds banner
print_banner() {
    echo ""
    echo -e "${BLUE}🎭✨════════════════════════════════════════════════════════════════════✨🎭${NC}"
    echo -e "${BLUE}║${NC}                ${YELLOW}Reynolds Autonomous Container Orchestration System${NC}                ${BLUE}║${NC}"
    echo -e "${BLUE}║${NC}                          ${PURPLE}Maximum Effort™ Applied${NC}                             ${BLUE}║${NC}"
    echo -e "${BLUE}🎭✨════════════════════════════════════════════════════════════════════✨🎭${NC}"
    echo ""
}

# Check prerequisites
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    # Check if Docker is installed and running
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed. Please install Docker first."
        exit 1
    fi
    
    if ! docker info &> /dev/null; then
        print_error "Docker is not running. Please start Docker first."
        exit 1
    fi
    
    # Check Docker version
    DOCKER_VERSION=$(docker version --format '{{.Server.Version}}' 2>/dev/null || echo "unknown")
    print_debug "Docker version: $DOCKER_VERSION"
    
    # Check if Docker Compose is available
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        print_error "Docker Compose is not available. Please install Docker Compose."
        exit 1
    fi
    
    # Determine compose command
    if command -v docker-compose &> /dev/null; then
        COMPOSE_CMD="docker-compose"
    else
        COMPOSE_CMD="docker compose"
    fi
    
    # Check available memory
    AVAILABLE_MEMORY=$(docker system info --format '{{.MemTotal}}' 2>/dev/null || echo "0")
    if [ "$AVAILABLE_MEMORY" -lt 8000000000 ]; then
        print_warning "Less than 8GB of RAM available. Performance may be impacted."
    fi
    
    print_success "Prerequisites check passed (Docker: $DOCKER_VERSION, Compose: $COMPOSE_CMD)"
}

# Generate secrets if they don't exist
generate_secrets() {
    print_status "Setting up secrets..."
    
    mkdir -p secrets
    
    # Generate database password if it doesn't exist
    if [ ! -f "secrets/db_password.txt" ]; then
        openssl rand -base64 32 > secrets/db_password.txt
        print_success "Generated database password"
    fi
    
    # Generate Grafana password if it doesn't exist
    if [ ! -f "secrets/grafana_password.txt" ]; then
        echo "admin" > secrets/grafana_password.txt
        print_warning "Using default Grafana password 'admin'. Change this for production!"
    fi
    
    # Create placeholder files for other secrets
    secrets=(
        "github_token.txt"
        "azure_credentials.json"
        "m365_credentials.json"
        "tenant_id.txt"
        "openai_api_key.txt"
    )
    
    for secret in "${secrets[@]}"; do
        if [ ! -f "secrets/$secret" ]; then
            case "$secret" in
                *.json)
                    echo '{}' > "secrets/$secret"
                    ;;
                *)
                    touch "secrets/$secret"
                    ;;
            esac
        fi
    done
    
    print_info "Secret files created. Please update them with your actual credentials:"
    print_info "  - secrets/github_token.txt: GitHub personal access token"
    print_info "  - secrets/azure_credentials.json: Azure service principal credentials"
    print_info "  - secrets/m365_credentials.json: Microsoft 365 credentials"
    print_info "  - secrets/tenant_id.txt: Microsoft 365 tenant ID"
    print_info "  - secrets/openai_api_key.txt: OpenAI API key"
}

# Create necessary directories
create_directories() {
    print_status "Creating directories..."
    
    # Create volume directories
    directories=(
        "volumes/reynolds-memory"
        "volumes/agent-memory/devops"
        "volumes/agent-memory/platform"
        "volumes/agent-memory/code"
        "volumes/redis-data"
        "volumes/postgres-data"
        "volumes/monitoring/prometheus"
        "volumes/monitoring/grafana"
        "volumes/logs"
        "context/project"
        "context/team"
        "context/infrastructure"
        "context/business"
        "config/prometheus/rules"
        "config/grafana/dashboards"
        "config/grafana/datasources"
        "config/nginx"
    )
    
    for dir in "${directories[@]}"; do
        mkdir -p "$dir"
        print_debug "Created directory: $dir"
    done
    
    # Set proper permissions
    chmod -R 755 volumes/
    chmod 700 secrets/
    
    print_success "Directories created with proper permissions"
}

# Setup configuration files
setup_configuration() {
    print_status "Setting up configuration files..."
    
    # Create basic config files if they don't exist
    if [ ! -f "config/reynolds.env" ]; then
        cat > config/reynolds.env << 'EOF'
# Reynolds Core Configuration
ORCHESTRATOR_MODE=development
AGENT_POOL_SIZE=6
LOG_LEVEL=info
METRICS_ENABLED=true
HEALTH_CHECK_INTERVAL=30s
REYNOLDS_PERSONALITY=maximum_effort
LOOP_PREVENTION_ENABLED=true
GITHUB_ISSUES_INTEGRATION=enabled
EOF
        print_success "Created Reynolds configuration"
    fi
    
    if [ ! -f "config/agents.env" ]; then
        cat > config/agents.env << 'EOF'
# Agent Configuration
AGENT_STARTUP_TIMEOUT=120s
AGENT_HEALTH_CHECK_INTERVAL=30s
AGENT_MEMORY_PERSISTENCE=true
AGENT_LEARNING_ENABLED=true
MCP_CONNECTION_TIMEOUT=30s
MCP_RETRY_ATTEMPTS=3
EOF
        print_success "Created agent configuration"
    fi
    
    print_success "Configuration files ready"
}

# Build Docker images
build_images() {
    print_status "Building Docker images..."
    
    # Build Reynolds orchestrator
    print_info "Building Reynolds orchestrator image..."
    docker build -t reynolds-orchestrator:latest ./reynolds
    
    # Build agent images
    agents=("devops-polyglot" "platform-specialist" "code-intelligence")
    
    for agent in "${agents[@]}"; do
        if [ -d "agents/$agent" ] && [ -f "agents/$agent/Dockerfile" ]; then
            print_info "Building $agent agent image..."
            docker build -t "$agent-agent:latest" "./agents/$agent"
        else
            print_warning "Agent directory not found: agents/$agent"
        fi
    done
    
    print_success "Docker images built successfully"
}

# Determine compose files to use
get_compose_files() {
    local env="${1:-development}"
    local files="-f $COMPOSE_FILE"
    
    case "$env" in
        "production")
            files="$files -f $COMPOSE_PROD"
            ;;
        "development"|*)
            if [ -f "$COMPOSE_OVERRIDE" ]; then
                files="$files -f $COMPOSE_OVERRIDE"
            fi
            ;;
    esac
    
    echo "$files"
}

# Start the orchestration system
start_system() {
    local environment="${1:-development}"
    local compose_files=$(get_compose_files "$environment")
    
    print_status "Starting Reynolds Orchestration System (Environment: $environment)..."
    
    # Set environment variables
    export DB_PASSWORD=$(cat secrets/db_password.txt 2>/dev/null || echo "defaultpassword")
    export COMPOSE_PROJECT_NAME="$PROJECT_NAME"
    
    # Start the system using Docker Compose
    print_info "Using compose files: $compose_files"
    eval "$COMPOSE_CMD $compose_files up -d"
    
    print_success "System containers started!"
}

# Wait for services to be ready with enhanced health checks
wait_for_services() {
    print_status "Waiting for services to be ready..."
    
    local start_time=$(date +%s)
    local services=("reynolds:8080/health" "redis:6379" "postgres:5432")
    
    for service_check in "${services[@]}"; do
        local service_name=${service_check%%:*}
        local endpoint=${service_check#*:}
        
        print_info "Checking $service_name..."
        
        local attempt=1
        local max_attempts=$((HEALTH_CHECK_TIMEOUT / HEALTH_CHECK_INTERVAL))
        local ready=false
        
        while [ $attempt -le $max_attempts ] && [ "$ready" = false ]; do
            case "$service_name" in
                "reynolds")
                    if curl -s "http://localhost:$endpoint" > /dev/null 2>&1; then
                        ready=true
                    fi
                    ;;
                "redis")
                    if docker exec "${PROJECT_NAME}_redis_1" redis-cli ping > /dev/null 2>&1; then
                        ready=true
                    fi
                    ;;
                "postgres")
                    if docker exec "${PROJECT_NAME}_postgres_1" pg_isready -U reynolds > /dev/null 2>&1; then
                        ready=true
                    fi
                    ;;
            esac
            
            if [ "$ready" = true ]; then
                print_success "$service_name is ready!"
                break
            fi
            
            if [ $attempt -eq $max_attempts ]; then
                print_error "$service_name failed to start within timeout"
                return 1
            fi
            
            print_debug "Waiting for $service_name... (attempt $attempt/$max_attempts)"
            sleep $HEALTH_CHECK_INTERVAL
            ((attempt++))
        done
    done
    
    local end_time=$(date +%s)
    local total_time=$((end_time - start_time))
    print_success "All services ready in ${total_time}s!"
}

# Show system status
show_status() {
    print_status "System Status:"
    echo ""
    
    # Show running containers
    print_info "Running containers:"
    docker ps --filter "name=${PROJECT_NAME}" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>/dev/null || docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    echo ""
    
    # Show service endpoints
    print_info "Service endpoints:"
    echo "  🎭 Reynolds Orchestrator: http://localhost:8080"
    echo "  📊 Grafana Dashboard:     http://localhost:3000 (admin/admin)"
    echo "  📈 Prometheus Metrics:    http://localhost:9090"
    echo ""
    
    # Show MCP endpoints
    print_info "MCP endpoints:"
    echo "  🔧 Capabilities:         GET  http://localhost:8080/mcp/capabilities"
    echo "  🛠️ Tools:                GET  http://localhost:8080/mcp/tools"
    echo "  📚 Resources:            GET  http://localhost:8080/mcp/resources"
    echo "  🎭 Orchestrate task:     POST http://localhost:8080/mcp/tools/orchestrate_task"
    echo ""
    
    # Show agent endpoints
    print_info "Agent endpoints (development mode):"
    echo "  🛠️ DevOps Agents:        http://localhost:8081-8083"
    echo "  🏢 Platform Agents:      http://localhost:8084-8085"
    echo "  💻 Code Agents:          http://localhost:8086-8087"
    echo ""
    
    # Show quick test command
    print_info "Quick test command:"
    echo "  curl -X POST http://localhost:8080/mcp/tools/get_reynolds_wisdom \\"
    echo "    -H 'Content-Type: application/json' \\"
    echo "    -d '{\"arguments\": {\"situation\": \"testing the orchestration system\"}}'"
    echo ""
}

# Test the system with comprehensive checks
test_system() {
    print_status "Running comprehensive system tests..."
    
    local tests_passed=0
    local tests_total=0
    
    # Test 1: Health endpoint
    ((tests_total++))
    print_info "Test 1: Health check..."
    if curl -s http://localhost:8080/health | jq -e '.status == "healthy" or .orchestrator == "operational"' > /dev/null 2>&1; then
        print_success "✓ Health check passed"
        ((tests_passed++))
    else
        print_error "✗ Health check failed"
    fi
    
    # Test 2: MCP capabilities
    ((tests_total++))
    print_info "Test 2: MCP capabilities..."
    if curl -s http://localhost:8080/mcp/capabilities | jq -e '.capabilities.tools | length > 0' > /dev/null 2>&1; then
        print_success "✓ MCP capabilities test passed"
        ((tests_passed++))
    else
        print_error "✗ MCP capabilities test failed"
    fi
    
    # Test 3: Reynolds wisdom
    ((tests_total++))
    print_info "Test 3: Reynolds wisdom..."
    local wisdom_response=$(curl -s -X POST http://localhost:8080/mcp/tools/get_reynolds_wisdom \
        -H 'Content-Type: application/json' \
        -d '{"arguments": {"situation": "testing the orchestration system"}}' 2>/dev/null)
    
    if echo "$wisdom_response" | jq -e '.content[0].text' > /dev/null 2>&1; then
        print_success "✓ Reynolds wisdom test passed"
        local wisdom=$(echo "$wisdom_response" | jq -r '.content[0].text' | head -n 1)
        print_info "Reynolds says: $wisdom"
        ((tests_passed++))
    else
        print_error "✗ Reynolds wisdom test failed"
    fi
    
    # Test 4: Database connectivity
    ((tests_total++))
    print_info "Test 4: Database connectivity..."
    if docker exec "${PROJECT_NAME}_postgres_1" pg_isready -U reynolds > /dev/null 2>&1; then
        print_success "✓ Database connectivity test passed"
        ((tests_passed++))
    else
        print_error "✗ Database connectivity test failed"
    fi
    
    # Test 5: Redis connectivity
    ((tests_total++))
    print_info "Test 5: Redis connectivity..."
    if docker exec "${PROJECT_NAME}_redis_1" redis-cli ping > /dev/null 2>&1; then
        print_success "✓ Redis connectivity test passed"
        ((tests_passed++))
    else
        print_error "✗ Redis connectivity test failed"
    fi
    
    # Test results
    echo ""
    if [ $tests_passed -eq $tests_total ]; then
        print_success "All tests passed! 🎉 ($tests_passed/$tests_total)"
        return 0
    else
        print_error "Some tests failed. ($tests_passed/$tests_total passed)"
        return 1
    fi
}

# Stop the system
stop_system() {
    local environment="${1:-development}"
    local compose_files=$(get_compose_files "$environment")
    
    print_status "Stopping Reynolds Orchestration System..."
    
    eval "$COMPOSE_CMD $compose_files down"
    
    print_success "System stopped"
}

# Clean up everything
cleanup_system() {
    local environment="${1:-development}"
    local compose_files=$(get_compose_files "$environment")
    
    print_status "Cleaning up system..."
    
    # Stop and remove containers
    eval "$COMPOSE_CMD $compose_files down -v --remove-orphans"
    
    # Remove custom images
    local images=("reynolds-orchestrator:latest" "devops-polyglot-agent:latest" "platform-specialist-agent:latest" "code-intelligence-agent:latest")
    for image in "${images[@]}"; do
        docker rmi "$image" 2>/dev/null || true
    done
    
    # Clean up data directories (optional)
    read -p "Do you want to remove all data directories? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        rm -rf volumes/
        print_success "Data directories removed"
    fi
    
    print_success "Cleanup completed"
}

# Show logs
show_logs() {
    local service=${1:-reynolds}
    local environment="${2:-development}"
    local compose_files=$(get_compose_files "$environment")
    
    print_status "Showing logs for $service..."
    
    eval "$COMPOSE_CMD $compose_files logs -f \"$service\""
}

# Scale services
scale_services() {
    local service="$1"
    local replicas="$2"
    local environment="${3:-development}"
    local compose_files=$(get_compose_files "$environment")
    
    if [ -z "$service" ] || [ -z "$replicas" ]; then
        print_error "Usage: scale <service> <replicas>"
        exit 1
    fi
    
    print_status "Scaling $service to $replicas replicas..."
    
    eval "$COMPOSE_CMD $compose_files up -d --scale \"$service=$replicas\""
    
    print_success "Scaled $service to $replicas replicas"
}

# Main script logic
main() {
    # Parse command line arguments
    local command="${1:-start}"
    local environment="development"
    
    # Parse flags
    while [[ $# -gt 0 ]]; do
        case $1 in
            --production)
                environment="production"
                shift
                ;;
            --debug)
                DEBUG="true"
                shift
                ;;
            -*)
                print_warning "Unknown option $1"
                shift
                ;;
            *)
                if [ -z "$command" ]; then
                    command="$1"
                fi
                shift
                ;;
        esac
    done
    
    print_banner
    
    case "$command" in
        "start")
            check_prerequisites
            generate_secrets
            create_directories
            setup_configuration
            build_images
            start_system "$environment"
            wait_for_services
            show_status
            test_system
            ;;
        "stop")
            stop_system "$environment"
            ;;
        "restart")
            stop_system "$environment"
            sleep 5
            start_system "$environment"
            wait_for_services
            show_status
            ;;
        "status")
            show_status
            ;;
        "test")
            test_system
            ;;
        "logs")
            show_logs "${2:-reynolds}" "$environment"
            ;;
        "cleanup")
            cleanup_system "$environment"
            ;;
        "build")
            build_images
            ;;
        "scale")
            scale_services "$2" "$3" "$environment"
            ;;
        *)
            echo "Usage: $0 {start|stop|restart|status|test|logs|cleanup|build|scale} [options]"
            echo ""
            echo "Commands:"
            echo "  start       - Start the orchestration system (default)"
            echo "  stop        - Stop the orchestration system"
            echo "  restart     - Restart the orchestration system"
            echo "  status      - Show system status and endpoints"
            echo "  test        - Run comprehensive system tests"
            echo "  logs        - Show logs (specify service name as second argument)"
            echo "  cleanup     - Stop system and clean up all resources"
            echo "  build       - Build Docker images only"
            echo "  scale       - Scale a service (usage: scale <service> <replicas>)"
            echo ""
            echo "Options:"
            echo "  --production - Use production configuration"
            echo "  --debug      - Enable debug output"
            echo ""
            echo "Examples:"
            echo "  $0 start"
            echo "  $0 start --production"
            echo "  $0 logs reynolds"
            echo "  $0 scale devops-agent 5"
            echo "  $0 test --debug"
            exit 1
            ;;
    esac
}

# Run main function
main "$@"