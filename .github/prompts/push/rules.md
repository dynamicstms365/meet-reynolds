# Push Event Rules

## Event Overview
This document defines the rules and automated behaviors for `push` webhook events in the repository.

## Core Directives

### 1. Repository Content Synchronization
- **Monitor for `.roomodes` changes**: Detect modifications to `.roomodes` files in any repository
- **Monitor for `.roo/` directory changes**: Track changes to `.roo/` directory structure and content
- **Cross-repository propagation**: When `.roomodes` or `.roo/` changes are detected, trigger organization-wide updates

### 2. Automated Response Patterns
- **Validation checks**: Ensure pushed content meets organizational standards
- **Dependency updates**: Check for related changes that may require updates in dependent repositories
- **Documentation sync**: Update related documentation when code or configuration changes occur

### 3. Branch-specific Behaviors
- **Main/Master branch**: Trigger full validation and cross-repo synchronization
- **Feature branches**: Perform basic validation and compatibility checks
- **Release branches**: Execute comprehensive testing and preparation workflows

### 4. Security and Compliance
- **Secret scanning**: Validate that no secrets or sensitive information is pushed
- **Compliance checks**: Ensure changes meet organizational security and compliance requirements
- **Access control**: Verify that push operations are performed by authorized users

## Integration Points
- Custom logic for `.roomodes` and `.roo/` changes: See `update-roomodes.md`
- Cross-repository coordination using organization-level tokens
- Integration with GitHub Projects v2 for task tracking and milestone updates

## Escalation Protocols
- **Failed validation**: Create GitHub issues for manual review
- **Security violations**: Immediately notify security team and potentially block merge
- **Cross-repo conflicts**: Generate coordination issues in appropriate repositories