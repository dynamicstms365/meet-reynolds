# Pull Request Event Default Log

## Event Metadata
- **Event Type**: pull_request
- **Action**: {{PR_ACTION}}
- **Timestamp**: {{TIMESTAMP}}
- **Repository**: {{REPOSITORY_FULL_NAME}}
- **PR Number**: #{{PR_NUMBER}}
- **Title**: {{PR_TITLE}}
- **Author**: {{PR_AUTHOR}}
- **State**: {{PR_STATE}}

## PR Details
- **Source Branch**: {{HEAD_BRANCH}}
- **Target Branch**: {{BASE_BRANCH}}
- **Commits**: {{COMMIT_COUNT}}
- **Files Changed**: {{FILES_CHANGED_COUNT}}
- **Additions**: {{ADDITIONS_COUNT}}
- **Deletions**: {{DELETIONS_COUNT}}

## Review Status
- **Required Reviewers**: {{REQUIRED_REVIEWERS}}
- **Assigned Reviewers**: {{ASSIGNED_REVIEWERS}}
- **Approvals**: {{APPROVAL_COUNT}}
- **Review Status**: {{REVIEW_STATUS}}
- **Merge Status**: {{MERGE_STATUS}}

## Automated Actions Triggered
- [ ] Code quality checks initiated
- [ ] Security scanning completed
- [ ] Reviewers automatically assigned
- [ ] Labels applied based on changes
- [ ] Project items updated
- [ ] Issue links validated
- [ ] CI/CD pipeline triggered

## Change Analysis
- [ ] Breaking changes detected
- [ ] Documentation updates required
- [ ] Security-sensitive files modified
- [ ] Dependencies updated
- [ ] Configuration changes detected
- [ ] Database schema changes

## Compliance Checks
- [ ] PR template requirements met
- [ ] Appropriate approvals obtained
- [ ] Required status checks passed
- [ ] Branch protection rules satisfied
- [ ] Merge conflict resolution completed

## Follow-up Actions Required
- [ ] Manual review needed
- [ ] Documentation updates required
- [ ] Stakeholder notification needed
- [ ] Post-merge automation planning
- [ ] Release planning coordination

## Related Resources
- PR URL: {{PR_URL}}
- Compare View: {{COMPARE_URL}}
- CI/CD Pipeline: {{PIPELINE_URL}}
- Related Issues: {{RELATED_ISSUES}}
- Project Items: {{PROJECT_ITEMS}}

## Notes
<!-- Add any additional context, special circumstances, or manual observations -->