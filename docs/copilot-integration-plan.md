# Copilot Integration Plan: AI PR Review & ALM Orchestration

## Overview

This document outlines the plan to integrate AI PR Review Bot and ALM Process Orchestrator functionality into our successfully deployed GitHub Copilot agent. We'll use GitHub CLI with copilot functionality for development and testing before implementing the features as MCP tools.

## 🎯 Integration Strategy

### Phase 1: Development & Testing (Current Phase)
- ✅ Use GitHub CLI copilot for prompt development and testing
- ✅ Create mock scenarios to validate functionality
- ✅ Iterate on prompts using the testing framework
- ✅ Test with real PR data to ensure accuracy

### Phase 2: MCP Tool Integration
- 🔲 Implement new MCP tools in the deployed copilot agent
- 🔲 Add PR review and ALM orchestration endpoints
- 🔲 Integrate with existing agent capabilities
- 🔲 Deploy and test in production

### Phase 3: Workflow Migration
- 🔲 Migrate standalone workflows to agent-based tools
- 🔲 Update repository webhooks to use agent endpoints
- 🔲 Deprecate separate workflow files
- 🔲 Monitor and optimize performance

## 🛠️ Development Framework Usage

### Quick Start

```bash
# Run all mock scenario tests
./scripts/dev/copilot-integration-test.sh

# Test with a real PR
./scripts/dev/copilot-integration-test.sh --real-pr 123

# Analyze previous test results
./scripts/dev/copilot-integration-test.sh --analyze
```

### Mock Scenarios

The framework includes three test scenarios:

1. **New Feature PR** (`scenario-new-feature`)
   - Large feature addition with OAuth2
   - No approvals, pending reviews
   - Tests reviewer assignment logic

2. **Hotfix PR** (`scenario-hotfix`) 
   - Small critical bug fix
   - Approved and ready to merge
   - Tests merge readiness detection

3. **Large Refactoring** (`scenario-refactor`)
   - Major architectural changes
   - Conflicts and failing checks
   - Tests risk assessment and blocking issues

### Testing Workflow

1. **Prompt Development**
   ```bash
   # Edit prompts in .github/prompts/
   vim .github/prompts/pr-review.txt
   vim .github/prompts/alm-orchestrator.txt
   ```

2. **Test Changes**
   ```bash
   ./scripts/dev/copilot-integration-test.sh
   ```

3. **Review Results**
   ```bash
   ls scripts/dev/results/
   cat scripts/dev/results/result-pr-review-new-feature.txt
   ```

4. **Iterate and Refine**
   - Adjust prompts based on outputs
   - Test with real PRs when satisfied
   - Document successful patterns

## 🔧 MCP Tool Implementation Plan

### New MCP Tools to Add

#### 1. `pr_review_analysis`
**Purpose**: Analyze PR and generate comprehensive review feedback

**Input Schema**:
```json
{
  "pr_number": "string",
  "repository": "string (optional)",
  "focus_areas": ["security", "performance", "architecture", "testing"]
}
```

**Output**: Structured review with findings, recommendations, and risk assessment

#### 2. `alm_orchestration`
**Purpose**: Analyze PR lifecycle state and recommend next actions

**Input Schema**:
```json
{
  "pr_number": "string", 
  "repository": "string (optional)",
  "context": "string (additional context)"
}
```

**Output**: JSON with priority, risk level, recommended actions, and project updates

#### 3. `reviewer_assignment`
**Purpose**: Suggest appropriate reviewers based on changed files and expertise

**Input Schema**:
```json
{
  "pr_number": "string",
  "changed_files": ["string"],
  "exclude_reviewers": ["string"]
}
```

**Output**: List of suggested reviewers with expertise justification

### Integration Points

#### A. Extend Existing MCP Server
Add new tools to `src/CopilotAgent/Services/McpService.cs`:

```csharp
// New tool definitions
private readonly Dictionary<string, McpTool> _tools = new()
{
    // ... existing tools ...
    ["pr_review_analysis"] = new McpTool
    {
        Name = "pr_review_analysis",
        Description = "Analyze pull request and generate comprehensive review feedback",
        InputSchema = // ... schema definition
    },
    ["alm_orchestration"] = new McpTool
    {
        Name = "alm_orchestration", 
        Description = "Analyze PR lifecycle and recommend next actions",
        InputSchema = // ... schema definition
    }
};
```

#### B. Add GitHub API Integration
Enhance GitHub integration in the agent to fetch:
- PR details and metadata
- File changes and diffs  
- Review status and approvals
- CI/CD check results
- Related issues and projects

#### C. Prompt Template System
Implement a prompt template system to use the refined prompts:

```csharp
public class PromptTemplateService
{
    public string PopulateTemplate(string templateName, Dictionary<string, object> variables)
    {
        var template = LoadTemplate(templateName);
        return ReplaceVariables(template, variables);
    }
}
```

## 🚀 Deployment Strategy

### Current Infrastructure
- ✅ **Azure Container Apps**: Successfully deployed and running
- ✅ **GitHub Container Registry**: Image build and push working
- ✅ **GitHub App Authentication**: Proper permissions configured
- ✅ **MCP Server**: 12 tools currently available
- ✅ **Health Endpoints**: Monitoring and validation in place

### Deployment Process

1. **Development Branch Testing**
   ```bash
   # Create feature branch
   git checkout -b feature/pr-alm-integration
   
   # Test locally
   ./scripts/dev/copilot-integration-test.sh
   
   # Commit refined prompts and new tools
   git add .github/prompts/ src/CopilotAgent/
   git commit -m "feat: add PR review and ALM orchestration tools"
   ```

2. **CI/CD Pipeline** 
   - Push triggers our fixed deployment workflow
   - Builds new container image with enhanced tools
   - Deploys to Azure Container Apps
   - Validates new MCP endpoints

3. **Production Testing**
   ```bash
   # Test new tools via MCP
   curl -X POST https://github-copilot-bot.livelygrass-868f2ee5.eastus.azurecontainerapps.io/mcp/tools \
     -H "Content-Type: application/json" \
     -d '{"tool": "pr_review_analysis", "arguments": {"pr_number": "123"}}'
   ```

## 📊 Success Metrics

### Development Phase
- [ ] All mock scenarios generate useful, actionable output
- [ ] Real PR tests produce accurate analysis
- [ ] Prompts are refined and consistent
- [ ] Testing framework is stable and reliable

### Integration Phase  
- [ ] New MCP tools are properly registered and accessible
- [ ] API endpoints respond correctly with expected schemas
- [ ] GitHub integration works with proper authentication
- [ ] Performance meets acceptable thresholds

### Production Phase
- [ ] Standalone workflows are successfully replaced
- [ ] PR review quality improves (measured by follow-up comments)
- [ ] ALM orchestration reduces manual intervention
- [ ] No regressions in existing agent functionality

## 🔄 Migration Timeline

### Week 1: Development & Testing
- [x] Create testing framework and mock scenarios
- [x] Develop and refine prompts using GitHub CLI copilot
- [ ] Test with real PRs from this repository
- [ ] Document successful patterns and edge cases

### Week 2: MCP Integration
- [ ] Implement new MCP tools in the agent
- [ ] Add GitHub API integration for PR data
- [ ] Create prompt template system
- [ ] Local testing and validation

### Week 3: Deployment & Testing
- [ ] Deploy enhanced agent to Azure
- [ ] Validate new MCP endpoints
- [ ] Test integration with real PRs
- [ ] Performance optimization if needed

### Week 4: Migration & Cleanup
- [ ] Update repository webhooks to use agent
- [ ] Migrate from standalone workflows
- [ ] Monitor production usage
- [ ] Deprecate old workflows

## 🔧 Next Immediate Steps

1. **Run the Testing Framework**
   ```bash
   ./scripts/dev/copilot-integration-test.sh
   ```

2. **Review Generated Results**
   ```bash
   ls scripts/dev/results/
   # Examine the outputs and refine prompts as needed
   ```

3. **Test with Real PR** 
   ```bash
   ./scripts/dev/copilot-integration-test.sh --real-pr <recent_pr_number>
   ```

4. **Iterate on Prompts**
   - Edit `.github/prompts/pr-review.txt` based on results
   - Edit `.github/prompts/alm-orchestrator.txt` for better JSON output
   - Rerun tests until satisfied with quality

5. **Begin MCP Integration**
   - Start implementing the new tools in `src/CopilotAgent/`
   - Use the refined prompts as the basis for the AI interactions

This plan provides a systematic approach to integrating the AI PR Review and ALM Orchestration functionality into our successfully deployed copilot agent, with thorough testing and validation at each step.