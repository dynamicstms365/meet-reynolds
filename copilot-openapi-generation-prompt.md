# GitHub Copilot: Generate Reynolds Communication & Orchestration API OpenAPI 3.0 Specification

## Prompt for GitHub Copilot AI Generation

**Context**: Generate a comprehensive OpenAPI 3.0 specification for the Reynolds Communication & Orchestration API based on the existing GitHub Copilot Bot Enhanced capabilities and the Azure APIM deployment requirements.

**Task**: Create a complete OpenAPI 3.0 YAML specification that includes:

### 1. API Information
- Title: "Reynolds Communication & Orchestration API"
- Version: "1.0.0"
- Description: Enterprise-grade communication and orchestration API with Maximum Effort™
- Contact: reynolds@nextgeneration.com
- License: MIT

### 2. Server Configurations
- Production: https://reynolds-apim-prod.azure-api.net/reynolds
- Development: https://reynolds-apim-dev.azure-api.net/reynolds
- Direct Container App: https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io

### 3. Authentication Schemes
- Azure AD OAuth2 with scopes: user.read, user.readwrite, team.readwrite
- API Key authentication via Ocp-Apim-Subscription-Key header

### 4. Core Endpoint Categories

#### A. Communication Endpoints
Based on Teams integration requirements:
- POST /api/communication/send-message
- POST /api/communication/send-batch  
- GET /api/communication/status/{userIdentifier}
- GET /api/communication/message-history/{userIdentifier}
- GET /api/communication/health

#### B. GitHub Integration Endpoints
Based on existing GitHub Copilot Bot Enhanced capabilities:
- GET /api/github/repositories
- GET /api/github/repositories/{owner}/{repo}/issues
- POST /api/github/repositories/{owner}/{repo}/issues
- GET /api/github/repositories/{owner}/{repo}/discussions
- POST /api/github/repositories/{owner}/{repo}/discussions
- POST /api/github/search (semantic search)
- GET /api/github/organization/analytics
- GET /api/github/organization/{organization}/discussions
- GET /api/github/organization/{organization}/issues

#### C. Orchestration Endpoints
For cross-platform coordination:
- POST /api/orchestration/coordinate-stakeholders
- POST /api/orchestration/cross-repo-coordination
- GET /api/orchestration/project-health
- GET /api/orchestration/dependency-analysis

#### D. Monitoring Endpoints
For system health and performance:
- GET /api/monitoring/system-status
- GET /api/monitoring/metrics
- GET /api/monitoring/alerts
- POST /api/monitoring/alerts

### 5. Required Data Schemas

#### Communication Schemas
- SendMessageRequest (userIdentifier, message, preferredMethod, priority, metadata)
- BatchMessageRequest (messages[], coordinationStrategy, batchMetadata)
- MessageResponse (messageId, status, deliveryMethod, timestamp, recipientInfo)
- UserCommunicationStatus (userIdentifier, status, preferences, statistics)

#### GitHub Integration Schemas  
- Repository (id, name, fullName, description, coordinationStatus)
- Issue (id, number, title, body, state, coordinationMetadata)
- Discussion (id, number, title, body, category, user)
- CreateIssueRequest (title, body, labels, assignees, coordinationOptions)
- OrganizationAnalytics (summary, healthMetrics, trends, recommendations)

#### Orchestration Schemas
- StakeholderCoordinationRequest (coordinationType, stakeholderGroups, priorityLevel)
- CrossRepoCoordinationRequest (orchestrationType, targetRepositories, coordinationScope)
- ProjectHealthAssessment (overallScore, dimensions, trends, predictions)
- DependencyAnalysis (summary, dependencyTypes, riskAssessment, recommendations)

#### Monitoring Schemas
- HealthStatus (status, timestamp, services, dependencies)
- SystemStatus (overallStatus, components, incidents, performance)
- PerformanceMetrics (timeRange, metrics, summary)
- Alert (id, title, severity, status, category, createdAt)

### 6. Requirements
- All endpoints must include proper HTTP status codes (200, 400, 401, 404, 500)
- Comprehensive error handling with ErrorResponse schema
- Pagination support where applicable (PaginationInfo schema)
- Examples for all request/response schemas
- Proper validation rules (maxLength, minimum, maximum, enum values)
- Tags for logical grouping of endpoints
- External documentation references

### 7. Enterprise Features
- Reynolds coordination metadata in responses
- Batch processing capabilities with parallel execution strategies
- Comprehensive monitoring and alerting
- Cross-platform stakeholder coordination
- Advanced analytics and health assessment
- Dependency tracking and risk assessment

### 8. Azure APIM Integration Features
- Proper server configurations for APIM endpoints
- Security schemes compatible with APIM authentication
- Rate limiting awareness (handled at APIM layer)
- Subscription key support for service-to-service authentication

**Generate the complete OpenAPI 3.0 YAML specification following these requirements with Maximum Effort™ applied to enterprise-grade API design.**