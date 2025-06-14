#!/bin/bash

# Reynolds Autonomous Container Orchestration System - Health Check Script
# This script performs comprehensive health validation of all system components

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
PROJECT_NAME="reynolds-orchestration"
TIMEOUT=30
VERBOSE=false
EXPORT_REPORT=false
REPORT_FORMAT="json"

# Health check endpoints
REYNOLDS_HEALTH="http://localhost:8080/health"
REYNOLDS_MCP="http://localhost:8080/mcp/capabilities"
GRAFANA_HEALTH="http://localhost:3000/api/health"
PROMETHEUS_HEALTH="http://localhost:9090/-/healthy"

# Function to print colored output
print_status() {
    echo -e "${BLUE}🏥 Reynolds Health Check:${NC} $1"
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
    if [ "$VERBOSE" = "true" ]; then
        echo -e "${CYAN}🔍${NC} $1"
    fi
}

# Function to print health check banner
print_health_banner() {
    echo ""
    echo -e "${GREEN}🏥💚════════════════════════════════════════════════════════════════════💚🏥${NC}"
    echo -e "${GREEN}║${NC}                ${YELLOW}Reynolds System Health Validation${NC}                           ${GREEN}║${NC}"
    echo -e "${GREEN}║${NC}                          ${PURPLE}Maximum Effort™ Monitoring${NC}                           ${GREEN}║${NC}"
    echo -e "${GREEN}🏥💚════════════════════════════════════════════════════════════════════💚🏥${NC}"
    echo ""
}

# Initialize health check results
init_health_results() {
    HEALTH_RESULTS=""
    HEALTH_SUMMARY=""
    TOTAL_CHECKS=0
    PASSED_CHECKS=0
    WARNING_CHECKS=0
    FAILED_CHECKS=0
    START_TIME=$(date +%s)
}

# Add health check result
add_health_result() {
    local component="$1"
    local status="$2"  # PASS, WARN, FAIL
    local message="$3"
    local details="$4"
    
    ((TOTAL_CHECKS++))
    
    case "$status" in
        "PASS")
            ((PASSED_CHECKS++))
            print_success "$component: $message"
            ;;
        "WARN")
            ((WARNING_CHECKS++))
            print_warning "$component: $message"
            ;;
        "FAIL")
            ((FAILED_CHECKS++))
            print_error "$component: $message"
            ;;
    esac
    
    if [ "$VERBOSE" = "true" ] && [ -n "$details" ]; then
        print_debug "$details"
    fi
    
    # Store for report
    local timestamp=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    HEALTH_RESULTS+="{\"component\":\"$component\",\"status\":\"$status\",\"message\":\"$message\",\"details\":\"$details\",\"timestamp\":\"$timestamp\"},"
}

# Check Docker daemon
check_docker_daemon() {
    print_info "Checking Docker daemon..."
    
    if ! command -v docker &> /dev/null; then
        add_health_result "Docker" "FAIL" "Docker command not found" "Docker is not installed or not in PATH"
        return 1
    fi
    
    if ! docker info &> /dev/null; then
        add_health_result "Docker" "FAIL" "Docker daemon not running" "Cannot connect to Docker daemon"
        return 1
    fi
    
    local docker_version=$(docker version --format '{{.Server.Version}}' 2>/dev/null || echo "unknown")
    add_health_result "Docker" "PASS" "Docker daemon healthy" "Version: $docker_version"
}

# Check container status
check_container_status() {
    print_info "Checking container status..."
    
    local expected_containers=("reynolds" "redis" "postgres" "prometheus" "grafana")
    local running_containers=()
    local failed_containers=()
    
    # Get running containers
    while IFS= read -r container; do
        if [ -n "$container" ]; then
            running_containers+=("$container")
        fi
    done < <(docker ps --filter "name=${PROJECT_NAME}" --format "{{.Names}}" | sed "s/${PROJECT_NAME}_//g" | sed 's/_[0-9]*$//')
    
    # Check each expected container
    for container in "${expected_containers[@]}"; do
        local found=false
        for running in "${running_containers[@]}"; do
            if [[ "$running" == *"$container"* ]]; then
                found=true
                break
            fi
        done
        
        if [ "$found" = "true" ]; then
            local status=$(docker ps --filter "name=${PROJECT_NAME}_${container}" --format "{{.Status}}")
            if [[ "$status" == *"healthy"* ]] || [[ "$status" == *"Up"* ]]; then
                add_health_result "Container-$container" "PASS" "Container running" "$status"
            else
                add_health_result "Container-$container" "WARN" "Container status unclear" "$status"
            fi
        else
            failed_containers+=("$container")
            add_health_result "Container-$container" "FAIL" "Container not running" "Expected container not found"
        fi
    done
    
    # Check agent containers
    local agent_types=("devops-agent" "platform-agent" "code-agent")
    for agent in "${agent_types[@]}"; do
        local agent_count=$(docker ps --filter "name=${PROJECT_NAME}_${agent}" --format "{{.Names}}" | wc -l)
        if [ "$agent_count" -gt 0 ]; then
            add_health_result "Agent-$agent" "PASS" "$agent_count replicas running" "Active agent instances"
        else
            add_health_result "Agent-$agent" "WARN" "No agent replicas running" "Agent may be scaled down"
        fi
    done
}

# Check network connectivity
check_network_connectivity() {
    print_info "Checking network connectivity..."
    
    # Check if agent network exists
    if docker network ls --filter "name=${PROJECT_NAME}_agent-network" --format "{{.Name}}" | grep -q "agent-network"; then
        add_health_result "Network" "PASS" "Agent network exists" "Docker network properly configured"
    else
        add_health_result "Network" "FAIL" "Agent network missing" "Docker network not found"
        return 1
    fi
    
    # Check container-to-container connectivity
    if docker exec "${PROJECT_NAME}_reynolds_1" ping -c 1 redis &> /dev/null; then
        add_health_result "Connectivity-Redis" "PASS" "Reynolds can reach Redis" "Internal network connectivity working"
    else
        add_health_result "Connectivity-Redis" "FAIL" "Cannot reach Redis from Reynolds" "Internal network connectivity issue"
    fi
    
    if docker exec "${PROJECT_NAME}_reynolds_1" ping -c 1 postgres &> /dev/null; then
        add_health_result "Connectivity-Postgres" "PASS" "Reynolds can reach Postgres" "Database connectivity working"
    else
        add_health_result "Connectivity-Postgres" "FAIL" "Cannot reach Postgres from Reynolds" "Database connectivity issue"
    fi
}

# Check service endpoints
check_service_endpoints() {
    print_info "Checking service endpoints..."
    
    # Check Reynolds health endpoint
    if curl -s --max-time $TIMEOUT "$REYNOLDS_HEALTH" > /dev/null 2>&1; then
        local health_response=$(curl -s --max-time $TIMEOUT "$REYNOLDS_HEALTH" 2>/dev/null || echo "{}")
        local status=$(echo "$health_response" | jq -r '.status // .orchestrator // "unknown"' 2>/dev/null || echo "unknown")
        
        if [[ "$status" == "healthy" ]] || [[ "$status" == "operational" ]]; then
            add_health_result "Reynolds-Health" "PASS" "Health endpoint responsive" "Status: $status"
        else
            add_health_result "Reynolds-Health" "WARN" "Health endpoint unclear status" "Status: $status"
        fi
    else
        add_health_result "Reynolds-Health" "FAIL" "Health endpoint not accessible" "Cannot connect to http://localhost:8080/health"
    fi
    
    # Check Reynolds MCP endpoint
    if curl -s --max-time $TIMEOUT "$REYNOLDS_MCP" > /dev/null 2>&1; then
        local mcp_response=$(curl -s --max-time $TIMEOUT "$REYNOLDS_MCP" 2>/dev/null || echo "{}")
        local tools_count=$(echo "$mcp_response" | jq '.capabilities.tools | length' 2>/dev/null || echo "0")
        
        if [ "$tools_count" -gt 0 ]; then
            add_health_result "Reynolds-MCP" "PASS" "MCP endpoint functional" "$tools_count tools available"
        else
            add_health_result "Reynolds-MCP" "WARN" "MCP endpoint responding but no tools" "May still be initializing"
        fi
    else
        add_health_result "Reynolds-MCP" "FAIL" "MCP endpoint not accessible" "Cannot connect to MCP service"
    fi
    
    # Check Grafana
    if curl -s --max-time $TIMEOUT "$GRAFANA_HEALTH" > /dev/null 2>&1; then
        add_health_result "Grafana" "PASS" "Grafana accessible" "Dashboard service running"
    else
        add_health_result "Grafana" "WARN" "Grafana not accessible" "Dashboard may be starting up"
    fi
    
    # Check Prometheus
    if curl -s --max-time $TIMEOUT "$PROMETHEUS_HEALTH" > /dev/null 2>&1; then
        add_health_result "Prometheus" "PASS" "Prometheus healthy" "Metrics collection active"
    else
        add_health_result "Prometheus" "WARN" "Prometheus not accessible" "Metrics collection may be impacted"
    fi
}

# Check data persistence
check_data_persistence() {
    print_info "Checking data persistence..."
    
    local volumes=("reynolds-memory" "redis-data" "postgres-data" "prometheus-data" "grafana-data")
    
    for volume in "${volumes[@]}"; do
        local volume_name="${PROJECT_NAME}_${volume}"
        if docker volume ls --filter "name=$volume_name" --format "{{.Name}}" | grep -q "$volume_name"; then
            # Check if volume has data
            local volume_path=$(docker volume inspect "$volume_name" --format "{{.Mountpoint}}" 2>/dev/null || echo "")
            if [ -n "$volume_path" ] && [ -d "$volume_path" ]; then
                add_health_result "Volume-$volume" "PASS" "Volume exists and mounted" "Data persistence active"
            else
                add_health_result "Volume-$volume" "WARN" "Volume exists but path unclear" "May be using bind mount"
            fi
        else
            add_health_result "Volume-$volume" "FAIL" "Volume missing" "Data persistence at risk"
        fi
    done
    
    # Check directory persistence for bind mounts
    if [ -d "volumes" ]; then
        local dir_count=$(find volumes -type d | wc -l)
        add_health_result "Bind-Mounts" "PASS" "Local directories exist" "$dir_count directories found"
    else
        add_health_result "Bind-Mounts" "WARN" "No local volume directories" "May be using Docker volumes only"
    fi
}

# Check resource usage
check_resource_usage() {
    print_info "Checking resource usage..."
    
    # Check overall Docker system resource usage
    local docker_info=$(docker system df --format "table {{.Type}}\t{{.TotalCount}}\t{{.Size}}" 2>/dev/null || echo "")
    
    # Check memory usage of containers
    local containers=$(docker ps --filter "name=${PROJECT_NAME}" --format "{{.Names}}")
    local total_memory=0
    
    for container in $containers; do
        local memory_usage=$(docker stats --no-stream --format "{{.MemUsage}}" "$container" 2>/dev/null | cut -d'/' -f1 | sed 's/[^0-9.]//g' || echo "0")
        if [[ "$memory_usage" =~ ^[0-9.]+$ ]]; then
            total_memory=$(echo "$total_memory + $memory_usage" | bc -l 2>/dev/null || echo "$total_memory")
        fi
    done
    
    if (( $(echo "$total_memory > 0" | bc -l 2>/dev/null || echo 0) )); then
        if (( $(echo "$total_memory < 8192" | bc -l 2>/dev/null || echo 0) )); then
            add_health_result "Resource-Memory" "PASS" "Memory usage normal" "${total_memory}MB total"
        elif (( $(echo "$total_memory < 16384" | bc -l 2>/dev/null || echo 0) )); then
            add_health_result "Resource-Memory" "WARN" "Memory usage elevated" "${total_memory}MB total"
        else
            add_health_result "Resource-Memory" "FAIL" "Memory usage high" "${total_memory}MB total"
        fi
    else
        add_health_result "Resource-Memory" "WARN" "Cannot determine memory usage" "Stats unavailable"
    fi
    
    # Check disk space
    local disk_usage=$(df -h . | tail -1 | awk '{print $5}' | sed 's/%//' || echo "0")
    if [ "$disk_usage" -lt 80 ]; then
        add_health_result "Resource-Disk" "PASS" "Disk space sufficient" "${disk_usage}% used"
    elif [ "$disk_usage" -lt 90 ]; then
        add_health_result "Resource-Disk" "WARN" "Disk space getting low" "${disk_usage}% used"
    else
        add_health_result "Resource-Disk" "FAIL" "Disk space critical" "${disk_usage}% used"
    fi
}

# Generate comprehensive health report
generate_health_report() {
    local end_time=$(date +%s)
    local duration=$((end_time - START_TIME))
    local timestamp=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    
    # Remove trailing comma from results
    HEALTH_RESULTS=${HEALTH_RESULTS%,}
    
    local report=""
    case "$REPORT_FORMAT" in
        "json")
            report=$(cat << EOF
{
  "health_check": {
    "timestamp": "$timestamp",
    "duration_seconds": $duration,
    "summary": {
      "total_checks": $TOTAL_CHECKS,
      "passed": $PASSED_CHECKS,
      "warnings": $WARNING_CHECKS,
      "failed": $FAILED_CHECKS,
      "overall_status": "$([ $FAILED_CHECKS -eq 0 ] && echo "HEALTHY" || echo "UNHEALTHY")"
    },
    "results": [
      $HEALTH_RESULTS
    ],
    "recommendations": [
$([ $FAILED_CHECKS -gt 0 ] && echo '      "Address failed health checks immediately",' || echo '')
$([ $WARNING_CHECKS -gt 0 ] && echo '      "Review warnings for potential issues",' || echo '')
$([ $PASSED_CHECKS -eq $TOTAL_CHECKS ] && echo '      "System is healthy - maintain current configuration"' || echo '')
    ]
  }
}
EOF
)
            ;;
        "text")
            report=$(cat << EOF
Reynolds Orchestration System Health Report
==========================================
Timestamp: $timestamp
Duration: ${duration}s

Summary:
  Total Checks: $TOTAL_CHECKS
  Passed: $PASSED_CHECKS
  Warnings: $WARNING_CHECKS
  Failed: $FAILED_CHECKS
  Overall Status: $([ $FAILED_CHECKS -eq 0 ] && echo "HEALTHY" || echo "UNHEALTHY")

Results:
$HEALTH_RESULTS

Recommendations:
$([ $FAILED_CHECKS -gt 0 ] && echo "- Address failed health checks immediately" || echo "")
$([ $WARNING_CHECKS -gt 0 ] && echo "- Review warnings for potential issues" || echo "")
$([ $PASSED_CHECKS -eq $TOTAL_CHECKS ] && echo "- System is healthy - maintain current configuration" || echo "")
EOF
)
            ;;
    esac
    
    if [ "$EXPORT_REPORT" = "true" ]; then
        local report_file="health-report-$(date +%Y%m%d_%H%M%S).$REPORT_FORMAT"
        echo "$report" > "$report_file"
        print_success "Health report exported to $report_file"
    fi
    
    if [ "$VERBOSE" = "true" ]; then
        echo ""
        print_info "Detailed Health Report:"
        echo "$report"
    fi
}

# Show health summary
show_health_summary() {
    echo ""
    print_status "Health Check Summary:"
    echo ""
    
    local overall_status="HEALTHY"
    if [ $FAILED_CHECKS -gt 0 ]; then
        overall_status="UNHEALTHY"
    elif [ $WARNING_CHECKS -gt 0 ]; then
        overall_status="DEGRADED"
    fi
    
    case "$overall_status" in
        "HEALTHY")
            print_success "System is HEALTHY 🎉"
            ;;
        "DEGRADED")
            print_warning "System is DEGRADED ⚠️"
            ;;
        "UNHEALTHY")
            print_error "System is UNHEALTHY 🚨"
            ;;
    esac
    
    echo "  📊 Total Checks: $TOTAL_CHECKS"
    echo "  ✅ Passed: $PASSED_CHECKS"
    echo "  ⚠️  Warnings: $WARNING_CHECKS"
    echo "  ❌ Failed: $FAILED_CHECKS"
    echo ""
    
    if [ $FAILED_CHECKS -gt 0 ]; then
        print_error "Critical issues detected - immediate attention required!"
        return 1
    elif [ $WARNING_CHECKS -gt 0 ]; then
        print_warning "Some components need attention - review warnings"
        return 2
    else
        print_success "All systems operational - Maximum Effort™ achieved!"
        return 0
    fi
}

# Main health check function
main() {
    local quick_check=false
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --verbose|-v)
                VERBOSE=true
                shift
                ;;
            --export)
                EXPORT_REPORT=true
                shift
                ;;
            --format)
                REPORT_FORMAT="$2"
                shift 2
                ;;
            --timeout)
                TIMEOUT="$2"
                shift 2
                ;;
            --quick)
                quick_check=true
                shift
                ;;
            --help|-h)
                echo "Usage: $0 [options]"
                echo ""
                echo "Options:"
                echo "  --verbose, -v        Show detailed output"
                echo "  --export             Export detailed report to file"
                echo "  --format FORMAT      Report format: json or text (default: json)"
                echo "  --timeout SECONDS    HTTP request timeout (default: 30)"
                echo "  --quick              Perform quick health check only"
                echo "  --help, -h           Show this help message"
                echo ""
                echo "Examples:"
                echo "  $0                     # Basic health check"
                echo "  $0 --verbose           # Detailed health check"
                echo "  $0 --export --format text  # Export text report"
                echo "  $0 --quick             # Quick status check"
                exit 0
                ;;
            *)
                print_warning "Unknown option: $1"
                shift
                ;;
        esac
    done
    
    print_health_banner
    
    # Initialize results tracking
    init_health_results
    
    # Perform health checks
    check_docker_daemon
    check_container_status
    
    if [ "$quick_check" = "false" ]; then
        check_network_connectivity
        check_service_endpoints
        check_data_persistence
        check_resource_usage
    fi
    
    # Generate report
    generate_health_report
    
    # Show summary and exit with appropriate code
    show_health_summary
}

# Install bc if not available (for floating point comparisons)
if ! command -v bc &> /dev/null; then
    print_debug "bc not found. Some resource calculations may be limited."
fi

# Run main function
main "$@"