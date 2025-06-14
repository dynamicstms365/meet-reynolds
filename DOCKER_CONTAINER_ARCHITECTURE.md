# Docker Container Architecture - Issue #70 Implementation

## Overview

This document describes the secure Docker container architecture implementation for the Copilot PowerPlatform repository, featuring integrated Azure OpenAI services, comprehensive security hardening, and production-ready deployment configurations.

## Architecture Components

### 1. Main Application Container (`Dockerfile`)

**Security Enhancements:**
- Multi-stage build process for reduced attack surface
- Non-root user execution (UID/GID 1000)
- Security updates during build process
- Restricted capabilities and privileges
- Enhanced health checks with Azure OpenAI validation

**Azure OpenAI Integration:**
- Secure endpoint configuration
- API key management through secrets
- Rate limiting and retry mechanisms
- Content safety and filtering
- Comprehensive timeout handling

**Key Features:**
- Production-grade ASP.NET Core 8.0 runtime
- TLS 1.2+ enforcement
- Comprehensive logging and monitoring
- Resource optimization and limits

### 2. Orchestration System (`autonomous-orchestration/`)

**Reynolds Orchestrator:**
- Supernatural project management capabilities
- MCP (Model Context Protocol) server integration
- Agent pool management and coordination
- Loop prevention and intelligent task decomposition

**Agent Clusters:**
- **DevOps Agent**: Infrastructure, CI/CD, Azure integration
- **Platform Agent**: Microsoft 365, Power Platform specialist
- **Code Intelligence Agent**: Code analysis, security scanning

**Supporting Infrastructure:**
- PostgreSQL database with encryption
- Redis cache with TLS
- Prometheus monitoring
- Grafana dashboards

### 3. Azure Container Instances Deployment

**ARM Template Features:**
- System-assigned managed identity
- Security context enforcement
- Resource limits and requests
- Log Analytics integration
- DNS configuration with custom labels

**Security Hardening:**
- No-new-privileges security policy
- Read-only root filesystem options
- Resource constraints enforcement
- Network isolation policies

## Security Implementation

### Container Security

```dockerfile
# Non-root user execution
RUN groupadd -r -g 1000 copilot && useradd --no-log-init -r -g copilot -u 1000 copilot
USER copilot

# Security environment variables
ENV COMPlus_EnableDiagnostics=0
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
```

### Secrets Management

**File-based Secrets:**
- `azure_openai_endpoint.txt` - Azure OpenAI service endpoint
- `azure_openai_key.txt` - API authentication key  
- `github_token.txt` - GitHub integration token
- `encryption_key.txt` - Application-level encryption
- `db_password.txt` - Database authentication

**Security Features:**
- File permissions restricted to 600
- Secret rotation capabilities
- Version history management
- Access logging and audit trails

### Network Security

**Docker Compose Network:**
```yaml
networks:
  agent-network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
    labels:
      - "com.reynolds.network=agent-coordination"
```

**Azure Container Instances:**
- Public IP with DNS labeling
- Port restrictions (8080, 8443 only)
- TLS termination support
- Network policies enforcement

### Azure OpenAI Integration Security

**Configuration Parameters:**
```yaml
AZURE_OPENAI_RATE_LIMIT_ENABLED: true
AZURE_OPENAI_CONTENT_SAFETY_ENABLED: true
AZURE_OPENAI_REQUEST_VALIDATION_ENABLED: true
AZURE_OPENAI_INPUT_SANITIZATION_ENABLED: true
AZURE_OPENAI_OUTPUT_FILTERING_ENABLED: true
```

**Rate Limiting:**
- 100 requests per minute
- 150,000 tokens per minute
- 10 concurrent requests maximum
- Burst allowance of 20 requests

## Deployment Guide

### Prerequisites

1. **Required Tools:**
   ```bash
   # Install Docker and Docker Compose
   sudo apt-get update
   sudo apt-get install docker.io docker-compose
   
   # Install Azure CLI
   curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
   
   # Install jq for JSON processing
   sudo apt-get install jq
   ```

2. **Authentication:**
   ```bash
   # Azure CLI login
   az login
   
   # Docker registry authentication
   echo $REGISTRY_PASSWORD | docker login ghcr.io -u $REGISTRY_USERNAME --password-stdin
   ```

### Local Deployment

1. **Setup Secrets:**
   ```bash
   # Create secrets directory
   mkdir -p autonomous-orchestration/secrets
   
   # Create required secret files
   echo "your-azure-openai-endpoint" > autonomous-orchestration/secrets/azure_openai_endpoint.txt
   echo "your-azure-openai-key" > autonomous-orchestration/secrets/azure_openai_key.txt
   echo "your-github-token" > autonomous-orchestration/secrets/github_token.txt
   
   # Set proper permissions
   chmod 600 autonomous-orchestration/secrets/*
   ```

2. **Deploy with Script:**
   ```bash
   # Development deployment
   ./deploy-container-architecture.sh --local --mode development
   
   # Production deployment
   ./deploy-container-architecture.sh --local --mode production
   ```

3. **Verify Deployment:**
   ```bash
   # Check container status
   docker ps
   
   # Run health checks
   cd autonomous-orchestration && bash health-check.sh --verbose
   
   # Test endpoints
   curl http://localhost:8080/health
   curl http://localhost:8080/mcp/capabilities
   ```

### Azure Deployment

1. **Prepare Azure Resources:**
   ```bash
   # Create resource group
   az group create --name copilot-powerplatform-rg --location eastus
   
   # Create Log Analytics workspace (optional)
   az monitor log-analytics workspace create \
     --resource-group copilot-powerplatform-rg \
     --workspace-name copilot-powerplatform-logs
   ```

2. **Deploy to Azure:**
   ```bash
   ./deploy-container-architecture.sh \
     --azure \
     --azure-rg copilot-powerplatform-rg \
     --azure-location eastus \
     --image-tag latest
   ```

3. **Monitor Deployment:**
   ```bash
   # Check deployment status
   az deployment group show \
     --resource-group copilot-powerplatform-rg \
     --name copilot-powerplatform-deployment-latest
   
   # Get container logs
   az container logs \
     --resource-group copilot-powerplatform-rg \
     --name copilot-powerplatform
   ```

## Configuration Reference

### Environment Variables

**Core Application:**
- `ASPNETCORE_ENVIRONMENT`: Runtime environment (Production/Development)
- `ASPNETCORE_URLS`: Binding URLs (http://+:8080)
- `LOGGING_LEVEL`: Log verbosity (Information/Debug)

**Azure OpenAI:**
- `AZURE_OPENAI_ENDPOINT`: Service endpoint URL
- `AZURE_OPENAI_API_KEY`: Authentication key
- `AZURE_OPENAI_DEPLOYMENT_NAME`: Model deployment name (gpt-4)
- `AZURE_OPENAI_API_VERSION`: API version (2024-02-15-preview)
- `AZURE_OPENAI_MAX_TOKENS`: Maximum tokens per request (4096)
- `AZURE_OPENAI_TEMPERATURE`: Response creativity (0.7)

**Security:**
- `SECURITY_HEADERS_ENABLED`: Enable security headers (true)
- `RATE_LIMITING_ENABLED`: Enable rate limiting (true)
- `CORS_ENABLED`: Enable CORS (true)
- `ENCRYPTION_AT_REST_ENABLED`: Enable data encryption (true)

### Resource Limits

**Container Resources:**
```yaml
resources:
  requests:
    cpu: 1.0
    memoryInGb: 2.0
  limits:
    cpu: 2.0
    memoryInGb: 4.0
```

**Database Configuration:**
- PostgreSQL 15 with encryption
- Connection pooling (20 connections)
- Query timeout (60 seconds)
- Backup retention (7 days)

## Monitoring and Observability

### Health Checks

**Application Health:**
- Endpoint: `http://localhost:8080/health`
- Interval: 30 seconds
- Timeout: 15 seconds
- Retries: 5

**Component Health:**
- Database connectivity
- Redis cache availability
- Azure OpenAI service status
- External API accessibility

### Metrics Collection

**Prometheus Metrics:**
- HTTP request metrics
- Azure OpenAI API usage
- Container resource utilization
- Security event counters
- Error rates and latencies

**Grafana Dashboards:**
- System overview dashboard
- Azure OpenAI integration metrics
- Security monitoring dashboard
- Performance analytics

### Logging

**Structured Logging:**
- JSON format for machine parsing
- Correlation IDs for request tracking
- Security event logging
- Performance metrics logging

**Log Aggregation:**
- Azure Log Analytics integration
- Centralized log collection
- Log retention policies
- Alert configuration

## Security Compliance

### Standards Compliance

- **SOC 2 Type II**: Security controls framework
- **GDPR**: Data privacy and protection
- **Azure Security Baseline**: Cloud security standards
- **OWASP**: Web application security guidelines

### Security Controls

1. **Authentication & Authorization:**
   - JWT token-based authentication
   - Role-based access control
   - API key management
   - Session management

2. **Data Protection:**
   - Encryption at rest (AES-256)
   - Encryption in transit (TLS 1.2+)
   - Key rotation policies
   - Data classification

3. **Network Security:**
   - Network segmentation
   - Firewall rules
   - DDoS protection
   - VPN access controls

4. **Monitoring & Auditing:**
   - Security event logging
   - Intrusion detection
   - Vulnerability scanning
   - Compliance reporting

## Troubleshooting

### Common Issues

1. **Container Won't Start:**
   ```bash
   # Check container logs
   docker logs <container-id>
   
   # Verify secrets availability
   ls -la autonomous-orchestration/secrets/
   
   # Check resource availability
   docker system df
   ```

2. **Azure OpenAI Connection Failed:**
   ```bash
   # Test endpoint connectivity
   curl -H "api-key: $AZURE_OPENAI_API_KEY" "$AZURE_OPENAI_ENDPOINT/openai/deployments?api-version=2024-02-15-preview"
   
   # Check rate limiting
   docker exec <container> curl http://localhost:8080/metrics | grep azure_openai
   ```

3. **Health Check Failures:**
   ```bash
   # Run detailed health check
   cd autonomous-orchestration
   bash health-check.sh --verbose --export --format text
   
   # Check individual components
   curl http://localhost:8080/health
   curl http://localhost:9090/-/healthy
   curl http://localhost:3000/api/health
   ```

### Support and Maintenance

**Regular Maintenance:**
- Weekly security updates
- Monthly secret rotation
- Quarterly security assessments
- Annual architecture reviews

**Monitoring Alerts:**
- High error rates (>5%)
- Resource exhaustion (>90%)
- Security events
- Service unavailability

**Backup and Recovery:**
- Database backups (daily)
- Configuration backups
- Disaster recovery procedures
- Business continuity planning

## Conclusion

This Docker container architecture provides a secure, scalable, and production-ready foundation for the Copilot PowerPlatform project with integrated Azure OpenAI services. The implementation follows security best practices, includes comprehensive monitoring, and supports both local development and cloud deployment scenarios.

For additional support or questions, refer to the project documentation or contact the development team.