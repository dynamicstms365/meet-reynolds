#!/bin/bash

# Local Reynolds Teams Testing Script
# For testing Reynolds before he slides into production

set -e

echo "🎭 Starting Reynolds Local Testing Environment..."
echo "Perfect for testing before sliding into production DMs!"

# Check if we're in the right directory
if [ ! -f "src/CopilotAgent/CopilotAgent.csproj" ]; then
    echo "❌ Please run this script from the project root directory"
    exit 1
fi

# Set local environment variables
export ASPNETCORE_ENVIRONMENT=Development
export EnableTeamsIntegration=true
export REYNOLDS_ORG_MODE=true
export TARGET_ORGANIZATION=dynamicstms365

# Set mock Teams credentials for local testing
export MicrosoftAppId="local-test-app-id"
export MicrosoftAppPassword="local-test-password"
export TenantId="local-test-tenant"
export BotUserId="reynolds@local.test"

echo "🔧 Environment configured for local Reynolds testing"
echo "   Teams Integration: Enabled"
echo "   Reynolds Mode: Organizational Orchestration"
echo "   Target Org: dynamicstms365"

# Build the project
echo "📦 Building Reynolds with Teams capabilities..."
dotnet build src/CopilotAgent/CopilotAgent.csproj

# Start the local server
echo "🚀 Starting Reynolds local server..."
echo "   URL: http://localhost:5000"
echo "   Teams Health: http://localhost:5000/api/health/teams"
echo "   Reynolds Status: http://localhost:5000/api/reynolds/status"
echo ""
echo "📋 Test Commands (run in another terminal):"
echo "   curl http://localhost:5000/api/reynolds/status"
echo "   curl -X POST http://localhost:5000/api/reynolds/test-chat \\"
echo "     -H 'Content-Type: application/json' \\"
echo "     -d '{\"userEmail\":\"test@example.com\",\"useM365Cli\":true}'"
echo ""
echo "Press Ctrl+C to stop Reynolds"
echo ""

# Run the application
cd src/CopilotAgent
dotnet run --urls="http://localhost:5000"