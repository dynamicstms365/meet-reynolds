#!/bin/bash

# Reynolds Autonomous Container Orchestration System - Dynamic Agent Scaling Script
# This script provides intelligent scaling of agent pools based on demand and performance metrics

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
METRICS_ENDPOINT="http://localhost:8080/metrics"
PROMETHEUS_ENDPOINT="http://localhost:9090"

# Scaling thresholds
CPU_THRESHOLD_SCALE_UP=80
CPU_THRESHOLD_SCALE_DOWN=30
MEMORY_THRESHOLD_SCALE_UP=85
MEMORY_THRESHOLD_SCALE_DOWN=40
QUEUE_THRESHOLD_SCALE_UP=10
QUEUE_THRESHOLD_SCALE_DOWN=2
RESPONSE_TIME_THRESHOLD=5000  # milliseconds

# Agent limits
MIN_REPLICAS=1
MAX_REPLICAS=10
DEFAULT_REPLICAS=2

# Function to print colored output
print_status() {
    echo -e "${BLUE}🎭 Reynolds Agent Scaler:${NC} $1"
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

# Function to print scaling banner
print_scaling_banner() {
    echo ""
    echo -e "${BLUE}🎭⚡════════════════════════════════════════════════════════════════════⚡🎭${NC}"
    echo -e "${BLUE}║${NC}                ${YELLOW}Reynolds Dynamic Agent Scaling System${NC}                        ${BLUE}║${NC}"
    echo -e "${BLUE}║${NC}                          ${PURPLE}Maximum Effort™ Scaling${NC}                             ${BLUE}║${NC}"
    echo -e "${BLUE}🎭⚡════════════════════════════════════════════════════════════════════⚡🎭${NC}"
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

# Get current agent metrics
get_agent_metrics() {
    local agent_type="$1"
    local metrics_data=""
    
    # Try to get metrics from Reynolds API
    if curl -s "$METRICS_ENDPOINT" > /dev/null 2>&1; then
        metrics_data=$(curl -s "$METRICS_ENDPOINT/agents/$agent_type" 2>/dev/null || echo "{}")
    fi
    
    # Parse metrics or use defaults
    local cpu_usage=$(echo "$metrics_data" | jq -r '.cpu_usage // 0' 2>/dev/null || echo "0")
    local memory_usage=$(echo "$metrics_data" | jq -r '.memory_usage // 0' 2>/dev/null || echo "0")
    local queue_length=$(echo "$metrics_data" | jq -r '.queue_length // 0' 2>/dev/null || echo "0")
    local response_time=$(echo "$metrics_data" | jq -r '.avg_response_time // 0' 2>/dev/null || echo "0")
    local current_replicas=$(echo "$metrics_data" | jq -r '.replicas // 1' 2>/dev/null || echo "1")
    
    echo "$cpu_usage $memory_usage $queue_length $response_time $current_replicas"
}

# Get current replicas for a service
get_current_replicas() {
    local service="$1"
    local replicas=$(docker ps --filter "name=${PROJECT_NAME}_${service}" --format "{{.Names}}" | wc -l)
    echo "$replicas"
}

# Calculate recommended replicas based on metrics
calculate_recommended_replicas() {
    local agent_type="$1"
    local cpu_usage="$2"
    local memory_usage="$3"
    local queue_length="$4"
    local response_time="$5"
    local current_replicas="$6"
    
    local recommended_replicas=$current_replicas
    local scale_up_reasons=()
    local scale_down_reasons=()
    
    # Check scale-up conditions
    if (( $(echo "$cpu_usage > $CPU_THRESHOLD_SCALE_UP" | bc -l) )); then
        scale_up_reasons+=("CPU usage: ${cpu_usage}%")
    fi
    
    if (( $(echo "$memory_usage > $MEMORY_THRESHOLD_SCALE_UP" | bc -l) )); then
        scale_up_reasons+=("Memory usage: ${memory_usage}%")
    fi
    
    if [ "$queue_length" -gt "$QUEUE_THRESHOLD_SCALE_UP" ]; then
        scale_up_reasons+=("Queue length: $queue_length")
    fi
    
    if [ "$response_time" -gt "$RESPONSE_TIME_THRESHOLD" ]; then
        scale_up_reasons+=("Response time: ${response_time}ms")
    fi
    
    # Check scale-down conditions
    if (( $(echo "$cpu_usage < $CPU_THRESHOLD_SCALE_DOWN" | bc -l) )) && \
       (( $(echo "$memory_usage < $MEMORY_THRESHOLD_SCALE_DOWN" | bc -l) )) && \
       [ "$queue_length" -lt "$QUEUE_THRESHOLD_SCALE_DOWN" ]; then
        scale_down_reasons+=("Low resource utilization")
    fi
    
    # Apply scaling logic
    if [ ${#scale_up_reasons[@]} -gt 0 ] && [ $current_replicas -lt $MAX_REPLICAS ]; then
        # Scale up: increase by 1 or more based on severity
        local scale_factor=1
        if [ ${#scale_up_reasons[@]} -gt 2 ]; then
            scale_factor=2
        fi
        recommended_replicas=$((current_replicas + scale_factor))
        
        # Ensure we don't exceed maximum
        if [ $recommended_replicas -gt $MAX_REPLICAS ]; then
            recommended_replicas=$MAX_REPLICAS
        fi
        
        print_debug "Scale up recommended for $agent_type: ${scale_up_reasons[*]}"
        
    elif [ ${#scale_down_reasons[@]} -gt 0 ] && [ $current_replicas -gt $MIN_REPLICAS ]; then
        # Scale down: decrease by 1
        recommended_replicas=$((current_replicas - 1))
        
        # Ensure we don't go below minimum
        if [ $recommended_replicas -lt $MIN_REPLICAS ]; then
            recommended_replicas=$MIN_REPLICAS
        fi
        
        print_debug "Scale down recommended for $agent_type: ${scale_down_reasons[*]}"
    fi
    
    echo "$recommended_replicas"
}

# Scale a specific agent service
scale_agent_service() {
    local service="$1"
    local target_replicas="$2"
    local environment="$3"
    local compose_files="$4"
    
    print_status "Scaling $service to $target_replicas replicas..."
    
    # Perform the scaling
    eval "$COMPOSE_CMD $compose_files up -d --scale \"$service=$target_replicas\""
    
    # Wait for containers to be ready
    local timeout=60
    local elapsed=0
    
    while [ $elapsed -lt $timeout ]; do
        local running_replicas=$(docker ps --filter "name=${PROJECT_NAME}_${service}" --filter "status=running" --format "{{.Names}}" | wc -l)
        
        if [ "$running_replicas" -eq "$target_replicas" ]; then
            print_success "Successfully scaled $service to $target_replicas replicas"
            return 0
        fi
        
        print_debug "Waiting for $service scaling... ($running_replicas/$target_replicas ready)"
        sleep 5
        elapsed=$((elapsed + 5))
    done
    
    print_warning "Scaling timeout for $service (some replicas may still be starting)"
    return 1
}

# Auto-scale all agents based on current metrics
auto_scale_agents() {
    local environment="$1"
    local compose_files="$2"
    local dry_run="$3"
    
    print_status "Analyzing agent metrics for auto-scaling..."
    
    local agents=("devops-agent" "platform-agent" "code-agent")
    local scaling_actions=()
    
    for agent in "${agents[@]}"; do
        print_info "Analyzing $agent metrics..."
        
        # Get current metrics
        local metrics=($(get_agent_metrics "$agent"))
        local cpu_usage=${metrics[0]}
        local memory_usage=${metrics[1]}
        local queue_length=${metrics[2]}
        local response_time=${metrics[3]}
        local current_replicas=$(get_current_replicas "$agent")
        
        print_debug "$agent metrics: CPU=${cpu_usage}%, Memory=${memory_usage}%, Queue=${queue_length}, Response=${response_time}ms, Replicas=${current_replicas}"
        
        # Calculate recommended replicas
        local recommended_replicas=$(calculate_recommended_replicas "$agent" "$cpu_usage" "$memory_usage" "$queue_length" "$response_time" "$current_replicas")
        
        if [ "$recommended_replicas" -ne "$current_replicas" ]; then
            local action="$agent:$current_replicas→$recommended_replicas"
            scaling_actions+=("$action")
            
            if [ "$dry_run" = "true" ]; then
                print_info "Would scale $agent from $current_replicas to $recommended_replicas replicas"
            else
                scale_agent_service "$agent" "$recommended_replicas" "$environment" "$compose_files"
            fi
        else
            print_info "$agent scaling not needed (current: $current_replicas replicas)"
        fi
    done
    
    if [ ${#scaling_actions[@]} -eq 0 ]; then
        print_success "No scaling actions needed - all agents optimally sized"
    else
        if [ "$dry_run" = "true" ]; then
            print_info "Scaling simulation completed: ${scaling_actions[*]}"
        else
            print_success "Auto-scaling completed: ${scaling_actions[*]}"
        fi
    fi
}

# Show current agent status and metrics
show_agent_status() {
    print_status "Current Agent Status:"
    echo ""
    
    local agents=("devops-agent" "platform-agent" "code-agent")
    
    for agent in "${agents[@]}"; do
        local current_replicas=$(get_current_replicas "$agent")
        local metrics=($(get_agent_metrics "$agent"))
        local cpu_usage=${metrics[0]}
        local memory_usage=${metrics[1]}
        local queue_length=${metrics[2]}
        local response_time=${metrics[3]}
        
        echo -e "${BLUE}$agent:${NC}"
        echo "  Replicas: $current_replicas"
        echo "  CPU Usage: ${cpu_usage}%"
        echo "  Memory Usage: ${memory_usage}%"
        echo "  Queue Length: $queue_length"
        echo "  Avg Response Time: ${response_time}ms"
        
        # Show running containers
        echo "  Containers:"
        docker ps --filter "name=${PROJECT_NAME}_${agent}" --format "    {{.Names}}: {{.Status}}"
        echo ""
    done
}

# Set scaling thresholds
set_scaling_thresholds() {
    local cpu_up="$1"
    local cpu_down="$2"
    local memory_up="$3"
    local memory_down="$4"
    local queue_up="$5"
    local queue_down="$6"
    
    if [ -n "$cpu_up" ]; then CPU_THRESHOLD_SCALE_UP="$cpu_up"; fi
    if [ -n "$cpu_down" ]; then CPU_THRESHOLD_SCALE_DOWN="$cpu_down"; fi
    if [ -n "$memory_up" ]; then MEMORY_THRESHOLD_SCALE_UP="$memory_up"; fi
    if [ -n "$memory_down" ]; then MEMORY_THRESHOLD_SCALE_DOWN="$memory_down"; fi
    if [ -n "$queue_up" ]; then QUEUE_THRESHOLD_SCALE_UP="$queue_up"; fi
    if [ -n "$queue_down" ]; then QUEUE_THRESHOLD_SCALE_DOWN="$queue_down"; fi
    
    print_success "Scaling thresholds updated"
    echo "  CPU: Scale up at ${CPU_THRESHOLD_SCALE_UP}%, scale down at ${CPU_THRESHOLD_SCALE_DOWN}%"
    echo "  Memory: Scale up at ${MEMORY_THRESHOLD_SCALE_UP}%, scale down at ${MEMORY_THRESHOLD_SCALE_DOWN}%"
    echo "  Queue: Scale up at ${QUEUE_THRESHOLD_SCALE_UP}, scale down at ${QUEUE_THRESHOLD_SCALE_DOWN}"
}

# Watch and auto-scale continuously
watch_and_scale() {
    local environment="$1"
    local compose_files="$2"
    local interval="${3:-60}"
    
    print_status "Starting continuous monitoring and auto-scaling (interval: ${interval}s)..."
    print_info "Press Ctrl+C to stop"
    echo ""
    
    while true; do
        local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
        echo -e "${CYAN}[$timestamp] Checking agent metrics...${NC}"
        
        auto_scale_agents "$environment" "$compose_files" "false"
        
        echo ""
        print_debug "Waiting ${interval}s before next check..."
        sleep "$interval"
    done
}

# Main function
main() {
    local command="${1:-status}"
    local environment="development"
    local dry_run=false
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --production)
                environment="production"
                shift
                ;;
            --dry-run)
                dry_run=true
                shift
                ;;
            --debug)
                DEBUG="true"
                shift
                ;;
            --help|-h)
                echo "Usage: $0 <command> [options] [arguments]"
                echo ""
                echo "Commands:"
                echo "  status                    Show current agent status and metrics"
                echo "  scale <agent> <replicas>  Scale specific agent to target replicas"
                echo "  auto                      Perform auto-scaling based on current metrics"
                echo "  watch [interval]          Continuously monitor and auto-scale (default: 60s)"
                echo "  thresholds [values]       Set scaling thresholds"
                echo ""
                echo "Options:"
                echo "  --production             Use production configuration"
                echo "  --dry-run                Show what would be scaled without actually scaling"
                echo "  --debug                  Enable debug output"
                echo "  --help, -h               Show this help message"
                echo ""
                echo "Examples:"
                echo "  $0 status"
                echo "  $0 scale devops-agent 5"
                echo "  $0 auto --dry-run"
                echo "  $0 watch 30"
                echo "  $0 thresholds 75 25 80 35 15 3"
                exit 0
                ;;
            *)
                if [ -z "$command" ]; then
                    command="$1"
                fi
                shift
                ;;
        esac
    done
    
    print_scaling_banner
    
    # Get compose setup
    local compose_files=$(get_compose_setup "$environment")
    
    case "$command" in
        "status")
            show_agent_status
            ;;
        "scale")
            local agent="$2"
            local replicas="$3"
            
            if [ -z "$agent" ] || [ -z "$replicas" ]; then
                print_error "Usage: scale <agent> <replicas>"
                print_info "Available agents: devops-agent, platform-agent, code-agent"
                exit 1
            fi
            
            if [ "$replicas" -lt "$MIN_REPLICAS" ] || [ "$replicas" -gt "$MAX_REPLICAS" ]; then
                print_error "Replicas must be between $MIN_REPLICAS and $MAX_REPLICAS"
                exit 1
            fi
            
            scale_agent_service "$agent" "$replicas" "$environment" "$compose_files"
            ;;
        "auto")
            auto_scale_agents "$environment" "$compose_files" "$dry_run"
            ;;
        "watch")
            local interval="${2:-60}"
            watch_and_scale "$environment" "$compose_files" "$interval"
            ;;
        "thresholds")
            set_scaling_thresholds "$2" "$3" "$4" "$5" "$6" "$7"
            ;;
        *)
            print_error "Unknown command: $command"
            print_info "Use '$0 --help' for usage information"
            exit 1
            ;;
    esac
}

# Install bc if not available (for floating point comparisons)
if ! command -v bc &> /dev/null; then
    print_warning "bc not found. Floating point comparisons may not work correctly."
fi

# Run main function
main "$@"