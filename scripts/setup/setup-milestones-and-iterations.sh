#!/usr/bin/env bash
# setup-milestones-and-iterations.sh
# Automates creation of milestones for each evolutionary phase, assigns issues, and sets project iteration values.
# Requirements: gh CLI, jq

ORG_REPO="$1" # e.g. myorg/myrepo
PROJECT_NUMBER="$2" # e.g. 1 (optional, for project automation)
if [ -z "$ORG_REPO" ]; then
  echo "Usage: $0 <org/repo> [project-number]"
  exit 1
fi

PHASES=(
  "Foundation"
  "Self-Acceleration"
  "Orchestration"
  "Evolution"
)

# 1. Create milestones if not exist
echo "Checking/creating milestones..."
for PHASE in "${PHASES[@]}"; do
  if ! gh api repos/$ORG_REPO/milestones | jq -e ".[] | select(.title == \"$PHASE\")" > /dev/null; then
    gh api repos/$ORG_REPO/milestones -f title="$PHASE" -f state=open
    echo "Created milestone: $PHASE"
  else
    echo "Milestone exists: $PHASE"
  fi
done

# 2. Assign issues to milestones based on phase label
ISSUES_JSON=$(gh issue list -R "$ORG_REPO" --json number,title,labels)
echo "$ISSUES_JSON" | jq -c '.[]' | while read -r ISSUE; do
  NUMBER=$(echo "$ISSUE" | jq -r '.number')
  TITLE=$(echo "$ISSUE" | jq -r '.title')
  LABELS=$(echo "$ISSUE" | jq -r '.labels[].name' | tr '\n' ',' | tr '[:upper:]' '[:lower:]')
  for PHASE in "${PHASES[@]}"; do
    # Support both phase names and phase-x labels
    PHASE_LOWER=$(echo "$PHASE" | tr '[:upper:]' '[:lower:]')
    PHASE_NUM=$(echo "$PHASE_LOWER" | awk '{if ($1=="foundation") print "phase-1"; else if ($1=="self-acceleration") print "phase-2"; else if ($1=="orchestration") print "phase-3"; else if ($1=="evolution") print "phase-4";}')
    if [[ "$LABELS" == *"$PHASE_LOWER"* || "$LABELS" == *"$PHASE_NUM"* ]]; then
      # Use milestone title instead of number for assignment
      gh issue edit $NUMBER -R "$ORG_REPO" --milestone "$PHASE"
      echo "Assigned issue #$NUMBER ($TITLE) to milestone $PHASE"
      # 3. Set project iteration/field if project number provided
      if [ -n "$PROJECT_NUMBER" ]; then
        # (Project automation logic here, as before)
        echo "[INFO] Project automation not yet implemented in this fix."
      fi
      break
    fi
  done
done

echo "Milestone and iteration assignment complete."
