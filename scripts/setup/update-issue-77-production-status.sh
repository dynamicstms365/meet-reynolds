#!/bin/bash

# Script to update GitHub Issue #77 with production status information
# This documents the required change to reflect Reynolds' current production status

REPO="dynamicstms365/copilot-powerplatform"
ISSUE_NUMBER=77

echo "🎭 Reynolds Issue #77 Update Script"
echo "=================================="
echo ""
echo "Issue: #${ISSUE_NUMBER} in ${REPO}"
echo "Purpose: Update to reflect Reynolds production status"
echo ""

# Updated issue body
UPDATED_BODY=$(cat << 'EOF'
## 🚀 Production Status

**Reynolds is already live and operational!** Reynolds is currently deployed as a GitHub app (ID: 1247205) with webhook endpoints actively receiving all GitHub events across the organization. The foundational event ingestion infrastructure is already in place and processing events in production via Azure Container Apps.

### Overview
Enhance the existing Reynolds GitHub Event Analyzer with actionable narrative generation capabilities. Reynolds is already ingesting all GitHub events (commits, pushes, issues, PRs, etc.) through webhook endpoints. This task focuses on building analytical capabilities on top of the existing production infrastructure to:
- Detect commits without associated issues or PRs
- Analyze if action is needed (open issue/discussion, track as good practice, etc.)
- Generate ongoing narratives of project activity for visualization or storytelling
- Prioritize events/actions for user review

### Next Steps (for assignee):
1. Review existing webhook event processing pipeline in the production Reynolds deployment.
2. Enhance the `GitHubWorkflowOrchestrator` service to add commit analysis logic for detecting untracked commits.
3. Develop analytical narrative generation capabilities on top of existing event ingestion.
4. Implement action prioritization logic within the current webhook processing flow.
5. Report findings and propose further enhancements to the existing production system.

Assign to: cege7480

---
After assignment, review the existing production webhook handlers and event processing infrastructure. Build upon the established foundation rather than creating new systems.
EOF
)

echo "📝 Updated Issue Description:"
echo "---"
echo "$UPDATED_BODY"
echo "---"
echo ""
echo "🔄 Key Changes Made:"
echo "• Added '🚀 Production Status' section highlighting Reynolds is live"
echo "• Updated language from 'Propose a sub-agent' to 'Enhance the existing'"
echo "• Clarified that webhook event ingestion infrastructure already exists"
echo "• Modified next steps to build upon existing production system"
echo "• Referenced specific code components (GitHubWorkflowOrchestrator)" 
echo ""
echo "💡 Evidence of Production Status:"
echo "• GitHub App ID: 1247205 (configured in container-github-app.json)"
echo "• Azure Container Apps deployment (confirmed in copilot-integration-plan.md)"
echo "• Active webhook processing (GitHubController.cs, GitHubWorkflowOrchestrator.cs)"
echo "• Production endpoints receiving all GitHub events"
echo ""

# Check if GitHub CLI is available
if command -v gh &> /dev/null; then
    echo "🚀 GitHub CLI detected. You can update the issue with:"
    echo "gh issue edit ${ISSUE_NUMBER} --repo ${REPO} --body-file /tmp/updated_issue_description.md"
else
    echo "📋 Manual Update Required:"
    echo "Navigate to: https://github.com/${REPO}/issues/${ISSUE_NUMBER}"
    echo "Click 'Edit' and replace the description with the content above."
fi

echo ""
echo "✅ Update script completed successfully"