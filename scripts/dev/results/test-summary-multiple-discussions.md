# Discussion Monitoring Test: multiple-discussions

## Scenario Description
User asks about topic matching multiple discussions - should present options and help choose

## Input Context
- **Topic**: Azure deployment automation
- **User Intent**: troubleshooting CI/CD pipeline issues
- **Keywords**: Azure, deployment, CI/CD, automation, pipeline, workflow
- **Repository**: dynamicstms365/copilot-powerplatform
- **User**: devops-engineer
- **Expected Action**: present_multiple_options

## Search Results
```json
[{"title": "Azure Container Apps deployment best practices", "number": 28, "category": "DevOps", "created_at": "2024-11-15T14:20:00Z", "author": "azure-expert", "body": "Discussion about optimizing Azure Container Apps deployments, including resource management and scaling strategies...", "comments_count": 23, "participants_count": 12, "labels": ["azure", "container-apps", "deployment"], "state": "open"}, {"title": "GitHub Actions workflow troubleshooting guide", "number": 35, "category": "CI/CD", "created_at": "2024-11-20T09:45:00Z", "author": "workflow-guru", "body": "Common issues and solutions for GitHub Actions workflows, covering authentication, secrets management, and deployment patterns...", "comments_count": 18, "participants_count": 9, "labels": ["github-actions", "troubleshooting", "ci-cd"], "state": "open"}, {"title": "Azure CLI deployment errors and fixes", "number": 31, "category": "Troubleshooting", "created_at": "2024-11-18T16:30:00Z", "author": "cli-specialist", "body": "Collection of Azure CLI error scenarios and their solutions, particularly for automated deployments...", "comments_count": 31, "participants_count": 15, "labels": ["azure-cli", "errors", "deployment"], "state": "open"}]
```

## Conversation History
```
User: I'm having issues with my Azure deployment pipeline. The workflow keeps failing at the container app deployment step with authentication errors.

Context: User has a deployment issue that could relate to multiple existing discussions.
```

## Testing Notes
This scenario tests the MCP server's ability to:
1. Analyze conversational context
2. Search for relevant discussions
3. Provide appropriate recommendations
4. Maintain conversational flow

## Next Steps for MCP Integration
1. Implement `discussion_search` MCP tool
2. Add conversation context tracking
3. Create discussion creation workflow
4. Test with real GitHub API integration
