# Issue Comment Event Rules

## Event Overview
This document defines the rules and automated behaviors for `issue_comment` webhook events in the repository.

## Core Directives

### 1. Comment Analysis and Processing
- **Command detection**: Identify and process special commands in comments (e.g., /assign, /label, /close)
- **Mention handling**: Process @mentions and trigger appropriate notifications
- **Content moderation**: Scan comments for inappropriate content or policy violations
- **Auto-responses**: Generate automatic responses for common questions or commands

### 2. Workflow Integration
- **Status updates**: Allow authorized users to update issue status via comments
- **Assignment changes**: Process assignment commands and validate permissions
- **Label management**: Handle label addition/removal through comment commands
- **Priority adjustments**: Allow priority changes through structured comment commands

### 3. Collaboration Enhancement
- **Expert routing**: Route technical questions to subject matter experts
- **Documentation linking**: Automatically link to relevant documentation based on comment content
- **FAQ automation**: Provide automatic responses to frequently asked questions
- **Translation support**: Offer translation assistance for international collaboration

### 4. Quality and Compliance
- **Approval workflows**: Process approval/rejection comments for workflow gates
- **Code review integration**: Handle code review comments and suggestions
- **Compliance tracking**: Track compliance-related discussions and decisions
- **Audit trail**: Maintain detailed audit trail of comment-based decisions

## Comment Processing Rules

### Command Recognition
- `/assign @username` - Assign issue to user
- `/label add:bug` - Add label to issue
- `/priority high` - Set issue priority
- `/milestone v1.2` - Assign to milestone
- `/close` - Close issue with comment
- `/reopen` - Reopen closed issue

### Permission Validation
- Verify commenter has appropriate permissions
- Validate team membership for team-specific commands
- Check repository-level permissions for administrative commands
- Enforce approval requirements for sensitive operations

### Automated Responses
- Welcome messages for first-time contributors
- Guidance on issue templates and requirements
- Links to relevant documentation and resources
- Status updates and progress notifications

## Security Considerations
- **Spam detection**: Identify and handle spam or malicious comments
- **Permission enforcement**: Ensure only authorized users can execute commands
- **Content filtering**: Filter out sensitive information in comments
- **Rate limiting**: Prevent comment spam and abuse

## Integration Points
- GitHub Projects v2 field updates via comments
- External system integration through comment webhooks
- Automated testing trigger via comment commands
- Documentation update triggers from technical discussions