#!/bin/bash
set -e

echo "🚀 Power Platform Copilot Agent - Post Create Setup"
echo "=================================================="

# Install Power Platform CLI
echo "📦 Installing Power Platform CLI..."
dotnet tool install --global Microsoft.PowerApps.CLI.Tool

# Install Microsoft 365 CLI
echo "📦 Installing Microsoft 365 CLI..."
npm install -g @pnp/cli-microsoft365

# Set up git configuration for development
echo "🔧 Setting up Git configuration..."
git config --global --add safe.directory ${PWD}

# Create .env file from template if it doesn't exist
if [ ! -f .env ]; then
    echo "📝 Creating .env file from template..."
    cp .env.example .env 2>/dev/null || echo "# Environment Variables" > .env
fi

# Restore .NET packages
echo "📦 Restoring .NET packages..."
cd src
dotnet restore

# Build the solution
echo "🔨 Building solution..."
dotnet build

# Create welcome script
echo "📋 Creating welcome script..."
cat > /usr/local/bin/reynolds << 'EOF'
#!/bin/bash

case "${1:-help}" in
    "onboard"|"welcome")
        echo "🎉 Welcome to Power Platform Development!"
        echo ""
        echo "This Codespace includes:"
        echo "  ✅ Power Platform CLI (pac)"
        echo "  ✅ Microsoft 365 CLI (m365)"
        echo "  ✅ .NET 8 SDK"
        echo "  ✅ Recommended VS Code extensions"
        echo ""
        echo "🚀 Quick Start:"
        echo "  1. Explore: ls -la && cat README.md"
        echo "  2. Build: cd src && dotnet build"
        echo "  3. Test: cd src && dotnet test"
        echo "  4. Run: cd src && dotnet run --project CopilotAgent"
        echo ""
        echo "📚 Documentation: ./docs/"
        echo "📋 Implementation Plan: ./STRATEGIC_IMPLEMENTATION_PLAN.md"
        echo ""
        echo "💡 Need help? Ask Reynolds: @reynolds <your question>"
        ;;
    "build")
        cd src && dotnet build
        ;;
    "test")
        cd src && dotnet test
        ;;
    "run")
        cd src && dotnet run --project CopilotAgent
        ;;
    "tools")
        echo "🛠️ Installed Tools:"
        echo "  pac: $(pac --version 2>/dev/null || echo 'Not available')"
        echo "  m365: $(m365 --version 2>/dev/null || echo 'Not available')"
        echo "  dotnet: $(dotnet --version)"
        echo "  node: $(node --version)"
        echo "  npm: $(npm --version)"
        ;;
    *)
        echo "🎭 Reynolds Copilot Agent Helper"
        echo ""
        echo "Commands:"
        echo "  reynolds onboard    - Show welcome and onboarding info"
        echo "  reynolds build      - Build the solution"
        echo "  reynolds test       - Run tests"
        echo "  reynolds run        - Start the development server"
        echo "  reynolds tools      - Show installed tool versions"
        echo ""
        echo "💡 This is your Power Platform development environment!"
        ;;
esac
EOF

chmod +x /usr/local/bin/reynolds

echo "✅ Post-create setup completed!"
echo ""
echo "🎉 Welcome! Your Power Platform development environment is ready."
echo "   Type 'reynolds onboard' to get started!"