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
