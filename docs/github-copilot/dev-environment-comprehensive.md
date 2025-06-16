# Customizing Development Environment for Copilot Coding Agent

> **Source:** [Customizing the development environment for Copilot coding agent](https://docs.github.com/en/enterprise-cloud@latest/copilot/customizing-copilot/customizing-the-development-environment-for-copilot-coding-agent)

## Quick Reference for AI Agents

### Development Environment Setup
```yaml
environment_setup:
  prerequisites:
    - github_enterprise_cloud_license
    - admin_permissions
    - repository_access
  
  configuration_files:
    - ".github/copilot-instructions.md"
    - ".github/copilot-workspace.yml"
    - "copilot-agent.config.json"
  
  power_platform_specific:
    tools:
      - pac_cli
      - m365_cli
      - dotnet_8_sdk
      - visual_studio_code
    
    extensions:
      - power_platform_tools
      - c_sharp_dev_kit
      - blazor_wasm_debugger
```

## Copilot Instructions Configuration

### 1. Repository-Level Instructions
```markdown
<!-- .github/copilot-instructions.md -->
# Power Platform Development Instructions for Copilot

## Primary Technologies
- **Language:** C# (.NET 9+)
- **Frontend:** Blazor WebAssembly
- **CLI Tools:** pac CLI, Microsoft 365 CLI
- **Cloud Platform:** Microsoft Power Platform

## Development Standards

### Code Style
- Follow Microsoft C# coding conventions
- Use async/await for all I/O operations
- Implement comprehensive error handling
- Include XML documentation for public APIs
- Use dependency injection patterns

### Power Platform Specific Guidelines
- Always validate CLI commands before execution
- Implement proper authentication handling
- Use environment-specific configurations
- Include audit logging for all operations
- Follow least-privilege security principles

### CLI Integration Patterns
```csharp
// Preferred pattern for pac CLI integration
public async Task<CliResult> ExecutePacCommandAsync(string command)
{
    // 1. Validate command syntax
    var validation = await _validator.ValidateAsync(command);
    if (!validation.IsValid)
        throw new InvalidCommandException(validation.Error);
    
    // 2. Execute with proper error handling
    try
    {
        var result = await _cliExecutor.ExecuteAsync($"pac {command}");
        
        // 3. Log execution
        _logger.LogInformation("Executed: pac {Command}, Result: {ExitCode}", 
                              command, result.ExitCode);
        
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to execute pac command: {Command}", command);
        throw;
    }
}
```

### Environment Management Priorities
1. **Authentication:** Always establish secure authentication first
2. **Validation:** Verify environment state before operations
3. **Backup:** Create backups before destructive operations
4. **Monitoring:** Log all significant operations
5. **Recovery:** Implement rollback mechanisms

### Required App Installation Process
When creating environments, always install these apps in order:
1. **PowerApps** - Core application platform
2. **Power Automate** - Workflow automation
3. **Power BI** - Analytics and reporting
4. **Dataverse** - Data platform (if not included)

```bash
# Standard app installation sequence
pac application install --environment-url $ENV_URL --application-name "PowerApps"
pac application install --environment-url $ENV_URL --application-name "Microsoft Flow"
pac application install --environment-url $ENV_URL --application-name "Power BI"
```

## File Structure Preferences
```
src/
├── PowerPlatformAgent/
│   ├── Core/
│   │   ├── Interfaces/
│   │   ├── Services/
│   │   └── Models/
│   ├── CLI/
│   │   ├── PacCliService.cs
│   │   ├── M365CliService.cs
│   │   └── Validators/
│   ├── Environment/
│   │   ├── EnvironmentManager.cs
│   │   ├── ApplicationInstaller.cs
│   │   └── ConfigurationValidator.cs
│   └── BlazorUI/
│       ├── Components/
│       ├── Pages/
│       └── Services/
```

## Testing Requirements
- Unit tests for all services (>90% coverage)
- Integration tests for CLI operations
- End-to-end tests for critical workflows
- Performance tests for bulk operations

## Security Guidelines
- Store sensitive data in GitHub secrets
- Use managed identities when possible
- Implement proper token refresh logic
- Audit all administrative operations
```

### 2. Workspace Configuration
```yaml
# .github/copilot-workspace.yml
workspace:
  name: "Power Platform Development"
  description: "Specialized environment for Power Platform agent development"
  
  tools:
    primary:
      - name: "pac"
        version: "latest"
        install_command: "dotnet tool install --global Microsoft.PowerApps.CLI.Tool"
        validation: "pac --version"
      
      - name: "m365"
        version: "latest"
        install_command: "npm install -g @pnp/cli-microsoft365"
        validation: "m365 --version"
      
      - name: "dotnet"
        version: "9.0"
        install_command: "curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 9.0.100"
        validation: "dotnet --version"
    
    development:
      - name: "Azure CLI"
        purpose: "Azure resource management"
      - name: "GitHub CLI"
        purpose: "Repository and workflow management"
  
  environment_variables:
    required:
      - TENANT_ID
      - POWER_PLATFORM_URL
      - GITHUB_TOKEN
    
    optional:
      - ENVIRONMENT_NAME_PREFIX
      - DEFAULT_REGION
      - LOG_LEVEL
  
  startup_script: |
    #!/bin/bash
    # Initialize development environment
    echo "Setting up Power Platform development environment..."
    
    # Install required tools
    ./scripts/setup-dev-environment.sh
    
    # Validate CLI tools
    pac --version || exit 1
    m365 --version || exit 1
    dotnet --version || exit 1
    
    # Setup authentication if credentials available
    if [ ! -z "$POWER_PLATFORM_URL" ] && [ ! -z "$TENANT_ID" ]; then
        echo "Setting up Power Platform authentication..."
        pac auth create --url "$POWER_PLATFORM_URL" --tenant "$TENANT_ID" --name "dev-environment"
    fi
    
    echo "Development environment ready!"
```

### 3. Agent-Specific Configuration
```json
{
  "copilot_agent": {
    "name": "power-platform-specialist",
    "version": "1.0.0",
    "description": "Specialized agent for Microsoft Power Platform development",
    
    "capabilities": {
      "environment_management": {
        "create_environments": true,
        "configure_environments": true,
        "manage_applications": true,
        "validate_setup": true
      },
      
      "cli_operations": {
        "pac_cli": {
          "allowed_commands": [
            "auth list",
            "auth create",
            "auth select",
            "env list",
            "env create",
            "env who",
            "solution list",
            "solution export",
            "solution import",
            "application list",
            "application install"
          ],
          "validation_required": true,
          "audit_logging": true
        },
        
        "m365_cli": {
          "allowed_commands": [
            "login",
            "status",
            "tenant list",
            "app list",
            "spo site list"
          ],
          "validation_required": true,
          "audit_logging": true
        }
      },
      
      "code_generation": {
        "languages": ["csharp", "blazor", "typescript"],
        "frameworks": ["asp.net core", "blazor webassembly"],
        "templates": ["power-platform-connector", "blazor-component", "cli-service"]
      }
    },
    
    "knowledge_sources": {
      "documentation": [
        "docs/cli-tools/",
        "docs/power-platform/",
        "docs/github-copilot/"
      ],
      "external_references": [
        "https://learn.microsoft.com/en-us/power-platform/",
        "https://pnp.github.io/cli-microsoft365/",
        "https://docs.microsoft.com/en-us/dotnet/"
      ]
    },
    
    "workflow_patterns": {
      "environment_setup": {
        "steps": [
          "validate_prerequisites",
          "authenticate_to_platform",
          "create_environment",
          "install_required_apps",
          "configure_environment",
          "validate_setup"
        ],
        "rollback_strategy": "automatic",
        "validation_points": ["after_each_step"]
      },
      
      "solution_deployment": {
        "steps": [
          "validate_solution_package",
          "backup_target_environment",
          "deploy_solution",
          "validate_deployment",
          "run_post_deployment_tests"
        ],
        "rollback_strategy": "manual_approval",
        "validation_points": ["before_deployment", "after_deployment"]
      }
    }
  }
}
```

## Development Workflow Integration

### 1. IDE Configuration

#### Visual Studio Code Settings
```json
{
  "powerPlatform.enableCodeGeneration": true,
  "powerPlatform.validateCliCommands": true,
  "powerPlatform.defaultEnvironment": "${workspaceFolder}/.env",
  
  "dotnet.completion.showCompletionItemsFromUnimportedNamespaces": true,
  "dotnet.inlayHints.enableInlayHintsForParameters": true,
  "dotnet.inlayHints.enableInlayHintsForLiteralParameters": true,
  
  "files.associations": {
    "*.pac": "powershell",
    "*.m365": "typescript"
  },
  
  "copilot.enable": {
    "*": true,
    "plaintext": false,
    "markdown": true,
    "csharp": true,
    "blazor": true
  }
}
```

#### Recommended Extensions
```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.csdevkit",
    "ms-dotnettools.blazorwasm-companion",
    "github.copilot",
    "github.copilot-chat",
    "ms-vscode.powershell",
    "ms-vscode.azure-account"
  ]
}
```

### 2. Build Configuration

#### Project File Template
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>power-platform-agent-secrets</UserSecretsId>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="docs\**\*.md" CopyToOutputDirectory="PreserveNewest" />
    <Content Include="scripts\**\*.sh" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

### 3. GitHub Actions Integration

#### Development Workflow
```yaml
name: Development Environment Validation

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  validate-environment:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 9.0.x
    
    - name: Setup Power Platform CLI
      run: dotnet tool install --global Microsoft.PowerApps.CLI.Tool
    
    - name: Setup Microsoft 365 CLI
      run: npm install -g @pnp/cli-microsoft365
    
    - name: Validate Environment Configuration
      run: |
        ./scripts/validate-dev-environment.sh
        
    - name: Build Solution
      run: dotnet build --configuration Release
    
    - name: Run Tests
      run: dotnet test --configuration Release --collect:"XPlat Code Coverage"
    
    - name: Test CLI Integration
      env:
        TENANT_ID: ${{ secrets.TENANT_ID }}
        POWER_PLATFORM_URL: ${{ secrets.POWER_PLATFORM_URL }}
      run: |
        ./scripts/test-cli-integration.sh
```

## Environment Setup Scripts

### 1. Development Environment Setup
```bash
#!/bin/bash
# scripts/setup-dev-environment.sh

set -e

echo "Setting up Power Platform development environment..."

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Install .NET 9 if not present
if ! command_exists dotnet; then
    echo "Installing .NET 9..."
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 9.0.100
    export PATH="$HOME/.dotnet:$PATH"
fi

# Install Power Platform CLI
if ! command_exists pac; then
    echo "Installing Power Platform CLI..."
    dotnet tool install --global Microsoft.PowerApps.CLI.Tool
fi

# Install Microsoft 365 CLI
if ! command_exists m365; then
    echo "Installing Microsoft 365 CLI..."
    npm install -g @pnp/cli-microsoft365
fi

# Install Azure CLI if not present
if ! command_exists az; then
    echo "Installing Azure CLI..."
    curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
fi

# Validate installations
echo "Validating tool installations..."
pac --version || { echo "pac CLI installation failed"; exit 1; }
m365 --version || { echo "m365 CLI installation failed"; exit 1; }
dotnet --version || { echo ".NET installation failed"; exit 1; }
az --version || { echo "Azure CLI installation failed"; exit 1; }

echo "Development environment setup complete!"
```

### 2. Environment Validation Script
```bash
#!/bin/bash
# scripts/validate-dev-environment.sh

set -e

echo "Validating development environment..."

# Check required environment variables
check_env_var() {
    if [ -z "${!1}" ]; then
        echo "Warning: Environment variable $1 is not set"
        return 1
    else
        echo "✓ $1 is set"
        return 0
    fi
}

# Validate tools
validate_tool() {
    if command -v "$1" >/dev/null 2>&1; then
        echo "✓ $1 is installed"
        return 0
    else
        echo "✗ $1 is not installed"
        return 1
    fi
}

# Check environment variables
echo "Checking environment variables..."
check_env_var "TENANT_ID" || ENV_ISSUES=1
check_env_var "POWER_PLATFORM_URL" || ENV_ISSUES=1
check_env_var "GITHUB_TOKEN" || ENV_ISSUES=1

# Validate required tools
echo "Validating required tools..."
validate_tool "pac" || TOOL_ISSUES=1
validate_tool "m365" || TOOL_ISSUES=1
validate_tool "dotnet" || TOOL_ISSUES=1
validate_tool "az" || TOOL_ISSUES=1

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
if [[ "$DOTNET_VERSION" == "9."* ]]; then
    echo "✓ .NET 9 is installed ($DOTNET_VERSION)"
else
    echo "✗ .NET 9 required, found: $DOTNET_VERSION"
    TOOL_ISSUES=1
fi

# Test CLI connections (if credentials available)
if [ ! -z "$TENANT_ID" ] && [ ! -z "$POWER_PLATFORM_URL" ]; then
    echo "Testing Power Platform connectivity..."
    if pac auth list >/dev/null 2>&1; then
        echo "✓ Power Platform authentication available"
    else
        echo "⚠ No Power Platform authentication configured"
    fi
fi

# Summary
if [ -z "$TOOL_ISSUES" ] && [ -z "$ENV_ISSUES" ]; then
    echo "✓ Development environment validation successful!"
    exit 0
else
    echo "✗ Development environment validation failed!"
    echo "Please address the issues above before proceeding."
    exit 1
fi
```

## Testing Framework Integration

### 1. Unit Test Configuration
```csharp
// Tests/PowerPlatformAgentTests.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

[TestFixture]
public class PowerPlatformAgentTests
{
    private IHost _host;
    private IPowerPlatformAgent _agent;
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddPowerPlatformAgent(options =>
                {
                    options.UseMockCliExecutor = true;
                    options.EnableValidation = true;
                });
            })
            .Build();
            
        _agent = _host.Services.GetRequiredService<IPowerPlatformAgent>();
    }
    
    [Test]
    public async Task Should_Validate_Environment_Creation_Request()
    {
        // Arrange
        var request = new EnvironmentCreationRequest
        {
            Name = "test-env",
            Type = "Sandbox",
            Region = "UnitedStates"
        };
        
        // Act
        var validation = await _agent.ValidateEnvironmentRequestAsync(request);
        
        // Assert
        Assert.That(validation.IsValid, Is.True);
    }
    
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _host?.Dispose();
    }
}
```

### 2. Integration Test Setup
```csharp
// Tests/Integration/CliIntegrationTests.cs
[TestFixture]
[Category("Integration")]
public class CliIntegrationTests
{
    private readonly string _testTenantId = Environment.GetEnvironmentVariable("TEST_TENANT_ID");
    private readonly string _testEnvironmentUrl = Environment.GetEnvironmentVariable("TEST_ENVIRONMENT_URL");
    
    [SetUp]
    public void SetUp()
    {
        Assume.That(_testTenantId, Is.Not.Null.And.Not.Empty, 
                   "TEST_TENANT_ID environment variable must be set for integration tests");
        Assume.That(_testEnvironmentUrl, Is.Not.Null.And.Not.Empty, 
                   "TEST_ENVIRONMENT_URL environment variable must be set for integration tests");
    }
    
    [Test]
    public async Task Should_List_Environments_Successfully()
    {
        // This test validates actual CLI connectivity
        var cliService = new PacCliService();
        
        var result = await cliService.ExecuteAsync("env list");
        
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Output, Is.Not.Empty);
    }
}
```

## Next Steps

1. **Initial Setup**
   - Create `.github/copilot-instructions.md` with Power Platform specifics
   - Configure workspace with required tools
   - Set up development environment scripts

2. **Tool Integration**
   - Validate pac CLI and m365 CLI installations
   - Configure authentication mechanisms
   - Test basic CLI operations

3. **Documentation Updates**
   - Update README with environment setup instructions
   - Create troubleshooting guides
   - Document best practices for development

4. **Testing Implementation**
   - Set up unit testing framework
   - Create integration tests for CLI operations
   - Implement continuous validation