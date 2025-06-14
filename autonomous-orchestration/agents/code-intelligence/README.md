# Code Intelligence Specialist Agent

## Philosophy
> "I code in your language, follow your patterns"

## Overview

The Code Intelligence Specialist is an autonomous agent that provides comprehensive multi-language development support and code analysis. It combines deep programming language expertise with advanced static analysis to deliver "Maximum Effort™" code quality, security, and performance optimization.

## Capabilities

### Core Expertise
- **Multi-Language Support**: 20+ programming languages with native tooling
- **Static Code Analysis**: Deep code quality assessment and pattern detection
- **Security Scanning**: Vulnerability detection and security best practices
- **Performance Analysis**: Bottleneck identification and optimization suggestions
- **Testing Automation**: Test generation, execution, and coverage analysis
- **Code Formatting**: Language-specific formatting and style enforcement
- **Dependency Management**: Vulnerability scanning and update recommendations
- **Refactoring Assistance**: Intelligent code improvement suggestions
- **Documentation Generation**: Automated documentation from code analysis
- **Build Optimization**: Build process analysis and improvement

### Supported Languages
- **Frontend**: JavaScript, TypeScript, HTML, CSS, SCSS, Less
- **Backend**: Python, Java, C#, Go, Rust, PHP, Ruby, Node.js
- **Mobile**: Swift, Kotlin, Dart (Flutter)
- **Functional**: Scala, R, Haskell
- **Data**: SQL, YAML, JSON, XML
- **Infrastructure**: Dockerfile, Terraform, Kubernetes YAML

### Tool Integrations
- **Linters**: ESLint, Pylint, Golangci-lint, RuboCop, Clippy
- **Formatters**: Prettier, Black, gofmt, rustfmt
- **Test Frameworks**: Jest, PyTest, JUnit, Go test, Cargo test
- **Build Tools**: Webpack, Vite, Maven, Gradle, Go build, Cargo
- **Security**: Snyk, Bandit, Gosec, npm audit, OWASP tools
- **Quality**: SonarQube, CodeClimate integration

## Architecture

### Agent Core
- **Main Process**: [`CodeAgent.js`](./CodeAgent.js)
- **Philosophy**: Language-native analysis with pattern recognition
- **Communication**: MCP integration with Reynolds orchestrator
- **Language Servers**: Multi-language LSP support for real-time analysis

### Container Specifications
- **Base Image**: Node.js 18 Alpine with multi-language toolchain
- **Security**: Non-root user, isolated execution environments
- **Tools**: Complete development toolchain for all supported languages
- **Volumes**: Workspace, context, memory, secrets, analysis results
- **Language Servers**: Active LSP servers for real-time code intelligence

## API Endpoints

### Health & Status
- `GET /health` - Agent health and language server status
- `GET /metrics` - Prometheus metrics and code quality scores
- `GET /capabilities` - Agent capabilities and supported languages
- `GET /language-servers` - Active language server status

### Task Execution
- `POST /execute` - Execute code intelligence tasks
- `POST /analyze` - Comprehensive code analysis endpoint
- `GET /` - Agent information and status

### Code Analysis
- `POST /analyze` - Full project analysis with quality metrics
- `POST /execute` - Specific task execution (lint, format, test, etc.)

## Supported Tasks

### Static Analysis
- `static-analysis` - Comprehensive code analysis across languages
- `code-quality-check` - Quality scoring and improvement suggestions
- `security-scan` - Security vulnerability detection and remediation

### Code Formatting & Standards
- `format-code` - Language-specific code formatting
- `lint-code` - Style guide enforcement and error detection
- `style-check` - Coding standards compliance verification

### Testing & Quality Assurance
- `run-tests` - Execute test suites with framework detection
- `test-coverage` - Generate comprehensive coverage reports
- `generate-tests` - AI-assisted test case generation

### Build & Performance
- `build-project` - Multi-language build process execution
- `optimize-build` - Build performance optimization
- `bundle-analyze` - Bundle size analysis and optimization
- `performance-analyze` - Runtime performance bottleneck detection

### Dependencies & Security
- `dependency-check` - Dependency analysis and vulnerability scanning
- `vulnerability-scan` - Security vulnerability assessment
- `update-dependencies` - Safe dependency update recommendations

### Code Intelligence
- `refactor-suggest` - Intelligent refactoring recommendations
- `documentation-generate` - Automated documentation generation

## Environment Variables

### Required
- `NODE_ENV` - Environment (production/development)
- `PORT` - Agent port (default: 8080)
- `AGENT_ID` - Unique agent identifier

### Optional
- `LOG_LEVEL` - Logging level (default: info)
- `REYNOLDS_MCP_ENDPOINT` - Reynolds orchestrator endpoint
- `SUPPORTED_LANGUAGES` - Override default language support
- `ANALYSIS_TIMEOUT` - Analysis timeout in seconds (default: 300)

### Tool-Specific
- `SONAR_TOKEN` - SonarQube authentication token
- `SNYK_TOKEN` - Snyk API token for security scanning
- `GITHUB_TOKEN` - GitHub token for dependency scanning

## Volume Mounts

### Required Volumes
- `/app/workspace` - Project source code and files
- `/app/context` - Analysis context and configuration
- `/app/memory` - Persistent agent learning and patterns
- `/app/secrets` - Secure credential storage
- `/app/analysis` - Analysis results and reports

### Mount Examples
```bash
docker run -v ./project:/app/workspace \
           -v ./context:/app/context \
           -v ./secrets:/app/secrets \
           -v ./analysis:/app/analysis \
           code-intelligence-agent
```

## Security

### Container Security
- Non-root user execution (codeagent:1001)
- Minimal Alpine base with security updates
- Isolated language execution environments
- Secure credential handling via volume mounts

### Code Analysis Security
- Static analysis without code execution
- Secure dependency scanning
- Vulnerability database integration
- Safe refactoring suggestions

## Metrics & Monitoring

### Prometheus Metrics
- `code_agent_tasks_total` - Total tasks processed by language and status
- `code_quality_score` - Real-time code quality scores by project/language
- `code_agent_health` - Agent health status
- `http_request_duration_seconds` - Request latency metrics

### Code Quality Metrics
- Quality scores (0-100) per language and project
- Security vulnerability counts and severity
- Test coverage percentages
- Build performance metrics

### Health Indicators
- Active language server count
- Task processing status
- Reynolds connection status
- Analysis tool availability

## Reynolds Integration

### MCP Communication
- Bidirectional task coordination with language context
- Real-time status reporting and heartbeat
- Capability registration with language support matrix
- Analysis result sharing and context preservation

### Orchestration Features
- Parallel multi-language analysis
- Cross-project pattern recognition
- Intelligent task routing based on language detection
- Performance optimization coordination

## Language-Specific Features

### JavaScript/TypeScript
- **Tools**: ESLint, Prettier, TypeScript compiler, Jest
- **Analysis**: Bundle analysis, dependency scanning, performance profiling
- **Frameworks**: React, Vue, Angular, Node.js specific optimizations

### Python
- **Tools**: Pylint, Black, MyPy, Bandit, PyTest
- **Analysis**: Import optimization, security scanning, performance profiling
- **Frameworks**: Django, Flask, FastAPI specific patterns

### Java
- **Tools**: Checkstyle, PMD, SpotBugs, JUnit
- **Analysis**: Memory analysis, dependency management, build optimization
- **Frameworks**: Spring, Maven, Gradle integration

### Go
- **Tools**: golangci-lint, gofmt, go vet, gosec
- **Analysis**: Concurrency analysis, performance optimization
- **Features**: Module management, build optimization

### C#
- **Tools**: Roslyn analyzers, dotnet format, NUnit
- **Analysis**: .NET Framework/Core compatibility, performance analysis
- **Features**: NuGet management, MSBuild optimization

### Rust
- **Tools**: Clippy, rustfmt, Cargo test
- **Analysis**: Memory safety verification, performance optimization
- **Features**: Cargo dependency management, cross-compilation

## Development

### Local Testing
```bash
npm install
npm run dev
```

### Docker Build
```bash
docker build -t code-intelligence-agent .
```

### Health Check
```bash
curl http://localhost:8080/health
```

### Language Server Testing
```bash
curl http://localhost:8080/language-servers
```

## Analysis Examples

### Full Project Analysis
```json
{
  "projectPath": "/app/workspace/my-project",
  "language": "javascript",
  "analysisType": "full"
}
```

### Security Scan
```json
{
  "taskType": "security-scan",
  "payload": {
    "projectPath": "/app/workspace/my-project",
    "language": "python",
    "scanType": "comprehensive"
  }
}
```

### Code Formatting
```json
{
  "taskType": "format-code",
  "payload": {
    "projectPath": "/app/workspace/my-project",
    "language": "typescript",
    "formatter": "prettier"
  }
}
```

### Test Execution
```json
{
  "taskType": "run-tests",
  "payload": {
    "projectPath": "/app/workspace/my-project",
    "language": "javascript",
    "testFramework": "jest",
    "coverage": true
  }
}
```

### Dependency Analysis
```json
{
  "taskType": "dependency-check",
  "payload": {
    "projectPath": "/app/workspace/my-project",
    "language": "python",
    "checkUpdates": true
  }
}
```

## Multi-Language Project Support

### Automatic Detection
- Language detection from file extensions and structure
- Framework identification and optimization
- Build tool detection and integration
- Dependency manager recognition

### Cross-Language Analysis
- Polyglot project support (e.g., Python backend + React frontend)
- Cross-language dependency tracking
- Unified security scanning across languages
- Coordinated build optimization

## Performance Optimization

### Analysis Optimization
- Parallel language server utilization
- Incremental analysis for large codebases
- Intelligent caching of analysis results
- Resource-aware task scheduling

### Memory Management
- Language server lifecycle management
- Analysis result streaming for large projects
- Garbage collection optimization
- Resource monitoring and alerting

## Contributing

This agent is part of the Reynolds Orchestration System. For contributions:

1. Follow the agent development guidelines
2. Maintain the "language-native analysis" philosophy
3. Ensure MCP protocol compliance
4. Add comprehensive multi-language testing
5. Update language support documentation
6. Test with real-world projects in each language

## Language Support Roadmap

### Current Support (v1.0)
- Full support for 20+ languages
- Native tooling integration
- Real-time analysis capabilities

### Planned Additions (v1.1)
- **Languages**: Dart, Elixir, Clojure, F#
- **Features**: AI-powered code suggestions, automated refactoring
- **Integration**: IDE plugins, CI/CD pipeline integration

## Support

For issues and questions:
- Check Reynolds orchestrator logs
- Verify agent health and language server endpoints
- Review Prometheus metrics for performance insights
- Validate language-specific tool availability
- Test with sample projects in each language
- Consult the autonomous orchestration documentation

## Troubleshooting

### Common Issues
1. **Language Server Startup**: Check container resources and tool availability
2. **Analysis Timeout**: Adjust `ANALYSIS_TIMEOUT` for large projects
3. **Memory Issues**: Monitor container memory usage during analysis
4. **Tool Missing**: Verify Dockerfile includes required language tools

### Debug Commands
```bash
# Check language server status
curl http://localhost:8080/language-servers

# Monitor real-time metrics
curl http://localhost:8080/metrics

# Test specific language analysis
curl -X POST http://localhost:8080/analyze \
  -H "Content-Type: application/json" \
  -d '{"projectPath":"/test","language":"javascript"}'