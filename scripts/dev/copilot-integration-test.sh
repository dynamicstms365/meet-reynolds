#!/bin/bash

# Copilot Integration Development and Testing Framework
# This script helps develop and test AI PR Review and ALM Orchestrator functionality
# using GitHub CLI copilot before integrating into the main copilot agent

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROMPTS_DIR="$ROOT_DIR/.github/prompts"
MOCK_DATA_DIR="$SCRIPT_DIR/mock-data"
RESULTS_DIR="$SCRIPT_DIR/results"

# Ensure directories exist
mkdir -p "$MOCK_DATA_DIR" "$RESULTS_DIR"

echo -e "${BLUE}🚀 Copilot Integration Development Framework${NC}"
echo "=================================================="

# Function to check prerequisites
check_prerequisites() {
    echo -e "${YELLOW}📋 Checking prerequisites...${NC}"
    
    # Check if gh CLI is installed
    if ! command -v gh &> /dev/null; then
        echo -e "${RED}❌ GitHub CLI not found. Please install: https://cli.github.com/${NC}"
        exit 1
    fi
    
    # Check if gh copilot is installed
    if ! gh copilot --version &> /dev/null; then
        echo -e "${YELLOW}📦 Installing GitHub Copilot CLI extension...${NC}"
        gh extension install github/gh-copilot || {
            echo -e "${RED}❌ Failed to install gh copilot extension${NC}"
            exit 1
        }
    fi
    
    # Check authentication
    if ! gh auth status &> /dev/null; then
        echo -e "${RED}❌ Not authenticated with GitHub. Run: gh auth login${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}✅ Prerequisites satisfied${NC}"
}

# Function to create mock PR scenarios
create_mock_scenarios() {
    echo -e "${YELLOW}📝 Creating mock PR scenarios...${NC}"
    
    # Scenario 1: New feature PR
    cat > "$MOCK_DATA_DIR/scenario-new-feature.json" << 'EOF'
{
  "pr_number": "123",
  "pr_title": "feat: implement user authentication with OAuth2",
  "pr_author": "developer1",
  "head_branch": "feature/oauth2-auth",
  "base_branch": "main",
  "pr_state": "OPEN",
  "mergeable_state": "MERGEABLE",
  "files_changed": 8,
  "additions": 245,
  "deletions": 12,
  "approvals_count": 0,
  "review_requests_count": 2,
  "checks_status": "PENDING",
  "commit_messages": "feat: add OAuth2 authentication\nfeat: implement user session management\ntest: add authentication tests",
  "changed_files": "src/auth/oauth.ts\nsrc/auth/session.ts\nsrc/middleware/auth.ts\ntests/auth.test.ts\npackage.json\nREADME.md\ndocs/authentication.md\n.env.example"
}
EOF

    # Scenario 2: Bug fix PR ready to merge
    cat > "$MOCK_DATA_DIR/scenario-hotfix.json" << 'EOF'
{
  "pr_number": "124",
  "pr_title": "fix: resolve memory leak in data processing pipeline",
  "pr_author": "senior-dev",
  "head_branch": "hotfix/memory-leak",
  "base_branch": "main",
  "pr_state": "OPEN",
  "mergeable_state": "MERGEABLE",
  "files_changed": 3,
  "additions": 15,
  "deletions": 8,
  "approvals_count": 2,
  "review_requests_count": 0,
  "checks_status": "SUCCESS",
  "commit_messages": "fix: resolve memory leak in data processing\ntest: add memory usage test",
  "changed_files": "src/processing/pipeline.ts\ntests/pipeline.test.ts\nsrc/utils/memory.ts"
}
EOF

    # Scenario 3: Large refactoring PR
    cat > "$MOCK_DATA_DIR/scenario-refactor.json" << 'EOF'
{
  "pr_number": "125",
  "pr_title": "refactor: migrate legacy API endpoints to new architecture",
  "pr_author": "architect",
  "head_branch": "refactor/api-migration",
  "base_branch": "develop",
  "pr_state": "OPEN",
  "mergeable_state": "CONFLICTED",
  "files_changed": 45,
  "additions": 1250,
  "deletions": 890,
  "approvals_count": 1,
  "review_requests_count": 3,
  "checks_status": "FAILURE",
  "commit_messages": "refactor: update API endpoints structure\nrefactor: migrate user endpoints\nrefactor: migrate data endpoints\ntest: update integration tests\ndocs: update API documentation",
  "changed_files": "src/api/v2/users.ts\nsrc/api/v2/data.ts\nsrc/api/legacy/users.ts\nsrc/api/legacy/data.ts\ntests/api/integration.test.ts\ndocs/api-reference.md"
}
EOF

    echo -e "${GREEN}✅ Mock scenarios created${NC}"
}

# Function to test PR review functionality
test_pr_review() {
    local scenario_file="$1"
    local scenario_name="$2"
    
    echo -e "${YELLOW}🔍 Testing PR Review for: $scenario_name${NC}"
    
    # Load mock data
    local mock_data=$(cat "$scenario_file")
    
    # Extract values for prompt substitution
    local pr_title=$(echo "$mock_data" | jq -r '.pr_title')
    local pr_author=$(echo "$mock_data" | jq -r '.pr_author')
    local head_branch=$(echo "$mock_data" | jq -r '.head_branch')
    local base_branch=$(echo "$mock_data" | jq -r '.base_branch')
    local files_changed=$(echo "$mock_data" | jq -r '.files_changed')
    local changed_files=$(echo "$mock_data" | jq -r '.changed_files')
    
    # Create populated prompt using safer substitution
    local populated_prompt=$(cat "$PROMPTS_DIR/pr-review.txt")
    populated_prompt="${populated_prompt//\{\{PR_TITLE\}\}/$pr_title}"
    populated_prompt="${populated_prompt//\{\{PR_AUTHOR\}\}/$pr_author}"
    populated_prompt="${populated_prompt//\{\{HEAD_BRANCH\}\}/$head_branch}"
    populated_prompt="${populated_prompt//\{\{BASE_BRANCH\}\}/$base_branch}"
    populated_prompt="${populated_prompt//\{\{FILES_CHANGED\}\}/$files_changed}"
    populated_prompt="${populated_prompt//\{\{CHANGED_FILES\}\}/$changed_files}"
    
    echo "$populated_prompt" > "$RESULTS_DIR/prompt-pr-review-$scenario_name.txt"
    
    # Test with GitHub Copilot
    echo -e "${BLUE}🤖 Running Copilot PR Review Test...${NC}"
    echo "$populated_prompt" | gh copilot suggest --target shell > "$RESULTS_DIR/result-pr-review-$scenario_name.txt" 2>&1
    
    echo -e "${GREEN}✅ PR Review test completed for $scenario_name${NC}"
    echo -e "${BLUE}📄 Results saved to: $RESULTS_DIR/result-pr-review-$scenario_name.txt${NC}"
}

# Function to test ALM orchestration functionality
test_alm_orchestration() {
    local scenario_file="$1"
    local scenario_name="$2"
    
    echo -e "${YELLOW}🔧 Testing ALM Orchestration for: $scenario_name${NC}"
    
    # Load mock data
    local mock_data=$(cat "$scenario_file")
    
    # Extract values for prompt substitution
    local pr_number=$(echo "$mock_data" | jq -r '.pr_number')
    local pr_title=$(echo "$mock_data" | jq -r '.pr_title')
    local pr_author=$(echo "$mock_data" | jq -r '.pr_author')
    local head_branch=$(echo "$mock_data" | jq -r '.head_branch')
    local base_branch=$(echo "$mock_data" | jq -r '.base_branch')
    local pr_state=$(echo "$mock_data" | jq -r '.pr_state')
    local mergeable_state=$(echo "$mock_data" | jq -r '.mergeable_state')
    local files_changed=$(echo "$mock_data" | jq -r '.files_changed')
    local additions=$(echo "$mock_data" | jq -r '.additions')
    local deletions=$(echo "$mock_data" | jq -r '.deletions')
    local approvals_count=$(echo "$mock_data" | jq -r '.approvals_count')
    local review_requests_count=$(echo "$mock_data" | jq -r '.review_requests_count')
    local checks_status=$(echo "$mock_data" | jq -r '.checks_status')
    local commit_messages=$(echo "$mock_data" | jq -r '.commit_messages')
    local changed_files=$(echo "$mock_data" | jq -r '.changed_files')
    
    # Create populated prompt using safer substitution
    local populated_prompt=$(cat "$PROMPTS_DIR/alm-orchestrator.txt")
    populated_prompt="${populated_prompt//\{\{PR_NUMBER\}\}/$pr_number}"
    populated_prompt="${populated_prompt//\{\{PR_TITLE\}\}/$pr_title}"
    populated_prompt="${populated_prompt//\{\{PR_AUTHOR\}\}/$pr_author}"
    populated_prompt="${populated_prompt//\{\{HEAD_BRANCH\}\}/$head_branch}"
    populated_prompt="${populated_prompt//\{\{BASE_BRANCH\}\}/$base_branch}"
    populated_prompt="${populated_prompt//\{\{PR_STATE\}\}/$pr_state}"
    populated_prompt="${populated_prompt//\{\{MERGEABLE_STATE\}\}/$mergeable_state}"
    populated_prompt="${populated_prompt//\{\{FILES_CHANGED\}\}/$files_changed}"
    populated_prompt="${populated_prompt//\{\{ADDITIONS\}\}/$additions}"
    populated_prompt="${populated_prompt//\{\{DELETIONS\}\}/$deletions}"
    populated_prompt="${populated_prompt//\{\{APPROVALS_COUNT\}\}/$approvals_count}"
    populated_prompt="${populated_prompt//\{\{REVIEW_REQUESTS_COUNT\}\}/$review_requests_count}"
    populated_prompt="${populated_prompt//\{\{CHECKS_STATUS\}\}/$checks_status}"
    populated_prompt="${populated_prompt//\{\{COMMIT_MESSAGES\}\}/$commit_messages}"
    populated_prompt="${populated_prompt//\{\{CHANGED_FILES\}\}/$changed_files}"
    
    echo "$populated_prompt" > "$RESULTS_DIR/prompt-alm-$scenario_name.txt"
    
    # Test with GitHub Copilot
    echo -e "${BLUE}🤖 Running Copilot ALM Orchestration Test...${NC}"
    echo "$populated_prompt" | gh copilot suggest --target shell > "$RESULTS_DIR/result-alm-$scenario_name.txt" 2>&1
    
    echo -e "${GREEN}✅ ALM Orchestration test completed for $scenario_name${NC}"
    echo -e "${BLUE}📄 Results saved to: $RESULTS_DIR/result-alm-$scenario_name.txt${NC}"
}

# Function to run all tests
run_all_tests() {
    echo -e "${YELLOW}🧪 Running all integration tests...${NC}"
    
    # Test each scenario
    for scenario in "$MOCK_DATA_DIR"/scenario-*.json; do
        if [[ -f "$scenario" ]]; then
            scenario_name=$(basename "$scenario" .json | sed 's/scenario-//')
            
            echo -e "\n${BLUE}===== Testing Scenario: $scenario_name =====${NC}"
            
            test_pr_review "$scenario" "$scenario_name"
            test_alm_orchestration "$scenario" "$scenario_name"
        fi
    done
}

# Function to compare results
analyze_results() {
    echo -e "\n${YELLOW}📊 Analyzing results...${NC}"
    
    echo -e "${BLUE}📁 Generated files:${NC}"
    ls -la "$RESULTS_DIR"
    
    echo -e "\n${BLUE}📝 Result summary:${NC}"
    for result in "$RESULTS_DIR"/result-*.txt; do
        if [[ -f "$result" ]]; then
            filename=$(basename "$result")
            size=$(wc -l < "$result")
            echo -e "  • $filename: $size lines"
        fi
    done
    
    echo -e "\n${GREEN}🎯 Next steps:${NC}"
    echo "1. Review the generated results in: $RESULTS_DIR"
    echo "2. Refine prompts based on the outputs"
    echo "3. Test with real PR data using: $0 --real-pr <pr_number>"
    echo "4. Integrate successful prompts into the copilot agent"
}

# Function to test with real PR
test_real_pr() {
    local pr_number="$1"
    
    if [[ -z "$pr_number" ]]; then
        echo -e "${RED}❌ Please provide a PR number${NC}"
        exit 1
    fi
    
    echo -e "${YELLOW}🔍 Testing with real PR #$pr_number...${NC}"
    
    # Fetch real PR data
    local pr_data=$(gh pr view "$pr_number" --json title,author,headRefName,baseRefName,state,mergeable,additions,deletions,changedFiles,reviewRequests,latestReviews,commits,statusCheckRollup)
    
    # Extract and save real data
    echo "$pr_data" > "$RESULTS_DIR/real-pr-$pr_number.json"
    
    # Run tests with real data
    test_pr_review "$RESULTS_DIR/real-pr-$pr_number.json" "real-$pr_number"
    test_alm_orchestration "$RESULTS_DIR/real-pr-$pr_number.json" "real-$pr_number"
    
    echo -e "${GREEN}✅ Real PR test completed${NC}"
}

# Main execution
main() {
    case "${1:-}" in
        --real-pr)
            check_prerequisites
            test_real_pr "$2"
            ;;
        --analyze)
            analyze_results
            ;;
        --help)
            echo "Usage: $0 [--real-pr <pr_number>] [--analyze] [--help]"
            echo ""
            echo "Options:"
            echo "  --real-pr <num>   Test with a real PR number"
            echo "  --analyze         Analyze existing results"
            echo "  --help            Show this help message"
            echo ""
            echo "Default: Run all mock scenario tests"
            ;;
        *)
            check_prerequisites
            create_mock_scenarios
            run_all_tests
            analyze_results
            ;;
    esac
}

# Run the main function
main "$@"