# DevOps Polyglot Specialist Agent

## Philosophy
> "I know GitHub like a developer, but think like DevOps"

## Overview

The DevOps Polyglot Specialist is an autonomous agent that bridges the gap between development and operations. It combines deep GitHub knowledge with comprehensive DevOps expertise to deliver "Maximum Effort™" infrastructure and deployment solutions.

## Capabilities

### Core Expertise
- **GitHub Operations**: Workflow optimization, Actions development, security scanning
- **CI/CD Management**: Pipeline creation, optimization, and monitoring
- **Kubernetes Deployment**: Container orchestration, scaling, health monitoring
- **Docker Operations**: Image building, optimization, multi-stage builds
- **Infrastructure as Code**: Terraform planning and execution
- **Helm Charts**: Package management and deployment
- **Azure DevOps**: Integration and pipeline management
- **Jenkins Automation**: Pipeline automation and optimization

### Tool Integrations
- GitHub CLI (`gh`)
- Azure CLI (`az`)
- Docker CLI
- Kubernetes CLI (`kubectl`)
- Terraform
- Helm
- Jenkins CLI

## Architecture

### Agent Core
- **Main Process**: [`DevOpsAgent.js`](./DevOpsAgent.js)
- **Philosophy**: Developer mindset with DevOps execution
- **Communication**: MCP integration with Reynolds orchestrator
- **Monitoring**: Prometheus metrics and health checks

### Container Specifications
- **Base Image**: Node.js 18 Alpine
- **Security**: Non-root user, minimal attack surface
- **Tools**: Full DevOps toolchain pre-installed
- **Volumes**: Workspace, context, memory, secrets mounting
- **Health Checks**: Comprehensive status monitoring

## API Endpoints

### Health & Status
- `GET /health` - Agent health and status
- `GET /metrics` - Prometheus metrics
- `GET /capabilities` - Agent capabilities and tools

### Task Execution
- `POST /execute` - Execute DevOps tasks
- `GET /` - Agent information and status

## Supported Tasks

### GitHub Operations
- `github-workflow-deploy` - Deploy GitHub Actions workflows
- `github-actions-optimize` - Optimize existing workflows
- `github-security-scan` - Perform security scans

### CI/CD Management
- `cicd-pipeline-create` - Create new CI/CD pipelines
- `cicd-pipeline-optimize` - Optimize pipeline performance

### Kubernetes Operations
- `k8s-deploy` - Deploy applications to Kubernetes
- `k8s-scale` - Scale deployments
- `k8s-monitor` - Monitor cluster health

### Docker Operations
- `docker-build` - Build optimized container images
- `docker-optimize` - Optimize Dockerfiles

### Infrastructure Operations
- `terraform-plan` - Plan infrastructure changes
- `terraform-apply` - Apply infrastructure changes
- `helm-deploy` - Deploy Helm charts

## Environment Variables

### Required
- `GITHUB_TOKEN` - GitHub API token
- `AZURE_CLIENT_ID` - Azure service principal ID
- `AZURE_CLIENT_SECRET` - Azure service principal secret
- `AZURE_TENANT_ID` - Azure tenant ID

### Optional
- `PORT` - Agent port (default: 8080)
- `LOG_LEVEL` - Logging level (default: info)
- `REYNOLDS_MCP_ENDPOINT` - Reynolds orchestrator endpoint
- `AGENT_ID` - Unique agent identifier

## Volume Mounts

### Required Volumes
- `/app/workspace` - Project workspace files
- `/app/context` - Context and instruction files
- `/app/memory` - Persistent agent memory
- `/app/secrets` - Secure credential storage

### Mount Examples
```bash
docker run -v ./workspace:/app/workspace \
           -v ./context:/app/context \
           -v ./secrets:/app/secrets \
           devops-polyglot-agent
```

## Security

### Container Security
- Non-root user execution
- Minimal base image (Alpine)
- Security scanning integration
- Credential isolation in volumes

### Secret Management
- Environment variable injection
- Volume-mounted secrets
- Secure token handling
- Audit logging

## Metrics & Monitoring

### Prometheus Metrics
- `devops_agent_tasks_total` - Total tasks processed
- `devops_agent_health` - Agent health status
- `http_request_duration_seconds` - Request latency

### Health Indicators
- Task processing status
- Reynolds connection status
- Tool availability
- Memory and CPU usage

## Reynolds Integration

### MCP Communication
- Bidirectional task coordination
- Status reporting and heartbeat
- Capability registration
- Context sharing

### Orchestration Features
- Parallel task execution
- Loop prevention
- Error handling and retry
- Performance optimization

## Development

### Local Testing
```bash
npm install
npm run dev
```

### Docker Build
```bash
docker build -t devops-polyglot-agent .
```

### Health Check
```bash
curl http://localhost:8080/health
```

## Examples

### Deploy GitHub Workflow
```json
{
  "taskType": "github-workflow-deploy",
  "payload": {
    "repository": "org/repo",
    "workflow": {
      "name": "ci.yml",
      "content": "..."
    },
    "branch": "main"
  }
}
```

### Scale Kubernetes Deployment
```json
{
  "taskType": "k8s-scale",
  "payload": {
    "namespace": "production",
    "deployment": "web-app",
    "replicas": 5
  }
}
```

### Build Docker Image
```json
{
  "taskType": "docker-build",
  "payload": {
    "dockerfile": "./Dockerfile",
    "tag": "myapp:latest",
    "buildArgs": {
      "NODE_ENV": "production"
    }
  }
}
```

## Contributing

This agent is part of the Reynolds Orchestration System. For contributions:

1. Follow the agent development guidelines
2. Maintain the "developer mindset, DevOps execution" philosophy
3. Ensure MCP protocol compliance
4. Add comprehensive testing
5. Update documentation

## Support

For issues and questions:
- Check Reynolds orchestrator logs
- Verify agent health endpoints
- Review Prometheus metrics
- Consult the autonomous orchestration documentation