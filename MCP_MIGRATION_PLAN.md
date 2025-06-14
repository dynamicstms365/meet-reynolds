# MCP SDK Migration Plan - Preview 0.2.0-preview.3
## Systematic Conversion from Interface-Based to Attribute-Based Architecture

### Current State Analysis
- **Total Tools**: 17 tools requiring conversion
- **Working Tools**: 2 proof-of-concept tools using correct `[McpServerToolType]` pattern
- **Legacy Tools**: 15 tools using obsolete `IMcpTool` interface
- **Broken Tools**: 2 tools with incorrect attribute syntax
- **Additional Issues**: 1 class naming bug, 1 server requiring rewrite

---

## Phase 1: Fix Immediate Compilation Issues (Priority 1)

### 1.1 Fix Class Naming Conflict
- **File**: `src/CopilotAgent/MCP/Tools/Reynolds/StrategicStakeholderCoordinationTool.cs:1026`
- **Issue**: `StrategicStrategicCoordinationResults` → `StrategicCoordinationResults`
- **Impact**: Resolves 1 compilation error

### 1.2 Fix Incorrectly Converted Tools (2 tools)
- **File**: `src/CopilotAgent/MCP/Tools/GitHub/SemanticSearchTool.cs`
  - **Current**: `[McpServerTool("semantic_search", "description")]`
  - **Fix**: Convert to `[McpServerToolType]` static class pattern
- **File**: `src/CopilotAgent/MCP/Tools/Reynolds/AnalyzeOrgProjectsTool.cs`
  - **Current**: `[McpServerTool("analyze_org_projects", "description")]`
  - **Fix**: Convert to `[McpServerToolType]` static class pattern

---

## Phase 2: Convert Legacy Interface-Based Tools (Priority 2)

### 2.1 Reynolds Organizational Tools (4 tools)
1. **CrossRepoOrchestrationTool.cs** - Complex orchestration logic
2. **StrategicStakeholderCoordinationTool.cs** - Stakeholder coordination
3. **OrgProjectHealthTool.cs** - Project health analysis
4. **OrgDependencyIntelligenceTool.cs** - Dependency intelligence

### 2.2 GitHub API Tools (11 tools)
1. **AddCommentTool.cs** - GitHub comment creation
2. **CreateDiscussionTool.cs** - GitHub discussion creation
3. **CreateIssueTool.cs** - GitHub issue creation
4. **GetDiscussionTool.cs** - GitHub discussion retrieval
5. **GetIssueTool.cs** - GitHub issue retrieval
6. **OrganizationDiscussionsTool.cs** - Org-level discussions
7. **OrganizationIssuesTool.cs** - Org-level issues
8. **PromptActionTool.cs** - AI-powered prompting
9. **SearchDiscussionsTool.cs** - Discussion search
10. **SearchIssuesTool.cs** - Issue search
11. **UpdateContentTool.cs** - Content updates

---

## Phase 3: Server Architecture Rewrite (Priority 3)

### 3.1 ReynoldsMcpServer.cs Rewrite
- **File**: `src/CopilotAgent/MCP/ReynoldsMcpServer.cs`
- **Issue**: Uses non-existent `IMcpServer` interface
- **Solution**: Remove or rewrite for preview SDK compatibility

---

## Conversion Pattern (Reference Implementation)

### Proven Working Pattern:
```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace CopilotAgent.MCP.Tools.GitHub
{
    [McpServerToolType]
    public static class ToolNameNew
    {
        [McpServerTool, Description("Tool description")]
        public static async Task<object> MethodName(
            [Description("Parameter description")] string param1,
            [Description("Parameter description")] int param2 = 10)
        {
            // Implementation
            return result;
        }
    }
}
```

### Key Changes Per Tool:
1. **Class Declaration**: `public class ToolName : IMcpTool` → `[McpServerToolType] public static class ToolName`
2. **Methods**: Instance methods → Static methods with `[McpServerTool, Description]`
3. **Parameters**: Remove `McpToolContext` → Add `[Description]` attributes
4. **Schema**: Remove `GetSchema()` method → Use parameter attributes
5. **Dependencies**: Handle DI differently in static context

---

## Dependency Injection Challenges

### Current Pattern (Interface-Based):
```csharp
public async Task<object> ExecuteAsync(McpToolContext context)
{
    var service = context.ServiceProvider.GetService<IGitHubIssuesService>();
}
```

### New Pattern (Static Methods):
```csharp
[McpServerTool, Description("...")]
public static async Task<object> MethodName(
    [FromServices] IGitHubIssuesService service,  // Option 1: Attribute injection
    [Description("...")] string param)
{
    // OR Option 2: Service locator pattern
    // OR Option 3: Mock data for proof-of-concept
}
```

---

## Expected Compilation Impact

### Before Conversion:
- **Errors**: 62 compilation errors
- **Working Tools**: 2/17 tools
- **Build Status**: FAILED

### After Phase 1:
- **Expected Errors**: ~55 compilation errors
- **Working Tools**: 4/17 tools

### After Phase 2:
- **Expected Errors**: ~5-10 compilation errors (server-related)
- **Working Tools**: 17/17 tools

### After Phase 3:
- **Expected Errors**: 0 compilation errors
- **Build Status**: SUCCESS

---

## Testing Strategy

### Per-Tool Validation:
1. Compile after each tool conversion
2. Verify tool registration in DI container
3. Test basic method invocation
4. Validate parameter binding

### Integration Testing:
1. Full build success
2. MCP server startup
3. Tool discovery and listing
4. End-to-end tool execution

---

## File Priority Order (Execution Sequence)

### Immediate Fixes:
1. `StrategicStakeholderCoordinationTool.cs` (class naming)
2. `SemanticSearchTool.cs` (attribute fix)
3. `AnalyzeOrgProjectsTool.cs` (attribute fix)

### Reynolds Tools (High Value):
4. `CrossRepoOrchestrationTool.cs`
5. `OrgProjectHealthTool.cs`
6. `OrgDependencyIntelligenceTool.cs`

### GitHub Tools (Core Functionality):
7. `GetIssueTool.cs` (replace existing with new)
8. `CreateIssueTool.cs`
9. `AddCommentTool.cs`
10. `UpdateContentTool.cs`
11. `SearchIssuesTool.cs`
12. `GetDiscussionTool.cs`
13. `CreateDiscussionTool.cs`
14. `SearchDiscussionsTool.cs`
15. `OrganizationIssuesTool.cs`
16. `OrganizationDiscussionsTool.cs`
17. `PromptActionTool.cs`

### Server Infrastructure:
18. `ReynoldsMcpServer.cs` (remove or rewrite)

---

## Success Criteria

✅ **Phase 1 Complete**: Compilation errors reduced to <50  
✅ **Phase 2 Complete**: All 17 tools converted and compiling  
✅ **Phase 3 Complete**: Clean build with 0 errors  
✅ **Final Validation**: MCP server starts and tools are discoverable  

---

*Generated: 2025-06-13 20:01 UTC*  
*Target SDK: ModelContextProtocol.Server 0.2.0-preview.3*  
*Architecture: Interface-based → Attribute-based static methods*