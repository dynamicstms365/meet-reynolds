#!/bin/bash
# Reynolds: Maximum Effort™ SEQ.NET + CopilotAgent Deployment Script
# Supernatural structured logging orchestration with beautiful web interface

set -e
set -o pipefail

# Colors for Reynolds' charming output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Reynolds banner with Maximum Effort™
echo -e "${PURPLE}🎭 ================================================${NC}"
echo -e "${PURPLE}   Reynolds SEQ.NET Logging Orchestration${NC}"
echo -e "${PURPLE}   Maximum Effort™ Applied to Log Visibility${NC}"
echo -e "${PURPLE}================================================${NC}"

# Function to print Reynolds-style messages
reynolds_log() {
    echo -e "${CYAN}🎭 Reynolds:${NC} $1"
}

reynolds_success() {
    echo -e "${GREEN}✅ Reynolds:${NC} $1"
}

reynolds_warning() {
    echo -e "${YELLOW}⚠️  Reynolds:${NC} $1"
}

reynolds_error() {
    echo -e "${RED}❌ Reynolds:${NC} $1"
}

# Step 1: Verify Prerequisites
reynolds_log "Verifying deployment prerequisites with supernatural precision..."

if ! command -v docker &> /dev/null; then
    reynolds_error "Docker not found! Even Reynolds needs container orchestration."
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    reynolds_error "Docker Compose not found! Parallel service deployment requires orchestration."
    exit 1
fi

reynolds_success "Prerequisites verified - Maximum Effort™ deployment ready!"

# Step 2: Clean up any existing containers
reynolds_log "Cleaning up existing containers for fresh deployment..."
docker-compose -f docker-compose.seq.yml down --volumes --remove-orphans 2>/dev/null || true
reynolds_success "Clean slate achieved - ready for supernatural deployment!"

# Step 3: Build the .NET 9.0 application
reynolds_log "Building .NET 9.0 CopilotAgent with Serilog integration..."
if [ ! -f "src/CopilotAgent/CopilotAgent.csproj" ]; then
    reynolds_error "CopilotAgent project not found! Check your project structure."
    exit 1
fi

# Restore and build with maximum effort
cd src/CopilotAgent
dotnet restore --verbosity minimal
dotnet build --configuration Release --no-restore --verbosity minimal
cd ../..

reynolds_success ".NET 9.0 build completed with supernatural efficiency!"

# Step 4: Deploy SEQ + CopilotAgent in parallel
reynolds_log "Orchestrating parallel deployment of SEQ.NET and CopilotAgent..."
reynolds_log "📊 SEQ Web Interface will be available at: http://localhost:8080"
reynolds_log "🤖 CopilotAgent API will be available at: http://localhost:8000"
reynolds_log "🎛️  Portainer Dashboard will be available at: http://localhost:9000"

# Deploy with build and detached mode for supernatural monitoring
docker-compose -f docker-compose.seq.yml up --build --detach

# Step 5: Health check orchestration
reynolds_log "Performing health checks with Maximum Effort™ monitoring..."

# Wait for SEQ to be healthy
reynolds_log "⏳ Waiting for SEQ.NET to achieve supernatural health..."
timeout=120
elapsed=0
while [ $elapsed -lt $timeout ]; do
    if curl -f -s http://localhost:8080/health >/dev/null 2>&1; then
        reynolds_success "SEQ.NET is healthy and ready for supernatural logging!"
        break
    fi
    sleep 5
    elapsed=$((elapsed + 5))
    echo -n "."
done

if [ $elapsed -ge $timeout ]; then
    reynolds_warning "SEQ.NET taking longer than expected - checking container status..."
    docker-compose -f docker-compose.seq.yml logs seq
fi

# Wait for CopilotAgent to be healthy
reynolds_log "⏳ Waiting for CopilotAgent to achieve Reynolds-level readiness..."
elapsed=0
while [ $elapsed -lt $timeout ]; do
    if curl -f -s http://localhost:8000/health >/dev/null 2>&1; then
        reynolds_success "CopilotAgent is healthy and logging with supernatural precision!"
        break
    fi
    sleep 5
    elapsed=$((elapsed + 5))
    echo -n "."
done

if [ $elapsed -ge $timeout ]; then
    reynolds_warning "CopilotAgent taking longer than expected - checking container status..."
    docker-compose -f docker-compose.seq.yml logs copilot-agent
fi

# Step 6: Display deployment summary with Reynolds charm
echo -e "\n${PURPLE}🎊 ================================================${NC}"
echo -e "${PURPLE}   SUPERNATURAL DEPLOYMENT COMPLETE!${NC}"
echo -e "${PURPLE}================================================${NC}"

reynolds_success "SEQ.NET Logging Dashboard: ${CYAN}http://localhost:8080${NC}"
echo -e "  👤 Username: ${YELLOW}admin${NC}"
echo -e "  🔑 Password: ${YELLOW}SupernaturalLogging123!${NC}"

reynolds_success "CopilotAgent API: ${CYAN}http://localhost:8000${NC}"
echo -e "  📖 API Docs: ${CYAN}http://localhost:8000/api-docs${NC}"
echo -e "  ❤️  Health Check: ${CYAN}http://localhost:8000/health${NC}"

reynolds_success "Portainer Container Management: ${CYAN}http://localhost:9000${NC}"

echo -e "\n${CYAN}🎭 Reynolds' Supernatural Logging Commands:${NC}"
echo -e "${YELLOW}# View live logs with supernatural formatting:${NC}"
echo -e "docker-compose -f docker-compose.seq.yml logs -f"
echo -e ""
echo -e "${YELLOW}# Test structured logging:${NC}"
echo -e "curl http://localhost:8000/health"
echo -e ""
echo -e "${YELLOW}# Generate test log entries:${NC}"
echo -e "curl -X POST http://localhost:8000/api/communication/send-message \\"
echo -e "  -H 'Content-Type: application/json' \\"
echo -e "  -d '{\"userIdentifier\": \"test@example.com\", \"message\": \"Testing SEQ logging!\"}'"
echo -e ""
echo -e "${YELLOW}# Stop the supernatural logging orchestra:${NC}"
echo -e "docker-compose -f docker-compose.seq.yml down"
echo -e ""
echo -e "${YELLOW}# Stop and clean everything (including volumes):${NC}"
echo -e "docker-compose -f docker-compose.seq.yml down --volumes"

echo -e "\n${GREEN}🎭 Reynolds says: Your logs will never look this good with plain console output!${NC}"
echo -e "${GREEN}   Visit SEQ at http://localhost:8080 for supernatural log visibility.${NC}"

# Step 7: Optional - Open browser to SEQ dashboard
if command -v xdg-open &> /dev/null; then
    reynolds_log "Opening SEQ dashboard in your browser with Maximum Effort™..."
    sleep 3
    xdg-open http://localhost:8080 2>/dev/null || true
elif command -v open &> /dev/null; then
    reynolds_log "Opening SEQ dashboard in your browser with Maximum Effort™..."
    sleep 3
    open http://localhost:8080 2>/dev/null || true
fi

echo -e "\n${PURPLE}🎭 Maximum Effort™ deployment orchestration complete!${NC}"