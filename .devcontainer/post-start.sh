#!/bin/bash
set -e

echo "🌟 Power Platform Copilot Agent - Starting up..."

# Show interactive welcome message
echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║  🎭 Welcome to Power Platform Copilot Agent Development!      ║"
echo "║                                                                ║"
echo "║  🚀 Your development environment is ready to use!             ║"
echo "║                                                                ║"
echo "║  Quick Start:                                                  ║"
echo "║    • Type 'reynolds onboard' for guided setup                 ║"
echo "║    • Run 'reynolds tools' to check tool installations         ║"
echo "║    • Use 'reynolds build' to build the project                ║"
echo "║                                                                ║"
echo "║  💡 Need help? The Reynolds agent is here to assist!          ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

# Check if this is the first startup
if [ ! -f ~/.reynolds-first-run ]; then
    echo "🎯 This appears to be your first time in this Codespace!"
    echo "   Run 'reynolds onboard' to start your interactive setup experience."
    echo ""
    touch ~/.reynolds-first-run
fi

# Verify tools are available
echo "🔍 Verifying development tools..."
if command -v pac >/dev/null 2>&1; then
    echo "  ✅ Power Platform CLI ready"
else
    echo "  ⏳ Power Platform CLI installing..."
fi

if command -v m365 >/dev/null 2>&1; then
    echo "  ✅ Microsoft 365 CLI ready"
else
    echo "  ⏳ Microsoft 365 CLI installing..."
fi

echo "  ✅ .NET SDK ready ($(dotnet --version))"
echo ""

# Display helpful tips
echo "💡 Pro Tips:"
echo "  • Use GitHub Copilot with '@reynolds' for specialized assistance"
echo "  • Check ./docs/ for comprehensive documentation"
echo "  • The agent supports CLI automation, code generation, and more!"
echo ""
echo "Happy coding! 🎉"