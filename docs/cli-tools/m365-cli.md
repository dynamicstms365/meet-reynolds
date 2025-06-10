# Microsoft 365 CLI Guide

> **Source:** [CLI for Microsoft 365 Documentation](https://pnp.github.io/cli-microsoft365/cmd/docs)

## Overview

The CLI for Microsoft 365 (`m365`) is a cross-platform command-line interface that allows you to manage your Microsoft 365 tenant and SharePoint Framework projects. This guide focuses on commands essential for Power Platform Copilot agent integration.

## Authentication Commands

### m365 login
Authenticate with Microsoft 365 services.

```bash
# Interactive login
m365 login

# Login with specific tenant
m365 login --tenant <tenant-id>

# Device code login (for CI/CD)
m365 login --authType deviceCode

# Certificate-based authentication
m365 login --authType certificate --certificateFile ./cert.pfx --password <password>

# Client credentials flow
m365 login --authType secret --appId <app-id> --secret <client-secret> --tenant <tenant-id>
```

#### Authentication Automation
```csharp
public class M365AuthService
{
    public async Task<AuthResult> AuthenticateAsync(AuthOptions options)
    {
        var command = options.AuthType switch
        {
            AuthType.Interactive => "m365 login",
            AuthType.DeviceCode => "m365 login --authType deviceCode",
            AuthType.Certificate => $"m365 login --authType certificate --certificateFile {options.CertificateFile} --password {options.Password}",
            AuthType.ClientCredentials => $"m365 login --authType secret --appId {options.AppId} --secret {options.ClientSecret} --tenant {options.TenantId}",
            _ => throw new ArgumentException("Invalid auth type")
        };

        var result = await ExecuteCommandAsync(command);
        return new AuthResult { Success = result.ExitCode == 0, Message = result.Output };
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var result = await ExecuteCommandAsync("m365 status");
        return result.ExitCode == 0 && result.Output.Contains("Logged in");
    }
}
```

## App Registration Management

### m365 app
Manage Azure AD app registrations for Power Platform integration.

```bash
# List app registrations
m365 app list

# Get app details
m365 app get --appId <app-id>

# Create new app registration
m365 app add --name "Power Platform Copilot Agent" --multitenant

# Update app registration
m365 app set --appId <app-id> --name "Updated App Name"

# Add app permissions
m365 app permission add --appId <app-id> --delegatedPermissions "https://service.powerapps.com/user_impersonation"

# Grant admin consent
m365 app permission grant --appId <app-id>
```

#### App Registration Automation
```csharp
public class AppRegistrationManager
{
    public async Task<AppRegistration> CreateAppRegistrationAsync(AppRegistrationSpec spec)
    {
        var createCommand = $@"m365 app add 
            --name ""{spec.Name}"" 
            --multitenant {spec.IsMultiTenant.ToString().ToLower()}
            --output json";

        var result = await ExecuteCommandAsync(createCommand);
        
        if (result.Success)
        {
            var appData = JsonSerializer.Deserialize<AppRegistrationResponse>(result.Output);
            
            // Add required permissions
            await AddPermissionsAsync(appData.AppId, spec.RequiredPermissions);
            
            // Grant admin consent if specified
            if (spec.GrantAdminConsent)
            {
                await GrantAdminConsentAsync(appData.AppId);
            }
            
            return new AppRegistration
            {
                AppId = appData.AppId,
                Name = spec.Name,
                IsMultiTenant = spec.IsMultiTenant
            };
        }
        
        throw new AppRegistrationException(result.Error);
    }

    private async Task AddPermissionsAsync(string appId, IEnumerable<Permission> permissions)
    {
        foreach (var permission in permissions)
        {
            var command = $@"m365 app permission add 
                --appId {appId} 
                --{permission.Type}Permissions ""{permission.Scope}""";
            
            await ExecuteCommandAsync(command);
        }
    }
}
```

## SharePoint Operations

### m365 spo
Manage SharePoint Online sites and lists for data storage.

```bash
# List sites
m365 spo site list

# Get site details
m365 spo site get --url <site-url>

# Create new site
m365 spo site add --url <site-url> --title "Power Platform Documentation"

# List site lists
m365 spo list list --webUrl <site-url>

# Create list
m365 spo list add --webUrl <site-url> --title "Agent Configurations" --baseTemplate GenericList
```

#### SharePoint Automation
```csharp
public class SharePointManager
{
    public async Task<Site> CreateDocumentationSiteAsync(SiteSpec spec)
    {
        var command = $@"m365 spo site add 
            --url {spec.Url} 
            --title ""{spec.Title}"" 
            --description ""{spec.Description}""
            --output json";

        var result = await ExecuteCommandAsync(command);
        
        if (result.Success)
        {
            var siteData = JsonSerializer.Deserialize<SiteResponse>(result.Output);
            
            // Create required lists
            await CreateConfigurationListAsync(spec.Url);
            await CreateLoggingListAsync(spec.Url);
            
            return new Site
            {
                Url = siteData.Url,
                Title = spec.Title,
                Id = siteData.Id
            };
        }
        
        throw new SharePointException(result.Error);
    }

    private async Task CreateConfigurationListAsync(string siteUrl)
    {
        var command = $@"m365 spo list add 
            --webUrl {siteUrl} 
            --title ""Agent Configurations"" 
            --baseTemplate GenericList";
        
        await ExecuteCommandAsync(command);
        
        // Add custom fields
        await AddListFieldsAsync(siteUrl, "Agent Configurations");
    }
}
```

## Teams Integration

### m365 teams
Manage Microsoft Teams for collaboration and notifications.

```bash
# List teams
m365 teams list

# Get team details
m365 teams get --teamId <team-id>

# Create new team
m365 teams add --name "Power Platform Development" --description "Copilot agent development team"

# Add team member
m365 teams user add --teamId <team-id> --userEmail <user-email>

# Send message to channel
m365 teams message send --teamId <team-id> --channelId <channel-id> --message "Deployment completed successfully"
```

#### Teams Notification Service
```csharp
public class TeamsNotificationService
{
    public async Task SendDeploymentNotificationAsync(string teamId, string channelId, DeploymentResult result)
    {
        var message = result.Success 
            ? $"✅ Deployment completed successfully. Environment: {result.EnvironmentName}"
            : $"❌ Deployment failed. Error: {result.Error}";

        var command = $@"m365 teams message send 
            --teamId {teamId} 
            --channelId {channelId} 
            --message ""{message}""";

        await ExecuteCommandAsync(command);
    }

    public async Task CreateDevelopmentTeamAsync(TeamSpec spec)
    {
        var createCommand = $@"m365 teams add 
            --name ""{spec.Name}"" 
            --description ""{spec.Description}""
            --output json";

        var result = await ExecuteCommandAsync(createCommand);
        
        if (result.Success)
        {
            var teamData = JsonSerializer.Deserialize<TeamResponse>(result.Output);
            
            // Add team members
            foreach (var member in spec.Members)
            {
                await AddTeamMemberAsync(teamData.Id, member.Email);
            }
        }
    }
}
```

## Graph API Operations

### m365 graph
Execute Microsoft Graph API calls directly.

```bash
# Get user profile
m365 graph get --resource "https://graph.microsoft.com/v1.0/me"

# List applications
m365 graph get --resource "https://graph.microsoft.com/v1.0/applications"

# Create group
m365 graph post --resource "https://graph.microsoft.com/v1.0/groups" --body '{"displayName":"Power Platform Developers","mailEnabled":false,"securityEnabled":true}'
```

## Validation and Testing

### Command Validation Framework
```csharp
public class M365CliValidator
{
    private readonly HashSet<string> _allowedCommands = new()
    {
        "m365 status",
        "m365 app list",
        "m365 app get",
        "m365 app add",
        "m365 spo site list",
        "m365 spo site get",
        "m365 teams list",
        "m365 teams get"
    };

    public bool IsCommandSafe(string command)
    {
        var baseCommand = ExtractBaseCommand(command);
        return _allowedCommands.Any(allowed => command.StartsWith(allowed));
    }

    public async Task<ValidationResult> ValidateCommandAsync(string command)
    {
        if (!IsCommandSafe(command))
        {
            return ValidationResult.Failure($"Command not allowed: {command}");
        }

        // Check authentication
        if (!await IsAuthenticatedAsync())
        {
            return ValidationResult.Failure("Not authenticated with Microsoft 365");
        }

        return ValidationResult.Success();
    }
}
```

### Testing Framework
```csharp
[Test]
public async Task Should_Authenticate_Successfully()
{
    // Arrange
    var authService = new M365AuthService();
    var options = new AuthOptions { AuthType = AuthType.DeviceCode };
    
    // Act
    var result = await authService.AuthenticateAsync(options);
    
    // Assert
    Assert.That(result.Success, Is.True);
}

[Test]
public async Task Should_Create_App_Registration()
{
    // Arrange
    var appManager = new AppRegistrationManager();
    var spec = new AppRegistrationSpec
    {
        Name = "Test App",
        IsMultiTenant = false,
        RequiredPermissions = new[]
        {
            new Permission { Type = "delegated", Scope = "https://service.powerapps.com/user_impersonation" }
        }
    };
    
    // Act
    var app = await appManager.CreateAppRegistrationAsync(spec);
    
    // Assert
    Assert.That(app.AppId, Is.Not.Null);
    Assert.That(app.Name, Is.EqualTo("Test App"));
}
```

## Error Handling and Logging

### CLI Error Management
```csharp
public class M365CliService
{
    private readonly ILogger<M365CliService> _logger;

    public async Task<CliResult> ExecuteAsync(string command)
    {
        try
        {
            _logger.LogInformation("Executing M365 CLI command: {Command}", SanitizeCommand(command));
            
            var result = await RunProcessAsync("m365", command);
            
            if (result.ExitCode != 0)
            {
                _logger.LogError("M365 CLI command failed: {Error}", result.StandardError);
                return CliResult.Failure(result.StandardError);
            }
            
            _logger.LogInformation("M365 CLI command completed successfully");
            return CliResult.Success(result.StandardOutput);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception executing M365 CLI command: {Command}", SanitizeCommand(command));
            return CliResult.Failure(ex.Message);
        }
    }

    private string SanitizeCommand(string command)
    {
        // Remove sensitive information from logs
        return command.Replace("--secret", "--secret ***")
                     .Replace("--password", "--password ***");
    }
}
```

## Integration Examples

### Complete App Registration Workflow
```csharp
public async Task<PowerPlatformApp> SetupPowerPlatformAppAsync(AppSetupRequest request)
{
    // 1. Authenticate
    await _authService.AuthenticateAsync(request.AuthOptions);
    
    // 2. Create app registration
    var appSpec = new AppRegistrationSpec
    {
        Name = request.AppName,
        IsMultiTenant = false,
        RequiredPermissions = new[]
        {
            new Permission { Type = "delegated", Scope = "https://service.powerapps.com/user_impersonation" },
            new Permission { Type = "delegated", Scope = "https://graph.microsoft.com/User.Read" }
        },
        GrantAdminConsent = true
    };
    
    var app = await _appManager.CreateAppRegistrationAsync(appSpec);
    
    // 3. Create SharePoint site for documentation
    var siteSpec = new SiteSpec
    {
        Url = $"https://{request.TenantName}.sharepoint.com/sites/{request.AppName.ToLower()}",
        Title = $"{request.AppName} Documentation",
        Description = "Documentation and configuration for Power Platform Copilot agent"
    };
    
    var site = await _sharePointManager.CreateDocumentationSiteAsync(siteSpec);
    
    // 4. Create Teams team for collaboration
    var teamSpec = new TeamSpec
    {
        Name = $"{request.AppName} Development",
        Description = "Development team for Power Platform Copilot agent",
        Members = request.TeamMembers
    };
    
    var team = await _teamsManager.CreateDevelopmentTeamAsync(teamSpec);
    
    return new PowerPlatformApp
    {
        AppRegistration = app,
        DocumentationSite = site,
        DevelopmentTeam = team
    };
}
```

## Best Practices

### Security
- Use certificate-based authentication for production
- Implement proper secret management
- Audit all CLI operations
- Follow principle of least privilege

### Performance
- Cache authentication tokens
- Use batch operations when available
- Implement proper timeout handling
- Monitor and optimize long-running operations

### Reliability
- Implement retry logic with exponential backoff
- Validate preconditions before operations
- Use transactional patterns where possible
- Implement proper error recovery

## Next Steps

1. Review [CLI Scripts and Samples](./scripts-samples.md)
2. Implement [Power Platform Integration](../power-platform/README.md)
3. Set up [Azure App Registration Automation](../implementation/azure-app-registration.md)
4. Configure [GitHub Enterprise Integration](../implementation/github-enterprise.md)