using Microsoft.OpenApi.Models;
using System.Reflection;

namespace CopilotAgent.Configuration;

/// <summary>
/// OpenAPI configuration optimized for Azure APIM MCP integration
/// </summary>
public static class OpenApiConfiguration
{
    public static void AddSwaggerGenWithMcpSupport(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Reynolds Communication & Orchestration API",
                Version = "v1.0",
                Description = @"
**Comprehensive API for bidirectional communication and organizational orchestration**

This API provides enterprise-grade communication capabilities designed for Azure APIM MCP (Model Context Protocol) integration. 
Built with Maximum Effort™ for parallel execution and supernatural coordination efficiency.

## Key Features

### 🎭 Communication Orchestration
- **Bidirectional Messaging**: Send and receive messages with multiple delivery methods
- **Command Parsing**: Intelligent parsing of structured commands like 'tell [user] to [action]'
- **User Resolution**: Flexible user identification via email, username, or display name
- **Delivery Optimization**: Automatic method selection for optimal message delivery

### 🚀 Integration Capabilities
- **Azure APIM Ready**: Fully compatible with Azure API Management MCP preview
- **Teams Integration**: Native Microsoft Teams messaging and chat creation
- **M365 CLI Support**: Command-line interface integration for advanced operations
- **Graph API Integration**: Direct Microsoft Graph API communication

### 📊 Monitoring & Telemetry
- **Health Monitoring**: Comprehensive service health checks and status reporting
- **Metrics Tracking**: Detailed performance and usage metrics
- **Audit Logging**: Complete audit trail for all communication activities
- **Error Handling**: Robust error handling with detailed error responses

### 🛡️ Security & Compliance
- **Enterprise Authentication**: Full Azure AD integration with role-based access
- **Audit Trails**: Complete security audit logging for compliance
- **Rate Limiting**: Built-in rate limiting and throttling support
- **Secure Communication**: End-to-end secure message delivery

## Usage Examples

### Basic Message Sending
```json
POST /api/communication/send-message
{
  ""userIdentifier"": ""christaylor@nextgeneration.com"",
  ""message"": ""Hello! This is a test message from Reynolds."",
  ""preferredMethod"": ""Auto""
}
```

### Command-Based Communication
```json
POST /api/communication/send-message
{
  ""userIdentifier"": ""chris taylor"",
  ""message"": ""Please respond with: Communication test successful"",
  ""preferredMethod"": ""DirectMessage""
}
```

### Status Checking
```json
GET /api/communication/status/christaylor@nextgeneration.com
```

## Azure APIM MCP Configuration

This API is designed to work seamlessly with Azure APIM's MCP preview feature. Key integration points:

- **Standardized Error Responses**: Consistent error format across all endpoints
- **Comprehensive Documentation**: Full OpenAPI 3.0 specification with examples
- **Health Check Endpoints**: Built-in health monitoring for APIM integration
- **Flexible Authentication**: Support for multiple authentication methods

## Reynolds Persona Integration

All communication is enhanced with Reynolds' supernatural charm and organizational coordination capabilities:
- **Maximum Effort™ Approach**: Parallel execution and optimization
- **Diplomatic Precision**: Professional yet engaging communication style
- **Organizational Intelligence**: Context-aware messaging and coordination
",
                Contact = new OpenApiContact
                {
                    Name = "Reynolds Coordination Team",
                    Email = "reynolds@nextgeneration.com",
                    Url = new Uri("https://github.com/dynamicstms365/copilot-powerplatform")
                },
                License = new OpenApiLicense
                {
                    Name = "Enterprise License",
                    Url = new Uri("https://github.com/dynamicstms365/copilot-powerplatform/blob/main/LICENSE")
                }
            });

            // Add comprehensive security definitions for Azure APIM
            options.AddSecurityDefinition("AzureAD", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    Implicit = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/authorize"),
                        TokenUrl = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            ["https://graph.microsoft.com/.default"] = "Access Microsoft Graph API",
                            ["api://reynolds-communication/Communication.ReadWrite"] = "Read and write communication data",
                            ["api://reynolds-communication/Communication.Admin"] = "Administrative access to communication services"
                        }
                    }
                },
                Description = "Azure Active Directory OAuth2 authentication"
            });

            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-API-Key",
                Description = "API Key for service-to-service authentication"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Bearer token authentication"
            });

            // Global security requirement
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "AzureAD"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Add XML comments for detailed documentation
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Add shared models documentation
            var sharedXmlPath = Path.Combine(AppContext.BaseDirectory, "Shared.xml");
            if (File.Exists(sharedXmlPath))
            {
                options.IncludeXmlComments(sharedXmlPath);
            }

            // Custom schema filters for better MCP integration
            options.SchemaFilter<McpSchemaFilter>();
            options.OperationFilter<McpOperationFilter>();
            options.DocumentFilter<McpDocumentFilter>();

            // Add examples for better API documentation
            options.EnableAnnotations();
            
            // Custom operation ordering for better readability
            options.OrderActionsBy(apiDesc => 
                $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}_{apiDesc.RelativePath}");
        });
    }
}

/// <summary>
/// Schema filter for MCP-specific enhancements
/// </summary>
public class McpSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Add MCP-specific schema enhancements
        if (context.Type == typeof(SendMessageRequest))
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["userIdentifier"] = new Microsoft.OpenApi.Any.OpenApiString("christaylor@nextgeneration.com"),
                ["message"] = new Microsoft.OpenApi.Any.OpenApiString("Please respond with: Test successful"),
                ["preferredMethod"] = new Microsoft.OpenApi.Any.OpenApiString("Auto"),
                ["context"] = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["source"] = new Microsoft.OpenApi.Any.OpenApiString("MCP"),
                    ["requestId"] = new Microsoft.OpenApi.Any.OpenApiString("req-123456")
                }
            };
        }
    }
}

/// <summary>
/// Operation filter for MCP-specific enhancements
/// </summary>
public class McpOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add MCP-specific headers
        operation.Parameters ??= new List<OpenApiParameter>();
        
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-MCP-Session-ID",
            In = ParameterLocation.Header,
            Required = false,
            Schema = new OpenApiSchema { Type = "string" },
            Description = "MCP session identifier for request correlation"
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Request-ID",
            In = ParameterLocation.Header,
            Required = false,
            Schema = new OpenApiSchema { Type = "string" },
            Description = "Unique request identifier for tracking and debugging"
        });

        // Add common response headers
        foreach (var response in operation.Responses.Values)
        {
            response.Headers ??= new Dictionary<string, OpenApiHeader>();
            
            response.Headers["X-Response-Time"] = new OpenApiHeader
            {
                Schema = new OpenApiSchema { Type = "string" },
                Description = "Response processing time in milliseconds"
            };

            response.Headers["X-Rate-Limit-Remaining"] = new OpenApiHeader
            {
                Schema = new OpenApiSchema { Type = "integer" },
                Description = "Number of requests remaining in current rate limit window"
            };
        }
    }
}

/// <summary>
/// Document filter for MCP-specific enhancements
/// </summary>
public class McpDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Add MCP-specific extensions
        swaggerDoc.Extensions.Add("x-mcp-compatible", new Microsoft.OpenApi.Any.OpenApiBoolean(true));
        swaggerDoc.Extensions.Add("x-mcp-version", new Microsoft.OpenApi.Any.OpenApiString("1.0"));
        swaggerDoc.Extensions.Add("x-reynolds-persona", new Microsoft.OpenApi.Any.OpenApiString("enabled"));
        
        // Add server information for different environments
        swaggerDoc.Servers = new List<OpenApiServer>
        {
            new OpenApiServer
            {
                Url = "https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io",
                Description = "Production Environment"
            },
            new OpenApiServer
            {
                Url = "https://localhost:7071",
                Description = "Development Environment"
            }
        };

        // Add tags for better organization
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new OpenApiTag
            {
                Name = "Communication",
                Description = "Bidirectional communication and messaging endpoints"
            },
            new OpenApiTag
            {
                Name = "Agent",
                Description = "Core agent processing and coordination"
            },
            new OpenApiTag
            {
                Name = "GitHub",
                Description = "GitHub integration and webhook processing"
            },
            new OpenApiTag
            {
                Name = "Health",
                Description = "Health monitoring and status endpoints"
            },
            new OpenApiTag
            {
                Name = "Reynolds",
                Description = "Reynolds persona and testing endpoints"
            }
        };
    }
}