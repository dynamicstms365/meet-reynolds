# Development Environment Customization

> **Source:** [Customizing the development environment for Copilot coding agent](https://docs.github.com/en/enterprise-cloud@latest/copilot/customizing-copilot/customizing-the-development-environment-for-copilot-coding-agent)

## Environment Setup for Power Platform Development

This guide configures your development environment for optimal Copilot agent development targeting Microsoft Power Platform.

## Prerequisites

### Required Tools
- **.NET 8.0 SDK** or later
- **Visual Studio 2022** or **VS Code** with C# extensions
- **Power Platform CLI** (`pac cli`)
- **Microsoft 365 CLI** (`m365 cli`)
- **Git** with GitHub CLI (`gh`)
- **Azure CLI** (`az`)

### Installation Scripts

#### Windows PowerShell
```powershell
# Install .NET 8.0 SDK
winget install Microsoft.DotNet.SDK.8

# Install Power Platform CLI
dotnet tool install --global Microsoft.PowerApps.CLI.Tool

# Install Microsoft 365 CLI
npm install -g @pnp/cli-microsoft365

# Install GitHub CLI
winget install GitHub.cli

# Install Azure CLI
winget install Microsoft.AzureCLI

# Verify installations
pac help
m365 help
gh --version
az --version
```

#### Linux/macOS
```bash
#!/bin/bash
# Install .NET 8.0 SDK
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0

# Install Power Platform CLI
dotnet tool install --global Microsoft.PowerApps.CLI.Tool

# Install Microsoft 365 CLI
npm install -g @pnp/cli-microsoft365

# Install GitHub CLI
# Ubuntu/Debian
sudo apt install gh
# macOS
brew install gh

# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Verify installations
pac help
m365 help
gh --version
az --version
```

## Project Structure

### Directory Layout
```
copilot-powerplatform/
├── src/
│   ├── CopilotAgent/
│   │   ├── CopilotAgent.csproj
│   │   ├── Program.cs
│   │   ├── Agents/
│   │   ├── Skills/
│   │   └── Services/
│   ├── CopilotAgent.Tests/
│   │   ├── CopilotAgent.Tests.csproj
│   │   ├── Unit/
│   │   └── Integration/
│   └── Shared/
├── docs/
├── scripts/
│   ├── setup/
│   ├── deployment/
│   └── validation/
├── .github/
│   └── workflows/
└── README.md
```

### Visual Studio Code Configuration

#### .vscode/settings.json
```json
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
```

#### .vscode/tasks.json
```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "dotnet",
            "type": "shell",
            "args": ["build", "src/CopilotAgent.sln"],
            "group": "build",
            "presentation": {
                "reveal": "silent"
            },
            "problemMatcher": "$msCompile"
        },
        {
            "label": "test",
            "command": "dotnet",
            "type": "shell",
            "args": ["test", "src/CopilotAgent.Tests/CopilotAgent.Tests.csproj"],
            "group": "test",
            "presentation": {
                "reveal": "always"
            }
        },
        {
            "label": "validate-pac-cli",
            "command": "pac",
            "type": "shell",
            "args": ["help"],
            "group": "test"
        },
        {
            "label": "validate-m365-cli",
            "command": "m365",
            "type": "shell",
            "args": ["help"],
            "group": "test"
        }
    ]
}
```

#### .vscode/launch.json
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Launch CopilotAgent",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/src/CopilotAgent/bin/Debug/net8.0/CopilotAgent.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src/CopilotAgent",
            "console": "internalConsole",
            "stopAtEntry": false,
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            }
        },
        {
            "name": "Attach to CopilotAgent",
            "type": "coreclr",
            "request": "attach"
        }
    ]
}
```

## Development Workflow

### 1. Environment Validation
```bash
# Create validation script
cat > scripts/validate-environment.sh << 'EOF'
#!/bin/bash
echo "Validating development environment..."

# Check .NET SDK
dotnet --version || { echo "❌ .NET SDK not found"; exit 1; }
echo "✅ .NET SDK installed"

# Check Power Platform CLI
pac help > /dev/null 2>&1 || { echo "❌ PAC CLI not found"; exit 1; }
echo "✅ Power Platform CLI installed"

# Check Microsoft 365 CLI
m365 help > /dev/null 2>&1 || { echo "❌ M365 CLI not found"; exit 1; }
echo "✅ Microsoft 365 CLI installed"

# Check GitHub CLI
gh --version > /dev/null 2>&1 || { echo "❌ GitHub CLI not found"; exit 1; }
echo "✅ GitHub CLI installed"

# Check Azure CLI
az --version > /dev/null 2>&1 || { echo "❌ Azure CLI not found"; exit 1; }
echo "✅ Azure CLI installed"

echo "🎉 Environment validation complete!"
EOF

chmod +x scripts/validate-environment.sh
```

### 2. Project Setup
```bash
# Create solution and projects
dotnet new sln -n CopilotAgent -o src
cd src
dotnet new web -n CopilotAgent
dotnet new xunit -n CopilotAgent.Tests
dotnet new classlib -n Shared

# Add projects to solution
dotnet sln add CopilotAgent/CopilotAgent.csproj
dotnet sln add CopilotAgent.Tests/CopilotAgent.Tests.csproj
dotnet sln add Shared/Shared.csproj

# Add project references
cd CopilotAgent
dotnet add reference ../Shared/Shared.csproj

cd ../CopilotAgent.Tests
dotnet add reference ../CopilotAgent/CopilotAgent.csproj
dotnet add reference ../Shared/Shared.csproj
```

### 3. Package Configuration
```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors>CS1591</WarningsNotAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
  </ItemGroup>
</Project>
```

## Authentication Setup

### GitHub Authentication
```bash
# Login to GitHub
gh auth login

# Configure Git
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"
```

### Power Platform Authentication
```bash
# Interactive login
pac auth create --name "dev-auth"

# List available environments
pac env list
```

### Microsoft 365 Authentication
```bash
# Interactive login
m365 login

# Verify access
m365 status
```

## IDE Extensions

### Visual Studio Code
- **C# Dev Kit**
- **GitHub Copilot**
- **GitHub Copilot Chat**
- **PowerShell**
- **Azure Tools**
- **REST Client**

### Visual Studio 2022
- **GitHub Copilot**
- **GitHub Copilot Chat**
- **Azure Development Workload**
- **ASP.NET and web development**

## Next Steps

1. Complete [CLI Tools Setup](../cli-tools/README.md)
2. Implement [Agent Development](./agent-development.md)
3. Configure [Power Platform Integration](../power-platform/README.md)
4. Set up [Testing Framework](../implementation/validation-testing.md)