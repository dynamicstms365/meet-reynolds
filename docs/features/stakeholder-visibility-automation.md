# Stakeholder Visibility Automation

This document describes the stakeholder visibility automation feature implemented in the Reynolds Copilot agent.

## Overview

The stakeholder visibility automation feature provides:

1. **Customizable stakeholder preferences** for update types and notification channels
2. **Dynamic dashboards** summarizing project progress
3. **Automated notifications** triggered by GitHub events
4. **Seamless integration** with GitHub webhook processing

## Features

### 1. Stakeholder Management

- Create, update, and delete stakeholder configurations
- Define repository access and update preferences
- Configure notification channels (Email, Teams, Dashboard, Webhook)
- Set notification frequency (Immediate, Hourly, Daily, Weekly)

### 2. Customizable Dashboards

- **Project Progress Summary**: Issues, Pull Requests, Discussions metrics
- **Configurable Widgets**: Issues summary, PR summary, recent activity, metrics
- **Real-time Data**: Generated on-demand with fresh GitHub data
- **Tailored Views**: Filtered by stakeholder preferences and repository access

### 3. Automated Notifications

- **Event-driven**: Triggered by GitHub webhook events (issues, pull requests)
- **Multi-channel Support**: Email, Microsoft Teams, Dashboard, and Webhook notifications
- **Preference-based Filtering**: Only sends notifications stakeholders want
- **Rich Content**: Structured notifications with relevant event data

### 4. GitHub Integration

- **Webhook Processing**: Enhanced OctokitWebhookEventProcessor with notification support
- **Real-time Updates**: Automatic stakeholder notifications on issue/PR changes
- **Existing Services**: Leverages GitHubIssuesService and GitHubDiscussionsService

## API Endpoints

### Stakeholder Management

```bash
# Get all stakeholders
GET /api/stakeholderdashboard/stakeholders

# Get specific stakeholder
GET /api/stakeholderdashboard/stakeholders/{id}

# Create stakeholder
POST /api/stakeholderdashboard/stakeholders

# Update stakeholder  
PUT /api/stakeholderdashboard/stakeholders/{id}

# Delete stakeholder
DELETE /api/stakeholderdashboard/stakeholders/{id}
```

### Dashboard

```bash
# Get project dashboard
GET /api/stakeholderdashboard/dashboard/project/{repository}?stakeholderId={id}

# Get organization dashboard
GET /api/stakeholderdashboard/dashboard/organization/{organization}?stakeholderId={id}

# Get dashboard configuration
GET /api/stakeholderdashboard/stakeholders/{id}/dashboard-config

# Update dashboard configuration
PUT /api/stakeholderdashboard/stakeholders/{id}/dashboard-config
```

### Notifications

```bash
# Get stakeholder notifications
GET /api/stakeholderdashboard/stakeholders/{id}/notifications

# Create manual notification
POST /api/stakeholderdashboard/notifications

# Broadcast notification to repository stakeholders
POST /api/stakeholderdashboard/notifications/broadcast

# Process pending notifications
POST /api/stakeholderdashboard/notifications/process
```

## Example Usage

### 1. Create a Stakeholder

```json
POST /api/stakeholderdashboard/stakeholders
{
  "name": "Project Manager",
  "email": "pm@company.com", 
  "repositories": ["company/main-app", "company/api-service"],
  "updatePreferences": {
    "issueProgressUpdates": true,
    "pullRequestStatusUpdates": true,
    "discussionUpdates": false,
    "frequency": "Daily",
    "channels": ["Email", "Teams"],
    "importantLabels": ["critical", "high-priority"]
  },
  "dashboardConfig": {
    "title": "Project Manager Dashboard",
    "widgets": [
      {"id": "issues", "type": "IssuesSummary", "order": 1},
      {"id": "prs", "type": "PullRequestsSummary", "order": 2}
    ]
  }
}
```

### 2. Get Project Dashboard

```bash
GET /api/stakeholderdashboard/dashboard/project/company%2Fmain-app?stakeholderId=stakeholder-id
```

Response includes:
- Issues summary (open/closed counts, recent issues)
- Pull requests summary (open/merged counts, recent PRs)
- Project metrics (health score, productivity)
- Recent activity across repository

### 3. Broadcast Notification

```json
POST /api/stakeholderdashboard/notifications/broadcast
{
  "repository": "company/main-app",
  "type": "ProjectSummary", 
  "data": {
    "message": "Weekly project update",
    "additionalInfo": "All critical issues resolved"
  }
}
```

## Data Models

### StakeholderConfiguration

```csharp
public class StakeholderConfiguration
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string[] Repositories { get; set; }
    public StakeholderUpdatePreferences UpdatePreferences { get; set; }
    public DashboardConfiguration DashboardConfig { get; set; }
    public bool IsActive { get; set; }
    // ... timestamps
}
```

### ProjectProgressSummary

```csharp
public class ProjectProgressSummary
{
    public string Repository { get; set; }
    public DateTime GeneratedAt { get; set; }
    public IssuesSummary Issues { get; set; }
    public PullRequestsSummary PullRequests { get; set; }
    public DiscussionsSummary Discussions { get; set; }
    public ProjectMetrics Metrics { get; set; }
    public RecentActivity[] RecentActivities { get; set; }
}
```

## Implementation Details

### Services

1. **StakeholderVisibilityService**: Core stakeholder CRUD operations
2. **DashboardService**: Dashboard generation and widget management
3. **NotificationService**: Notification creation, sending, and processing
4. **Enhanced OctokitWebhookEventProcessor**: Automatic notification triggers

### Storage

- **In-memory storage** for this implementation (production should use persistent database)
- **Thread-safe operations** using ConcurrentDictionary and ConcurrentQueue
- **Audit logging** for all operations via SecurityAuditService

### Security

- **Input validation** and sanitization
- **Audit logging** for all stakeholder and notification operations
- **Error handling** with graceful degradation
- **Repository access validation** (stakeholders can only access configured repositories)

## Testing

Comprehensive unit tests included:

- **StakeholderVisibilityServiceTests**: 11 test methods covering CRUD operations
- **NotificationServiceTests**: 8 test methods covering notification workflows
- **Build validation**: All tests pass, no breaking changes

## Future Enhancements

1. **Persistent Database**: Replace in-memory storage with proper database
2. **Advanced Filtering**: More sophisticated notification filtering rules
3. **Email Templates**: Rich HTML email templates for notifications
4. **Slack Integration**: Additional notification channel
5. **Analytics**: Usage metrics and effectiveness tracking
6. **Webhook Signatures**: Enhanced security for outgoing webhooks

## Dependencies

- Existing GitHub integration services (GitHubIssuesService, GitHubDiscussionsService)
- SecurityAuditService for audit logging
- Octokit webhook processing infrastructure
- .NET 8 dependency injection container