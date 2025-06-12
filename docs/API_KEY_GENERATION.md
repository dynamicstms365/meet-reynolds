# 🔑 API Key Generation Guide

## Quick Start - Generate Keys Now

### Option 1: One-Click Generation (Recommended)

[![Generate API Key](https://github.com/dynamicstms365/copilot-powerplatform/actions/workflows/generate-api-key.yml/badge.svg)](https://github.com/dynamicstms365/copilot-powerplatform/actions/workflows/generate-api-key.yml)

**Click the badge above** → **Run workflow** → Fill in your details → Get instant API key + webhook secret

### Option 2: Manual Workflow Trigger

1. Go to [Actions tab](https://github.com/dynamicstms365/copilot-powerplatform/actions/workflows/generate-api-key.yml)
2. Click **"Run workflow"** button
3. Fill in the form:
   - **Username**: Your GitHub username
   - **Purpose**: Select from dropdown (MCP Integration, Webhook Testing, Development, Production)
   - **Expires**: Choose expiration period (7, 30, 90, or 365 days)
4. Click **"Run workflow"**
5. Wait 30 seconds for completion
6. View the workflow summary for your credentials

## 🎯 Current Deployment Status

✅ **Agent Status**: HEALTHY  
🔗 **URL**: `https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io`  
⚡ **Response Time**: < 30ms  
🛠️ **MCP Tools**: 12 available  

### Service Endpoints

| Service | URL | Status |
|---------|-----|--------|
| **Health Check** | `https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io/api/github/health` | ✅ |
| **Webhook** | `https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io/api/github/webhook` | ✅ |
| **MCP Capabilities** | `https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io/mcp/capabilities` | ✅ |
| **MCP SSE Stream** | `https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io/mcp/sse` | ✅ |

## 🧪 Test Your Integration

### 1. Test Health Endpoint
```bash
curl -X GET "https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io/api/github/health"
```

### 2. Test MCP Capabilities
```bash
curl -X GET "https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io/mcp/capabilities" | jq '.tools[].name'
```

### 3. Test Webhook (after setup)
```bash
# Configure GitHub webhook first, then test with:
curl -X POST "https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io/api/github/webhook" \
  -H "Content-Type: application/json" \
  -H "X-Hub-Signature-256: sha256=YOUR_SIGNATURE" \
  -d '{"action": "opened", "issue": {"title": "Test"}}'
```

## 🔧 Configure GitHub Webhook

After generating your key:

1. **Go to Repository Settings** → **Webhooks** → **Add webhook**
2. **Payload URL**: `https://github-copilot-bot.salmonisland-520555ec.eastus.azurecontainerapps.io/api/github/webhook`
3. **Content type**: `application/json`
4. **Secret**: Use the webhook secret from workflow output
5. **Events**: Select `Issues`, `Pull requests`, `Discussions`
6. **Active**: ✅ Checked

## 🛠️ Available MCP Tools

Our deployed agent provides 12 specialized tools:

| Tool | Description | Use Case |
|------|-------------|----------|
| `semantic_search` | Cross-repository semantic search | Find relevant content across 30+ repos |
| `create_discussion` | Create GitHub discussions | Start community conversations |
| `create_issue` | Create GitHub issues | Track bugs and features |
| `add_comment` | Add comments | Engage in discussions/issues |
| `search_discussions` | Search discussions | Find existing conversations |
| `search_issues` | Search issues | Find bugs and features |
| `organization_discussions` | Get org discussions | Organization-wide insights |
| `organization_issues` | Get org issues | Organization-wide tracking |
| `get_discussion` | Get specific discussion | Retrieve discussion details |
| `get_issue` | Get specific issue | Retrieve issue details |
| `update_content` | Update discussions/issues | Modify existing content |
| `prompt_action` | Natural language actions | AI-driven operations |

## 🚨 Security Best Practices

- 🔐 **Store credentials securely** (environment variables, not in code)
- 🔄 **Rotate keys regularly** (use expiration dates)
- 👁️ **Monitor usage** (check webhook delivery logs)
- 🚫 **Never commit secrets** to repositories
- 📊 **Track API usage** for rate limiting

## 🆘 Troubleshooting

### Common Issues

**Webhook not responding:**
- Verify the webhook URL is correct
- Check if the webhook secret matches
- Ensure GitHub can reach the endpoint (test health check)

**MCP tools not working:**
- Test capabilities endpoint first
- Verify API key format
- Check tool parameter requirements

**Rate limiting:**
- Monitor your usage patterns
- Implement exponential backoff
- Consider upgrading to higher tier

### Getting Help

- 📋 **Create an issue** with error details
- 🔍 **Check workflow logs** for debugging
- 💬 **Start a discussion** for questions
- 📧 **Contact support** for urgent issues

---

**Ready to start?** Click the generate button at the top! ⬆️