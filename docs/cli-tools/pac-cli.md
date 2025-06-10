# Power Platform CLI Guide

> **Source:** [Power Platform CLI Reference](https://learn.microsoft.com/en-us/power-platform/developer/cli/reference/)

## Overview

The Power Platform CLI (`pac`) is the primary command-line tool for managing Power Platform environments, solutions, and applications. This guide focuses on essential commands for Copilot agent automation.

## Authentication Commands

### pac auth
Manage authentication profiles for Power Platform environments.

```bash
# Create new authentication profile
pac auth create --name "dev-environment"

# List authentication profiles
pac auth list

# Select active profile
pac auth select --name "dev-environment"

# Clear authentication
pac auth clear
```

#### Automation Example
```csharp
public async Task<AuthResult> AuthenticateAsync(string environmentUrl, string tenantId)
{
    var commands = new[]
    {
        $"pac auth create --url {environmentUrl} --tenant {tenantId}"
    };
    
    return await ExecuteCliCommandsAsync(commands);
}
```

## Environment Management

### pac env
Core commands for Power Platform environment lifecycle management.

#### Environment Operations
```bash
# List all environments
pac env list

# Create new environment
pac env create --name "MyEnvironment" --type "Sandbox" --region "UnitedStates"

# Get environment details
pac env who

# Reset environment
pac env reset

# Delete environment (admin only)
pac env delete --environment-id <env-id>
```

#### Environment Automation
```csharp
public class EnvironmentManager
{
    public async Task<Environment> CreateEnvironmentAsync(EnvironmentSpec spec)
    {
        var createCommand = $@"pac env create 
            --name ""{spec.Name}"" 
            --type ""{spec.Type}"" 
            --region ""{spec.Region}"" 
            --description ""{spec.Description}""";

        var result = await ExecuteCommandAsync(createCommand);
        
        if (result.Success)
        {
            return await GetEnvironmentDetailsAsync(spec.Name);
        }
        
        throw new EnvironmentCreationException(result.Error);
    }

    public async Task<bool> ValidateEnvironmentAsync(string environmentName)
    {
        var command = $"pac env list --filter \"displayName eq '{environmentName}'\"";
        var result = await ExecuteCommandAsync(command);
        
        return result.Success && result.Output.Contains(environmentName);
    }
}
```

## Solution Management

### pac solution
Manage Power Platform solutions and their lifecycle.

```bash
# List solutions
pac solution list

# Export solution
pac solution export --name "MySolution" --path "./solutions/"

# Import solution
pac solution import --path "./solutions/MySolution.zip"

# Clone solution for development
pac solution clone --name "MySolution" --outputDirectory "./src/"

# Create new solution
pac solution init --publisher-name "MyPublisher" --publisher-prefix "mp"
```

#### Solution Automation
```csharp
public class SolutionManager
{
    public async Task<SolutionExportResult> ExportSolutionAsync(string solutionName, string outputPath)
    {
        var command = $@"pac solution export 
            --name ""{solutionName}"" 
            --path ""{outputPath}"" 
            --managed false";

        var result = await ExecuteCommandAsync(command);
        
        return new SolutionExportResult
        {
            Success = result.Success,
            FilePath = Path.Combine(outputPath, $"{solutionName}.zip"),
            Message = result.Output
        };
    }

    public async Task<bool> ImportSolutionAsync(string solutionPath)
    {
        var command = $@"pac solution import 
            --path ""{solutionPath}"" 
            --force-overwrite";

        var result = await ExecuteCommandAsync(command);
        return result.Success;
    }
}
```

## Application Management

### pac application
Manage Power Platform applications.

```bash
# List applications
pac application list

# Install application
pac application install --application-id <app-id>

# Create application
pac application create --name "MyApp" --display-name "My Application"
```

## Data Operations

### pac data
Work with environment data.

```bash
# Export data
pac data export --schema-file "schema.xml" --data-file "data.zip"

# Import data
pac data import --data-file "data.zip"
```

## Package Management

### pac package
Manage solution packages.

```bash
# Deploy packages
pac package deploy --package "MyPackage.zip"

# Show package info
pac package show --package "MyPackage.zip"
```

## Validation and Testing

### Command Validation Framework
```csharp
public class PacCliValidator
{
    private readonly HashSet<string> _allowedCommands = new()
    {
        "pac auth list",
        "pac auth create",
        "pac auth select",
        "pac env list",
        "pac env create",
        "pac env who",
        "pac solution list",
        "pac solution export",
        "pac solution import",
        "pac application list"
    };

    public bool IsCommandSafe(string command)
    {
        var baseCommand = ExtractBaseCommand(command);
        return _allowedCommands.Contains(baseCommand);
    }

    public async Task<ValidationResult> ValidateCommandAsync(string command)
    {
        if (!IsCommandSafe(command))
        {
            return ValidationResult.Failure($"Command not allowed: {command}");
        }

        // Additional validation logic
        return await PerformSyntaxValidationAsync(command);
    }
}
```

### Testing Commands
```csharp
[Test]
public async Task Should_List_Environments_Successfully()
{
    // Arrange
    var command = "pac env list";
    
    // Act
    var result = await _cliExecutor.ExecuteAsync(command);
    
    // Assert
    Assert.That(result.ExitCode, Is.EqualTo(0));
    Assert.That(result.Output, Is.Not.Empty);
}

[Test]
public async Task Should_Validate_Environment_Creation_Parameters()
{
    // Arrange
    var spec = new EnvironmentSpec
    {
        Name = "test-env",
        Type = "Sandbox",
        Region = "UnitedStates"
    };
    
    // Act
    var command = _environmentManager.GenerateCreateCommand(spec);
    var isValid = await _validator.ValidateCommandAsync(command);
    
    // Assert
    Assert.That(isValid.Success, Is.True);
}
```

## Error Handling and Logging

### CLI Error Management
```csharp
public class PacCliService
{
    private readonly ILogger<PacCliService> _logger;

    public async Task<CliResult> ExecuteAsync(string command)
    {
        try
        {
            _logger.LogInformation("Executing PAC CLI command: {Command}", command);
            
            var result = await RunProcessAsync("pac", command);
            
            if (result.ExitCode != 0)
            {
                _logger.LogError("PAC CLI command failed: {Error}", result.StandardError);
                return CliResult.Failure(result.StandardError);
            }
            
            _logger.LogInformation("PAC CLI command completed successfully");
            return CliResult.Success(result.StandardOutput);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception executing PAC CLI command: {Command}", command);
            return CliResult.Failure(ex.Message);
        }
    }
}
```

## Best Practices

### Security
- Always validate commands before execution
- Use authentication profiles instead of inline credentials
- Implement audit logging for all CLI operations
- Restrict dangerous operations (delete, reset)

### Performance
- Cache authentication tokens
- Batch operations when possible
- Use async/await for long-running operations
- Implement timeout handling

### Reliability
- Retry failed operations with exponential backoff
- Validate environment state before operations
- Implement rollback mechanisms for critical operations
- Monitor and alert on failures

## Integration Examples

### Environment Provisioning Workflow
```csharp
public async Task<ProvisioningResult> ProvisionEnvironmentAsync(EnvironmentRequest request)
{
    var steps = new[]
    {
        () => AuthenticateAsync(request.TenantId),
        () => CreateEnvironmentAsync(request.EnvironmentSpec),
        () => ValidateEnvironmentAsync(request.EnvironmentSpec.Name),
        () => InstallRequiredAppsAsync(request.RequiredApps),
        () => ConfigureEnvironmentAsync(request.Configuration)
    };

    foreach (var step in steps)
    {
        var result = await step();
        if (!result.Success)
        {
            await RollbackAsync();
            return ProvisioningResult.Failure(result.Error);
        }
    }

    return ProvisioningResult.Success();
}
```

## Next Steps

1. Review [Microsoft 365 CLI Guide](./m365-cli.md)
2. Explore [CLI Scripts and Samples](./scripts-samples.md)
3. Implement [Environment Management](../power-platform/environment-management.md)
4. Set up [Validation Framework](../implementation/validation-testing.md)