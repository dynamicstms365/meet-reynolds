using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace CopilotAgent.Configuration;

/// <summary>
/// Reynolds .NET 9.0 OpenAPI configuration with Maximum Effort™ MCP integration
/// </summary>
public static class Net9OpenApiConfiguration
{
    public static void AddReynoldsOpenApiWithMcpSupport(this IServiceCollection services)
    {
        // Reynolds: .NET 9.0 built-in OpenAPI with supernatural MCP enhancements
        services.AddOpenApi("v1", options =>
        {
            // .NET 9.0 uses OpenAPI 3.0 by default
            
            // Configure OpenAPI document
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                // Set basic API information
                document.Info = new OpenApiInfo
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
                };

                // Add server information for APIM integration
                document.Servers = new List<OpenApiServer>
                {
                    new OpenApiServer
                    {
                        Url = "https://ngl-apim.azure-api.net/reynolds",
                        Description = "Production APIM Environment"
                    },
                    new OpenApiServer
                    {
                        Url = "https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io",
                        Description = "Direct Container App Environment"
                    },
                    new OpenApiServer
                    {
                        Url = "https://localhost:7071",
                        Description = "Development Environment"
                    }
                };

                // Add comprehensive security schemes
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
                {
                    ["AzureAD"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Flows = new OpenApiOAuthFlows
                        {
                            Implicit = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = new Uri("https://login.microsoftonline.com/2518be7e-c933-4905-af64-24ad0157202f/oauth2/v2.0/authorize"),
                                TokenUrl = new Uri("https://login.microsoftonline.com/2518be7e-c933-4905-af64-24ad0157202f/oauth2/v2.0/token"),
                                Scopes = new Dictionary<string, string>
                                {
                                    ["https://graph.microsoft.com/.default"] = "Access Microsoft Graph API",
                                    ["api://reynolds-communication/Communication.ReadWrite"] = "Read and write communication data",
                                    ["api://reynolds-communication/Communication.Admin"] = "Administrative access to communication services"
                                }
                            }
                        },
                        Description = "Azure Active Directory OAuth2 authentication for Next Generation Logistics"
                    },
                    ["ApiKey"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Header,
                        Name = "Ocp-Apim-Subscription-Key",
                        Description = "APIM subscription key for service-to-service authentication"
                    },
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "JWT Bearer token authentication"
                    }
                };

                // Add MCP-specific extensions
                document.Extensions.Add("x-mcp-compatible", new Microsoft.OpenApi.Any.OpenApiBoolean(true));
                document.Extensions.Add("x-mcp-version", new Microsoft.OpenApi.Any.OpenApiString("1.0"));
                document.Extensions.Add("x-reynolds-persona", new Microsoft.OpenApi.Any.OpenApiString("enabled"));
                document.Extensions.Add("x-apim-integration", new Microsoft.OpenApi.Any.OpenApiString("optimized"));

                // Add tags for better organization
                document.Tags = new List<OpenApiTag>
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
                    },
                    new OpenApiTag
                    {
                        Name = "CrossPlatform",
                        Description = "Cross-platform event routing and coordination"
                    }
                };

                return Task.CompletedTask;
            });

            // Reynolds: Add document transformer to fix malformed $ref strings at generation time
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                FixMalformedReferences(document);
                return Task.CompletedTask;
            });

            // Add operation transformer for MCP enhancements
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                // Add common MCP headers to all operations
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

                    response.Headers["X-Reynolds-Coordination-ID"] = new OpenApiHeader
                    {
                        Schema = new OpenApiSchema { Type = "string" },
                        Description = "Reynolds coordination tracking identifier"
                    };
                }

                return Task.CompletedTask;
            });

            // Add schema transformer for enhanced documentation and complex model handling
            options.AddSchemaTransformer((schema, context, cancellationToken) =>
            {
                // Reynolds: Fix for complex nested model schema generation
                var typeName = context.JsonTypeInfo.Type.Name;
                
                // Add unique schema IDs to prevent conflicts between similar models
                if (typeName == "GitHubIssuePayload")
                {
                    // Webhook payload model
                    schema.Extensions.Add("x-schema-id", new Microsoft.OpenApi.Any.OpenApiString("GitHubIssuePayload"));
                }
                else if (typeName == "GitHubIssue")
                {
                    // Internal domain model
                    schema.Extensions.Add("x-schema-id", new Microsoft.OpenApi.Any.OpenApiString("GitHubIssueInternal"));
                }
                else if (typeName == "GitHubPullRequestPayload")
                {
                    // Webhook payload model
                    schema.Extensions.Add("x-schema-id", new Microsoft.OpenApi.Any.OpenApiString("GitHubPullRequestPayload"));
                }
                else if (typeName == "GitHubPullRequest")
                {
                    // Internal domain model
                    schema.Extensions.Add("x-schema-id", new Microsoft.OpenApi.Any.OpenApiString("GitHubPullRequestInternal"));
                }
                
                // Handle complex nested models that cause malformed $ref issues
                if (typeName == "IssuePRSynchronizationReport" ||
                    typeName == "IssuePRRelation" ||
                    typeName.Contains("GitHubIssue") ||
                    typeName.Contains("GitHubPullRequest"))
                {
                    // Force inline schema for complex nested structures to prevent malformed $ref generation
                    schema.Extensions.Add("x-inline-schema", new Microsoft.OpenApi.Any.OpenApiBoolean(true));
                    
                    // Add description to clarify the model purpose
                    if (string.IsNullOrEmpty(schema.Description))
                    {
                        schema.Description = typeName switch
                        {
                            "IssuePRSynchronizationReport" => "Report containing synchronized issue-PR relationships with Reynolds coordination intelligence",
                            "IssuePRRelation" => "Individual issue-PR relationship with synchronization status and recommended actions",
                            "GitHubIssue" => "Internal GitHub issue representation for coordination and analysis",
                            "GitHubPullRequest" => "Internal GitHub pull request representation for coordination and analysis",
                            _ => $"Reynolds-enhanced {typeName} model for enterprise coordination"
                        };
                    }
                }

                // Add examples for key request/response types
                if (typeName == "SendMessageRequest")
                {
                    schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
                    {
                        ["userIdentifier"] = new Microsoft.OpenApi.Any.OpenApiString("christaylor@nextgeneration.com"),
                        ["message"] = new Microsoft.OpenApi.Any.OpenApiString("Hello Chris! Test message from Reynolds APIM integration."),
                        ["preferredMethod"] = new Microsoft.OpenApi.Any.OpenApiString("Auto"),
                        ["context"] = new Microsoft.OpenApi.Any.OpenApiObject
                        {
                            ["source"] = new Microsoft.OpenApi.Any.OpenApiString("APIM-MCP"),
                            ["requestId"] = new Microsoft.OpenApi.Any.OpenApiString("req-123456"),
                            ["coordinationLevel"] = new Microsoft.OpenApi.Any.OpenApiString("Maximum")
                        }
                    };
                }

                return Task.CompletedTask;
            });
        });
    }

    /// <summary>
    /// Reynolds: Fix malformed $ref strings with double hash symbols
    /// Recursively traverses the OpenAPI document and fixes invalid references
    /// </summary>
    private static void FixMalformedReferences(OpenApiDocument document)
    {
        if (document?.Components?.Schemas == null) return;

        // Fix malformed references in all schemas
        foreach (var schema in document.Components.Schemas.Values)
        {
            FixSchemaReferences(schema);
        }

        // Fix malformed references in paths
        if (document.Paths != null)
        {
            foreach (var path in document.Paths.Values)
            {
                FixPathItemReferences(path);
            }
        }
    }

    /// <summary>
    /// Recursively fix references in a schema
    /// </summary>
    private static void FixSchemaReferences(OpenApiSchema schema)
    {
        if (schema == null) return;

        // Fix direct reference - remove malformed references entirely
        if (!string.IsNullOrEmpty(schema.Reference?.ReferenceV3) &&
            schema.Reference.ReferenceV3.Contains("#/components/schemas/#/"))
        {
            // Remove malformed reference and convert to a generic object schema
            schema.Reference = null;
            schema.Type = "object";
            schema.Description = "Schema reference was malformed and converted to generic object";
            schema.AdditionalPropertiesAllowed = true;
        }

        // Fix properties
        if (schema.Properties != null)
        {
            foreach (var property in schema.Properties.Values)
            {
                FixSchemaReferences(property);
            }
        }

        // Fix items (for arrays)
        if (schema.Items != null)
        {
            FixSchemaReferences(schema.Items);
        }

        // Fix additionalProperties
        if (schema.AdditionalProperties != null)
        {
            FixSchemaReferences(schema.AdditionalProperties);
        }

        // Fix allOf, oneOf, anyOf
        if (schema.AllOf != null)
        {
            foreach (var subSchema in schema.AllOf)
            {
                FixSchemaReferences(subSchema);
            }
        }

        if (schema.OneOf != null)
        {
            foreach (var subSchema in schema.OneOf)
            {
                FixSchemaReferences(subSchema);
            }
        }

        if (schema.AnyOf != null)
        {
            foreach (var subSchema in schema.AnyOf)
            {
                FixSchemaReferences(subSchema);
            }
        }
    }

    /// <summary>
    /// Create an inline schema for malformed references as a fallback
    /// </summary>
    private static OpenApiSchema CreateInlineSchemaForMalformedReference(string malformedRef)
    {
        // Analyze the malformed reference to guess the intended schema type
        if (malformedRef.Contains("/items/") || malformedRef.Contains("Array"))
        {
            return new OpenApiSchema
            {
                Type = "array",
                Items = new OpenApiSchema
                {
                    Type = "object",
                    AdditionalPropertiesAllowed = true
                },
                Description = $"Array schema inferred from malformed reference: {malformedRef}"
            };
        }
        
        return new OpenApiSchema
        {
            Type = "object",
            AdditionalPropertiesAllowed = true,
            Description = $"Generic object schema for malformed reference: {malformedRef}"
        };
    }

    /// <summary>
    /// Fix references in path items
    /// </summary>
    private static void FixPathItemReferences(OpenApiPathItem pathItem)
    {
        if (pathItem?.Operations == null) return;

        foreach (var operation in pathItem.Operations.Values)
        {
            FixOperationReferences(operation);
        }
    }

    /// <summary>
    /// Fix references in operations
    /// </summary>
    private static void FixOperationReferences(OpenApiOperation operation)
    {
        if (operation == null) return;

        // Fix request body
        if (operation.RequestBody?.Content != null)
        {
            foreach (var mediaType in operation.RequestBody.Content.Values)
            {
                if (mediaType.Schema != null)
                {
                    FixSchemaReferences(mediaType.Schema);
                }
            }
        }

        // Fix responses
        if (operation.Responses != null)
        {
            foreach (var response in operation.Responses.Values)
            {
                if (response.Content != null)
                {
                    foreach (var mediaType in response.Content.Values)
                    {
                        if (mediaType.Schema != null)
                        {
                            FixSchemaReferences(mediaType.Schema);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Fix malformed reference string with double hash symbols
    /// </summary>
    private static string FixReferenceString(string reference)
    {
        if (string.IsNullOrEmpty(reference)) return reference;

        // Fix malformed references like "#/components/schemas/#/properties/..."
        if (reference.Contains("#/components/schemas/#/"))
        {
            // Reynolds: Replace malformed references with inline schemas
            if (reference.Contains("/properties/issuePRRelations/items/properties/issue/properties/labels"))
            {
                // This should be an array of strings for labels
                return "#/components/schemas/StringArray";
            }
            else if (reference.Contains("/properties/issuePRRelations/items/properties/issue/properties/assignees"))
            {
                // This should be an array of strings for assignees
                return "#/components/schemas/StringArray";
            }
            else if (reference.Contains("/properties/issuePRRelations/items/properties/issue/properties/comments"))
            {
                // This should reference the GitHubComment schema
                return "#/components/schemas/GitHubComment";
            }
            else if (reference.Contains("/properties/issuePRRelations/items/properties/issue/properties/metadata"))
            {
                // This should be a generic object
                return "#/components/schemas/ObjectDictionary";
            }
            else if (reference.Contains("/properties/issuePRRelations/items/properties/relatedPRs/items"))
            {
                // This should reference the GitHubPullRequest schema
                return "#/components/schemas/GitHubPullRequest";
            }
        }

        return reference;
    }
}