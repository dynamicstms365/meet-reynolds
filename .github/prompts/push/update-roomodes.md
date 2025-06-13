# Organization-wide .roomodes and .roo/ Directory Update Logic

## Purpose
This document defines the custom automation logic for propagating `.roomodes` and `.roo/` directory changes across the entire organization when detected in push events.

## Detection Triggers
- **`.roomodes` file changes**: Any modification to `.roomodes` files in any repository
- **`.roo/` directory changes**: Any addition, modification, or deletion of files within `.roo/` directories
- **Pattern matching**: Detect related configuration files that should be synchronized

## Propagation Strategy

### 1. Organization Repository Discovery
```bash
# Discover all repositories in the organization
gh repo list {{ORG_NAME}} --limit 1000 --json name,defaultBranch,private

# Filter repositories that should receive updates
# - Include repositories with existing .roomodes or .roo/ directories
# - Include repositories marked for synchronization in metadata
```

### 2. Change Analysis and Preparation
```bash
# Analyze the specific changes
git diff --name-only {{BASE_SHA}} {{HEAD_SHA}} | grep -E "^(\.roomodes|\.roo/)"

# Prepare change sets for each target repository
# - Determine which changes are universal vs. repository-specific
# - Validate compatibility with target repository configurations
```

### 3. Cross-Repository Update Process
```bash
# For each target repository:
# 1. Create feature branch for the update
gh api repos/{{ORG}}/{{REPO}}/git/refs/heads/main --jq '.object.sha' | \
xargs -I {} gh api repos/{{ORG}}/{{REPO}}/git/refs \
  --method POST \
  --field ref='refs/heads/sync-roomodes-{{TIMESTAMP}}' \
  --field sha='{}'

# 2. Apply the relevant changes
# 3. Create pull request for review
# 4. Auto-merge if validation passes (for trusted changes)
```

### 4. Validation and Safety Checks
- **Pre-flight validation**: Ensure changes don't break existing functionality
- **Compatibility checking**: Verify changes work with target repository's existing configuration
- **Rollback preparation**: Prepare rollback procedures in case of issues

## Repository Selection Criteria
- **Explicit inclusion**: Repositories with `.github/sync-roomodes.yml` configuration
- **Pattern matching**: Repositories already containing `.roomodes` or `.roo/` directories
- **Organization policy**: Apply to all repositories unless explicitly excluded

## Conflict Resolution
- **Merge conflicts**: Generate issues for manual resolution
- **Policy conflicts**: Escalate to repository maintainers
- **Validation failures**: Create detailed reports and block propagation

## Monitoring and Reporting
- **Success tracking**: Log successful propagations to organization dashboard
- **Failure alerts**: Immediate notification for failed propagations
- **Audit trail**: Complete record of all changes propagated across the organization

## Configuration Override
Repositories can opt-out or customize behavior via `.github/roomodes-sync.yml`:
```yaml
# Example configuration
enabled: true
exclude_patterns:
  - "*.local.md"
  - ".roo/private/*"
custom_validation:
  - script: "./scripts/validate-roomodes.sh"
  - required_reviewers: ["@org/security-team"]
```

## Security Considerations
- **Permission validation**: Ensure propagation process has appropriate permissions
- **Content filtering**: Never propagate sensitive or private information
- **Audit logging**: Complete audit trail of all propagation activities
- **Rate limiting**: Prevent overwhelming the organization with simultaneous updates