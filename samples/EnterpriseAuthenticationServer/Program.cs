using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using EnterpriseAuthenticationServer.Services;
using EnterpriseAuthenticationServer.Tools;

var builder = WebApplication.CreateBuilder(args);

// === CRITICAL FIX FOR ISSUE #365: HttpContextAccessor DI Lifetime ===
// This addresses the DI lifetime mismatch between MCP server (Singleton) and HttpContextAccessor (Scoped)
builder.Services.AddHttpContextAccessor();

// Configure authentication BEFORE MCP server registration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
    });

builder.Services.AddAuthorization();

// === CRITICAL FIX FOR ISSUE #520: Enterprise Authentication Service ===
// Register EnterpriseAuthService as Scoped to match HttpContextAccessor lifetime
builder.Services.AddScoped<EnterpriseAuthService>();

// === OFFICIAL MCP SDK PATTERN (Not custom extensions) ===
// This is the pattern that works vs custom AddReynoldsMcpServer() approach
builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        // Configure for enterprise scenarios
        options.Stateless = false; // Enable session management for enterprise auth
        options.BeforeSessionAsync = async (context, cancellationToken) =>
        {
            // Enterprise authentication check before session creation
            await context.HttpContext.RequestServices
                .GetRequiredService<EnterpriseAuthService>()
                .ValidateAuthenticationAsync(context.HttpContext, cancellationToken);
        };
    })
    .WithTools<EnterpriseAuthTool>()
    .WithTools<OrganizationAnalysisTool>();

var app = builder.Build();

// Authentication middleware MUST come before authorization
app.UseAuthentication();
app.UseAuthorization();

// === CRITICAL FIX FOR ISSUE #409: Missing MapMcp() Call ===
// This was the root cause of 404 errors on SSE endpoints
app.MapMcp()
    .RequireAuthorization(); // Require authentication for all MCP endpoints

app.Run();