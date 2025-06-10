#!/bin/bash

# Validation script for Power Platform Copilot Agent environment
# This script validates the development environment setup

set -e

echo "🔍 Validating Power Platform Copilot Agent Environment..."
echo "============================================================="

# Check .NET SDK
echo "📋 Checking .NET SDK..."
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo "✅ .NET SDK found: $DOTNET_VERSION"
else
    echo "❌ .NET SDK not found"
    exit 1
fi

# Check Power Platform CLI
echo "📋 Checking Power Platform CLI..."
if command -v pac &> /dev/null; then
    PAC_VERSION=$(pac help | head -n 1 || echo "Version info not available")
    echo "✅ Power Platform CLI found: $PAC_VERSION"
else
    echo "⚠️  Power Platform CLI not found (optional for build)"
fi

# Check Microsoft 365 CLI
echo "📋 Checking Microsoft 365 CLI..."
if command -v m365 &> /dev/null; then
    M365_VERSION=$(m365 --version 2>/dev/null || echo "Version info not available")
    echo "✅ Microsoft 365 CLI found: $M365_VERSION"
else
    echo "⚠️  Microsoft 365 CLI not found (optional for build)"
fi

# Check Git
echo "📋 Checking Git..."
if command -v git &> /dev/null; then
    GIT_VERSION=$(git --version)
    echo "✅ Git found: $GIT_VERSION"
else
    echo "❌ Git not found"
    exit 1
fi

# Check project structure
echo "📋 Validating project structure..."
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"

if [ -f "$PROJECT_ROOT/src/CopilotAgent.sln" ]; then
    echo "✅ Solution file found"
else
    echo "❌ Solution file not found"
    exit 1
fi

if [ -f "$PROJECT_ROOT/src/CopilotAgent/CopilotAgent.csproj" ]; then
    echo "✅ Main project file found"
else
    echo "❌ Main project file not found"
    exit 1
fi

if [ -f "$PROJECT_ROOT/src/CopilotAgent.Tests/CopilotAgent.Tests.csproj" ]; then
    echo "✅ Test project file found"
else
    echo "❌ Test project file not found"
    exit 1
fi

# Build the solution
echo "📋 Building solution..."
cd "$PROJECT_ROOT/src"
if dotnet build > /dev/null 2>&1; then
    echo "✅ Solution builds successfully"
else
    echo "❌ Solution build failed"
    exit 1
fi

# Run tests
echo "📋 Running tests..."
if dotnet test > /dev/null 2>&1; then
    echo "✅ All tests pass"
else
    echo "❌ Some tests failed"
    exit 1
fi

# Check documentation
echo "📋 Checking documentation..."
if [ -f "$PROJECT_ROOT/docs/README.md" ]; then
    echo "✅ Documentation found"
else
    echo "❌ Documentation not found"
    exit 1
fi

echo ""
echo "🎉 Environment validation completed successfully!"
echo "✅ Ready for Power Platform Copilot Agent development"
echo ""
echo "Next steps:"
echo "1. Configure authentication: pac auth create --name dev-auth"
echo "2. Verify M365 access: m365 login"
echo "3. Start development: cd src && dotnet run --project CopilotAgent"