# GitHub App Registration & Setup Guide

This document provides comprehensive instructions for setting up the GitHub App that serves as the foundation for the Copilot Power Platform ecosystem.

## Overview

The GitHub App integration is the **root of the binary tree architecture** and enables:
- Real-time webhook event processing
- Secure GitHub API authentication using installation tokens
- Repository access, issue management, and PR automation
- Security audit logging for all GitHub operations

## Prerequisites

- GitHub Enterprise Cloud organization (`dynamicstms365`)
- Administrator access to the organization
- Access to organization secrets for storing credentials

## GitHub App Registration

### 1. Create the GitHub App

1. Navigate to your GitHub organization settings: `https://github.com/organizations/dynamicstms365/settings/apps`
2. Click **"New GitHub App"**
3. Fill out the application details:

```yaml
App Name: "Power Platform Copilot Agent"
Description: "Specialized agent for Microsoft Power Platform development and automation"
Homepage URL: "https://github.com/dynamicstms365/copilot-powerplatform"
Webhook URL: "https://your-domain.com/api/github/webhook"
Webhook secret: [Generate a secure random string]
```

### 2. Configure Permissions

Set the following **Repository permissions**:

```yaml
Actions: Write
Contents: Read
Issues: Write
Metadata: Read
Pull requests: Write
Secrets: Read
```

Set the following **Organization permissions**:

```yaml
Members: Read
```

### 3. Subscribe to Events

Enable the following webhook events:

```yaml
- Repository dispatch
- Installation
- Installation repositories
- Push
- Pull request
- Issues
```

### 4. Installation Settings

- **Where can this GitHub App be installed?**: Only on this account

## Security Configuration

### 1. Generate Private Key

1. After creating the app, scroll to **"Private keys"** section
2. Click **"Generate a private key"**
3. Download the `.pem` file and store it securely

### 2. Install the App

1. Go to **"Install App"** in the left sidebar
2. Click **"Install"** next to your organization
3. Choose **"All repositories"** or select specific repositories
4. Note the **Installation ID** from the URL after installation

### 3. Store Credentials as Organization Secrets

Add the following secrets to your GitHub organization:

| Secret Name | Description | Example |
|------------|-------------|---------|
| `NGL_DEVOPS_APP_ID` | The App ID from the GitHub App settings | `123456` |
| `NGL_DEVOPS_INSTALLATION_ID` | Installation ID from the install URL | `789012` |
| `NGL_DEVOPS_PRIVATE_KEY` | Contents of the downloaded `.pem` file | `-----BEGIN PRIVATE KEY-----\n...` |

**Important**: The private key should include the full PEM format including headers and line breaks.

## Service Configuration

### 1. GitHub Actions Integration (Recommended)

For optimal security and integration, use GitHub Actions with organization secrets and variables:

#### Organization Variables
- `NGL_DEVOPS_APP_ID`: The GitHub App ID (can be public)

#### Organization Secrets  
- `NGL_DEVOPS_PRIVATE_KEY`: The GitHub App private key (PEM format)

#### GitHub Actions Workflow Example
```yaml
steps:
  - name: Generate GitHub App Token
    id: generate_token
    uses: actions/create-github-app-token@v1
    with:
      app-id: ${{ vars.NGL_DEVOPS_APP_ID }}
      private-key: ${{ secrets.NGL_DEVOPS_PRIVATE_KEY }}
      
  - name: Use GitHub App Token
    env:
      GITHUB_TOKEN: ${{ steps.generate_token.outputs.token }}
    run: |
      # The generated token automatically handles installation context
      # and provides scoped access to the organization repositories
```

### 2. Alternative Configuration Methods

#### Configuration File (appsettings.json):
```json
{
  "NGL_DEVOPS_APP_ID": "your-app-id",
  "NGL_DEVOPS_INSTALLATION_ID": "your-installation-id", 
  "NGL_DEVOPS_PRIVATE_KEY": "your-private-key-pem"
}
```

#### Environment Variables:
```bash
export NGL_DEVOPS_APP_ID="your-app-id"
export NGL_DEVOPS_INSTALLATION_ID="your-installation-id"
export NGL_DEVOPS_PRIVATE_KEY="your-private-key-pem"
```

> **Note**: When using GitHub Actions with `actions/create-github-app-token@v1`, the installation ID is automatically resolved and you don't need to set `NGL_DEVOPS_INSTALLATION_ID`.

### 2. Service Registration

The services are automatically registered in `Program.cs`:

```csharp
// Register GitHub integration services
builder.Services.AddHttpClient<IGitHubAppAuthService, GitHubAppAuthService>();
builder.Services.AddScoped<ISecurityAuditService, SecurityAuditService>();
```

### 3. Azure Container Deployment (Recommended for Production)

For production orchestration and scalability, deploy the service using Azure Container Instances or Azure Container Environment:

#### Azure Container Instance Deployment
```yaml
# azure-container-deployment.yml
name: Deploy GitHub App Service

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Deploy Container Instance
        uses: azure/container-instances-deploy@v1
        with:
          resource-group: 'copilot-powerplatform-deploy-rg'
          name: 'github-copilot-bot'
          image: 'ghcr.io/dynamicstms365/copilot-powerplatform/copilot-agent:latest'
          registry-server: 'ghcr.io'
          registry-username: ${{ github.actor }}
          registry-password: ${{ secrets.GITHUB_TOKEN }}
          environment-variables: |
            NGL_DEVOPS_APP_ID=${{ vars.NGL_DEVOPS_APP_ID }}
          secure-environment-variables: |
            NGL_DEVOPS_PRIVATE_KEY=${{ secrets.NGL_DEVOPS_PRIVATE_KEY }}
          ports: '8080'
          location: 'eastus'
```

> **⚠️ Container Registry Authentication**
> 
> When deploying to Azure Container Apps, ensure proper authentication to GitHub Container Registry:
> 
> 1. **GitHub Token Permissions**: The `GITHUB_TOKEN` must have `packages:read` permission
> 2. **Registry Credentials**: Use `--registry-password-secret` for enhanced security
> 3. **Container Naming**: Ensure consistent naming (`github-copilot-bot`) across all configurations
> 4. **Pre-deployment Testing**: Run `scripts/validation/test-registry-auth.sh` to validate authentication
> 
> **Common Authentication Issues:**
> - `DENIED: denied` errors indicate insufficient registry permissions
> - Container name mismatches can cause deployment target confusion
> - GitHub token expiration or insufficient scopes prevent image pulling
```

#### Azure Container Environment
For more complex orchestration with multiple containers:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: github-app-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: github-app-service
  template:
    metadata:
      labels:
        app: github-app-service
    spec:
      containers:
      - name: copilot-agent
        image: your-registry/copilot-agent:latest
        ports:
        - containerPort: 80
        env:
        - name: NGL_DEVOPS_APP_ID
          valueFrom:
            configMapKeyRef:
              name: github-config
              key: app-id
        - name: NGL_DEVOPS_PRIVATE_KEY
          valueFrom:
            secretKeyRef:
              name: github-secrets
              key: private-key
```

> **Benefits of Container Deployment:**
> - **Auto-scaling**: Handle webhook spikes automatically
> - **High Availability**: Multiple instances for redundancy  
> - **Security**: Isolated environment with Azure RBAC
> - **Cost Optimization**: Pay-per-use scaling
> - **Easy Management**: Azure portal integration

## API Endpoints

### Troubleshooting Container Deployment

#### Common Issues and Solutions

**1. Registry Authentication Failures (`DENIED: denied`)**

**Problem**: Azure Container Apps cannot pull image from GitHub Container Registry
```
ERROR: Failed to provision revision for container app 'github-copilot-bot'. 
Field 'template.containers.github-copilot-bot.image' is invalid: 
'DENIED: denied' when accessing ghcr.io
```

**Solutions**:
- Verify `GITHUB_TOKEN` has `packages:read` permission
- Ensure the image exists: `ghcr.io/dynamicstms365/copilot-powerplatform/copilot-agent:latest`
- Test authentication locally: `scripts/validation/test-registry-auth.sh`
- Check container app registry credentials are properly configured

**2. Container Name Mismatches**

**Problem**: Deployment targets wrong container or fails to find existing resources

**Solutions**:
- Use consistent naming: `github-copilot-bot` across all configurations
- Update ARM templates, YAML configs, and deployment scripts
- Verify workflow environment variables match container names

**3. Image Pull Timeouts**

**Problem**: Container Apps timeout when pulling large images

**Solutions**:
- Use multi-stage Docker builds to reduce image size
- Pre-validate image accessibility before deployment
- Consider using Azure Container Registry for faster pulls

**4. Authentication Token Expiration**

**Problem**: GitHub tokens expire or lose permissions

**Solutions**:
- Use GitHub App tokens with longer expiration
- Rotate secrets regularly in GitHub Actions
- Monitor token expiration dates

#### Validation Commands

```bash
# Test registry authentication
./scripts/validation/test-registry-auth.sh

# Check Azure Container App status
az containerapp show --name github-copilot-bot --resource-group copilot-powerplatform-deploy-rg

# View container app logs
az containerapp logs show --name github-copilot-bot --resource-group copilot-powerplatform-deploy-rg

# Test image pull manually
docker pull ghcr.io/dynamicstms365/copilot-powerplatform/copilot-agent:latest
```

### Webhook Endpoint

**POST** `/api/github/webhook`

Processes GitHub webhook events in real-time.

**Request Headers:**
- `X-GitHub-Event`: Event type (e.g., "push", "pull_request")
- `X-GitHub-Delivery`: Unique delivery ID
- `X-Hub-Signature-256`: Webhook signature (for validation)

**Request Body:**
```json
{
  "action": "opened",
  "event": "pull_request",
  "repository": {
    "id": 123456,
    "name": "copilot-powerplatform",
    "full_name": "dynamicstms365/copilot-powerplatform",
    "private": false
  },
  "installation": {
    "id": 789012
  },
  "sender": {
    "login": "username",
    "type": "User"
  }
}
```

### Connectivity Testing

**GET** `/api/github/test`

Tests GitHub App connectivity and returns installation details.

**Response:**
```json
{
  "success": true,
  "installationId": "789012",
  "repositories": [
    "dynamicstms365/copilot-powerplatform",
    "dynamicstms365/other-repo"
  ],
  "permissions": [
    "contents:read",
    "issues:write",
    "actions:write"
  ],
  "tokenExpiresAt": "2024-01-11T15:30:00Z"
}
```

### Health Check

**GET** `/api/github/health`

Returns service health status.

### Installation Information

**GET** `/api/github/installation-info`

Returns current installation and token information.

## Validation & Testing

### 1. Manual Testing

Test the GitHub App setup:

```bash
# Test connectivity
curl -X GET https://your-domain.com/api/github/test

# Check health
curl -X GET https://your-domain.com/api/github/health

# Get installation info
curl -X GET https://your-domain.com/api/github/installation-info
```

### 2. Webhook Testing

1. Use the GitHub App settings to **redeliver** recent webhook events
2. Monitor application logs for webhook processing
3. Check the **Recent Deliveries** section for delivery status

### 3. Automated Testing

Run the test suite to validate functionality:

```bash
dotnet test src/CopilotAgent.Tests/CopilotAgent.Tests.csproj
```

## Security Features

### 1. Audit Logging

All GitHub operations are logged with structured logging:

```
SECURITY_AUDIT: GitHub_Webhook_Received | User: username | Repo: org/repo | Action: opened | Result: SUCCESS
```

### 2. Token Management

- Installation tokens are cached and automatically refreshed
- Tokens expire after 1 hour and are renewed as needed
- JWT tokens for app authentication expire after 10 minutes

### 3. Permission Validation

- Only required permissions are requested
- Tokens include scope validation
- All API calls are audited

## Troubleshooting

### Common Issues

**1. "GitHub App credentials not configured"**
- Verify all three secrets are set: APP_ID, INSTALLATION_ID, PRIVATE_KEY
- Check that the private key includes proper PEM headers

**2. "Failed to get installation access token"**
- Verify the App ID and Installation ID are correct
- Check that the private key format is valid (PKCS#8 or PKCS#1)
- Ensure the app is installed on the organization

**3. "Webhook delivery failed"**
- Check that the webhook URL is accessible from GitHub
- Verify the endpoint is handling POST requests
- Check application logs for processing errors

### Debugging

Enable detailed logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "CopilotAgent.Services.GitHubAppAuthService": "Debug",
      "CopilotAgent.Controllers.GitHubController": "Debug"
    }
  }
}
```

### Health Checks

Monitor the following metrics:

- **Webhook delivery success rate**: Should be >99%
- **Authentication success rate**: Should be 100%
- **Token refresh frequency**: Should be < 1 hour
- **API rate limit usage**: Should be < 80% of quota

## Production Deployment

### 1. Environment Setup

For production deployment:

1. Use **Azure Key Vault** or similar for secret management
2. Configure **Application Insights** for monitoring
3. Set up **Log Analytics** for security audit trail
4. Enable **health checks** for monitoring

### 2. Scaling Considerations

- Use **Redis** for token caching in multi-instance deployments
- Implement **webhook signature validation** for security
- Set up **rate limiting** for API endpoints
- Configure **load balancing** for high availability

### 3. Monitoring

Set up alerts for:
- Failed webhook deliveries
- Authentication failures
- High API usage
- Security audit anomalies

## Integration with Power Platform

The GitHub App serves as the foundation for:

1. **Automated environment creation** triggered by repository events
2. **CI/CD workflows** for Power Platform solutions
3. **Issue-to-code automation** using Copilot agents
4. **Security compliance** and audit trails

## Next Steps

After completing the GitHub App setup:

1. **Configure webhook endpoints** in your hosting environment
2. **Test connectivity** using the provided endpoints
3. **Set up monitoring** and alerting
4. **Implement additional webhook handlers** for specific Power Platform workflows
5. **Document organization-specific procedures** for team members

---

**Last Updated**: January 11, 2025  
**Setup Time**: < 15 minutes for new environments  
**Success Criteria**: >99% webhook delivery, 100% authentication success  
**Architecture Role**: Root of binary tree - foundational component