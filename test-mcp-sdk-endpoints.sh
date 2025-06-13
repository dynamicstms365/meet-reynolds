#!/bin/bash
# test-mcp-sdk-endpoints.sh
# Reynolds MCP Server SDK Endpoint Testing Script

BASE_URL="${MCP_BASE_URL:-http://localhost:80}"
AUTH_TOKEN="${MCP_AUTH_TOKEN:-test-token}"

echo "🧪 Testing Reynolds MCP Server SDK Endpoints"
echo "Base URL: $BASE_URL"
echo "======================================"

# Test 1: Health Check
echo "1. Testing Health Check..."
curl -s -X GET "$BASE_URL/health" \
  -H "Content-Type: application/json" | jq . || echo "Health check failed"

echo -e "\n"

# Test 2: Readiness Check
echo "2. Testing Readiness Check..."
curl -s -X GET "$BASE_URL/health/ready" \
  -H "Content-Type: application/json" | jq . || echo "Readiness check failed"

echo -e "\n"

# Test 3: MCP Initialize
echo "3. Testing MCP Initialize..."
curl -s -X POST "$BASE_URL/mcp/stdio" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "method": "initialize",
    "params": {
      "clientInfo": {
        "name": "test-client",
        "version": "1.0.0"
      }
    }
  }' | jq . || echo "Initialize failed"

echo -e "\n"

# Test 4: List Tools
echo "4. Testing Tool Discovery..."
curl -s -X POST "$BASE_URL/mcp/stdio" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "method": "tools/list"
  }' | jq . || echo "Tool list failed"

echo -e "\n"

# Test 5: Execute GitHub Tool (semantic_search)
echo "5. Testing GitHub Tool (semantic_search)..."
curl -s -X POST "$BASE_URL/mcp/stdio" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "method": "tools/call",
    "params": {
      "name": "semantic_search",
      "arguments": {
        "query": "authentication",
        "repository": "dynamicstms365/copilot-powerplatform",
        "scope": "all"
      }
    }
  }' | jq . || echo "Semantic search failed"

echo -e "\n"

# Test 6: Execute Reynolds Tool (analyze_org_projects)
echo "6. Testing Reynolds Tool (analyze_org_projects)..."
curl -s -X POST "$BASE_URL/mcp/stdio" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "method": "tools/call",
    "params": {
      "name": "analyze_org_projects",
      "arguments": {
        "project_scope": "active",
        "analysis_level": "comprehensive"
      }
    }
  }' | jq . || echo "Org analysis failed"

echo -e "\n"

# Test 7: SSE Transport
echo "7. Testing SSE Transport..."
timeout 5s curl -s -X GET "$BASE_URL/mcp/sse" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -H "Accept: text/event-stream" \
  --no-buffer || echo "SSE transport test completed (timeout expected)"

echo -e "\n✅ MCP SDK Endpoint Testing Complete"
echo "======================================"
echo "🎭 Reynolds says: Maximum Effort™ applied to endpoint validation!"