# 🚀 Enterprise MCP SDK Package Setup

This guide shows you how to consume the enterprise-enhanced MCP SDK packages with immediate access to fixes and improvements while the official PR process runs in parallel.

## 🎯 Quick Start

### 1. Authentication Setup

First, configure GitHub Package Registry authentication:

```bash
# Add your GitHub token (needs read:packages scope)
dotnet nuget add source https://nuget.pkg.github.com/cege7480/index.json \
  --name github-cege7480 \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_TOKEN
```

### 2. Project Configuration

Copy the [`enterprise-nuget.config`](./enterprise-nuget.config) to your project root:

```bash
# Copy to your project directory
cp enterprise-nuget.config /path/to/your/project/nuget.config
```

### 3. Package References

Add enterprise packages to your `.csproj`:

```xml
<PackageReference Include="ModelContextProtocol" Version="*-enterprise.*" />
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="*-enterprise.*" />
<PackageReference Include="ModelContextProtocol.Core" Version="*-enterprise.*" />
```

## 📦 Available Enterprise Packages

### Core Packages
- **ModelContextProtocol** - Main package with enterprise DI fixes
- **ModelContextProtocol.AspNetCore** - HTTP-based MCP servers with enterprise auth
- **ModelContextProtocol.Core** - Low-level APIs with enterprise enhancements

### Enterprise Enhancements Included

✅ **Issue #365 Fix**: HttpContextAccessor DI lifetime resolution  
✅ **Issue #520 Fix**: Enterprise authentication patterns  
✅ **Issue #409 Fix**: Hosted server connection improvements  
✅ **Complete Documentation**: Enterprise implementation guides  

## 🔄 Automated Publishing

The enterprise packages are automatically published when:
- ✅ Push to `feature/issue-365-httpcontextaccessor-fix` branch
- ✅ Manual workflow dispatch
- ✅ Daily scheduled builds (08:00 UTC)

Version format: `{base-version}-enterprise.{build-number}`

## 🛠️ Usage Examples

### Basic MCP Server with Enterprise Auth

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// Enterprise-enhanced DI registration
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .AddHttpContextAccessor(); // ✅ Fixed lifetime scope

await builder.Build().RunAsync();
```

### Enterprise Authentication Setup

```csharp
// Enterprise authentication patterns (Issue #520 fix)
builder.Services.AddAuthentication("Enterprise")
    .AddScheme<EnterpriseAuthenticationSchemeOptions, EnterpriseAuthenticationHandler>(
        "Enterprise", options => { });
```

## 🔍 Monitoring Package Updates

Check for new enterprise packages:

```bash
# List available versions
dotnet package search ModelContextProtocol --source github-cege7480

# Update to latest enterprise build
dotnet add package ModelContextProtocol --version "*-enterprise.*"
```

## ⚡ CI/CD Integration

For automated builds, use the enterprise packages in your pipeline:

```yaml
# Example GitHub Actions step
- name: Add enterprise package source
  run: |
    dotnet nuget add source https://nuget.pkg.github.com/cege7480/index.json \
      --name github-cege7480 \
      --username ${{ github.actor }} \
      --password ${{ secrets.GITHUB_TOKEN }}

- name: Restore with enterprise packages
  run: dotnet restore
```

## 📋 Migration Path

1. **Immediate**: Use enterprise packages for development/testing
2. **Short-term**: Continue using enterprise packages as fixes are validated
3. **Long-term**: Migrate to official packages once PRs are merged and released

## 🆘 Troubleshooting

### Authentication Issues
```bash
# Clear NuGet cache if needed
dotnet nuget locals all --clear

# Re-authenticate
dotnet nuget add source https://nuget.pkg.github.com/cege7480/index.json \
  --name github-cege7480 \
  --username YOUR_USERNAME \
  --password YOUR_TOKEN
```

### Version Conflicts
```bash
# Check package sources
dotnet nuget list source

# Force package source
dotnet add package ModelContextProtocol --source github-cege7480
```

## 🎉 Benefits

✅ **Immediate Access**: Get fixes without waiting for official release  
✅ **Parallel Development**: Continue development while PR process proceeds  
✅ **Enterprise Ready**: Battle-tested enterprise authentication patterns  
✅ **Automated Updates**: Daily builds ensure latest improvements  
✅ **Easy Migration**: Seamless transition to official packages later  

---

*For questions or issues, check the [enterprise solutions documentation](./MCP_ENTERPRISE_SOLUTIONS_SUMMARY.md) or create an issue.*