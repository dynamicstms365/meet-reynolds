# Scope Creep Monitoring Feature

## Overview

The Reynolds Copilot agent now includes comprehensive scope creep monitoring to help teams detect and prevent project scope expansion in real-time. This feature continuously monitors project parameters and provides Reynolds-style diplomatic alerts when scope deviations are detected.

## Features

### Real-time Monitoring
- **Webhook Integration**: Automatically captures GitHub events (issues, PRs) 
- **Continuous Assessment**: Compares current project state against defined scope parameters
- **Threshold-based Alerting**: Configurable deviation thresholds (default: 25%)

### Reynolds Personality
- **Diplomatic Messages**: Witty, professional scope creep detection messages
- **Smart Recommendations**: Context-aware mitigation suggestions
- **Actionable Insights**: Clear guidance on next steps

### Comprehensive Metrics
- **Issue Tracking**: Monitors issue count vs. expected
- **Pull Request Monitoring**: Tracks PR count against baseline
- **Task Management**: Compares task completion against scope
- **Deviation Calculations**: Precise percentage-based scope analysis

## API Endpoints

### Analyze Project Scope
```http
POST /api/github/scope/analyze
```

Analyzes current project scope and returns detailed metrics.

**Request Body:**
```json
{
  "repository": "owner/repo-name",
  "scopeParameters": {
    "projectId": "my-project",
    "repository": "owner/repo-name",
    "expectedIssueCount": 10,
    "expectedPullRequestCount": 3,
    "expectedTaskCount": 10,
    "scopeDeviationThreshold": 0.25,
    "projectStartDate": "2024-01-01T00:00:00Z",
    "projectEndDate": "2024-06-01T00:00:00Z",
    "scopeKeywords": ["feature", "enhancement"]
  }
}
```

**Response:**
```json
{
  "projectId": "my-project",
  "repository": "owner/repo-name",
  "currentIssueCount": 12,
  "expectedIssueCount": 10,
  "issueDeviation": 0.20,
  "overallDeviation": 0.15,
  "hasCreep": false,
  "creepIndicators": [],
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Check for Scope Creep
```http
POST /api/github/scope/check
```

Checks for scope creep and returns alerts if detected.

**Response with Scope Creep:**
```json
{
  "projectId": "my-project",
  "repository": "owner/repo-name",
  "severity": "Medium",
  "summary": "Issues: 15 vs 10 expected (+50%)",
  "reynoldsMessage": "This is amazing work! Also, it's grown like 400% from the original issue. Coincidence?",
  "changes": [
    "Issue count exceeded expected by 50%"
  ],
  "recommendations": [
    "Monitor closely to prevent further scope expansion",
    "Consider prioritizing features to stay within original scope"
  ],
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Get Recent Alerts
```http
GET /api/github/scope/{repository}/alerts?hours=24
```

Retrieves recent scope creep alerts for a repository.

### Check if Within Scope
```http
POST /api/github/scope/within-scope
```

Returns a simple boolean indicating if the project is within defined scope.

## Configuration

### Scope Parameters

| Parameter | Description | Default | Example |
|-----------|-------------|---------|---------|
| `expectedIssueCount` | Expected number of issues | - | 10 |
| `expectedPullRequestCount` | Expected number of PRs | - | 3 |
| `expectedTaskCount` | Expected number of tasks | - | 10 |
| `scopeDeviationThreshold` | Deviation threshold (%) | 0.25 | 0.30 |
| `projectStartDate` | Project start date | - | "2024-01-01" |
| `projectEndDate` | Project end date (optional) | null | "2024-06-01" |
| `scopeKeywords` | Keywords to monitor | [] | ["feature", "bug"] |

### Monitoring Options

Configure the scope monitoring service behavior:

```json
{
  "enableScopeMonitoring": true,
  "scopeDeviationThreshold": 0.25,
  "monitoringWindow": "01:00:00",
  "enableReynoldsMessages": true,
  "enableRecommendations": true,
  "maxRecentAdditionsToTrack": 10
}
```

## Severity Levels

| Severity | Deviation Range | Description |
|----------|----------------|-------------|
| **Low** | 0-24% | Minor scope expansion |
| **Medium** | 25-49% | Moderate scope creep |
| **High** | 50-99% | Significant scope expansion |
| **Critical** | 100%+ | Severe scope creep |

## Reynolds-Style Messages

The system includes 8 diplomatic scope creep detection messages:

- "This is amazing work! Also, it's grown like 400% from the original issue. Coincidence?"
- "I'm getting 'feature creep' vibes here. Should we Aviation Gin this into two separate bottles?"
- "Love the additions! Quick check - are we still hitting our deadline or are we in 'time is a construct' territory?"
- "This started as a bicycle and became a Tesla. Impressive! But should we maybe document that?"
- And 4 more witty, professional messages...

## Automatic Event Recording

The system automatically records scope-relevant events via webhooks:

### Monitored Events
- **Issues**: opened, closed, reopened, edited
- **Pull Requests**: opened, closed, reopened, edited
- **Project Items**: created, updated, deleted

### Event Data Captured
- Event type and action
- Item details (title, author, timestamps)
- Repository information
- User context

## Integration with Existing Services

### Audit Trail
All scope events and alerts are logged through the existing `SecurityAuditService`:
- Event recording with full context
- Alert generation with severity tracking
- Historical analysis support

### GitHub API
Leverages existing GitHub services:
- `GitHubIssuesService` for issue data
- `OctokitWebhookEventProcessor` for real-time events
- `GitHubAppAuthService` for authentication

## Example Usage

### 1. Define Project Scope
```csharp
var scopeParameters = new ProjectScopeParameters
{
    ProjectId = "q1-user-dashboard",
    Repository = "myorg/user-dashboard", 
    ExpectedIssueCount = 15,
    ExpectedPullRequestCount = 5,
    ExpectedTaskCount = 15,
    ScopeDeviationThreshold = 0.30, // 30% threshold
    ProjectStartDate = DateTime.Parse("2024-01-01"),
    ProjectEndDate = DateTime.Parse("2024-03-31"),
    ScopeKeywords = new List<string> { "dashboard", "user", "analytics" }
};
```

### 2. Check for Scope Creep
```csharp
var alert = await scopeMonitoringService.CheckForScopeCreepAsync(
    "myorg/user-dashboard", 
    scopeParameters);

if (alert != null)
{
    Console.WriteLine($"Scope creep detected: {alert.Summary}");
    Console.WriteLine($"Reynolds says: {alert.ReynoldsMessage}");
    
    foreach (var recommendation in alert.Recommendations)
    {
        Console.WriteLine($"• {recommendation}");
    }
}
```

### 3. Monitor in Real-time
The webhook integration automatically monitors scope changes. No manual intervention required - alerts are generated and logged automatically when scope deviations are detected.

## Best Practices

### Setting Realistic Thresholds
- Start with 25-30% deviation threshold
- Adjust based on project type and team dynamics
- Consider project phase (early phases may need higher thresholds)

### Regular Monitoring
- Review scope metrics weekly
- Act on Medium+ severity alerts promptly
- Use historical data to improve future scope planning

### Team Communication
- Share scope parameters with the entire team
- Use Reynolds messages to lighten serious scope discussions
- Document scope changes and rationale

## Troubleshooting

### Common Issues

1. **No alerts generated**: Check webhook configuration and scope parameters
2. **Too many false positives**: Increase deviation threshold
3. **Missing events**: Verify GitHub App permissions and webhook setup

### Logging

All scope monitoring activities are logged with appropriate levels:
- **Debug**: Event recording and routine checks
- **Info**: Scope analysis and alert generation  
- **Warning**: Scope creep detection
- **Error**: Processing failures

Check logs for detailed troubleshooting information.