#!/bin/bash

# Setup script for Power Platform Copilot Agent development environment
# This script sets up the complete development environment

set -e

echo "🚀 Setting up Power Platform Copilot Agent Development Environment"
echo "=================================================================="

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to install .NET SDK
install_dotnet() {
    echo "📦 Installing .NET 8.0 SDK..."
    
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        # Linux installation
        wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        sudo dpkg -i packages-microsoft-prod.deb
        rm packages-microsoft-prod.deb
        sudo apt-get update
        sudo apt-get install -y apt-transport-https dotnet-sdk-8.0
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS installation
        if command_exists brew; then
            brew install --cask dotnet-sdk
        else
            echo "Please install Homebrew first: https://brew.sh/"
            exit 1
        fi
    else
        echo "Please install .NET 8.0 SDK manually: https://dotnet.microsoft.com/download"
        exit 1
    fi
}

# Function to install Power Platform CLI
install_pac_cli() {
    echo "📦 Installing Power Platform CLI..."
    
    if command_exists dotnet; then
        dotnet tool install --global Microsoft.PowerApps.CLI.Tool
    else
        echo "❌ .NET SDK required for Power Platform CLI"
        exit 1
    fi
}

# Function to install Microsoft 365 CLI
install_m365_cli() {
    echo "📦 Installing Microsoft 365 CLI..."
    
    if command_exists npm; then
        npm install -g @pnp/cli-microsoft365
    else
        echo "❌ Node.js/npm required for Microsoft 365 CLI"
        echo "Please install Node.js: https://nodejs.org/"
        exit 1
    fi
}

# Function to install GitHub CLI
install_gh_cli() {
    echo "📦 Installing GitHub CLI..."
    
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg
        sudo chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg
        echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null
        sudo apt update
        sudo apt install gh
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        if command_exists brew; then
            brew install gh
        else
            echo "Please install Homebrew first: https://brew.sh/"
            exit 1
        fi
    else
        echo "Please install GitHub CLI manually: https://cli.github.com/"
        exit 1
    fi
}

# Check and install .NET SDK
if ! command_exists dotnet; then
    install_dotnet
else
    echo "✅ .NET SDK already installed"
fi

# Check and install Power Platform CLI
if ! command_exists pac; then
    echo "🤔 Power Platform CLI not found. Install it? (y/n)"
    read -r response
    if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
        install_pac_cli
    fi
else
    echo "✅ Power Platform CLI already installed"
fi

# Check and install Microsoft 365 CLI
if ! command_exists m365; then
    echo "🤔 Microsoft 365 CLI not found. Install it? (y/n)"
    read -r response
    if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
        install_m365_cli
    fi
else
    echo "✅ Microsoft 365 CLI already installed"
fi

# Check and install GitHub CLI
if ! command_exists gh; then
    echo "🤔 GitHub CLI not found. Install it? (y/n)"
    read -r response
    if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
        install_gh_cli
    fi
else
    echo "✅ GitHub CLI already installed"
fi

# Setup project
echo "📋 Setting up project..."
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"

cd "$PROJECT_ROOT"

# Restore and build
echo "📦 Restoring NuGet packages..."
cd src
dotnet restore

echo "🔨 Building solution..."
dotnet build

echo "🧪 Running tests..."
dotnet test

# Setup Git hooks (if in a git repo)
if [ -d "$PROJECT_ROOT/.git" ]; then
    echo "📝 Setting up Git hooks..."
    
    # Pre-commit hook
    cat > "$PROJECT_ROOT/.git/hooks/pre-commit" << 'EOF'
#!/bin/bash
# Pre-commit hook for Power Platform Copilot Agent

echo "🔍 Running pre-commit validation..."

# Run tests
cd src
if ! dotnet test --verbosity quiet; then
    echo "❌ Tests failed. Commit aborted."
    exit 1
fi

# Run build
if ! dotnet build --verbosity quiet; then
    echo "❌ Build failed. Commit aborted."
    exit 1
fi

echo "✅ Pre-commit validation passed"
EOF

    chmod +x "$PROJECT_ROOT/.git/hooks/pre-commit"
    echo "✅ Git pre-commit hook installed"
fi

# Create VSCode settings if not exists
if [ ! -f "$PROJECT_ROOT/.vscode/settings.json" ] && command_exists code; then
    echo "📝 Creating VSCode settings..."
    mkdir -p "$PROJECT_ROOT/.vscode"
    
    cat > "$PROJECT_ROOT/.vscode/settings.json" << 'EOF'
{
    "dotnet.defaultSolution": "src/CopilotAgent.sln",
    "omnisharp.enableRoslynAnalyzers": true,
    "editor.formatOnSave": true,
    "editor.codeActionsOnSave": {
        "source.fixAll": true,
        "source.organizeImports": true
    },
    "files.exclude": {
        "**/bin": true,
        "**/obj": true,
        "**/.vs": true
    },
    "dotnet.completion.showCompletionItemsFromUnimportedNamespaces": true,
    "csharp.semanticHighlighting.enabled": true
}
EOF
    echo "✅ VSCode settings created"
fi

echo ""
echo "🎉 Setup completed successfully!"
echo ""
echo "📚 Next steps:"
echo "1. Configure authentication:"
echo "   - Power Platform: pac auth create --name dev-auth"
echo "   - Microsoft 365: m365 login"
echo "   - GitHub: gh auth login"
echo ""
echo "2. Start development:"
echo "   cd src && dotnet run --project CopilotAgent"
echo ""
echo "3. Run validation:"
echo "   ./scripts/validation/validate-environment.sh"
echo ""
echo "4. Read documentation:"
echo "   - Main docs: docs/README.md"
echo "   - CLI guides: docs/cli-tools/"
echo "   - GitHub Copilot: docs/github-copilot/"
echo ""
echo "Happy coding! 🚀"