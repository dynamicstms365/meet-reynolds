# Platform Specialist Agent

## Philosophy
> "I understand business needs like a consultant, code like a developer"

## Overview

The Platform Specialist is an autonomous agent that bridges business requirements with technical implementation across the Microsoft 365 and Power Platform ecosystem. It combines deep business process understanding with comprehensive technical expertise to deliver "Maximum Effort™" business solutions.

## Capabilities

### Core Expertise
- **Power Platform Development**: PowerApps, Power Automate, Power BI, Power Pages
- **Microsoft 365 Integration**: Teams, SharePoint, Outlook, OneDrive
- **SharePoint Management**: Site creation, list management, workflow automation
- **Teams App Development**: Custom apps, bots, messaging extensions
- **Microsoft Graph Operations**: User management, data access, automation
- **Dynamics 365 Integration**: Customization, workflow automation, data integration
- **Compliance Management**: Policy creation, governance, audit trails
- **Business Intelligence**: Analytics, reporting, dashboard creation

### Tool Integrations
- PAC CLI (Power Platform CLI)
- M365 CLI (Microsoft 365 CLI)
- PnP PowerShell
- Microsoft Graph PowerShell
- Azure CLI
- SharePoint REST APIs
- Power Platform APIs

## Architecture

### Agent Core
- **Main Process**: [`PlatformAgent.js`](./PlatformAgent.js)
- **Philosophy**: Business consultant mindset with developer execution
- **Communication**: MCP integration with Reynolds orchestrator
- **Monitoring**: Prometheus metrics and health checks

### Container Specifications
- **Base Image**: Node.js 18 Alpine with PowerShell Core
- **Security**: Non-root user, minimal attack surface
- **Tools**: Complete M365 and Power Platform toolchain
- **Volumes**: Workspace, context, memory, secrets, solutions mounting
- **Health Checks**: Comprehensive status monitoring

## API Endpoints

### Health & Status
- `GET /health` - Agent health and status
- `GET /metrics` - Prometheus metrics
- `GET /capabilities` - Agent capabilities and tools

### Task Execution
- `POST /execute` - Execute platform tasks
- `GET /` - Agent information and status

## Supported Tasks

### Power Platform Operations
- `powerapps-create` - Create PowerApps applications
- `powerapps-deploy` - Deploy PowerApps to environments
- `powerautomate-workflow` - Create Power Automate workflows
- `powerbi-dashboard` - Create Power BI dashboards and reports

### Microsoft 365 Integration
- `sharepoint-site-create` - Create SharePoint sites
- `sharepoint-list-manage` - Manage SharePoint lists and libraries
- `teams-app-deploy` - Deploy Microsoft Teams applications
- `graph-api-operation` - Execute Microsoft Graph API operations

### Dynamics 365 Operations
- `dynamics-customize` - Customize Dynamics 365 entities
- `dynamics-workflow` - Create Dynamics 365 workflows

### Compliance & Governance
- `compliance-policy` - Create compliance policies
- `governance-setup` - Setup governance frameworks

### Analytics & Business Intelligence
- `analytics-setup` - Configure analytics solutions
- `business-intelligence` - Create BI solutions

## Environment Variables

### Required
- `AZURE_CLIENT_ID` - Azure App Registration Client ID
- `AZURE_CLIENT_SECRET` - Azure App Registration Secret
- `AZURE_TENANT_ID` - Azure Tenant ID
- `M365_USERNAME` - M365 Service Account Username
- `M365_PASSWORD` - M365 Service Account Password

### Optional
- `PORT` - Agent port (default: 8080)
- `LOG_LEVEL` - Logging level (default: info)
- `REYNOLDS_MCP_ENDPOINT` - Reynolds orchestrator endpoint
- `AGENT_ID` - Unique agent identifier
- `POWER_PLATFORM_ENVIRONMENT` - Default Power Platform environment

## Volume Mounts

### Required Volumes
- `/app/workspace` - Project workspace files
- `/app/context` - Context and instruction files
- `/app/memory` - Persistent agent memory
- `/app/secrets` - Secure credential storage
- `/app/solutions` - Power Platform solution packages

### Mount Examples
```bash
docker run -v ./workspace:/app/workspace \
           -v ./context:/app/context \
           -v ./secrets:/app/secrets \
           -v ./solutions:/app/solutions \
           platform-specialist-agent
```

## Security

### Container Security
- Non-root user execution
- Minimal base image (Alpine)
- PowerShell execution policy restrictions
- Credential isolation in volumes

### Secret Management
- Environment variable injection
- Volume-mounted secrets
- Azure Key Vault integration
- Secure token handling

## Metrics & Monitoring

### Prometheus Metrics
- `platform_agent_tasks_total` - Total tasks processed
- `platform_agent_health` - Agent health status
- `http_request_duration_seconds` - Request latency

### Health Indicators
- Task processing status
- Reynolds connection status
- M365 connectivity status
- Power Platform environment health

## Reynolds Integration

### MCP Communication
- Bidirectional task coordination
- Status reporting and heartbeat
- Capability registration
- Context sharing

### Orchestration Features
- Business process automation
- Cross-platform integration
- Error handling and retry
- Solution deployment coordination

## Development

### Local Testing
```bash
npm install
npm run dev
```

### Docker Build
```bash
docker build -t platform-specialist-agent .
```

### Health Check
```bash
curl http://localhost:8080/health
```

## PowerShell Module Requirements

### Installed Modules
- `PnP.PowerShell` - SharePoint operations
- `Microsoft.Graph` - Graph API operations
- `ExchangeOnlineManagement` - Exchange operations
- `MicrosoftTeams` - Teams management

### CLI Tools
- `pac` - Power Platform CLI
- `m365` - Microsoft 365 CLI
- `az` - Azure CLI

## Examples

### Create PowerApp
```json
{
  "taskType": "powerapps-create",
  "payload": {
    "appName": "Employee Directory",
    "appType": "canvas",
    "dataSource": {
      "type": "sharepoint",
      "listUrl": "https://tenant.sharepoint.com/sites/hr/Lists/Employees"
    },
    "requirements": {
      "responsive": true,
      "offline": false
    }
  }
}
```

### Create SharePoint Site
```json
{
  "taskType": "sharepoint-site-create",
  "payload": {
    "siteName": "Project Collaboration",
    "template": "Team Site",
    "permissions": [
      {
        "group": "Project Team",
        "permission": "Contribute"
      }
    ],
    "features": ["Lists", "Libraries", "Calendar"]
  }
}
```

### Deploy Teams App
```json
{
  "taskType": "teams-app-deploy",
  "payload": {
    "appName": "Task Manager Bot",
    "appPackage": "./teams-app.zip",
    "targetScope": "organization",
    "permissions": [
      "chat:read",
      "user:read"
    ]
  }
}
```

### Create Power Automate Workflow
```json
{
  "taskType": "powerautomate-workflow",
  "payload": {
    "workflowName": "Approval Process",
    "trigger": {
      "type": "sharepoint",
      "event": "item_created"
    },
    "actions": [
      {
        "type": "approval",
        "approvers": ["manager@company.com"]
      },
      {
        "type": "email",
        "template": "approval_notification"
      }
    ],
    "connectors": ["sharepoint", "outlook", "approvals"]
  }
}
```

## Business Process Patterns

### Common Solutions
- **Document Management**: SharePoint + Power Automate + Teams
- **Approval Workflows**: Power Automate + Outlook + SharePoint
- **Data Collection**: PowerApps + SharePoint Lists + Power BI
- **Team Collaboration**: Teams + SharePoint + Planner
- **Customer Engagement**: Dynamics 365 + Power Platform + M365

### Integration Patterns
- **Canvas Apps**: SharePoint data + Power Automate workflows
- **Model-Driven Apps**: Dataverse + Business Process Flows
- **Teams Integration**: SPFx + Teams Toolkit + Graph API
- **Analytics**: Power BI + Dataverse + SharePoint

## Contributing

This agent is part of the Reynolds Orchestration System. For contributions:

1. Follow the agent development guidelines
2. Maintain the "consultant mindset, developer execution" philosophy
3. Ensure MCP protocol compliance
4. Add comprehensive business process testing
5. Update documentation with business scenarios

## Support

For issues and questions:
- Check Reynolds orchestrator logs
- Verify agent health endpoints
- Review Prometheus metrics
- Validate M365 and Power Platform connectivity
- Consult the autonomous orchestration documentation