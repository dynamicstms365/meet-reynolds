# Power Platform CLI - Environment Management & App Installation Guide

> **Source:** [Power Platform CLI Reference](https://learn.microsoft.com/en-us/power-platform/developer/cli/reference/)

## Quick Reference for AI Agents

### Environment Lifecycle Overview
```yaml
environment_lifecycle:
  phases:
    - authentication
    - environment_creation
    - app_installation
    - configuration
    - validation
  
  critical_apps:
    required_order:
      1: "Microsoft PowerApps"
      2: "Microsoft Power Automate" 
      3: "Microsoft Power BI"
      4: "Common Data Service"
      5: "AI Builder"
  
  validation_checkpoints:
    - post_authentication
    - post_environment_creation
    - post_each_app_installation
    - post_configuration
    - final_validation
```

### Common Environment Types
```yaml
environment_types:
  Sandbox:
    description: "Development and testing"
    limitations: "Limited capacity, auto-delete after inactivity"
    use_cases: ["development", "testing", "proof-of-concept"]
    
  Production:
    description: "Live production workloads"
    limitations: "Requires premium licensing"
    use_cases: ["live_applications", "business_critical_workloads"]
    
  Trial:
    description: "30-day trial environment"
    limitations: "Limited time, reduced capacity"
    use_cases: ["evaluation", "demos", "quick_testing"]
    
  Developer:
    description: "Personal development environment"
    limitations: "Single user, limited capacity"
    use_cases: ["individual_development", "learning", "experimentation"]
```

## Environment Authentication

### Authentication Methods
```bash
# Interactive authentication (recommended for initial setup)
pac auth create --name "dev-environment" --url https://myorg.crm.dynamics.com

# Service principal authentication (for automation)
pac auth create --name "automation" \
  --url https://myorg.crm.dynamics.com \
  --tenant 12345678-1234-1234-1234-123456789012 \
  --applicationId 87654321-4321-4321-4321-210987654321 \
  --clientSecret "your-client-secret"

# Certificate-based authentication (most secure)
pac auth create --name "cert-auth" \
  --url https://myorg.crm.dynamics.com \
  --tenant 12345678-1234-1234-1234-123456789012 \
  --applicationId 87654321-4321-4321-4321-210987654321 \
  --certificatePath "./cert.pfx" \
  --certificatePassword "cert-password"

# Username/password authentication (not recommended for production)
pac auth create --name "user-auth" \
  --url https://myorg.crm.dynamics.com \
  --username "user@tenant.com" \
  --password "user-password"
```

### Authentication Management
```bash
# List all authentication profiles
pac auth list

# Select active authentication profile
pac auth select --name "dev-environment"

# Test current authentication
pac auth who

# Clear authentication (logout)
pac auth clear

# Clear specific authentication profile
pac auth delete --name "old-profile"
```

### Authentication Automation
```csharp
public class PowerPlatformAuthenticationManager
{
    private readonly ILogger<PowerPlatformAuthenticationManager> _logger;
    private readonly SecretManager _secretManager;
    
    public async Task<AuthenticationResult> AuthenticateAsync(PowerPlatformCredentials credentials)
    {
        try
        {
            // Build authentication command based on credential type
            var command = credentials.Type switch
            {
                CredentialType.ServicePrincipal => BuildServicePrincipalCommand(credentials),
                CredentialType.Certificate => BuildCertificateCommand(credentials),
                CredentialType.Interactive => BuildInteractiveCommand(credentials),
                _ => throw new NotSupportedException($"Credential type {credentials.Type} not supported")
            };
            
            _logger.LogInformation("Authenticating to Power Platform: {Environment}", credentials.EnvironmentUrl);
            
            var result = await ExecuteCommandAsync(command);
            
            if (result.ExitCode == 0)
            {
                // Verify authentication worked
                var whoResult = await ExecuteCommandAsync("pac auth who");
                if (whoResult.ExitCode == 0)
                {
                    _logger.LogInformation("Successfully authenticated to Power Platform");
                    return AuthenticationResult.Success(ExtractUserInfo(whoResult.Output));
                }
            }
            
            _logger.LogError("Authentication failed: {Error}", result.StandardError);
            return AuthenticationResult.Failure(result.StandardError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Power Platform authentication");
            return AuthenticationResult.Failure(ex.Message);
        }
    }
    
    private string BuildServicePrincipalCommand(PowerPlatformCredentials credentials)
    {
        return $@"pac auth create --name ""{credentials.ProfileName}"" 
                 --url ""{credentials.EnvironmentUrl}"" 
                 --tenant ""{credentials.TenantId}"" 
                 --applicationId ""{credentials.ClientId}"" 
                 --clientSecret ""{credentials.ClientSecret}""";
    }
    
    public async Task<bool> ValidateAuthenticationAsync()
    {
        var result = await ExecuteCommandAsync("pac auth who");
        return result.ExitCode == 0 && !result.Output.Contains("No profiles");
    }
}
```

## Environment Creation and Management

### Creating Environments
```bash
# Create sandbox environment (most common)
pac env create --name "MyDevEnvironment" \
  --type Sandbox \
  --region "UnitedStates" \
  --currency "USD" \
  --language "1033" \
  --description "Development environment for project"

# Create production environment
pac env create --name "MyProdEnvironment" \
  --type Production \
  --region "Europe" \
  --currency "EUR" \
  --language "1031" \
  --description "Production environment"

# Create developer environment (personal)
pac env create --name "MyPersonalDev" \
  --type Developer \
  --region "UnitedStates"

# Create environment with Dataverse database
pac env create --name "MyEnvWithDB" \
  --type Sandbox \
  --region "UnitedStates" \
  --currency "USD" \
  --language "1033" \
  --dataverse true
```

### Environment Information and Management
```bash
# List all environments
pac env list

# List environments with detailed information
pac env list --output json

# Get current environment information
pac env who

# Select different environment
pac env select --environment https://myorg.crm.dynamics.com

# Get environment details
pac env get --environment https://myorg.crm.dynamics.com

# Update environment properties
pac env update --environment https://myorg.crm.dynamics.com \
  --display-name "Updated Environment Name"

# Reset environment (removes all data and customizations)
pac env reset --environment https://myorg.crm.dynamics.com

# Delete environment (admin only)
pac env delete --environment https://myorg.crm.dynamics.com
```

### Environment Creation Automation
```csharp
public class EnvironmentManager
{
    public async Task<EnvironmentCreationResult> CreateEnvironmentAsync(EnvironmentSpec spec)
    {
        // Validate prerequisites
        var validationResult = await ValidateEnvironmentCreationAsync(spec);
        if (!validationResult.IsValid)
        {
            return EnvironmentCreationResult.Failure(validationResult.Errors);
        }
        
        try
        {
            // Build creation command
            var command = BuildEnvironmentCreationCommand(spec);
            
            _logger.LogInformation("Creating environment: {Name}", spec.Name);
            
            // Execute creation command with timeout
            var result = await ExecuteCommandWithTimeoutAsync(command, TimeSpan.FromMinutes(10));
            
            if (result.ExitCode == 0)
            {
                // Extract environment URL from output
                var environmentUrl = ExtractEnvironmentUrl(result.Output);
                
                // Wait for environment to be fully provisioned
                await WaitForEnvironmentProvisioningAsync(environmentUrl);
                
                // Validate environment is accessible
                var environment = await ValidateEnvironmentAsync(environmentUrl);
                
                _logger.LogInformation("Environment created successfully: {Url}", environmentUrl);
                
                return EnvironmentCreationResult.Success(environment);
            }
            
            _logger.LogError("Environment creation failed: {Error}", result.StandardError);
            return EnvironmentCreationResult.Failure(result.StandardError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during environment creation");
            return EnvironmentCreationResult.Failure(ex.Message);
        }
    }
    
    private string BuildEnvironmentCreationCommand(EnvironmentSpec spec)
    {
        var command = $@"pac env create 
            --name ""{spec.Name}"" 
            --type ""{spec.Type}"" 
            --region ""{spec.Region}""";
            
        if (!string.IsNullOrEmpty(spec.Currency))
            command += $@" --currency ""{spec.Currency}""";
            
        if (spec.LanguageCode.HasValue)
            command += $@" --language ""{spec.LanguageCode}""";
            
        if (!string.IsNullOrEmpty(spec.Description))
            command += $@" --description ""{spec.Description}""";
            
        if (spec.IncludeDataverse)
            command += " --dataverse true";
            
        return command;
    }
    
    private async Task WaitForEnvironmentProvisioningAsync(string environmentUrl)
    {
        var maxAttempts = 30; // 5 minutes with 10-second intervals
        var attempt = 0;
        
        while (attempt < maxAttempts)
        {
            try
            {
                var result = await ExecuteCommandAsync($"pac env who --environment {environmentUrl}");
                if (result.ExitCode == 0 && !result.Output.Contains("provisioning"))
                {
                    return; // Environment is ready
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Environment not ready yet (attempt {Attempt}): {Error}", attempt + 1, ex.Message);
            }
            
            await Task.Delay(TimeSpan.FromSeconds(10));
            attempt++;
        }
        
        throw new EnvironmentProvisioningTimeoutException("Environment provisioning timed out");
    }
}
```

## Application Installation and Management

### Core Power Platform Applications
```bash
# List available applications in tenant
pac application list

# List installed applications in current environment
pac application list --environment https://myorg.crm.dynamics.com

# Install Microsoft PowerApps (Core platform)
pac application install --application-id "fd140aaf-4df4-11dd-bd17-0019b9312238" \
  --environment https://myorg.crm.dynamics.com

# Install Microsoft Power Automate
pac application install --application-id "4c5b2e7c-7f19-4f8f-98e4-a01b6b69a41d" \
  --environment https://myorg.crm.dynamics.com

# Install Power BI
pac application install --application-id "78528b2b-79a1-4e92-91a1-e4e1e8b8e8e8" \
  --environment https://myorg.crm.dynamics.com

# Install AI Builder
pac application install --application-id "2e49aa90-9be8-11ea-bb37-0242ac130002" \
  --environment https://myorg.crm.dynamics.com

# Install Common Data Service (Dataverse)
pac application install --application-id "c6a0d1d4-7b1e-4f8e-9e1a-8b2c3d4e5f6g" \
  --environment https://myorg.crm.dynamics.com
```

### Application Management Operations
```bash
# Check application installation status
pac application show --application-id "fd140aaf-4df4-11dd-bd17-0019b9312238" \
  --environment https://myorg.crm.dynamics.com

# Update application to latest version
pac application update --application-id "fd140aaf-4df4-11dd-bd17-0019b9312238" \
  --environment https://myorg.crm.dynamics.com

# Uninstall application (careful!)
pac application uninstall --application-id "fd140aaf-4df4-11dd-bd17-0019b9312238" \
  --environment https://myorg.crm.dynamics.com

# List application dependencies
pac application dependencies --application-id "fd140aaf-4df4-11dd-bd17-0019b9312238"
```

### Required Application Installation Framework
```csharp
public class ApplicationInstallationManager
{
    // Standard Power Platform applications with correct IDs
    private readonly Dictionary<string, ApplicationInfo> _standardApplications = new()
    {
        {
            "PowerApps",
            new ApplicationInfo
            {
                Id = "fd140aaf-4df4-11dd-bd17-0019b9312238",
                Name = "Microsoft PowerApps",
                Description = "Core Power Platform application development",
                InstallOrder = 1,
                Required = true
            }
        },
        {
            "PowerAutomate",
            new ApplicationInfo
            {
                Id = "4c5b2e7c-7f19-4f8f-98e4-a01b6b69a41d", 
                Name = "Microsoft Power Automate",
                Description = "Workflow automation and process orchestration",
                InstallOrder = 2,
                Required = true
            }
        },
        {
            "PowerBI",
            new ApplicationInfo
            {
                Id = "78528b2b-79a1-4e92-91a1-e4e1e8b8e8e8",
                Name = "Microsoft Power BI",
                Description = "Business analytics and reporting",
                InstallOrder = 3,
                Required = false
            }
        },
        {
            "AIBuilder",
            new ApplicationInfo
            {
                Id = "2e49aa90-9be8-11ea-bb37-0242ac130002",
                Name = "AI Builder",
                Description = "AI capabilities for Power Platform",
                InstallOrder = 4,
                Required = false
            }
        },
        {
            "Dataverse",
            new ApplicationInfo
            {
                Id = "c6a0d1d4-7b1e-4f8e-9e1a-8b2c3d4e5f6g",
                Name = "Common Data Service",
                Description = "Data platform for Power Platform",
                InstallOrder = 5,
                Required = true
            }
        }
    };
    
    public async Task<AppInstallationResult> InstallRequiredAppsAsync(
        string environmentUrl, 
        AppInstallationSpec spec)
    {
        var results = new List<SingleAppInstallationResult>();
        
        // Get applications to install in correct order
        var appsToInstall = GetApplicationsToInstall(spec)
            .OrderBy(app => app.InstallOrder)
            .ToList();
        
        _logger.LogInformation("Installing {Count} applications in environment {Environment}", 
                              appsToInstall.Count, environmentUrl);
        
        foreach (var app in appsToInstall)
        {
            var result = await InstallSingleApplicationAsync(environmentUrl, app);
            results.Add(result);
            
            if (!result.Success && app.Required)
            {
                _logger.LogError("Required application {AppName} failed to install", app.Name);
                return AppInstallationResult.Failure($"Required app {app.Name} installation failed", results);
            }
            
            // Wait between installations to avoid throttling
            await Task.Delay(TimeSpan.FromSeconds(30));
        }
        
        // Validate all installations
        var validationResult = await ValidateAppInstallationsAsync(environmentUrl, appsToInstall);
        
        return new AppInstallationResult
        {
            Success = validationResult.AllAppsInstalled,
            InstalledApps = results.Where(r => r.Success).Select(r => r.Application).ToList(),
            FailedApps = results.Where(r => !r.Success).Select(r => r.Application).ToList(),
            ValidationResult = validationResult,
            TotalInstallationTime = results.Sum(r => r.InstallationTime.TotalSeconds)
        };
    }
    
    private async Task<SingleAppInstallationResult> InstallSingleApplicationAsync(
        string environmentUrl, 
        ApplicationInfo app)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Installing {AppName} in environment {Environment}", 
                                  app.Name, environmentUrl);
            
            // Check if app is already installed
            var existingApp = await CheckAppInstallationStatusAsync(environmentUrl, app.Id);
            if (existingApp?.IsInstalled == true)
            {
                _logger.LogInformation("{AppName} is already installed", app.Name);
                return SingleAppInstallationResult.AlreadyInstalled(app, stopwatch.Elapsed);
            }
            
            // Install the application
            var command = $@"pac application install 
                --application-id ""{app.Id}"" 
                --environment ""{environmentUrl}""";
            
            var result = await ExecuteCommandWithTimeoutAsync(command, TimeSpan.FromMinutes(10));
            
            if (result.ExitCode == 0)
            {
                // Wait for installation to complete
                await WaitForAppInstallationAsync(environmentUrl, app.Id);
                
                _logger.LogInformation("{AppName} installed successfully", app.Name);
                return SingleAppInstallationResult.Success(app, stopwatch.Elapsed);
            }
            
            _logger.LogError("{AppName} installation failed: {Error}", app.Name, result.StandardError);
            return SingleAppInstallationResult.Failure(app, result.StandardError, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception installing {AppName}", app.Name);
            return SingleAppInstallationResult.Failure(app, ex.Message, stopwatch.Elapsed);
        }
    }
    
    private async Task WaitForAppInstallationAsync(string environmentUrl, string appId)
    {
        var maxAttempts = 20; // 10 minutes with 30-second intervals
        var attempt = 0;
        
        while (attempt < maxAttempts)
        {
            try
            {
                var status = await CheckAppInstallationStatusAsync(environmentUrl, appId);
                if (status?.IsInstalled == true && status.Status == "Installed")
                {
                    return; // Installation complete
                }
                
                if (status?.Status == "Failed")
                {
                    throw new AppInstallationFailedException($"Application installation failed with status: {status.Status}");
                }
            }
            catch (AppInstallationFailedException)
            {
                throw; // Re-throw installation failures
            }
            catch (Exception ex)
            {
                _logger.LogDebug("App installation not complete yet (attempt {Attempt}): {Error}", 
                               attempt + 1, ex.Message);
            }
            
            await Task.Delay(TimeSpan.FromSeconds(30));
            attempt++;
        }
        
        throw new AppInstallationTimeoutException("Application installation timed out");
    }
    
    private async Task<AppInstallationStatus> CheckAppInstallationStatusAsync(string environmentUrl, string appId)
    {
        var command = $@"pac application show 
            --application-id ""{appId}"" 
            --environment ""{environmentUrl}"" 
            --output json";
            
        var result = await ExecuteCommandAsync(command);
        
        if (result.ExitCode == 0)
        {
            var appInfo = JsonSerializer.Deserialize<AppInstallationStatus>(result.Output);
            return appInfo;
        }
        
        return null;
    }
}
```

### Application Installation Validation
```csharp
public class ApplicationValidator
{
    public async Task<AppValidationResult> ValidateAppInstallationsAsync(
        string environmentUrl, 
        List<ApplicationInfo> expectedApps)
    {
        var validationResults = new List<AppValidationItem>();
        
        foreach (var app in expectedApps)
        {
            var validation = await ValidateSingleAppAsync(environmentUrl, app);
            validationResults.Add(validation);
        }
        
        return new AppValidationResult
        {
            EnvironmentUrl = environmentUrl,
            TotalAppsChecked = expectedApps.Count,
            InstalledApps = validationResults.Count(r => r.IsInstalled),
            FailedApps = validationResults.Count(r => !r.IsInstalled && r.Required),
            AllRequiredAppsInstalled = validationResults.Where(r => r.Required).All(r => r.IsInstalled),
            AllAppsInstalled = validationResults.All(r => r.IsInstalled),
            ValidationDetails = validationResults
        };
    }
    
    private async Task<AppValidationItem> ValidateSingleAppAsync(string environmentUrl, ApplicationInfo app)
    {
        try
        {
            var status = await CheckAppInstallationStatusAsync(environmentUrl, app.Id);
            
            return new AppValidationItem
            {
                ApplicationInfo = app,
                IsInstalled = status?.IsInstalled == true,
                Status = status?.Status ?? "Unknown",
                Version = status?.Version,
                InstallDate = status?.InstallDate,
                Required = app.Required,
                ValidationPassed = status?.IsInstalled == true || !app.Required
            };
        }
        catch (Exception ex)
        {
            return new AppValidationItem
            {
                ApplicationInfo = app,
                IsInstalled = false,
                Status = "Error",
                Error = ex.Message,
                Required = app.Required,
                ValidationPassed = !app.Required
            };
        }
    }
}
```

## Environment Configuration and Setup

### Post-Creation Configuration
```bash
# Configure environment settings
pac env settings set --environment https://myorg.crm.dynamics.com \
  --setting-name "SharePointIntegration" \
  --setting-value "true"

# Set up security roles
pac security-role list --environment https://myorg.crm.dynamics.com

# Configure data loss prevention policies
pac dlp-policy list --environment https://myorg.crm.dynamics.com

# Set environment variables
pac env-variable create --environment https://myorg.crm.dynamics.com \
  --name "APIEndpoint" \
  --value "https://api.mycompany.com"
```

### Environment Validation Framework
```csharp
public class EnvironmentValidator
{
    public async Task<EnvironmentValidationResult> ValidateCompleteEnvironmentAsync(
        string environmentUrl,
        EnvironmentValidationSpec spec)
    {
        var validations = new List<ValidationCheck>
        {
            () => ValidateEnvironmentAccessibilityAsync(environmentUrl),
            () => ValidateRequiredAppsAsync(environmentUrl, spec.RequiredApps),
            () => ValidateDataverseSetupAsync(environmentUrl),
            () => ValidateSecurityConfigurationAsync(environmentUrl),
            () => ValidateIntegrationsAsync(environmentUrl, spec.RequiredIntegrations),
            () => ValidatePerformanceAsync(environmentUrl)
        };
        
        var results = new List<ValidationResult>();
        
        foreach (var validation in validations)
        {
            try
            {
                var result = await validation();
                results.Add(result);
                
                if (!result.Passed && result.Critical)
                {
                    // Stop validation if critical check fails
                    break;
                }
            }
            catch (Exception ex)
            {
                results.Add(ValidationResult.Error(validation.Method.Name, ex.Message));
            }
        }
        
        return new EnvironmentValidationResult
        {
            EnvironmentUrl = environmentUrl,
            ValidationResults = results,
            OverallStatus = DetermineOverallStatus(results),
            ValidationCompletedAt = DateTime.UtcNow,
            AllCriticalChecksPassed = results.Where(r => r.Critical).All(r => r.Passed),
            AllChecksPassed = results.All(r => r.Passed)
        };
    }
    
    private async Task<ValidationResult> ValidateEnvironmentAccessibilityAsync(string environmentUrl)
    {
        try
        {
            var result = await ExecuteCommandAsync($"pac env who --environment {environmentUrl}");
            
            if (result.ExitCode == 0)
            {
                return ValidationResult.Success("Environment Accessibility", 
                                              "Environment is accessible and responsive");
            }
            
            return ValidationResult.Failure("Environment Accessibility", 
                                           $"Environment not accessible: {result.StandardError}");
        }
        catch (Exception ex)
        {
            return ValidationResult.Error("Environment Accessibility", ex.Message);
        }
    }
    
    private async Task<ValidationResult> ValidateDataverseSetupAsync(string environmentUrl)
    {
        try
        {
            // Check if Dataverse is provisioned
            var result = await ExecuteCommandAsync($"pac org who --environment {environmentUrl}");
            
            if (result.ExitCode == 0)
            {
                // Verify basic Dataverse functionality
                var tablesResult = await ExecuteCommandAsync($"pac data list-tables --environment {environmentUrl}");
                
                if (tablesResult.ExitCode == 0)
                {
                    return ValidationResult.Success("Dataverse Setup", 
                                                  "Dataverse is properly configured and accessible");
                }
            }
            
            return ValidationResult.Warning("Dataverse Setup", 
                                           "Dataverse may not be fully configured");
        }
        catch (Exception ex)
        {
            return ValidationResult.Error("Dataverse Setup", ex.Message);
        }
    }
}
```

## Complete Environment Setup Orchestration

### Full Environment Provisioning Workflow
```csharp
public class EnvironmentProvisioningOrchestrator
{
    public async Task<ProvisioningResult> ProvisionCompleteEnvironmentAsync(
        EnvironmentProvisioningRequest request)
    {
        var provisioningSteps = new List<ProvisioningStep>
        {
            new ProvisioningStep("Authentication", 
                () => AuthenticateAsync(request.Credentials)),
            
            new ProvisioningStep("Environment Creation", 
                () => CreateEnvironmentAsync(request.EnvironmentSpec)),
            
            new ProvisioningStep("Wait for Provisioning", 
                () => WaitForEnvironmentReadyAsync(request.EnvironmentSpec.Name)),
            
            new ProvisioningStep("App Installation", 
                () => InstallRequiredAppsAsync(request.EnvironmentSpec, request.RequiredApps)),
            
            new ProvisioningStep("Environment Configuration", 
                () => ConfigureEnvironmentAsync(request.EnvironmentSpec, request.Configuration)),
            
            new ProvisioningStep("Security Setup", 
                () => SetupSecurityAsync(request.EnvironmentSpec, request.SecurityConfig)),
            
            new ProvisioningStep("Integration Configuration", 
                () => SetupIntegrationsAsync(request.EnvironmentSpec, request.Integrations)),
            
            new ProvisioningStep("Final Validation", 
                () => ValidateCompleteSetupAsync(request.EnvironmentSpec))
        };
        
        var results = new List<ProvisioningStepResult>();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            foreach (var step in provisioningSteps)
            {
                _logger.LogInformation("Executing provisioning step: {StepName}", step.Name);
                
                var stepResult = await ExecuteProvisioningStepAsync(step);
                results.Add(stepResult);
                
                if (!stepResult.Success)
                {
                    _logger.LogError("Provisioning step failed: {StepName} - {Error}", 
                                   step.Name, stepResult.Error);
                    
                    // Attempt rollback
                    await RollbackProvisioningAsync(request.EnvironmentSpec, results);
                    
                    return ProvisioningResult.Failure(
                        $"Provisioning failed at step: {step.Name}",
                        results,
                        stopwatch.Elapsed);
                }
                
                _logger.LogInformation("Provisioning step completed: {StepName}", step.Name);
            }
            
            stopwatch.Stop();
            
            _logger.LogInformation("Environment provisioning completed successfully in {Duration}", 
                                 stopwatch.Elapsed);
            
            return ProvisioningResult.Success(results, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during environment provisioning");
            await RollbackProvisioningAsync(request.EnvironmentSpec, results);
            
            return ProvisioningResult.Failure(ex.Message, results, stopwatch.Elapsed);
        }
    }
    
    private async Task RollbackProvisioningAsync(
        EnvironmentSpec environmentSpec, 
        List<ProvisioningStepResult> completedSteps)
    {
        try
        {
            _logger.LogWarning("Starting provisioning rollback for environment: {EnvironmentName}", 
                              environmentSpec.Name);
            
            // Rollback in reverse order
            var rollbackSteps = completedSteps
                .Where(s => s.Success && s.SupportsRollback)
                .Reverse()
                .ToList();
            
            foreach (var step in rollbackSteps)
            {
                try
                {
                    await step.RollbackAction?.Invoke();
                    _logger.LogInformation("Rolled back step: {StepName}", step.StepName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rollback step: {StepName}", step.StepName);
                }
            }
            
            // Finally, delete the environment if it was created
            if (completedSteps.Any(s => s.StepName == "Environment Creation" && s.Success))
            {
                try
                {
                    await DeleteEnvironmentAsync(environmentSpec.Name);
                    _logger.LogInformation("Environment deleted during rollback: {EnvironmentName}", 
                                         environmentSpec.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete environment during rollback: {EnvironmentName}", 
                                   environmentSpec.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during provisioning rollback");
        }
    }
}
```

## Monitoring and Maintenance

### Environment Health Monitoring
```csharp
public class EnvironmentHealthMonitor
{
    public async Task<EnvironmentHealthReport> GenerateHealthReportAsync(string environmentUrl)
    {
        var healthChecks = new List<Task<HealthCheckResult>>
        {
            CheckEnvironmentAvailabilityAsync(environmentUrl),
            CheckApplicationStatusAsync(environmentUrl),
            CheckDataverseHealthAsync(environmentUrl),
            CheckStorageUtilizationAsync(environmentUrl),
            CheckAPICallsUtilizationAsync(environmentUrl),
            CheckSecurityStatusAsync(environmentUrl)
        };
        
        var results = await Task.WhenAll(healthChecks);
        
        return new EnvironmentHealthReport
        {
            EnvironmentUrl = environmentUrl,
            CheckResults = results.ToList(),
            OverallHealth = DetermineOverallHealth(results),
            ReportGeneratedAt = DateTime.UtcNow,
            RecommendedActions = GenerateRecommendations(results)
        };
    }
}
```

## Next Steps

1. **Environment Setup**
   - Implement complete provisioning orchestrator
   - Create environment validation framework
   - Set up monitoring and health checks

2. **Application Management**
   - Build robust app installation pipeline
   - Implement dependency resolution
   - Create update and maintenance workflows

3. **Security and Compliance**
   - Implement security validation
   - Set up compliance monitoring
   - Create audit and reporting capabilities

4. **Integration and Automation**
   - Connect with Microsoft 365 CLI workflows
   - Build complete tenant provisioning
   - Implement automated maintenance tasks