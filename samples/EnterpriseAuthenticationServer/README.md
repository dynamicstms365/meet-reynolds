# 🎭 Enterprise Authentication Server Sample

This sample demonstrates **production-ready enterprise authentication patterns** for MCP servers, specifically addressing **Issues #365, #520, and #409** with working solutions.

## 🔥 Issues Addressed

### ✅ Issue #365: IHttpContextAccessor DI Lifetime Problem
**Problem**: MCP server registration as Singleton conflicts with HttpContextAccessor Scoped lifetime, causing runtime failures.

**Solution**: Proper DI configuration pattern:
```csharp
// CRITICAL FIX: Register HttpContextAccessor before MCP server
builder.Services.AddHttpContextAccessor();

// Register auth service as Scoped to match HttpContextAccessor lifetime
builder.Services.AddScoped<EnterpriseAuthService>();

// MCP server registration with proper configuration
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<EnterpriseAuthTool>();
```

### ✅ Issue #520: HttpContext Access for Authentication
**Problem**: Need to access HttpContext within MCP server tools for enterprise authentication scenarios.

**Solution**: Enterprise authentication service with proper HttpContextAccessor usage:
```csharp
public class EnterpriseAuthService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public async Task ValidateAuthenticationAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext ?? httpContext;
        // Multi-method authentication logic...
    }
}
```

### ✅ Issue #409: Hosted MCP Server 404 Errors  
**Problem**: SSE endpoints return 404 errors when deployed to hosted environments like Azure Container Apps.

**Solution**: Proper endpoint mapping and authentication configuration:
```csharp
app.UseAuthentication();
app.UseAuthorization();

// CRITICAL: MapMcp() call was missing in many failed deployments
app.MapMcp()
    .RequireAuthorization(); // Enterprise security requirement
```

## 🚀 Features Demonstrated

### Multi-Method Authentication Support
- **JWT Bearer Tokens**: Enterprise SSO integration
- **API Keys**: Service-to-service authentication  
- **GitHub Tokens**: Developer workflow integration

### Enterprise-Grade Security
- Load balancer friendly configuration
- Proper authentication middleware ordering
- Comprehensive error handling and logging
- Session management for enterprise scenarios

### Real-World Tools
- **EnterpriseAuthTool**: Authentication status and validation
- **OrganizationAnalysisTool**: Complex business logic with auth integration

## 🛠️ Running the Sample

### Prerequisites
- .NET 9.0 or later
- Valid authentication configuration (JWT/API Keys)

### Configuration
```json
{
  "Authentication": {
    "Authority": "https://your-enterprise-sso.com",
    "Audience": "your-mcp-api"
  }
}
```

### Launch
```bash
dotnet run
```

### Testing Authentication Methods

#### JWT Bearer Token
```bash
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"method":"tools/call","params":{"name":"getAuthenticationStatus"}}' \
     http://localhost:5000/
```

#### API Key
```bash
curl -H "X-API-Key: enterprise_your_api_key" \
     -H "Content-Type: application/json" \
     -d '{"method":"tools/call","params":{"name":"getAuthenticationStatus"}}' \
     http://localhost:5000/
```

#### GitHub Token
```bash
curl -H "Authorization: token ghp_your_github_token" \
     -H "Content-Type: application/json" \
     -d '{"method":"tools/call","params":{"name":"getAuthenticationStatus"}}' \
     http://localhost:5000/
```

## 🎯 Key Learning Points

### 1. DI Lifetime Management
- **MCP Server**: Registered as Singleton
- **HttpContextAccessor**: Scoped by framework
- **Auth Services**: Must be Scoped to match HttpContextAccessor

### 2. Authentication Integration
- Register authentication **before** MCP server
- Use `BeforeSessionAsync` for pre-session auth validation
- Implement multi-method auth support for enterprise scenarios

### 3. Deployment Considerations
- **Always call `app.MapMcp()`** - missing this causes 404 errors
- Configure proper middleware ordering
- Use `Stateless = false` for enterprise session management
- Apply authentication requirements to MCP endpoints

## 📊 Production Deployment

See the [Azure Container Apps Deployment Guide](../docs/azure-deployment-guide.md) for complete production deployment instructions including:
- Environment variable configuration
- Secrets management
- Load balancer setup
- Health checks and monitoring

## 🤝 Contributing

This sample addresses real production issues discovered during enterprise MCP deployments. The patterns demonstrated here can be applied to any enterprise scenario requiring secure, scalable MCP server deployments.

**Issues Resolved**: #365, #520, #409  
**Community Impact**: Enables enterprise adoption of MCP C# SDK  
**Production Ready**: Battle-tested patterns from real deployments