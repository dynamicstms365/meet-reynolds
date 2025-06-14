#!/bin/bash

# Reynolds Autonomous Container Orchestration System - Graceful Shutdown Script
# This script gracefully stops the Reynolds Orchestrator and all agent pools

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
GRACEFUL_SHUTDOWN_TIMEOUT=60
FORCE_SHUTDOWN_TIMEOUT=30

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

# Function to print Reynolds shutdown banner
print_shutdown_banner() {
    echo ""
    echo -e "${RED}🎭💀════════════════════════════════════════════════════════════════════💀🎭${NC}"
    echo -e "${RED}║${NC}                ${YELLOW}Reynolds Orchestration System Shutdown${NC}                         ${RED}║${NC}"
    echo -e "${RED}║${NC}                          ${PURPLE}Graceful Maximum Effort™${NC}                              ${RED}║${NC}"
    echo -e "${RED}🎭💀════════════════════════════════════════════════════════════════════💀🎭${NC}"
    echo ""
}

# Determine compose command and files
get_compose_setup() {
    local environment="${1:-development}"
    
    # Determine compose command
    if command -v docker-compose &> /dev/null; then
        COMPOSE_CMD="docker-compose"
    else
        COMPOSE_CMD="docker compose"
    fi
    
    # Determine compose files
    local files="-f $COMPOSE_FILE"
    case "$environment" in
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

# Save agent memory before shutdown
save_agent_memory() {
    print_status "Saving agent memory and learning patterns..."
    
    local agents=("reynolds" "devops-agent" "platform-agent" "code-agent")
    local timestamp=$(date +"%Y%m%d_%H%M%S")
    
    for agent in "${agents[@]}"; do
        local container_name="${PROJECT_NAME}_${agent}_1"
        
        # Check if container exists and is running
        if docker ps --format '{{.Names}}' | grep -q "^${container_name}$"; then
            print_info "Saving memory for $agent..."
            
            # Create backup directory
            mkdir -p "volumes/backups/${timestamp}/${agent}"
            
            # Save memory state via API if available
            if [ "$agent" = "reynolds" ]; then
                curl -s -X POST "http://localhost:8080/api/memory/save" \
                    -H "Content-Type: application/json" \
                    -d '{"backup": true, "timestamp": "'$timestamp'"}' \
                    > "volumes/backups/${timestamp}/${agent}/memory_state.json" 2>/dev/null || true
            fi
            
            # Copy memory files
            docker cp "${container_name}:/app/memory" "volumes/backups/${timestamp}/${agent}/" 2>/dev/null || true
            
            print_debug "Memory saved for $agent"
        else
            print_debug "Container $container_name not running, skipping memory save"
        fi
    done
    
    print_success "Agent memory saved to volumes/backups/${timestamp}/"
}

# Gracefully stop agents
graceful_agent_shutdown() {
    print_status "Initiating graceful agent shutdown..."
    
    local agents=("devops-agent" "platform-agent" "code-agent")
    
    # Send shutdown signal to agents via MCP
    for agent in "${agents[@]}"; do
        print_info "Sending graceful shutdown signal to $agent..."
        
        # Send shutdown command via Reynolds orchestrator
        curl -s -X POST "http://localhost:8080/mcp/tools/shutdown_agent" \
            -H "Content-Type: application/json" \
            -d "{\"arguments\": {\"agentType\": \"$agent\", \"graceful\": true}}" \
            > /dev/null 2>&1 || true
            
        print_debug "Shutdown signal sent to $agent"
    done
    
    # Wait for agents to finish current tasks
    print_info "Waiting for agents to complete current tasks..."
    local wait_time=0
    local max_wait=$GRACEFUL_SHUTDOWN_TIMEOUT
    
    while [ $wait_time -lt $max_wait ]; do
        local running_tasks=$(curl -s "http://localhost:8080/api/tasks/active" 2>/dev/null | jq '.count' 2>/dev/null || echo "0")
        
        if [ "$running_tasks" -eq 0 ]; then
            print_success "All agent tasks completed"
            break
        fi
        
        print_debug "Waiting for $running_tasks active tasks to complete... (${wait_time}s/${max_wait}s)"
        sleep 5
        wait_time=$((wait_time + 5))
    done
    
    if [ $wait_time -ge $max_wait ]; then
        print_warning "Graceful shutdown timeout reached, proceeding with forced shutdown"
    fi
}

# Stop monitoring services first
stop_monitoring_services() {
    local compose_files="$1"
    
    print_status "Stopping monitoring services..."
    
    local monitoring_services=("grafana" "prometheus")
    
    for service in "${monitoring_services[@]}"; do
        print_info "Stopping $service..."
        eval "$COMPOSE_CMD $compose_files stop $service" 2>/dev/null || true
    done
    
    print_success "Monitoring services stopped"
}

# Stop application services
stop_application_services() {
    local compose_files="$1"
    
    print_status "Stopping application services..."
    
    # Stop agents first
    local agent_services=("devops-agent" "platform-agent" "code-agent")
    
    for service in "${agent_services[@]}"; do
        print_info "Stopping $service..."
        eval "$COMPOSE_CMD $compose_files stop $service" 2>/dev/null || true
    done
    
    # Stop Reynolds orchestrator
    print_info "Stopping Reynolds orchestrator..."
    eval "$COMPOSE_CMD $compose_files stop reynolds" 2>/dev/null || true
    
    print_success "Application services stopped"
}

# Stop infrastructure services
stop_infrastructure_services() {
    local compose_files="$1"
    
    print_status "Stopping infrastructure services..."
    
    local infra_services=("redis" "postgres")
    
    for service in "${infra_services[@]}"; do
        print_info "Stopping $service..."
        eval "$COMPOSE_CMD $compose_files stop $service" 2>/dev/null || true
    done
    
    print_success "Infrastructure services stopped"
}

# Perform complete system shutdown
complete_shutdown() {
    local environment="$1"
    local compose_files="$2"
    local force="$3"
    
    if [ "$force" = "true" ]; then
        print_status "Performing forced shutdown..."
        eval "$COMPOSE_CMD $compose_files down --timeout $FORCE_SHUTDOWN_TIMEOUT"
    else
        print_status "Performing graceful shutdown sequence..."
        
        # Save agent memory
        save_agent_memory
        
        # Gracefully shutdown agents
        graceful_agent_shutdown
        
        # Stop services in proper order
        stop_monitoring_services "$compose_files"
        stop_application_services "$compose_files"
        stop_infrastructure_services "$compose_files"
        
        # Remove containers
        print_status "Removing containers..."
        eval "$COMPOSE_CMD $compose_files down"
    fi
    
    print_success "System shutdown completed"
}

# Clean up resources
cleanup_resources() {
    local remove_volumes="$1"
    local remove_networks="$2"
    
    print_status "Cleaning up resources..."
    
    # Remove unused networks
    if [ "$remove_networks" = "true" ]; then
        print_info "Removing orphaned networks..."
        docker network prune -f 2>/dev/null || true
    fi
    
    # Remove unused volumes
    if [ "$remove_volumes" = "true" ]; then
        print_info "Removing orphaned volumes..."
        docker volume prune -f 2>/dev/null || true
    fi
    
    # Remove unused images
    print_info "Removing orphaned images..."
    docker image prune -f 2>/dev/null || true
    
    print_success "Resource cleanup completed"
}

# Show system status before shutdown
show_pre_shutdown_status() {
    print_status "Current system status:"
    echo ""
    
    # Show running containers
    print_info "Running containers:"
    docker ps --filter "name=${PROJECT_NAME}" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>/dev/null || docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    echo ""
    
    # Show active tasks if Reynolds is running
    if curl -s "http://localhost:8080/health" > /dev/null 2>&1; then
        local active_tasks=$(curl -s "http://localhost:8080/api/tasks/active" 2>/dev/null | jq '.count' 2>/dev/null || echo "unknown")
        print_info "Active tasks: $active_tasks"
        echo ""
    fi
}

# Generate shutdown report
generate_shutdown_report() {
    local timestamp=$(date +"%Y-%m-%d %H:%M:%S")
    local report_file="volumes/logs/shutdown_report_$(date +%Y%m%d_%H%M%S).json"
    
    print_status "Generating shutdown report..."
    
    mkdir -p "volumes/logs"
    
    cat > "$report_file" << EOF
{
  "shutdown_timestamp": "$timestamp",
  "shutdown_type": "${1:-graceful}",
  "environment": "${2:-development}",
  "containers_stopped": [
$(docker ps -a --filter "name=${PROJECT_NAME}" --format '    "{{.Names}}",' | sed '$s/,$//')
  ],
  "volumes_preserved": [
$(docker volume ls --filter "name=${PROJECT_NAME}" --format '    "{{.Name}}",' | sed '$s/,$//')
  ],
  "system_info": {
    "docker_version": "$(docker version --format '{{.Server.Version}}' 2>/dev/null)",
    "compose_command": "$COMPOSE_CMD",
    "shutdown_duration": "$(date +%s)"
  }
}
EOF
    
    print_success "Shutdown report saved to $report_file"
}

# Main shutdown function
main() {
    local environment="development"
    local force_shutdown=false
    local remove_volumes=false
    local remove_networks=false
    local show_status=true
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --production)
                environment="production"
                shift
                ;;
            --force)
                force_shutdown=true
                shift
                ;;
            --remove-volumes)
                remove_volumes=true
                shift
                ;;
            --remove-networks)
                remove_networks=true
                shift
                ;;
            --quiet)
                show_status=false
                shift
                ;;
            --debug)
                DEBUG="true"
                shift
                ;;
            --help|-h)
                echo "Usage: $0 [options]"
                echo ""
                echo "Options:"
                echo "  --production       Use production configuration"
                echo "  --force           Force immediate shutdown (skip graceful shutdown)"
                echo "  --remove-volumes  Remove all volumes during cleanup"
                echo "  --remove-networks Remove all networks during cleanup"
                echo "  --quiet          Suppress status output"
                echo "  --debug          Enable debug output"
                echo "  --help, -h       Show this help message"
                echo ""
                echo "Examples:"
                echo "  $0                          # Graceful shutdown (development)"
                echo "  $0 --production             # Graceful shutdown (production)"
                echo "  $0 --force                  # Immediate shutdown"
                echo "  $0 --force --remove-volumes # Complete cleanup"
                exit 0
                ;;
            *)
                print_warning "Unknown option: $1"
                shift
                ;;
        esac
    done
    
    print_shutdown_banner
    
    # Get compose setup
    local compose_files=$(get_compose_setup "$environment")
    
    # Show pre-shutdown status
    if [ "$show_status" = "true" ]; then
        show_pre_shutdown_status
    fi
    
    # Confirm shutdown if not forced
    if [ "$force_shutdown" = "false" ]; then
        echo ""
        read -p "Proceed with graceful shutdown? (y/N): " -n 1 -r
        echo ""
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            print_info "Shutdown cancelled"
            exit 0
        fi
    fi
    
    # Perform shutdown
    complete_shutdown "$environment" "$compose_files" "$force_shutdown"
    
    # Clean up resources
    cleanup_resources "$remove_volumes" "$remove_networks"
    
    # Generate shutdown report
    generate_shutdown_report "$environment"
    
    print_success "🎭 Reynolds Orchestration System has been gracefully shut down! 🎭"
    
    if [ "$environment" = "production" ]; then
        print_info "Production system shutdown completed. All data has been preserved."
    else
        print_info "Development system shutdown completed. Use './start-orchestration.sh start' to restart."
    fi
}

# Run main function
main "$@"