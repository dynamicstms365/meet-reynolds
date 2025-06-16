# 🎭 Reynolds Azure APIM MCP Integration Deployment Guide

**Maximum Effort™ deployment strategy for enterprise-grade communication orchestration**

## Overview

This guide orchestrates the deployment of Reynolds Communication & Orchestration API through Azure API Management (APIM) with MCP preview integration. This approach provides enterprise-grade API management while leveraging Azure's native MCP capabilities.

## Architecture Overview

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────┐
│   AI/MCP Clients │────│  Azure APIM      │────│  Container App      │
│                 │    │  (MCP Preview)   │    │  Reynolds API       │
│ - Claude        │    │                  │    │                     │
│ - OpenAI        │    │ - Authentication │    │ - Communication     │
│ - Custom Tools  │    │ - Rate Limiting  │    │ - Teams Integration │
│                 │    │ - Monitoring     │    │ - GitHub Webhooks   │
└─────────────────┘    └──────────────────┘    └─────────────────────┘
```

## Prerequisites

### 1. Azure Resources
- Azure subscription with APIM capability
- Container Apps environment (already deployed)
- Azure Active Directory tenant
- Application Insights instance
- Event Hub namespace (optional, for advanced telemetry)

### 2. Environment Variables
```bash
# Required for APIM deployment
export AZURE_AD_TENANT_ID="your-tenant-id"
export AZURE_AD_CLIENT_ID="your-client-id"
export APPLICATION_INSIGHTS_INSTRUMENTATION_KEY="your-instrumentation-key"

# Optional for enhanced monitoring
export EVENT_HUB_CONNECTION_STRING="your-event-hub-connection"
export TF_STATE_STORAGE_ACCOUNT="your-terraform-state-storage"
```

### 3. Permissions
- Azure APIM Contributor role
- Azure AD Application Administrator
- Container Apps Contributor

## Deployment Steps

### Phase 1: Container App Verification

1. **Verify Current Deployment**
   ```bash
   # Check if our enhanced API is deployed
   curl -X GET "https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io/api/communication/health"
   ```

2. **Verify OpenAPI Documentation**
   ```bash
   # Access enhanced Swagger documentation
   curl -X GET "https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io/api-docs/v1/swagger.json"
   ```

### Phase 2: Azure APIM Deployment

1. **Deploy APIM Instance**
   ```bash
   # Using Azure CLI
   az apim create \
     --resource-group "reynolds-rg" \
     --name "reynolds-apim-${ENVIRONMENT}" \
     --location "East US" \
     --publisher-name "Reynolds Organization" \
     --publisher-email "reynolds@nextgeneration.com" \
     --sku-name "Developer" \
     --enable-managed-identity
   ```

2. **Import API Definition**
   ```bash
   # Import OpenAPI specification
   az apim api import \
     --resource-group "reynolds-rg" \
     --service-name "reynolds-apim-${ENVIRONMENT}" \
     --api-id "reynolds-communication-api" \
     --path "reynolds" \
     --specification-url "https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io/api-docs/v1/swagger.json" \
     --specification-format "OpenApi"
   ```

3. **Configure MCP Integration**
   ```bash
   # Apply MCP configuration (requires APIM MCP preview)
   az apim api update \
     --resource-group "reynolds-rg" \
     --service-name "reynolds-apim-${ENVIRONMENT}" \
     --api-id "reynolds-communication-api" \
     --set mcpIntegration.enabled=true
   ```

### Phase 3: Security Configuration

1. **Azure AD Integration**
   ```bash
   # Configure OAuth2 validation
   az apim policy apply \
     --resource-group "reynolds-rg" \
     --service-name "reynolds-apim-${ENVIRONMENT}" \
     --api-id "reynolds-communication-api" \
     --policy-file "azure-apim-policies.xml"
   ```

2. **API Key Setup**
   ```bash
   # Create service-to-service API key
   az apim subscription create \
     --resource-group "reynolds-rg" \
     --service-name "reynolds-apim-${ENVIRONMENT}" \
     --subscription-id "reynolds-service-subscription" \
     --display-name "Reynolds Service Key" \
     --scope "/apis/reynolds-communication-api"
   ```

### Phase 4: Testing & Validation

1. **Health Check Verification**
   ```bash
   # Test through APIM endpoint
   curl -X GET \
     "https://reynolds-apim-${ENVIRONMENT}.azure-api.net/reynolds/api/communication/health" \
     -H "Ocp-Apim-Subscription-Key: ${APIM_SUBSCRIPTION_KEY}"
   ```

2. **Communication Test**
   ```bash
   # Test Chris Taylor communication
   curl -X POST \
     "https://reynolds-apim-${ENVIRONMENT}.azure-api.net/reynolds/api/communication/send-message" \
     -H "Authorization: Bearer ${ACCESS_TOKEN}" \
     -H "Content-Type: application/json" \
     -H "Ocp-Apim-Subscription-Key: ${APIM_SUBSCRIPTION_KEY}" \
     -d '{
       "userIdentifier": "christaylor@nextgeneration.com",
       "message": "Test message from Reynolds APIM MCP integration",
       "preferredMethod": "Auto"
     }'
   ```

## Chris Taylor Communication Testing

### 1. Direct API Testing

```bash
# Test 1: Basic message delivery
curl -X POST \
  "https://reynolds-apim-prod.azure-api.net/reynolds/api/communication/send-message" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userIdentifier": "christaylor@nextgeneration.com",
    "message": "Hello Chris! This is a test of our APIM MCP integration system.",
    "preferredMethod": "Auto"
  }'

# Test 2: Command-based communication
curl -X POST \
  "https://reynolds-apim-prod.azure-api.net/reynolds/api/communication/send-message" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userIdentifier": "chris taylor",
    "message": "Please respond with: APIM MCP integration test successful",
    "preferredMethod": "DirectMessage"
  }'

# Test 3: Status checking
curl -X GET \
  "https://reynolds-apim-prod.azure-api.net/reynolds/api/communication/status/christaylor@nextgeneration.com" \
  -H "Authorization: Bearer $ACCESS_TOKEN"
```

### 2. MCP Client Testing

Once APIM MCP integration is configured, you can test with MCP clients:

```python
# Example using MCP client library
import mcp_client

client = mcp_client.Client("https://reynolds-apim-prod.azure-api.net/mcp")

# Send message via MCP protocol
result = await client.call_tool("send_message", {
    "userIdentifier": "christaylor@nextgeneration.com",
    "message": "MCP protocol test message",
    "preferredMethod": "Auto"
})

# Check communication status
status = await client.call_tool("get_communication_status", {
    "userIdentifier": "christaylor@nextgeneration.com"
})
```

## Monitoring & Troubleshooting

### 1. APIM Analytics
- Monitor API calls and performance in Azure portal
- Check APIM Analytics dashboard for usage patterns
- Review error rates and response times

### 2. Container App Logs
```bash
# Stream logs from Container App
az containerapp logs show \
  --resource-group "reynolds-rg" \
  --name "github-copilot-bot" \
  --follow
```

### 3. Application Insights
- Query telemetry data for detailed insights
- Set up alerts for communication failures
- Monitor user engagement metrics

### 4. Common Issues

#### Issue: 401 Unauthorized
**Solution**: Verify Azure AD token and APIM subscription key

#### Issue: 404 Not Found
**Solution**: Check API import status and routing configuration

#### Issue: 500 Internal Server Error
**Solution**: Review Container App logs and backend service health

#### Issue: MCP Integration Not Working
**Solution**: Verify APIM MCP preview feature is enabled and configured

## Security Considerations

### 1. Authentication
- Use Azure AD OAuth2 for user authentication
- Implement API keys for service-to-service communication
- Regular token rotation and key management

### 2. Rate Limiting
- Configure appropriate rate limits based on usage patterns
- Implement per-user and global rate limiting
- Monitor for abuse and adjust limits as needed

### 3. Data Protection
- Ensure message content is handled securely
- Implement audit logging for compliance
- Regular security reviews and updates

## Maintenance & Updates

### 1. API Versioning
- Use semantic versioning for API updates
- Maintain backward compatibility
- Gradual rollout of new features

### 2. Performance Optimization
- Regular performance reviews
- Cache optimization for frequently accessed data
- Database query optimization

### 3. Monitoring & Alerting
- Set up comprehensive monitoring
- Configure alerts for critical issues
- Regular health checks and status reports

## Rollback Procedures

### 1. APIM Configuration Rollback
```bash
# Restore previous API configuration
az apim api restore \
  --resource-group "reynolds-rg" \
  --service-name "reynolds-apim-${ENVIRONMENT}" \
  --api-id "reynolds-communication-api" \
  --backup-name "previous-config"
```

### 2. Container App Rollback
```bash
# Rollback to previous Container App revision
az containerapp revision set-mode \
  --resource-group "reynolds-rg" \
  --name "github-copilot-bot" \
  --mode "single" \
  --revision-name "previous-revision"
```

## Success Metrics

### 1. Communication Success Rate
- Target: >99% message delivery success
- Measurement: Monitor delivery confirmations and error rates

### 2. Response Time
- Target: <500ms API response time
- Measurement: APIM analytics and Application Insights

### 3. User Engagement
- Target: Successful bidirectional communication
- Measurement: Message exchange tracking and user feedback

## Next Steps

1. **Enhanced MCP Features**: Explore advanced MCP capabilities as they become available
2. **Multi-Channel Integration**: Extend to Slack, Discord, and other platforms
3. **AI-Powered Routing**: Implement intelligent message routing based on content analysis
4. **Advanced Analytics**: Implement comprehensive communication analytics and insights

---

**Reynolds Note**: This deployment guide represents Maximum Effort™ in enterprise API orchestration. The combination of robust OpenAPI design with Azure APIM MCP integration provides unprecedented flexibility and scalability for organizational communication coordination.

*Just Reynolds - Maximum Effort™ • Minimal Drama*