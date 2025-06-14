#!/bin/bash

# Reynolds Autonomous Container Orchestration System - Comprehensive Validation Script
# This script validates and tests the entire orchestration system for deployment readiness

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
VALIDATION_LOG="validation-$(date +%Y%m%d_%H%M%S).log"
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0
USE_SIMPLE_DOCKERFILES=true

# Function to print colored output
print_header() {
    echo -e "${BLUE}🎭🔧════════════════════════════════════════════════════════════════════🔧🎭${NC}"
    echo -e "${BLUE}║${NC}                ${YELLOW}Reynolds Orchestration System Validation${NC}                        ${BLUE}║${NC}"
    echo -e "${BLUE}║${NC}                          ${PURPLE}Maximum Effort™ Testing${NC}                              ${BLUE}║${NC}"
    echo -e "${BLUE}🎭🔧════════════════════════════════════════════════════════════════════🔧🎭${NC}"
    echo ""
}

print_test() {
    echo -e "${CYAN}🧪 Test:${NC} $1"
}

print_success() {
    echo -e "${GREEN}✅${NC} $1"
    ((PASSED_TESTS++))
}

print_error() {
    echo -e "${RED}❌${NC} $1"
    ((FAILED_TESTS++))
}

print_info() {
    echo -e "${PURPLE}ℹ️${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠️${NC} $1"
}

# Initialize validation log
init_validation() {
    echo "Reynolds Orchestration System Validation - $(date)" > "$VALIDATION_LOG"
    echo "=========================================" >> "$VALIDATION_LOG"
    echo "" >> "$VALIDATION_LOG"
}

# Log test result
log_test() {
    local test_name="$1"
    local result="$2"
    local details="$3"
    
    ((TOTAL_TESTS++))
    echo "[$result] $test_name" >> "$VALIDATION_LOG"
    if [ -n "$details" ]; then
        echo "    Details: $details" >> "$VALIDATION_LOG"
    fi
    echo "" >> "$VALIDATION_LOG"
}

# Test 1: Validate Prerequisites
test_prerequisites() {
    print_test "Prerequisites Check"
    
    # Check Docker
    if command -v docker &> /dev/null && docker info &> /dev/null; then
        local docker_version=$(docker version --format '{{.Server.Version}}')
        print_success "Docker is available (version: $docker_version)"
        log_test "Docker Prerequisites" "PASS" "Version: $docker_version"
    else
        print_error "Docker is not available or not running"
        log_test "Docker Prerequisites" "FAIL" "Docker not found or daemon not running"
        return 1
    fi
    
    # Check Docker Compose
    if command -v docker-compose &> /dev/null || docker compose version &> /dev/null; then
        print_success "Docker Compose is available"
        log_test "Docker Compose Prerequisites" "PASS" "Command available"
    else
        print_error "Docker Compose is not available"
        log_test "Docker Compose Prerequisites" "FAIL" "Command not found"
        return 1
    fi
    
    # Check available resources
    local available_memory=$(docker system info --format '{{.MemTotal}}' 2>/dev/null || echo "0")
    if [ "$available_memory" -gt 4000000000 ]; then
        print_success "Sufficient memory available ($(($available_memory / 1000000000))GB)"
        log_test "Memory Prerequisites" "PASS" "$(($available_memory / 1000000000))GB available"
    else
        print_warning "Limited memory available - may impact performance"
        log_test "Memory Prerequisites" "WARN" "$(($available_memory / 1000000000))GB available"
    fi
}

# Test 2: Validate Node.js Dependencies
test_nodejs_dependencies() {
    print_test "Node.js Dependencies Validation"
    
    local components=("reynolds" "agents/devops-polyglot" "agents/platform-specialist" "agents/code-intelligence")
    
    for component in "${components[@]}"; do
        if [ -f "$component/package.json" ]; then
            cd "$component"
            if npm install --dry-run > /dev/null 2>&1; then
                print_success "$component dependencies are valid"
                log_test "Dependencies: $component" "PASS" "npm install --dry-run successful"
            else
                print_error "$component dependencies have issues"
                log_test "Dependencies: $component" "FAIL" "npm install --dry-run failed"
            fi
            cd - > /dev/null
        else
            print_error "$component/package.json not found"
            log_test "Dependencies: $component" "FAIL" "package.json missing"
        fi
    done
}

# Test 3: Validate Docker Builds
test_docker_builds() {
    print_test "Docker Image Build Validation"
    
    # Test Reynolds build
    print_info "Building Reynolds orchestrator..."
    if docker build -t reynolds-test:latest ./reynolds > /dev/null 2>&1; then
        print_success "Reynolds orchestrator builds successfully"
        log_test "Docker Build: Reynolds" "PASS" "Image built successfully"
    else
        print_error "Reynolds orchestrator build failed"
        log_test "Docker Build: Reynolds" "FAIL" "Docker build failed"
    fi
    
    # Test agent builds with simplified Dockerfiles
    local agents=("devops-polyglot" "platform-specialist" "code-intelligence")
    
    for agent in "${agents[@]}"; do
        print_info "Building $agent agent..."
        local dockerfile="Dockerfile"
        if [ "$USE_SIMPLE_DOCKERFILES" = "true" ] && [ -f "agents/$agent/Dockerfile.simple" ]; then
            dockerfile="Dockerfile.simple"
        fi
        
        if docker build -f "agents/$agent/$dockerfile" -t "$agent-test:latest" "./agents/$agent" > /dev/null 2>&1; then
            print_success "$agent agent builds successfully"
            log_test "Docker Build: $agent" "PASS" "Image built with $dockerfile"
        else
            print_error "$agent agent build failed"
            log_test "Docker Build: $agent" "FAIL" "Build failed with $dockerfile"
        fi
    done
}

# Test 4: Validate Docker Compose Configuration
test_docker_compose_config() {
    print_test "Docker Compose Configuration Validation"
    
    # Check main compose file
    if docker-compose -f docker-compose.yml config > /dev/null 2>&1; then
        print_success "Main docker-compose.yml is valid"
        log_test "Compose Config: Main" "PASS" "Syntax validation passed"
    else
        print_error "Main docker-compose.yml has syntax errors"
        log_test "Compose Config: Main" "FAIL" "Syntax validation failed"
    fi
    
    # Check orchestration compose file
    if docker-compose -f docker-compose.orchestration.yaml config > /dev/null 2>&1; then
        print_success "docker-compose.orchestration.yaml is valid"
        log_test "Compose Config: Orchestration" "PASS" "Syntax validation passed"
    else
        print_error "docker-compose.orchestration.yaml has syntax errors"
        log_test "Compose Config: Orchestration" "FAIL" "Syntax validation failed"
    fi
}

# Test 5: Validate File Structure and Permissions
test_file_structure() {
    print_test "File Structure and Permissions Validation"
    
    # Check essential files
    local essential_files=(
        "docker-compose.yml"
        "docker-compose.orchestration.yaml"
        "start-orchestration.sh"
        "stop-orchestration.sh"
        "health-check.sh"
        "reynolds/Dockerfile"
        "reynolds/package.json"
        "reynolds/src/index.js"
    )
    
    for file in "${essential_files[@]}"; do
        if [ -f "$file" ]; then
            print_success "$file exists"
            log_test "File Structure: $file" "PASS" "File exists"
        else
            print_error "$file is missing"
            log_test "File Structure: $file" "FAIL" "File missing"
        fi
    done
    
    # Check script permissions
    local scripts=("start-orchestration.sh" "stop-orchestration.sh" "health-check.sh")
    for script in "${scripts[@]}"; do
        if [ -x "$script" ]; then
            print_success "$script is executable"
            log_test "Permissions: $script" "PASS" "Script is executable"
        else
            print_error "$script is not executable"
            log_test "Permissions: $script" "FAIL" "Script not executable"
        fi
    done
}

# Test 6: Test System Startup
test_system_startup() {
    print_test "System Startup Test"
    
    print_info "Starting system with simplified configuration..."
    
    # Backup original Dockerfiles and use simplified ones
    if [ "$USE_SIMPLE_DOCKERFILES" = "true" ]; then
        for agent in "devops-polyglot" "platform-specialist" "code-intelligence"; do
            if [ -f "agents/$agent/Dockerfile.simple" ]; then
                cp "agents/$agent/Dockerfile" "agents/$agent/Dockerfile.original" 2>/dev/null || true
                cp "agents/$agent/Dockerfile.simple" "agents/$agent/Dockerfile"
            fi
        done
    fi
    
    # Create minimal secrets for testing
    mkdir -p secrets
    echo "test_token" > secrets/github_token.txt
    echo "{}" > secrets/azure_credentials.json
    echo "{}" > secrets/m365_credentials.json
    echo "test-tenant-id" > secrets/tenant_id.txt
    echo "test-key" > secrets/openai_api_key.txt
    echo "testpassword123" > secrets/db_password.txt
    echo "admin" > secrets/grafana_password.txt
    
    # Start core services only for testing
    if timeout 300 ./start-orchestration.sh start > startup.log 2>&1; then
        print_success "System startup completed"
        log_test "System Startup" "PASS" "Started successfully"
        
        # Wait a moment for services to initialize
        sleep 30
        
        return 0
    else
        print_error "System startup failed"
        log_test "System Startup" "FAIL" "Startup timeout or error"
        print_info "Check startup.log for details"
        return 1
    fi
}

# Test 7: Test Health Checks
test_health_checks() {
    print_test "Health Check Validation"
    
    # Test Reynolds health endpoint
    if curl -f -s http://localhost:8080/health > /dev/null 2>&1; then
        print_success "Reynolds health endpoint is responsive"
        log_test "Health Check: Reynolds" "PASS" "HTTP 200 response"
    else
        print_error "Reynolds health endpoint is not responding"
        log_test "Health Check: Reynolds" "FAIL" "No response or HTTP error"
    fi
    
    # Test infrastructure services
    if docker ps --format '{{.Names}}' | grep -q postgres; then
        if docker exec "${PROJECT_NAME}_postgres_1" pg_isready -U reynolds > /dev/null 2>&1; then
            print_success "PostgreSQL is healthy"
            log_test "Health Check: PostgreSQL" "PASS" "Database ready"
        else
            print_error "PostgreSQL health check failed"
            log_test "Health Check: PostgreSQL" "FAIL" "pg_isready failed"
        fi
    else
        print_warning "PostgreSQL container not running"
        log_test "Health Check: PostgreSQL" "WARN" "Container not found"
    fi
    
    if docker ps --format '{{.Names}}' | grep -q redis; then
        if docker exec "${PROJECT_NAME}_redis_1" redis-cli ping > /dev/null 2>&1; then
            print_success "Redis is healthy"
            log_test "Health Check: Redis" "PASS" "Redis ping successful"
        else
            print_error "Redis health check failed"
            log_test "Health Check: Redis" "FAIL" "Redis ping failed"
        fi
    else
        print_warning "Redis container not running"
        log_test "Health Check: Redis" "WARN" "Container not found"
    fi
}

# Test 8: Test MCP Functionality
test_mcp_functionality() {
    print_test "MCP Functionality Test"
    
    # Test MCP capabilities endpoint
    if curl -f -s http://localhost:8080/mcp/capabilities > /dev/null 2>&1; then
        local capabilities=$(curl -s http://localhost:8080/mcp/capabilities 2>/dev/null)
        if echo "$capabilities" | jq -e '.capabilities' > /dev/null 2>&1; then
            print_success "MCP capabilities endpoint is functional"
            log_test "MCP: Capabilities" "PASS" "Valid JSON response"
        else
            print_error "MCP capabilities endpoint returns invalid data"
            log_test "MCP: Capabilities" "FAIL" "Invalid JSON response"
        fi
    else
        print_error "MCP capabilities endpoint is not accessible"
        log_test "MCP: Capabilities" "FAIL" "HTTP error or no response"
    fi
    
    # Test MCP tools endpoint
    if curl -f -s http://localhost:8080/mcp/tools > /dev/null 2>&1; then
        print_success "MCP tools endpoint is accessible"
        log_test "MCP: Tools" "PASS" "HTTP 200 response"
    else
        print_error "MCP tools endpoint is not accessible"
        log_test "MCP: Tools" "FAIL" "HTTP error or no response"
    fi
}

# Test 9: Test System Shutdown
test_system_shutdown() {
    print_test "System Shutdown Test"
    
    print_info "Initiating graceful shutdown..."
    
    if timeout 120 ./stop-orchestration.sh --force > shutdown.log 2>&1; then
        print_success "System shutdown completed successfully"
        log_test "System Shutdown" "PASS" "Graceful shutdown successful"
    else
        print_error "System shutdown failed or timed out"
        log_test "System Shutdown" "FAIL" "Shutdown timeout or error"
    fi
    
    # Restore original Dockerfiles
    if [ "$USE_SIMPLE_DOCKERFILES" = "true" ]; then
        for agent in "devops-polyglot" "platform-specialist" "code-intelligence"; do
            if [ -f "agents/$agent/Dockerfile.original" ]; then
                mv "agents/$agent/Dockerfile.original" "agents/$agent/Dockerfile"
            fi
        done
    fi
}

# Cleanup function
cleanup_test_environment() {
    print_info "Cleaning up test environment..."
    
    # Force stop any remaining containers
    docker-compose -f docker-compose.yml down --timeout 30 > /dev/null 2>&1 || true
    
    # Remove test images
    docker rmi reynolds-test:latest devops-polyglot-test:latest platform-specialist-test:latest code-intelligence-test:latest > /dev/null 2>&1 || true
    
    # Clean up test files
    rm -f startup.log shutdown.log
}

# Generate validation report
generate_validation_report() {
    echo "" >> "$VALIDATION_LOG"
    echo "Validation Summary:" >> "$VALIDATION_LOG"
    echo "Total Tests: $TOTAL_TESTS" >> "$VALIDATION_LOG"
    echo "Passed: $PASSED_TESTS" >> "$VALIDATION_LOG"
    echo "Failed: $FAILED_TESTS" >> "$VALIDATION_LOG"
    echo "Success Rate: $(( PASSED_TESTS * 100 / TOTAL_TESTS ))%" >> "$VALIDATION_LOG"
    
    local overall_status="PASS"
    if [ $FAILED_TESTS -gt 0 ]; then
        overall_status="FAIL"
    fi
    
    echo "Overall Status: $overall_status" >> "$VALIDATION_LOG"
    echo "Completed: $(date)" >> "$VALIDATION_LOG"
}

# Main validation function
main() {
    print_header
    
    init_validation
    
    print_info "Starting comprehensive validation of Reynolds Orchestration System..."
    echo ""
    
    # Run all validation tests
    test_prerequisites || true
    test_nodejs_dependencies || true
    test_docker_builds || true
    test_docker_compose_config || true
    test_file_structure || true
    
    # System integration tests
    if test_system_startup; then
        test_health_checks || true
        test_mcp_functionality || true
        test_system_shutdown || true
    else
        print_warning "Skipping integration tests due to startup failure"
    fi
    
    # Cleanup
    cleanup_test_environment
    
    # Generate final report
    generate_validation_report
    
    echo ""
    print_info "Validation Summary:"
    echo "  📊 Total Tests: $TOTAL_TESTS"
    echo "  ✅ Passed: $PASSED_TESTS"
    echo "  ❌ Failed: $FAILED_TESTS"
    echo "  📈 Success Rate: $(( PASSED_TESTS * 100 / TOTAL_TESTS ))%"
    echo ""
    
    if [ $FAILED_TESTS -eq 0 ]; then
        print_success "🎉 All validation tests passed! System is ready for deployment!"
        echo ""
        print_info "To start the system: ./start-orchestration.sh start"
        print_info "To monitor health: ./health-check.sh"
        print_info "To stop the system: ./stop-orchestration.sh"
        echo ""
        print_info "Detailed validation log saved to: $VALIDATION_LOG"
        exit 0
    else
        print_error "❌ Validation failed! $FAILED_TESTS test(s) failed."
        print_info "Please review the issues and fix them before deployment."
        print_info "Detailed validation log saved to: $VALIDATION_LOG"
        exit 1
    fi
}

# Run main function
main "$@"