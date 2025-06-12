# Octokit Libraries for .NET - Comprehensive Integration Guide

## Overview

**Strategic Recommendation**: Migrate from custom GitHub webhook implementation to official Octokit libraries for enhanced type safety, security, and maintainability.

## Library Comparison

### Octokit.NET (Primary GitHub API Client)
- **Repository**: [octokit/octokit.net](https://github.com/octokit/octokit.net)
- **Stars**: 2,790 ⭐ (High adoption, stable)
- **Documentation**: https://octokitnet.readthedocs.io/en/latest/
- **Purpose**: Complete GitHub API client for all GitHub operations
- **Platforms**: .NET 4.6.1+, .NET Standard 2.0+

### Octokit.Webhooks.NET (Webhook Specialist)
- **Repository**: [octokit/webhooks.net](https://github.com/octokit/webhooks.net)  
- **Stars**: 58 ⭐ (Newer, growing)
- **Purpose**: Specialized GitHub webhook handling
- **Key Advantage**: Type-safe webhook processing vs custom parsing

## Installation & Setup

### Core Packages
```bash
# Main GitHub API client
dotnet add package Octokit

# Reactive extensions version (optional)
dotnet add package Octokit.Reactive  

# ASP.NET Core webhook handling
dotnet add package Octokit.Webhooks.AspNetCore

# Azure Functions webhook handling (isolated process only)
dotnet add package Octokit.Webhooks.AzureFunctions
```

## Implementation Patterns

### 1. GitHub API Client (Octokit.NET)

#### Basic Usage
```csharp
var github = new GitHubClient(new ProductHeaderValue("MyAmazingApp"));
var user = await github.User.Get("half-ogre");
Console.WriteLine($"{user.Followers} folks love the half ogre!");
```

#### GitHub App Authentication
```csharp
var github = new GitHubClient(new ProductHeaderValue("MyGitHubApp"))
{
    Credentials = new Credentials("your_personal_access_token")
};

// For GitHub Apps
var appClient = new GitHubClient(new ProductHeaderValue("MyApp"))
{
    Credentials = new Credentials("jwt_token_here", AuthenticationType.Bearer)
};
```

### 2. Webhook Processing (Octokit.Webhooks.NET)

#### ASP.NET Core Implementation
```csharp
// 1. Create custom webhook processor
public sealed class MyWebhookEventProcessor : WebhookEventProcessor
{
    private readonly ILogger<MyWebhookEventProcessor> _logger;
    
    public MyWebhookEventProcessor(ILogger<MyWebhookEventProcessor> logger)
    {
        _logger = logger;
    }
    
    // Override specific event handlers
    protected override Task ProcessPullRequestWebhookAsync(
        WebhookHeaders headers, 
        PullRequestEvent pullRequestEvent, 
        PullRequestAction action)
    {
        _logger.LogInformation($"PR {action}: {pullRequestEvent.PullRequest.Title}");
        
        // Your business logic here
        return Task.CompletedTask;
    }
    
    protected override Task ProcessIssuesWebhookAsync(
        WebhookHeaders headers,
        IssuesEvent issuesEvent,
        IssuesAction action)
    {
        _logger.LogInformation($"Issue {action}: {issuesEvent.Issue.Title}");
        return Task.CompletedTask;
    }
    
    protected override Task ProcessWorkflowRunWebhookAsync(
        WebhookHeaders headers,
        WorkflowRunEvent workflowRunEvent,
        WorkflowRunAction action)
    {
        _logger.LogInformation($"Workflow {action}: {workflowRunEvent.WorkflowRun.Name}");
        return Task.CompletedTask;
    }
}

// 2. Register in DI container
builder.Services.AddSingleton<WebhookEventProcessor, MyWebhookEventProcessor>();

// 3. Map webhook endpoint  
app.UseEndpoints(endpoints =>
{
    // Maps to /api/github/webhooks with optional secret
    endpoints.MapGitHubWebhooks("/api/github/webhooks", "your_webhook_secret");
});
```

#### Available Event Processors
```csharp
// Most common webhook events with strong typing:
protected override Task ProcessPullRequestWebhookAsync(WebhookHeaders, PullRequestEvent, PullRequestAction) 
protected override Task ProcessIssuesWebhookAsync(WebhookHeaders, IssuesEvent, IssuesAction)
protected override Task ProcessWorkflowRunWebhookAsync(WebhookHeaders, WorkflowRunEvent, WorkflowRunAction)
protected override Task ProcessPushWebhookAsync(WebhookHeaders, PushEvent)
protected override Task ProcessReleaseWebhookAsync(WebhookHeaders, ReleaseEvent, ReleaseAction)
protected override Task ProcessRepositoryWebhookAsync(WebhookHeaders, RepositoryEvent, RepositoryAction)
```

#### Azure Functions Implementation
```csharp
// 1. Host configuration
new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(collection =>
    {
        collection.AddSingleton<WebhookEventProcessor, MyWebhookEventProcessor>();
    })
    .ConfigureGitHubWebhooks("your_webhook_secret")
    // Or with configuration
    .ConfigureGitHubWebhooks(config => config["GitHub:WebhookSecret"])
    .Build();
```

## Migration Strategy from Custom Implementation

### Current Custom Code Issues
1. **Manual signature validation** - Error-prone security implementation
2. **String-based parsing** - No type safety, runtime errors
3. **Switch statement routing** - Maintenance overhead  
4. **Custom error handling** - Inconsistent patterns
5. **Manual JSON deserialization** - Performance overhead

### Octokit.Webhooks.NET Benefits
1. ✅ **Automatic signature validation** - Built-in security
2. ✅ **Strong typing** - Compile-time safety
3. ✅ **Clean inheritance model** - Override only needed events
4. ✅ **Framework integration** - Native ASP.NET Core support
5. ✅ **Performance optimized** - Efficient JSON processing

### Migration Steps
1. **Install packages**: `Octokit.Webhooks.AspNetCore`
2. **Create processor**: Inherit from `WebhookEventProcessor`
3. **Port event logic**: Move switch cases to override methods
4. **Update registration**: Replace custom controller with `MapGitHubWebhooks()`
5. **Test thoroughly**: Validate all webhook event types
6. **Remove custom code**: Clean up manual implementation

## Integration with Current Architecture

### Environment Variables
```csharp
// Use existing environment variables
builder.Services.Configure<GitHubSettings>(config =>
{
    config.AppId = Environment.GetEnvironmentVariable("NGL_DEVOPS_APP_ID");
    config.PrivateKey = Environment.GetEnvironmentVariable("NGL_DEVOPS_PRIVATE_KEY");
    config.WebhookSecret = Environment.GetEnvironmentVariable("NGL_DEVOPS_WEBHOOK_SECRET");
});
```

### GitHub App Integration
```csharp
public class GitHubAppService
{
    private readonly GitHubClient _github;
    
    public GitHubAppService(IConfiguration config)
    {
        var appId = config["NGL_DEVOPS_APP_ID"];
        var privateKey = config["NGL_DEVOPS_PRIVATE_KEY"];
        
        // Create JWT for GitHub App authentication
        var jwt = CreateJwtToken(appId, privateKey);
        
        _github = new GitHubClient(new ProductHeaderValue("NGL-DevOps-Bot"))
        {
            Credentials = new Credentials(jwt, AuthenticationType.Bearer)
        };
    }
    
    public async Task<Installation> GetInstallationAsync(long installationId)
    {
        return await _github.GitHubApps.GetInstallation(installationId);
    }
}
```

## Security Patterns

### Webhook Secret Validation
```csharp
// Automatic validation - no manual signature checking required
endpoints.MapGitHubWebhooks("/api/github/webhook", 
    Environment.GetEnvironmentVariable("NGL_DEVOPS_WEBHOOK_SECRET"));
```

### GitHub App Token Management
```csharp
public class GitHubTokenService
{
    public async Task<string> GetInstallationTokenAsync(long installationId)
    {
        var installation = await _github.GitHubApps.CreateInstallationToken(installationId);
        return installation.Token;
    }
}
```

## Performance Optimizations

### Webhook Processing
- ✅ **Async/await patterns** - Non-blocking operations
- ✅ **Efficient JSON parsing** - Optimized deserialization  
- ✅ **Event filtering** - Process only needed events
- ✅ **Background processing** - Queue intensive operations

### API Rate Limiting
```csharp
// Built-in rate limit handling
var github = new GitHubClient(new ProductHeaderValue("MyApp"))
{
    Credentials = new Credentials("token"),
};

// Check rate limits
var rateLimit = await github.Miscellaneous.GetRateLimits();
Console.WriteLine($"Remaining: {rateLimit.Resources.Core.Remaining}");
```

## Testing Strategies

### Unit Testing Webhooks
```csharp
[Test]
public async Task ProcessPullRequest_ShouldLogCorrectly()
{
    // Arrange
    var processor = new MyWebhookEventProcessor(logger);
    var headers = new WebhookHeaders(/* test data */);
    var prEvent = new PullRequestEvent(/* test data */);
    
    // Act
    await processor.ProcessPullRequestWebhookAsync(headers, prEvent, PullRequestAction.Opened);
    
    // Assert
    // Verify logging, business logic, etc.
}
```

### Integration Testing
```csharp
[Test]
public async Task WebhookEndpoint_ShouldProcessValidPayload()
{
    // Test complete webhook pipeline with test server
    var response = await client.PostAsync("/api/github/webhooks", content);
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
}
```

## Monitoring & Observability

### Logging Patterns
```csharp
protected override Task ProcessPullRequestWebhookAsync(
    WebhookHeaders headers, 
    PullRequestEvent pullRequestEvent, 
    PullRequestAction action)
{
    using var scope = _logger.BeginScope(new Dictionary<string, object>
    {
        ["EventType"] = "pull_request",
        ["Action"] = action.ToString(),
        ["Repository"] = pullRequestEvent.Repository.FullName,
        ["PullRequestId"] = pullRequestEvent.PullRequest.Id
    });
    
    _logger.LogInformation("Processing pull request webhook");
    
    // Business logic here
    
    return Task.CompletedTask;
}
```

### Metrics Collection
```csharp
// Use built-in telemetry
services.AddApplicationInsightsTelemetry();

// Custom metrics in processor
private readonly TelemetryClient _telemetry;

protected override Task ProcessWorkflowRunWebhookAsync(...)
{
    _telemetry.TrackEvent("WorkflowProcessed", new Dictionary<string, string>
    {
        ["WorkflowName"] = workflowRunEvent.Workflow.Name,
        ["Status"] = workflowRunEvent.WorkflowRun.Status.ToString()
    });
    
    return Task.CompletedTask;
}
```

## Next Steps

1. **Evaluate current webhook implementation** - Audit existing code
2. **Create migration plan** - Prioritize event types  
3. **Implement Octokit.Webhooks.NET** - Start with core events
4. **Test thoroughly** - Validate all webhook scenarios
5. **Deploy incrementally** - Feature flag migration
6. **Monitor performance** - Compare before/after metrics
7. **Remove legacy code** - Clean up custom implementation

## References

- **Octokit.NET Docs**: https://octokitnet.readthedocs.io/en/latest/
- **GitHub API Docs**: https://docs.github.com/en/rest
- **Webhook Events**: https://docs.github.com/en/developers/webhooks-and-events/webhooks/webhook-events-and-payloads
- **GitHub Apps**: https://docs.github.com/en/developers/apps/getting-started-with-apps