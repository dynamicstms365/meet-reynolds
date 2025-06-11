# Microsoft 365 CLI - Comprehensive Guide for AI Agents

> **Sources:**
>
> - [CLI Microsoft 365 Documentation](https://pnp.github.io/cli-microsoft365/cmd/docs)
> - [CLI Microsoft 365 Sample Scripts](https://pnp.github.io/cli-microsoft365/sample-scripts/introduction)

## Quick Reference for AI Agents

### Command Categories Overview

```yaml
m365_cli_categories:
  authentication:
    commands: ["login", "logout", "status"]
    purpose: "Manage authentication to Microsoft 365"
    
  tenant:
    commands: ["tenant list", "tenant report", "tenant settings"]
    purpose: "Tenant-level operations and reporting"
    
  sharepoint:
    commands: ["spo site", "spo list", "spo file", "spo folder"]
    purpose: "SharePoint Online management"
    
  teams:
    commands: ["teams team", "teams channel", "teams app"]
    purpose: "Microsoft Teams management"
    
  power_platform:
    commands: ["pp solution", "pp environment", "pp dataverse"]
    purpose: "Power Platform integration"
    
  graph:
    commands: ["graph user", "graph group", "graph application"]
    purpose: "Microsoft Graph API operations"
```

### Common Usage Patterns

```yaml
typical_workflows:
  environment_setup:
    - m365_login
    - validate_permissions
    - list_available_resources
    
  app_deployment:
    - create_app_registration
    - configure_permissions
    - deploy_solution
    
  user_management:
    - list_users
    - manage_groups
    - assign_licenses
```

## Authentication Commands

### Core Authentication

```bash
# Interactive login
m365 login

# Login with specific credentials
m365 login --authType password --userName user@tenant.com --password <password>

# Login using certificate
m365 login --authType certificate --certificateFile ./cert.pfx --password <certPassword>

# Login using service principal
m365 login --authType secret --appId <appId> --tenant <tenantId> --secret <secret>

# Check authentication status
m365 status

# Logout
m365 logout
```

### Authentication Automation

```csharp
public class M365AuthenticationService
{
    private readonly ILogger<M365AuthenticationService> _logger;
    private readonly CliExecutor _cliExecutor;
    
    public async Task<AuthenticationResult> AuthenticateAsync(M365AuthConfig config)
    {
        try
        {
            var command = config.AuthType switch
            {
                AuthType.Interactive => "m365 login",
                AuthType.ServicePrincipal => $"m365 login --authType secret " +
                    $"--appId {config.AppId} --tenant {config.TenantId} --secret {config.Secret}",
                AuthType.Certificate => $"m365 login --authType certificate " +
                    $"--certificateFile {config.CertificatePath} --password {config.CertificatePassword}",
                _ => throw new NotSupportedException($"Auth type {config.AuthType} not supported")
            };
            
            var result = await _cliExecutor.ExecuteAsync(command);
            
            if (result.ExitCode == 0)
            {
                _logger.LogInformation("Successfully authenticated to Microsoft 365");
                return AuthenticationResult.Success();
            }
            
            _logger.LogError("Authentication failed: {Error}", result.StandardError);
            return AuthenticationResult.Failure(result.StandardError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during authentication");
            return AuthenticationResult.Failure(ex.Message);
        }
    }
    
    public async Task<bool> IsAuthenticatedAsync()
    {
        var result = await _cliExecutor.ExecuteAsync("m365 status");
        return result.ExitCode == 0 && result.StandardOutput.Contains("Logged in");
    }
}
```

## Tenant Management

### Tenant Information
```bash
# Get tenant details
m365 tenant report

# List tenant service health
m365 tenant serviceannouncement healthoverviews list

# Get tenant settings
m365 tenant settings list

# Update tenant settings
m365 tenant settings set --settingName <setting> --value <value>
```

### Service Health Monitoring
```bash
# Get current service health
m365 tenant serviceannouncement healthoverviews list

# Get service messages
m365 tenant serviceannouncement messages list

# Get specific service incidents
m365 tenant serviceannouncement issues list --query "service eq 'Exchange Online'"
```

### Automation Example
```csharp
public class TenantManagementService
{
    public async Task<TenantHealthReport> GetTenantHealthAsync()
    {
        var healthCommand = "m365 tenant serviceannouncement healthoverviews list --output json";
        var result = await _cliExecutor.ExecuteAsync(healthCommand);
        
        if (result.ExitCode == 0)
        {
            var healthData = JsonSerializer.Deserialize<HealthOverview[]>(result.StandardOutput);
            return new TenantHealthReport
            {
                Services = healthData,
                OverallStatus = DetermineOverallStatus(healthData),
                GeneratedAt = DateTime.UtcNow
            };
        }
        
        throw new TenantHealthException($"Failed to retrieve tenant health: {result.StandardError}");
    }
    
    public async Task<List<ServiceMessage>> GetCriticalMessagesAsync()
    {
        var messagesCommand = "m365 tenant serviceannouncement messages list --query \"classification eq 'Incident'\" --output json";
        var result = await _cliExecutor.ExecuteAsync(messagesCommand);
        
        if (result.ExitCode == 0)
        {
            return JsonSerializer.Deserialize<List<ServiceMessage>>(result.StandardOutput);
        }
        
        return new List<ServiceMessage>();
    }
}
```

## SharePoint Online Management

### Site Operations
```bash
# List all sites
m365 spo site list

# Get site information
m365 spo site get --url https://tenant.sharepoint.com/sites/sitename

# Create new site
m365 spo site add --url https://tenant.sharepoint.com/sites/newsite --title "New Site" --template "STS#3"

# Set site properties
m365 spo site set --url https://tenant.sharepoint.com/sites/sitename --title "Updated Title"

# Delete site (move to recycle bin)
m365 spo site remove --url https://tenant.sharepoint.com/sites/sitename

# Restore site from recycle bin
m365 spo site restore --url https://tenant.sharepoint.com/sites/sitename
```

### List and Library Management
```bash
# List all lists in a site
m365 spo list list --webUrl https://tenant.sharepoint.com/sites/sitename

# Create new list
m365 spo list add --webUrl https://tenant.sharepoint.com/sites/sitename --title "My List" --baseTemplate GenericList

# Get list information
m365 spo list get --webUrl https://tenant.sharepoint.com/sites/sitename --title "My List"

# Add list item
m365 spo listitem add --webUrl https://tenant.sharepoint.com/sites/sitename --listTitle "My List" --Title "Item Title"

# Update list item
m365 spo listitem set --webUrl https://tenant.sharepoint.com/sites/sitename --listTitle "My List" --id 1 --Title "Updated Title"
```

### File and Folder Operations
```bash
# Upload file
m365 spo file add --webUrl https://tenant.sharepoint.com/sites/sitename --folder "Shared Documents" --path ./localfile.txt

# Download file
m365 spo file get --webUrl https://tenant.sharepoint.com/sites/sitename --url "/sites/sitename/Shared Documents/file.txt" --path ./downloaded.txt

# Copy file
m365 spo file copy --webUrl https://tenant.sharepoint.com/sites/sitename --sourceUrl "/sites/sitename/Shared Documents/source.txt" --targetUrl "/sites/sitename/Archive/target.txt"

# Create folder
m365 spo folder add --webUrl https://tenant.sharepoint.com/sites/sitename --parentFolderUrl "Shared Documents" --name "New Folder"
```

### SharePoint Automation Framework
```csharp
public class SharePointOnlineService
{
    public async Task<Site> CreateSiteAsync(SiteCreationRequest request)
    {
        var command = $@"m365 spo site add 
            --url {request.Url} 
            --title ""{request.Title}"" 
            --template ""{request.Template}"" 
            --owners ""{string.Join(",", request.Owners)}"" 
            --output json";
            
        var result = await _cliExecutor.ExecuteAsync(command);
        
        if (result.ExitCode == 0)
        {
            var site = JsonSerializer.Deserialize<Site>(result.StandardOutput);
            
            // Configure additional settings
            await ConfigureSiteSettingsAsync(request.Url, request.Settings);
            
            return site;
        }
        
        throw new SiteCreationException($"Failed to create site: {result.StandardError}");
    }
    
    public async Task<bool> ProvisionSiteCollectionAsync(SiteCollectionSpec spec)
    {
        var tasks = new List<Task>
        {
            CreateSiteAsync(spec.MainSite),
            SetupNavigationAsync(spec.MainSite.Url, spec.Navigation),
            ApplyBrandingAsync(spec.MainSite.Url, spec.Branding),
            CreateSubsitesAsync(spec.MainSite.Url, spec.Subsites)
        };
        
        try
        {
            await Task.WhenAll(tasks);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Site collection provisioning failed");
            await RollbackSiteCollectionAsync(spec.MainSite.Url);
            return false;
        }
    }
}
```

## Microsoft Teams Management

### Team Operations
```bash
# List all teams
m365 teams team list

# Create new team
m365 teams team add --name "Project Team" --description "Team for project collaboration"

# Get team information
m365 teams team get --teamId <team-id>

# Archive team
m365 teams team archive --teamId <team-id>

# Unarchive team
m365 teams team unarchive --teamId <team-id>
```

### Channel Management
```bash
# List channels in a team
m365 teams channel list --teamId <team-id>

# Create channel
m365 teams channel add --teamId <team-id> --name "General Discussion" --description "General team discussions"

# Get channel information
m365 teams channel get --teamId <team-id> --channelId <channel-id>

# Create private channel
m365 teams channel add --teamId <team-id> --name "Private Channel" --type private
```

### Teams App Management
```bash
# List apps in tenant app catalog
m365 teams app list

# Upload app to tenant catalog
m365 teams app publish --filePath ./app.zip

# Install app to team
m365 teams app install --teamId <team-id> --appId <app-id>

# Update app
m365 teams app update --appId <app-id> --filePath ./updated-app.zip
```

### Teams Automation
```csharp
public class TeamsManagementService
{
    public async Task<Team> CreateProjectTeamAsync(ProjectTeamRequest request)
    {
        // Create team
        var createCommand = $@"m365 teams team add 
            --name ""{request.Name}"" 
            --description ""{request.Description}""
            --template ""{request.Template}""
            --output json";
            
        var result = await _cliExecutor.ExecuteAsync(createCommand);
        
        if (result.ExitCode != 0)
            throw new TeamCreationException(result.StandardError);
            
        var team = JsonSerializer.Deserialize<Team>(result.StandardOutput);
        
        // Create standard channels
        await CreateStandardChannelsAsync(team.Id, request.Channels);
        
        // Add members
        await AddTeamMembersAsync(team.Id, request.Members);
        
        // Install required apps
        await InstallTeamAppsAsync(team.Id, request.RequiredApps);
        
        return team;
    }
    
    private async Task CreateStandardChannelsAsync(string teamId, List<ChannelSpec> channels)
    {
        foreach (var channel in channels)
        {
            var command = $@"m365 teams channel add 
                --teamId {teamId} 
                --name ""{channel.Name}"" 
                --description ""{channel.Description}""
                --type {channel.Type}";
                
            await _cliExecutor.ExecuteAsync(command);
        }
    }
}
```

## Power Platform Integration

### Environment Operations
```bash
# List Power Platform environments (via Graph API)
m365 pp environment list

# Get environment details
m365 pp environment get --name <environment-name>

# Create new environment
m365 pp environment add --displayName "Development Environment" --location "unitedstates" --type "Sandbox"
```

### Solution Management
```bash
# List solutions in environment
m365 pp solution list --environment <environment-id>

# Export solution
m365 pp solution export --environment <environment-id> --name <solution-name> --path ./solutions/

# Import solution
m365 pp solution import --environment <environment-id> --path ./solution.zip
```

### Dataverse Operations
```bash
# List Dataverse tables
m365 pp dataverse table list --environment <environment-id>

# Get table information
m365 pp dataverse table get --environment <environment-id> --name <table-name>

# Create table
m365 pp dataverse table add --environment <environment-id> --name "CustomTable" --displayName "Custom Table"
```

## Microsoft Graph Operations

### User Management
```bash
# List users
m365 aad user list

# Get user information
m365 aad user get --id user@tenant.com

# Create user
m365 aad user add --displayName "John Doe" --mailNickname "johndoe" --userPrincipalName "johndoe@tenant.com"

# Update user
m365 aad user set --id johndoe@tenant.com --displayName "John Smith"

# Delete user
m365 aad user remove --id johndoe@tenant.com
```

### Group Management
```bash
# List groups
m365 aad group list

# Create security group
m365 aad group add --displayName "Security Group" --mailNickname "securitygroup" --securityEnabled

# Add member to group
m365 aad group member add --groupId <group-id> --userId <user-id>

# Remove member from group
m365 aad group member remove --groupId <group-id> --userId <user-id>
```

### Application Registration
```bash
# List app registrations
m365 aad app list

# Create app registration
m365 aad app add --name "My Application" --redirectUris "https://localhost:3000"

# Get app registration
m365 aad app get --appId <app-id>

# Update app registration
m365 aad app set --appId <app-id> --name "Updated Application Name"

# Delete app registration
m365 aad app remove --appId <app-id>
```

### Graph API Automation
```csharp
public class GraphApiService
{
    public async Task<AppRegistration> CreateAppRegistrationAsync(AppRegistrationRequest request)
    {
        var command = $@"m365 aad app add 
            --name ""{request.Name}"" 
            --redirectUris ""{string.Join(",", request.RedirectUris)}""
            --implicitFlow {request.EnableImplicitFlow}
            --output json";
            
        var result = await _cliExecutor.ExecuteAsync(command);
        
        if (result.ExitCode == 0)
        {
            var app = JsonSerializer.Deserialize<AppRegistration>(result.StandardOutput);
            
            // Configure API permissions
            if (request.ApiPermissions?.Any() == true)
            {
                await ConfigureApiPermissionsAsync(app.AppId, request.ApiPermissions);
            }
            
            // Create client secret if needed
            if (request.CreateClientSecret)
            {
                var secret = await CreateClientSecretAsync(app.AppId);
                app.ClientSecret = secret;
            }
            
            return app;
        }
        
        throw new AppRegistrationException($"Failed to create app registration: {result.StandardError}");
    }
    
    public async Task<UserProvisioningResult> ProvisionUsersAsync(List<UserSpec> users)
    {
        var results = new List<UserCreationResult>();
        var semaphore = new SemaphoreSlim(5); // Limit concurrent operations
        
        var tasks = users.Select(async user =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await CreateUserAsync(user);
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        var creationResults = await Task.WhenAll(tasks);
        
        return new UserProvisioningResult
        {
            TotalRequested = users.Count,
            SuccessfulCreations = creationResults.Count(r => r.Success),
            FailedCreations = creationResults.Count(r => !r.Success),
            Results = creationResults.ToList()
        };
    }
}
```

## Sample Scripts and Automation Patterns

### 1. Environment Provisioning Script
```bash
#!/bin/bash
# Complete M365 environment setup

set -e

echo "Starting Microsoft 365 environment provisioning..."

# Authenticate
m365 login

# Create SharePoint sites
echo "Creating SharePoint sites..."
m365 spo site add --url "https://tenant.sharepoint.com/sites/projecthub" --title "Project Hub" --template "STS#3"
m365 spo site add --url "https://tenant.sharepoint.com/sites/documents" --title "Document Center" --template "BDR#0"

# Create Teams
echo "Creating Teams..."
TEAM_ID=$(m365 teams team add --name "Project Collaboration" --description "Main project team" --query "id" --output text)

# Create channels
m365 teams channel add --teamId "$TEAM_ID" --name "Development" --description "Development discussions"
m365 teams channel add --teamId "$TEAM_ID" --name "Testing" --description "Testing coordination"

# Create security groups
echo "Creating security groups..."
m365 aad group add --displayName "Project Team Members" --mailNickname "projectteam" --securityEnabled

# Create app registration
echo "Creating app registration..."
m365 aad app add --name "Project Application" --redirectUris "https://localhost:3000"

echo "Environment provisioning complete!"
```

### 2. User Management Automation
```csharp
public class UserLifecycleManager
{
    public async Task<OnboardingResult> OnboardUserAsync(UserOnboardingRequest request)
    {
        var steps = new List<Func<Task<StepResult>>>
        {
            () => CreateUserAccountAsync(request.UserDetails),
            () => AssignLicensesAsync(request.UserDetails.UserPrincipalName, request.Licenses),
            () => AddToGroupsAsync(request.UserDetails.UserPrincipalName, request.SecurityGroups),
            () => ProvisionSharePointAccessAsync(request.UserDetails.UserPrincipalName, request.SharePointSites),
            () => AddToTeamsAsync(request.UserDetails.UserPrincipalName, request.Teams),
            () => ConfigureMailboxAsync(request.UserDetails.UserPrincipalName, request.MailboxSettings)
        };
        
        var results = new List<StepResult>();
        
        foreach (var step in steps)
        {
            try
            {
                var result = await step();
                results.Add(result);
                
                if (!result.Success)
                {
                    _logger.LogError("Onboarding step failed: {Error}", result.Error);
                    await RollbackOnboardingAsync(request.UserDetails.UserPrincipalName, results);
                    return OnboardingResult.Failure(result.Error, results);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during user onboarding step");
                await RollbackOnboardingAsync(request.UserDetails.UserPrincipalName, results);
                return OnboardingResult.Failure(ex.Message, results);
            }
        }
        
        return OnboardingResult.Success(results);
    }
}
```

### 3. Reporting and Analytics
```csharp
public class M365ReportingService
{
    public async Task<TenantActivityReport> GenerateActivityReportAsync(DateRange dateRange)
    {
        var tasks = new[]
        {
            GetSharePointActivityAsync(dateRange),
            GetTeamsActivityAsync(dateRange),
            GetEmailActivityAsync(dateRange),
            GetOneDriveActivityAsync(dateRange)
        };
        
        var results = await Task.WhenAll(tasks);
        
        return new TenantActivityReport
        {
            DateRange = dateRange,
            SharePointActivity = results[0],
            TeamsActivity = results[1],
            EmailActivity = results[2],
            OneDriveActivity = results[3],
            GeneratedAt = DateTime.UtcNow
        };
    }
    
    private async Task<SharePointActivityData> GetSharePointActivityAsync(DateRange dateRange)
    {
        var command = $"m365 spo report activityuserdetail --period D{dateRange.Days} --output json";
        var result = await _cliExecutor.ExecuteAsync(command);
        
        if (result.ExitCode == 0)
        {
            return JsonSerializer.Deserialize<SharePointActivityData>(result.StandardOutput);
        }
        
        return new SharePointActivityData();
    }
}
```

## Error Handling and Best Practices

### 1. Command Validation Framework
```csharp
public class M365CommandValidator
{
    private readonly HashSet<string> _allowedCommands = new()
    {
        // Authentication
        "m365 login",
        "m365 logout",
        "m365 status",
        
        // Read operations
        "m365 spo site list",
        "m365 teams team list",
        "m365 aad user list",
        "m365 aad group list",
        
        // Safe creation operations
        "m365 spo site add",
        "m365 teams team add",
        "m365 aad group add"
    };
    
    public ValidationResult ValidateCommand(string command)
    {
        var baseCommand = ExtractBaseCommand(command);
        
        if (!_allowedCommands.Contains(baseCommand))
        {
            return ValidationResult.Failure($"Command not allowed: {baseCommand}");
        }
        
        // Additional validation logic
        return ValidateCommandSyntax(command);
    }
}
```

### 2. Retry and Error Recovery
```csharp
public class M365ServiceWithRetry
{
    private readonly RetryPolicy _retryPolicy;
    
    public M365ServiceWithRetry()
    {
        _retryPolicy = Policy
            .Handle<M365CliException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} for command after {Delay}ms", 
                                     retryCount, timespan.TotalMilliseconds);
                });
    }
    
    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
    {
        return await _retryPolicy.ExecuteAsync(operation);
    }
}
```

### 3. Audit Logging
```csharp
public class M365AuditLogger
{
    public async Task LogCommandExecutionAsync(string command, CommandResult result, string userId)
    {
        var auditEntry = new M365AuditEntry
        {
            Command = command,
            UserId = userId,
            ExecutedAt = DateTime.UtcNow,
            Success = result.ExitCode == 0,
            Output = result.StandardOutput,
            Error = result.StandardError,
            ExecutionTimeMs = result.ExecutionTime.TotalMilliseconds
        };
        
        await _auditRepository.SaveAsync(auditEntry);
        
        if (!auditEntry.Success)
        {
            _logger.LogError("M365 CLI command failed: {Command} - {Error}", command, result.StandardError);
        }
    }
}
```

## Integration with Power Platform CLI

### Combined Automation Workflows
```csharp
public class IntegratedPlatformService
{
    private readonly PacCliService _pacCli;
    private readonly M365CliService _m365Cli;
    
    public async Task<EnvironmentSetupResult> SetupCompleteEnvironmentAsync(EnvironmentSetupRequest request)
    {
        // 1. Authenticate to both platforms
        await _m365Cli.AuthenticateAsync(request.M365Credentials);
        await _pacCli.AuthenticateAsync(request.PowerPlatformCredentials);
        
        // 2. Create SharePoint site for document storage
        var sharePointSite = await _m365Cli.CreateSiteAsync(new SiteCreationRequest
        {
            Url = $"https://{request.TenantName}.sharepoint.com/sites/{request.ProjectName}",
            Title = $"{request.ProjectName} Document Library",
            Template = "STS#3"
        });
        
        // 3. Create Power Platform environment
        var powerPlatformEnv = await _pacCli.CreateEnvironmentAsync(new EnvironmentCreationRequest
        {
            Name = $"{request.ProjectName}-Environment",
            Type = "Sandbox",
            Region = request.Region
        });
        
        // 4. Create Teams team for collaboration
        var team = await _m365Cli.CreateTeamAsync(new TeamCreationRequest
        {
            Name = $"{request.ProjectName} Team",
            Description = $"Collaboration team for {request.ProjectName}"
        });
        
        // 5. Install required Power Platform apps
        await _pacCli.InstallAppsAsync(powerPlatformEnv.Id, request.RequiredApps);
        
        // 6. Configure security groups
        var securityGroup = await _m365Cli.CreateSecurityGroupAsync(new GroupCreationRequest
        {
            DisplayName = $"{request.ProjectName} Users",
            MailNickname = $"{request.ProjectName.ToLower()}users"
        });
        
        return new EnvironmentSetupResult
        {
            SharePointSite = sharePointSite,
            PowerPlatformEnvironment = powerPlatformEnv,
            Team = team,
            SecurityGroup = securityGroup,
            SetupCompletedAt = DateTime.UtcNow
        };
    }
}
```

## Next Steps

1. **Tool Integration**
   - Set up M365 CLI with proper authentication
   - Configure command validation frameworks
   - Implement retry and error handling mechanisms

2. **Automation Development**
   - Create reusable automation scripts
   - Implement user lifecycle management
   - Build reporting and analytics capabilities

3. **Security Implementation**
   - Configure secure authentication methods
   - Implement audit logging
   - Set up proper permission models

4. **Testing and Validation**
   - Create comprehensive test suites
   - Implement integration testing
   - Set up monitoring and alerting