#!/bin/bash

# Discussion Monitoring Test Framework
# Tests the discussion monitoring prompt with various conversational scenarios

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROMPTS_DIR="$ROOT_DIR/.github/prompts"
MOCK_DATA_DIR="$SCRIPT_DIR/mock-data"
RESULTS_DIR="$SCRIPT_DIR/results"

echo -e "${BLUE}🤖 Discussion Monitoring Test Framework${NC}"
echo "=================================================="

# Function to test discussion monitoring functionality
test_discussion_monitoring() {
    local scenario_file="$1"
    local scenario_name="$2"
    
    echo -e "${YELLOW}💬 Testing Discussion Monitoring for: $scenario_name${NC}"
    
    # Load mock data
    local mock_data=$(cat "$scenario_file")
    
    # Extract values for prompt substitution
    local conversation_topic=$(echo "$mock_data" | jq -r '.conversation_topic')
    local user_intent=$(echo "$mock_data" | jq -r '.user_intent')
    local keywords=$(echo "$mock_data" | jq -r '.keywords | join(", ")')
    local repository=$(echo "$mock_data" | jq -r '.repository')
    local username=$(echo "$mock_data" | jq -r '.username')
    local discussion_search_results=$(echo "$mock_data" | jq -r '.discussion_search_results')
    local conversation_history=$(echo "$mock_data" | jq -r '.conversation_history')
    local expected_action=$(echo "$mock_data" | jq -r '.expected_action')
    
    # Create populated prompt using safer substitution
    local populated_prompt=$(cat "$PROMPTS_DIR/discussion-monitor.txt")
    populated_prompt="${populated_prompt//\{\{CONVERSATION_TOPIC\}\}/$conversation_topic}"
    populated_prompt="${populated_prompt//\{\{USER_INTENT\}\}/$user_intent}"
    populated_prompt="${populated_prompt//\{\{KEYWORDS\}\}/$keywords}"
    populated_prompt="${populated_prompt//\{\{REPOSITORY\}\}/$repository}"
    populated_prompt="${populated_prompt//\{\{USERNAME\}\}/$username}"
    populated_prompt="${populated_prompt//\{\{DISCUSSION_SEARCH_RESULTS\}\}/$discussion_search_results}"
    populated_prompt="${populated_prompt//\{\{CONVERSATION_HISTORY\}\}/$conversation_history}"
    
    # Save populated prompt
    echo "$populated_prompt" > "$RESULTS_DIR/prompt-discussion-$scenario_name.txt"
    
    # Create test summary
    cat > "$RESULTS_DIR/test-summary-$scenario_name.md" << EOF
# Discussion Monitoring Test: $scenario_name

## Scenario Description
$(echo "$mock_data" | jq -r '.scenario_description')

## Input Context
- **Topic**: $conversation_topic
- **User Intent**: $user_intent
- **Keywords**: $keywords
- **Repository**: $repository
- **User**: $username
- **Expected Action**: $expected_action

## Search Results
\`\`\`json
$discussion_search_results
\`\`\`

## Conversation History
\`\`\`
$conversation_history
\`\`\`

## Testing Notes
This scenario tests the MCP server's ability to:
1. Analyze conversational context
2. Search for relevant discussions
3. Provide appropriate recommendations
4. Maintain conversational flow

## Next Steps for MCP Integration
1. Implement \`discussion_search\` MCP tool
2. Add conversation context tracking
3. Create discussion creation workflow
4. Test with real GitHub API integration
EOF

    echo -e "${GREEN}✅ Discussion monitoring test completed for $scenario_name${NC}"
    echo -e "${BLUE}📄 Prompt saved to: $RESULTS_DIR/prompt-discussion-$scenario_name.txt${NC}"
    echo -e "${BLUE}📝 Summary saved to: $RESULTS_DIR/test-summary-$scenario_name.md${NC}"
}

# Function to run discussion monitoring tests
run_discussion_tests() {
    echo -e "${YELLOW}💬 Running discussion monitoring tests...${NC}"
    
    # Test each conversation scenario
    for scenario in "$MOCK_DATA_DIR"/conversation-*.json; do
        if [[ -f "$scenario" ]]; then
            scenario_name=$(basename "$scenario" .json | sed 's/conversation-//')
            
            echo -e "\n${BLUE}===== Testing Conversation Scenario: $scenario_name =====${NC}"
            test_discussion_monitoring "$scenario" "$scenario_name"
        fi
    done
}

# Function to create MCP integration test plan
create_mcp_integration_plan() {
    echo -e "${YELLOW}📋 Creating MCP Integration Test Plan...${NC}"
    
    cat > "$RESULTS_DIR/mcp-integration-plan.md" << 'EOF'
# MCP Integration Test Plan for Discussion Monitoring

## Overview
This plan outlines how to test the discussion monitoring functionality through the deployed MCP server.

## Required MCP Tools

### 1. `discussion_search`
**Purpose**: Search for discussions based on keywords and context
**Input Schema**:
```json
{
  "repository": "string",
  "keywords": ["string"],
  "topic": "string",
  "limit": "number (default: 5)"
}
```

### 2. `conversation_analyze`
**Purpose**: Analyze conversation context and determine user intent
**Input Schema**:
```json
{
  "conversation_text": "string",
  "user": "string",
  "context": "string"
}
```

### 3. `discussion_suggest`
**Purpose**: Generate suggestions based on search results and context
**Input Schema**:
```json
{
  "search_results": "array",
  "conversation_context": "string",
  "user_intent": "string"
}
```

## Testing Scenarios via MCP Server

### Test 1: No Discussion Exists
```bash
curl -X POST https://github-copilot-bot.livelygrass-868f2ee5.eastus.azurecontainerapps.io/mcp/tools \
  -H "Content-Type: application/json" \
  -d '{
    "tool": "discussion_search",
    "arguments": {
      "repository": "dynamicstms365/copilot-powerplatform",
      "keywords": ["Power Platform", "canvas app", "performance"],
      "topic": "Power Platform development best practices"
    }
  }'
```

### Test 2: Discussion Exists
```bash
curl -X POST https://github-copilot-bot.livelygrass-868f2ee5.eastus.azurecontainerapps.io/mcp/tools \
  -H "Content-Type: application/json" \
  -d '{
    "tool": "discussion_search",
    "arguments": {
      "repository": "dynamicstms365/copilot-powerplatform",
      "keywords": ["GitHub Copilot", "MCP server", "integration"],
      "topic": "GitHub Copilot integration patterns"
    }
  }'
```

### Test 3: Multiple Discussions
```bash
curl -X POST https://github-copilot-bot.livelygrass-868f2ee5.eastus.azurecontainerapps.io/mcp/tools \
  -H "Content-Type: application/json" \
  -d '{
    "tool": "discussion_search",
    "arguments": {
      "repository": "dynamicstms365/copilot-powerplatform",
      "keywords": ["Azure", "deployment", "CI/CD"],
      "topic": "Azure deployment automation"
    }
  }'
```

## Conversation State Management

### Option 1: Repository-based Knowledge Store
- Store conversation context in repository files
- Use structured naming: `.github/conversations/{user}-{timestamp}.json`
- Auto-index with existing knowledge base system
- Simple to implement, version controlled

### Option 2: Redis DataStore (Future Enhancement)
- Real-time conversation tracking
- Session management across multiple interactions
- Scalable for high-volume usage
- Requires additional infrastructure

### Option 3: GitHub Issues/Discussions as State Store
- Use issue comments for conversation threads
- Link related discussions automatically
- Natural integration with GitHub workflow
- Leverages existing permissions and notifications

## Prompt Testing Workflow

1. **Update prompts** in `.github/prompts/discussion-monitor.txt`
2. **Run test framework**: `./scripts/dev/discussion-monitor-test.sh`
3. **Review generated responses** in results directory
4. **Test via MCP API** using curl commands above
5. **Iterate and refine** based on actual responses

## Integration Checkpoints

- [ ] MCP tools registered and accessible
- [ ] GitHub API integration working
- [ ] Prompt template system implemented
- [ ] Conversation state management chosen and implemented
- [ ] Real-world testing with actual discussions
- [ ] Performance optimization and caching

## Success Metrics

- **Accurate discussion matching**: >90% relevance for existing discussions
- **Helpful new discussion suggestions**: Clear, actionable titles and descriptions
- **Conversational flow**: Natural, non-intrusive interactions
- **Context retention**: Maintains conversation history across interactions
EOF

    echo -e "${GREEN}✅ MCP Integration plan created: $RESULTS_DIR/mcp-integration-plan.md${NC}"
}

# Function to analyze all results
analyze_discussion_results() {
    echo -e "\n${YELLOW}📊 Analyzing discussion monitoring results...${NC}"
    
    echo -e "${BLUE}📁 Generated test files:${NC}"
    ls -la "$RESULTS_DIR"/prompt-discussion-*.txt "$RESULTS_DIR"/test-summary-*.md 2>/dev/null || echo "No discussion test files found"
    
    echo -e "\n${BLUE}📝 Test scenarios completed:${NC}"
    for summary in "$RESULTS_DIR"/test-summary-*.md; do
        if [[ -f "$summary" ]]; then
            scenario=$(basename "$summary" .md | sed 's/test-summary-//')
            echo -e "  • $scenario: $(wc -l < "$summary") lines"
        fi
    done
    
    echo -e "\n${GREEN}🎯 Next steps:${NC}"
    echo "1. Review generated prompts and summaries in: $RESULTS_DIR"
    echo "2. Implement MCP tools for discussion monitoring"
    echo "3. Test with deployed agent: $RESULTS_DIR/mcp-integration-plan.md"
    echo "4. Choose conversation state management approach"
    echo "5. Deploy and validate with real conversations"
}

# Main execution
main() {
    case "${1:-}" in
        --analyze)
            analyze_discussion_results
            ;;
        --plan)
            create_mcp_integration_plan
            ;;
        --help)
            echo "Usage: $0 [--analyze] [--plan] [--help]"
            echo ""
            echo "Options:"
            echo "  --analyze     Analyze existing test results"
            echo "  --plan        Create MCP integration test plan"
            echo "  --help        Show this help message"
            echo ""
            echo "Default: Run all discussion monitoring tests"
            ;;
        *)
            run_discussion_tests
            create_mcp_integration_plan
            analyze_discussion_results
            ;;
    esac
}

# Run the main function
main "$@"