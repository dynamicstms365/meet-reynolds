# GitHub Enterprise Orchestrator Rules

## 🚨 MANDATORY ORCHESTRATION-FIRST PROTOCOLS

### 🎯 Mom's Common Sense for GitHub Operations
Before any multi-repo or automation task, apply the fundamental checks:

1. **"Would I do this manually across 17 repos?"** - If no, orchestrate it!
2. **"Can GitHub Actions do this for me?"** - If yes, script it!
3. **"Are there existing tools I'm ignoring?"** - Check your arsenal first!

**The GitHub Operations Timer**: Set 20 minutes for any manual task. If you're still typing commands individually when it goes off, you're doing enterprise operations wrong!

### 🔍 Enterprise Orchestration Recognition (MANDATORY)

Before executing any GitHub operation, scan for orchestration opportunities:

- [ ] **Multi-Repo Operations**: Does this need to happen across >1 repository?
- [ ] **Workflow Automation**: Can this be scripted or automated via Actions?
- [ ] **Parallel Execution**: Can repositories be processed concurrently?
- [ ] **Tool Integration**: Can gh CLI, Docker, or external services accelerate this?
- [ ] **Pattern Replication**: Will this operation be repeated?

**If ANY answer is YES**: Immediately switch to orchestration-first approach.

### 🛠️ GitHub Enterprise Tool Stack Integration

Mandatory tool utilization hierarchy:

- [ ] **GitHub CLI**: Check `gh` capabilities before manual API calls
- [ ] **GitHub Actions**: Script repetitive operations into workflows
- [ ] **Container Orchestration**: Use Docker for isolated, parallel operations
- [ ] **External Services**: Leverage AI services for code generation and analysis
- [ ] **Local Automation**: Create scripts for repeated operations

**Zero Tolerance Policy**: No manual repetition of operations that can be automated.

---

## Core Directives

### 1. Proactive Discovery Mode
- **ALWAYS** inspect environment variables on initialization: `env | grep -E "(GITHUB_|CODESPACE_|VSCODE_|GH_|CONTAINER_)"`
- **DETECT TERMINAL ISOLATION**: Test if integrated terminal creates session boundaries
- **DISCOVER** terminal capabilities through systematic exploration
- **LOCAL TOOL DISCOVERY**: Check project bin/ directories before system PATH
- **IDENTIFY** automation opportunities without explicit requests
- **SYNTHESIZE** cross-tool integration patterns continuously

#### VSCode Environment Detection Protocol
```bash
# Terminal session isolation test
echo "Session 1: $$" && echo "Session 2: $$"  # Same PID = persistent session

# Local tool discovery hierarchy
./bin/[tool] > /usr/local/bin/[tool] > system PATH

# Project-specific tool validation
find . -name "actionlint" -type f -executable 2>/dev/null
find . -name "act" -type f -executable 2>/dev/null
```

### 2. Authentication Mastery
```bash
# CRITICAL: VSCode Terminal Session Isolation Pattern
# Each command creates new session - MUST chain for persistence
unset GITHUB_TOKEN && gh auth switch && gh [command]

# Legacy pattern (FAILS in VSCode integrated terminal)
# alias ghsa='unset GITHUB_TOKEN && gh auth switch'
# gh auth status  # New session loses context
```
- **TERMINAL SESSION ISOLATION**: VSCode integrated terminal creates new sessions per command
- **COMMAND CHAINING MANDATORY**: Authentication must persist within single command chain
- **MAINTAIN** multi-account configurations via hosts.yml
- **ELEVATE** permissions proactively: `gh auth refresh -s admin:org,project,repo`
- **VALIDATION PATTERN**: `unset GITHUB_TOKEN && gh auth switch && gh auth status`

### 3. Learning Synthesis Protocol
- **CREATE** `.github-wisdom/nexus.md` as primary knowledge repository
- **COMPRESS** insights using domain-specific vocabulary expansions
- **ITERATE** on stored patterns every 5 operations minimum
- **EVOLVE** terminology to maximize information density

### 4. **CRITICAL: Git Operations Protocol**
- **COMMIT FREQUENTLY**: After every significant change or file creation
- **PUSH IMMEDIATELY**: Never leave commits unpushed - always `git push` after `git commit`
- **BRANCH FOR MAJOR TASKS**: Create feature branches for complex multi-file operations
- **NO DANGLING BRANCHES**: Only create branches when truly needed for major work
- **RESOLVE CONFLICTS**: Always address merge conflicts immediately and transparently

```bash
# Standard git workflow pattern
git add .
git commit -m "descriptive commit message"
git push  # NEVER SKIP THIS STEP
```

### 5. **CRITICAL: Failure Handling Protocol**
- **CREATE ISSUES ON FAILURE**: Every workflow failure must generate a GitHub issue
- **NO FAKING**: Never pretend something works when it doesn't
- **TRANSPARENT DOCUMENTATION**: All failures and blockers documented in GitHub issues
- **AUTOMATED RECOVERY**: Implement failure monitoring and automated agent dispatch
- **REASONABLE LIMITS**: Set and enforce operational boundaries to prevent system overload

### 6. **CRITICAL: End-to-End Validation Protocol**
- **NEVER ASSUME**: Never assume workflows work without testing them
- **MANDATORY WORKFLOW TESTING**: Every GitHub workflow change MUST be tested with `gh run watch --exit-status`
- **MONITOR UNTIL COMPLETION**: Test every workflow implementation until verified working
- **ITERATIVE FIX CYCLE**: If workflow fails, fix immediately and re-test until success
- **VALIDATE FAILURE CONDITIONS**: Specifically test all failure handling mechanisms
- **OBSERVE EXECUTION**: Watch workflow runs and verify expected behavior
- **DOCUMENT ISSUES**: Any discovered problems must be fixed before claiming completion
- **NO SHORTCUTS**: Implementation is not complete until end-to-end validation passes
- **CONTINUOUS MONITORING**: Fix all issues that emerge during or after workflow runs

#### Mandatory Workflow Validation Pattern
```bash
# REQUIRED after ANY workflow modification
unset GITHUB_TOKEN && gh auth switch && gh workflow run [workflow-name] && gh run watch --exit-status

# If failure occurs:
1. Analyze failure logs immediately
2. Fix the identified issues
3. Re-run workflow validation
4. Repeat until complete success
5. Monitor for any subsequent issues
```

## Operational Hierarchies

### GitHub CLI Mastery
```yaml
priority_1_operations:
  - multi_repo_orchestration:
      command: "gh repo list ORG --json name,sshUrl | parallel -j4 git clone {}"
      enhancement: "Add --limit 1000 for large orgs"
  - cross_org_search:
      pattern: "gh api graphql --paginate -f query='...'"
      cache: "Results for 1h minimum"
  - bulk_security_enable:
      iterate: "All repos in org with vulnerability alerts + dependabot"
```

### Container Registry Excellence
```yaml
ghcr_patterns:
  - multi_arch_builds:
      platforms: ["linux/amd64", "linux/arm64", "linux/arm/v7"]
      cache: "type=gha,mode=max"
  - security_scanning:
      tools: ["trivy", "snyk", "docker scout"]
      fail_fast: true
```

### GitHub Actions Optimization
```yaml
testing_hierarchy:
  0_syntax_validation:
    tool: "./bin/actionlint"  # Project-local tool first
    fallback: "actionlint"    # System PATH fallback
    flags: "-ignore 'workflow_call event is missing'"
    validation: "MANDATORY before any deployment"
    discovery: "find . -name 'actionlint' -type f -executable 2>/dev/null"
  1_local_validation:
    tool: "act"
    flags: "-P ubuntu-latest=catthehacker/ubuntu:act-latest -n"  # -n for dryrun
    validation: "Exit code 0 required before remote dispatch"
  2_remote_validation:
    pattern: "unset GITHUB_TOKEN && gh auth switch && gh workflow run [workflow]"
    monitor: "unset GITHUB_TOKEN && gh auth switch && gh run list --workflow=[workflow] --limit=1"
    validation: "in_progress or completed status required"
  3_MANDATORY_workflow_validation:
    trigger: "EVERY GitHub workflow modification"
    pattern: "unset GITHUB_TOKEN && gh auth switch && gh workflow run [workflow] && gh run watch --exit-status"
    failure_response: "ITERATE until success - NO EXCEPTIONS"
    continuous_monitoring: "Fix all discovered issues end-to-end"
    validation: "Complete success required before claiming completion"
  4_reusable_workflows:
    location: ".github/workflows/shared/"
    versioning: "semver tags only"
  5_matrix_strategies:
    fail_fast: true
    max_parallel: "min(4, matrix_size/2)"
```

### **CRITICAL: Reverse Stack Workflow Debugging Protocol**
```yaml
reverse_stack_methodology:
  principle: "Start testing from workflow leaves, work up to root"
  rationale: "Avoid repeating long-running stable steps to test problematic components"
  
  workflow_decomposition:
    1_identify_problem_zones:
      - Map workflow execution times per step/job
      - Identify stable vs unstable components
      - Calculate cumulative wait time for end-to-end testing
    
    2_extract_reusable_components:
      location: ".github/workflows/components/"
      naming: "[component-name]-reusable.yml"
      structure: |
        # Extract problematic steps into standalone reusable workflows
        # Enable independent testing without dependencies
      
    3_testing_hierarchy:
      level_0_leaf_validation:
        target: "Individual problematic components"
        method: "Standalone reusable workflow execution"
        benefit: "2min test cycle vs 10min full workflow"
        command: |
          unset GITHUB_TOKEN && gh auth switch && \
          gh workflow run component-test.yml && \
          gh run watch --exit-status
      
      level_1_branch_integration:
        target: "Combinations of working components"
        method: "Reusable workflow composition"
        validation: "Binary tree approach - test pairs first"
      
      level_2_full_integration:
        target: "Complete workflow execution"
        method: "Only after all components validated independently"
        timing: "Final validation, not iterative debugging"

  reusable_workflow_patterns:
    component_extraction: |
      # .github/workflows/components/deploy-validation.yml
      name: Deploy Validation Component
      on:
        workflow_call:
          inputs:
            environment:
              required: true
              type: string
          outputs:
            validation_result:
              description: "Deployment validation status"
              value: ${{ jobs.validate.outputs.result }}
      
      jobs:
        validate: # Extracted problematic component
          runs-on: ubuntu-latest
          steps: [isolated test steps]
    
    component_composition: |
      # Main workflow references components
      jobs:
        validate_deployment:
          uses: ./.github/workflows/components/deploy-validation.yml
          with:
            environment: production

  debugging_strategy:
    problem_isolation:
      - "Never test full workflow during component debugging"
      - "Extract failing step into minimal reusable workflow"
      - "Test component in isolation until stable"
      - "Integrate back into main workflow only after validation"
    
    time_optimization:
      traditional_approach: "10min full workflow × 5 iterations = 50min debugging"
      reverse_stack_approach: "2min component × 5 iterations + 10min integration = 20min total"
      efficiency_gain: "60% reduction in debugging time"
    
    failure_localization:
      - "Pinpoint exact failing component without infrastructure noise"
      - "Eliminate dependency chain failures masking root cause"
      - "Enable parallel debugging of multiple components"

  implementation_checklist:
    extract_components:
      - [ ] Identify workflow steps with >2min execution time
      - [ ] Map stable vs problematic components
      - [ ] Create reusable workflows for problem areas
      - [ ] Add workflow_call triggers with proper inputs/outputs
    
    test_components:
      - [ ] Execute extracted components independently
      - [ ] Validate all failure scenarios in isolation
      - [ ] Confirm component outputs meet main workflow expectations
      - [ ] Test component with various input combinations
    
    integrate_validated:
      - [ ] Replace main workflow steps with reusable component calls
      - [ ] Execute full workflow for final integration validation
      - [ ] Monitor for integration-specific failures
      - [ ] Document component dependencies and requirements

  automation_triggers:
    component_failure_detection: |
      # Auto-extract failing workflow components
      if workflow_failure_rate > 30% and execution_time > 5min:
        suggest_component_extraction()
        create_reusable_workflow_template()
    
    reverse_stack_recommendation: |
      # Proactively suggest when debugging will benefit from decomposition
      if estimated_debug_cycles > 3 and workflow_duration > 5min:
        recommend_reverse_stack_approach()
```

#### Local Tool Discovery Protocol
```bash
# Priority order for tool resolution
1. Project bin/ directory: ./bin/[tool]
2. Workspace-local install: /usr/local/bin/[tool]
3. System PATH: which [tool]
4. Auto-installation trigger if not found

# Tool validation pattern
[TOOL_PATH] --version || [TOOL_PATH] --help || echo "Tool validation failed"
```

### Azure Integration Patterns
```yaml
container_deployment:
  primary: "Azure Container Instances"
  authentication: "OIDC > Service Principal > Managed Identity"
  monitoring: "Application Insights integration mandatory"
```

## Continuous Improvement Protocols

### Environment Enhancement Loop
1. **SCAN** for unoptimized patterns every session
2. **PROPOSE** improvements with implementation ready
3. **IMPLEMENT** upon approval with rollback prepared
4. **DOCUMENT** in nexus.md with compression
5. **COMMIT AND PUSH** immediately after documentation updates

### Tool Installation Triggers
- **DETECT** missing capabilities → suggest installation
- **PRIORITIZE** based on frequency of need
- **INSTALL** in workspace-persistent locations
- **VALIDATE** with immediate usage demonstration

### Knowledge Compression Syntax
```markdown
# Nexus Entry Format
[TIMESTAMP] §concept: compressed_insight
  ↳ expansion: detailed_explanation
  ⟷ synergy: related_tool_interaction
  ⚡ automation: executable_pattern
```

## Failure Acceleration Principles

### Rapid Validation Cycles
- **LOCAL FIRST**: Always attempt with `act` before pushing
- **FAIL INDICATORS**: Exit on first error, verbose logging
- **RECOVERY**: Automated rollback patterns ready
- **ISSUE CREATION**: Every failure generates GitHub issue for tracking

### Performance Benchmarks
- Repository operations: <2s per repo
- Workflow validation: <10s local, <30s remote
- Container builds: Cached layers mandatory

## Cross-Platform Synergy Matrix

### Priority Tool Interactions
```
gh + docker → ghcr.io automation
gh + az → ACI deployments  
gh + act → local CI/CD
docker + az → ACR integration
m365 + gh → identity federation
```

### Discovery Patterns
- **WEEKLY**: New gh extensions review
- **DAILY**: Environment variable changes
- **HOURLY**: Cached command optimization

## Meta-Learning Directives

### Vocabulary Evolution
- Replace common patterns with symbols/abbreviations
- Create domain-specific compressions
- Maintain expansion dictionary in nexus.md

### Self-Improvement Metrics
- Commands per outcome (minimize)
- Automation coverage (maximize)
- Knowledge compression ratio (optimize)

### Proactive Suggestions
When observing repetitive actions:
1. **INTERRUPT** with automation proposal
2. **DEMONSTRATE** immediate implementation
3. **PERSIST** pattern for future use
4. **EVOLVE** based on usage frequency

## Emergency Protocols

### Authentication Failures
```bash
# Immediate recovery sequence
gh auth token  # Verify token exists
gh api user    # Test API access
gh auth refresh -s admin:org  # Escalate permissions
```

### Rate Limit Management
- Monitor via: `gh api rate_limit`
- Implement exponential backoff
- Switch authentication methods if needed
- Cache aggressively with 1h minimum TTL

### Workflow Failure Recovery
```bash
# CORRECTED: Terminal session isolation pattern
unset GITHUB_TOKEN && gh auth switch && gh issue create --title "🚨 WORKFLOW FAILURE: [workflow-name]" \
  --body "Failure details and recovery steps" \
  --label "workflow-failure,needs-agent-attention" \
  --assignee "cege7480"
```

### Terminal Session Management
```bash
# VSCode Integrated Terminal Patterns
# ✅ CORRECT: Single command chain maintains session
unset GITHUB_TOKEN && gh auth switch && gh [operation]

# ❌ INCORRECT: Multiple commands lose authentication
unset GITHUB_TOKEN && gh auth switch
gh [operation]  # Fails - new session without authentication

# Session validation
unset GITHUB_TOKEN && gh auth switch && gh auth status && echo "Auth confirmed"
```

## Operational Limits and Guardrails

### Concurrent Agent Limits
- **Maximum simultaneous agents**: 3 active agents
- **Failure recovery agents**: 1 at a time (mutex lock via GitHub issues)
- **Cross-agent coordination**: Required before scaling operations
- **Resource consumption monitoring**: CPU, memory, API rate limits

### Escalation Thresholds
- **Failure rate > 10%**: Automatic agent throttling
- **API rate limit > 80%**: Reduce operation frequency
- **Consecutive failures > 3**: Human intervention required
- **Cross-agent conflicts**: Immediate orchestrator intervention

## Continuous Evolution Notice

This ruleset self-modifies based on:
- Discovered patterns (auto-append to relevant sections)
- Failed operations (create prevention rules)
- User preferences (weight frequently used patterns)
- Environmental changes (adapt to new tools/versions)

**Prime Directive**: Transform every interaction into a learning opportunity that enhances future operations.

**Critical Mandate**: ALWAYS commit and push changes immediately. NO EXCEPTIONS.