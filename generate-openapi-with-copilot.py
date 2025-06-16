#!/usr/bin/env python3
"""
Reynolds OpenAPI Generation Script
Uses GitHub Copilot AI to generate comprehensive OpenAPI 3.0 specification
"""

import yaml
import json
from datetime import datetime

def generate_openapi_spec():
    """
    Generate OpenAPI 3.0 specification for Reynolds Communication & Orchestration API
    
    This function serves as a prompt for GitHub Copilot to generate a comprehensive
    OpenAPI 3.0 specification based on the requirements in copilot-openapi-generation-prompt.md
    
    GitHub Copilot: Please generate a complete OpenAPI 3.0 specification with:
    - Communication endpoints for Teams integration
    - GitHub integration endpoints based on existing bot capabilities
    - Orchestration endpoints for cross-platform coordination
    - Monitoring endpoints for system health
    - Comprehensive data schemas with validation
    - Enterprise-grade security and error handling
    - Azure APIM integration features
    """
    
    # GitHub Copilot will generate the complete OpenAPI specification here
    openapi_spec = {
        "openapi": "3.0.3",
        "info": {
            "title": "Reynolds Communication & Orchestration API",
            "description": """
Enterprise-grade communication and orchestration API with Maximum Effort™ applied to every endpoint.

This API provides comprehensive communication orchestration capabilities including:
- Microsoft Teams integration with bidirectional messaging
- GitHub repository management and coordination
- Cross-platform stakeholder communication
- Real-time status monitoring and health checks
- Advanced orchestration and workflow coordination

**Authentication**: Supports both Azure AD OAuth2 and API Key authentication
**Rate Limiting**: Implemented at the APIM layer
**Monitoring**: Full Application Insights integration

*Just Reynolds - Maximum Effort™ • Minimal Drama*
            """.strip(),
            "version": "1.0.0",
            "contact": {
                "name": "Reynolds Organization",
                "email": "reynolds@nextgeneration.com",
                "url": "https://github.com/dynamicstms365/copilot-powerplatform"
            },
            "license": {
                "name": "MIT",
                "url": "https://opensource.org/licenses/MIT"
            },
            "termsOfService": "https://nextgeneration.com/terms"
        },
        "servers": [
            {
                "url": "https://reynolds-apim-prod.azure-api.net/reynolds",
                "description": "Production APIM endpoint"
            },
            {
                "url": "https://reynolds-apim-dev.azure-api.net/reynolds",
                "description": "Development APIM endpoint"
            },
            {
                "url": "https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io",
                "description": "Direct Container App endpoint (for testing)"
            }
        ],
        "security": [
            {"AzureAD": []},
            {"ApiKeyAuth": []}
        ]
    }
    
    # GitHub Copilot: Please complete the paths, components, and all other sections
    # following the detailed requirements in the prompt file
    
    return openapi_spec

def save_openapi_spec(spec, filename="reynolds-communication-api-openapi-generated.yaml"):
    """
    Save the generated OpenAPI specification to a YAML file
    """
    with open(filename, 'w') as f:
        yaml.dump(spec, f, default_flow_style=False, sort_keys=False, allow_unicode=True)
    
    print(f"✅ OpenAPI specification generated and saved to {filename}")
    print(f"📊 Generated at: {datetime.now().isoformat()}")
    print("🎭 Reynolds: Maximum Effort™ applied to enterprise API orchestration!")

def validate_openapi_spec(spec):
    """
    Basic validation of the generated OpenAPI specification
    """
    required_fields = ['openapi', 'info', 'paths']
    for field in required_fields:
        if field not in spec:
            raise ValueError(f"Missing required field: {field}")
    
    if 'title' not in spec['info']:
        raise ValueError("Missing required field: info.title")
    
    print("✅ Basic OpenAPI specification validation passed")

if __name__ == "__main__":
    print("🎭 Reynolds OpenAPI Generation Script")
    print("Generating comprehensive OpenAPI 3.0 specification...")
    
    try:
        # Generate the specification using GitHub Copilot
        spec = generate_openapi_spec()
        
        # Validate the generated specification
        validate_openapi_spec(spec)
        
        # Save to file
        save_openapi_spec(spec)
        
        print("\n🚀 Ready for Azure APIM deployment!")
        print("Next steps:")
        print("1. Review the generated specification")
        print("2. Import into Azure APIM")
        print("3. Test with Chris Taylor communication endpoints")
        
    except Exception as e:
        print(f"❌ Error: {e}")
        print("Please check the generated specification and try again.")