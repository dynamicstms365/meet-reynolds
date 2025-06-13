# GitHub Issue-PR Synchronization API Endpoints

This document describes the REST API endpoints added to the GitHubController for GitHub Issue-PR synchronization functionality.

## Base URL
All endpoints are prefixed with `/api/github/`

## Endpoints

### 1. Generate Synchronization Report

**GET** `/api/github/sync/report/{repository}`

Generates a comprehensive synchronization report for a repository showing issue-PR relationships, orphaned items, and synchronization status.

**Parameters:**
- `repository` (path) - Repository name in format `owner/repo`

**Response:**
```json
{
  "generatedAt": "2025-01-11T10:30:00Z",
  "repository": "owner/repo",
  "issuePRRelations": [
    {
      "issue": {
        "number": 123,
        "title": "Fix authentication bug",
        "state": "open"
      },
      "relatedPRs": [
        {
          "number": 456,
          "title": "Fix auth issue #123",
          "state": "open",
          "isMerged": false
        }
      ],
      "synchronizationStatus": "synchronized",
      "recommendedAction": "No action required"
    }
  ],
  "orphanedPRs": [],
  "orphanedIssues": [],
  "summary": {
    "totalIssues": 10,
    "totalPRs": 8,
    "synchronizedRelations": 7,
    "needsUpdateRelations": 1,
    "conflictedRelations": 0,
    "orphanedPRs": 0,
    "orphanedIssues": 2
  }
}
```

**Example:**
```bash
curl -X GET "https://your-api/api/github/sync/report/microsoft/copilot-powerplatform"
```

### 2. Synchronize Single Issue

**POST** `/api/github/sync/issue/{repository}/{issueNumber}`

Synchronizes a specific issue with its related pull requests.

**Parameters:**
- `repository` (path) - Repository name in format `owner/repo`
- `issueNumber` (path) - Issue number to synchronize

**Response:**
```json
{
  "success": true,
  "message": "Issue #123 synchronized successfully",
  "issueNumber": 123,
  "repository": "owner/repo"
}
```

**Example:**
```bash
curl -X POST "https://your-api/api/github/sync/issue/microsoft/copilot-powerplatform/123"
```

### 3. Synchronize All Issues

**POST** `/api/github/sync/all/{repository}`

Synchronizes all issues in a repository with their related pull requests.

**Parameters:**
- `repository` (path) - Repository name in format `owner/repo`

**Response:**
```json
{
  "success": true,
  "message": "Synchronized 15 issues in repository microsoft/copilot-powerplatform",
  "synchronizedCount": 15,
  "repository": "microsoft/copilot-powerplatform"
}
```

**Example:**
```bash
curl -X POST "https://your-api/api/github/sync/all/microsoft/copilot-powerplatform"
```

### 4. Get Pull Requests

**GET** `/api/github/pullrequests/{repository}`

Retrieves pull requests for a repository with filtering options.

**Parameters:**
- `repository` (path) - Repository name in format `owner/repo`
- `state` (query, optional) - PR state filter: `all`, `open`, `closed` (default: `all`)
- `limit` (query, optional) - Maximum number of PRs to return (default: `100`)

**Response:**
```json
[
  {
    "nodeId": "PR_kwDOABCDE...",
    "number": 123,
    "title": "Fix authentication bug",
    "body": "This PR fixes #456",
    "url": "https://github.com/owner/repo/pull/123",
    "repository": "owner/repo",
    "author": "username",
    "state": "open",
    "isMerged": false,
    "createdAt": "2025-01-10T10:00:00Z",
    "updatedAt": "2025-01-11T10:00:00Z",
    "mergedAt": null,
    "closedAt": null,
    "headBranch": "feature-auth-fix",
    "baseBranch": "main",
    "labels": ["bug", "priority-high"],
    "assignees": ["developer1"],
    "linkedIssueNumbers": [456],
    "metadata": {}
  }
]
```

**Example:**
```bash
curl -X GET "https://your-api/api/github/pullrequests/microsoft/copilot-powerplatform?state=open&limit=50"
```

### 5. Get Single Pull Request

**GET** `/api/github/pullrequests/{repository}/{pullRequestNumber}`

Retrieves a specific pull request by number.

**Parameters:**
- `repository` (path) - Repository name in format `owner/repo`
- `pullRequestNumber` (path) - Pull request number

**Response:**
```json
{
  "nodeId": "PR_kwDOABCDE...",
  "number": 123,
  "title": "Fix authentication bug",
  "body": "This PR fixes #456",
  "url": "https://github.com/owner/repo/pull/123",
  "repository": "owner/repo",
  "author": "username",
  "state": "open",
  "isMerged": false,
  "createdAt": "2025-01-10T10:00:00Z",
  "updatedAt": "2025-01-11T10:00:00Z",
  "mergedAt": null,
  "closedAt": null,
  "headBranch": "feature-auth-fix",
  "baseBranch": "main",
  "labels": ["bug", "priority-high"],
  "assignees": ["developer1"],
  "linkedIssueNumbers": [456],
  "metadata": {}
}
```

**Example:**
```bash
curl -X GET "https://your-api/api/github/pullrequests/microsoft/copilot-powerplatform/123"
```

### 6. Get Pull Requests Linked to Issue

**GET** `/api/github/sync/linked-prs/{repository}/{issueNumber}`

Finds all pull requests that reference or are linked to a specific issue.

**Parameters:**
- `repository` (path) - Repository name in format `owner/repo`
- `issueNumber` (path) - Issue number to find linked PRs for

**Response:**
```json
[
  {
    "number": 123,
    "title": "Fix issue #456",
    "state": "open",
    "isMerged": false,
    "linkedIssueNumbers": [456]
  },
  {
    "number": 124,
    "title": "Additional fix for #456",
    "state": "closed",
    "isMerged": true,
    "linkedIssueNumbers": [456]
  }
]
```

**Example:**
```bash
curl -X GET "https://your-api/api/github/sync/linked-prs/microsoft/copilot-powerplatform/456"
```

### 7. Get Issues Linked to Pull Request

**GET** `/api/github/sync/linked-issues/{repository}/{pullRequestNumber}`

Finds all issues that are referenced or linked to a specific pull request.

**Parameters:**
- `repository` (path) - Repository name in format `owner/repo`
- `pullRequestNumber` (path) - Pull request number to find linked issues for

**Response:**
```json
[
  {
    "number": 456,
    "title": "Authentication not working",
    "state": "open",
    "author": "user1",
    "assignees": ["developer1"],
    "labels": ["bug", "priority-high"]
  }
]
```

**Example:**
```bash
curl -X GET "https://your-api/api/github/sync/linked-issues/microsoft/copilot-powerplatform/123"
```

## Issue Reference Patterns

The synchronization service recognizes these patterns in PR titles and descriptions:

- **Direct references:** `#123`
- **Fix keywords:** `fix #123`, `fixes #123`, `fixed #123`
- **Close keywords:** `close #123`, `closes #123`, `closed #123`  
- **Resolve keywords:** `resolve #123`, `resolves #123`, `resolved #123`

## Synchronization Status Values

- `"synchronized"` - Issue state matches expected state based on PRs
- `"needs_update"` - Issue state doesn't match expected state  
- `"conflict"` - Multiple PRs with conflicting states requiring manual review

## Error Responses

All endpoints return standard HTTP error responses:

**400 Bad Request:**
```json
{
  "error": "Invalid request parameters"
}
```

**500 Internal Server Error:**
```json
{
  "error": "Internal server error message"
}
```

## Authentication

All endpoints require valid GitHub App authentication. The service automatically handles authentication using the configured GitHub App credentials.

## Rate Limiting

The service includes built-in rate limiting protection with delays between API calls to respect GitHub's API limits.

## Logging and Auditing

All synchronization operations are logged and audited with detailed information including:
- Operation type and result
- Repository and resource identifiers
- Execution timestamps
- Error details (if any)

## Real-time Integration

The synchronization service is automatically triggered by GitHub webhooks for:
- Pull request state changes (opened, closed, merged)
- Issue references in PR descriptions
- Related issue updates

This ensures that synchronization happens in real-time without manual intervention.