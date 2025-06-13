# Pull Request Review Event Default Log

## Event Metadata
- **Event Type**: pull_request_review
- **Action**: {{REVIEW_ACTION}}
- **Timestamp**: {{TIMESTAMP}}
- **Repository**: {{REPOSITORY_FULL_NAME}}
- **PR Number**: #{{PR_NUMBER}}
- **Review ID**: {{REVIEW_ID}}
- **Reviewer**: {{REVIEWER_LOGIN}}

## PR Context
- **PR Title**: {{PR_TITLE}}
- **PR Author**: {{PR_AUTHOR}}
- **PR State**: {{PR_STATE}}
- **Source Branch**: {{HEAD_BRANCH}}
- **Target Branch**: {{BASE_BRANCH}}
- **Files Changed**: {{FILES_CHANGED_COUNT}}

## Review Details
- **Review State**: {{REVIEW_STATE}}
- **Review Body**: {{REVIEW_BODY_LENGTH}} characters
- **Comments Count**: {{REVIEW_COMMENTS_COUNT}}
- **Suggestions Count**: {{SUGGESTIONS_COUNT}}
- **Approval Status**: {{APPROVAL_STATUS}}

## Review Analysis
- **Change Categories**: {{CHANGE_CATEGORIES}}
- **Security Impact**: {{SECURITY_IMPACT}}
- **Breaking Changes**: {{BREAKING_CHANGES}}
- **Test Coverage**: {{TEST_COVERAGE_IMPACT}}
- **Documentation Impact**: {{DOCUMENTATION_IMPACT}}

## Automated Actions Triggered
- [ ] Review quality validation performed
- [ ] Approval requirements checked
- [ ] Expert reviewer notifications sent
- [ ] Merge readiness assessment completed
- [ ] Project status updated
- [ ] Review metrics recorded
- [ ] Follow-up reminders scheduled

## Review Requirements Status
- **Required Reviewers**: {{REQUIRED_REVIEWERS}}
- **Completed Reviews**: {{COMPLETED_REVIEWS}}
- **Pending Reviews**: {{PENDING_REVIEWS}}
- **Blocking Reviews**: {{BLOCKING_REVIEWS}}
- **Approval Threshold**: {{APPROVAL_THRESHOLD}}
- **Current Approvals**: {{CURRENT_APPROVALS}}

## Quality Gates
- [ ] Code quality review completed
- [ ] Security review performed (if required)
- [ ] Architecture review completed (if required)
- [ ] Test coverage validated
- [ ] Documentation review completed
- [ ] Compliance requirements met

## Review Feedback Summary
- **Positive Feedback**: {{POSITIVE_FEEDBACK_COUNT}}
- **Improvement Suggestions**: {{IMPROVEMENT_SUGGESTIONS_COUNT}}
- **Critical Issues**: {{CRITICAL_ISSUES_COUNT}}
- **Blocking Issues**: {{BLOCKING_ISSUES_COUNT}}
- **Resolved Comments**: {{RESOLVED_COMMENTS_COUNT}}

## Follow-up Actions Required
- [ ] Address reviewer feedback
- [ ] Resolve blocking comments
- [ ] Update documentation
- [ ] Additional testing required
- [ ] Re-request review after changes
- [ ] Escalate conflicting feedback

## Integration Events
- **Project Status Updates**: {{PROJECT_STATUS_UPDATES}}
- **Merge Readiness**: {{MERGE_READINESS_STATUS}}
- **Automated Tests**: {{AUTOMATED_TEST_TRIGGERS}}
- **Notification Events**: {{NOTIFICATION_EVENTS}}

## Related Resources
- Review URL: {{REVIEW_URL}}
- PR URL: {{PR_URL}}
- Reviewer Profile: {{REVIEWER_PROFILE_URL}}
- Related Reviews: {{RELATED_REVIEWS}}
- CI/CD Status: {{CICD_STATUS}}

## Notes
<!-- Add any additional context, special circumstances, or manual observations -->