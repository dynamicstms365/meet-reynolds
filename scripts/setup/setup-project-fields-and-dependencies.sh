#!/usr/bin/env bash
# setup-project-fields-and-dependencies.sh
# Automates project field assignment and dependency mapping for GitHub Project v2
# Usage: ./setup-project-fields-and-dependencies.sh

set -euo pipefail
set -x

ORG="dynamicstms365"
REPO="copilot-powerplatform"
PROJECT_NUMBER=20
DASHBOARD_MD="docs/org-profiles/dashboard.md"

# 1. Fetch all project items and fields
echo "Fetching project fields..."
FIELDS_JSON=$(gh project field-list --owner $ORG $PROJECT_NUMBER --format json)
echo "$FIELDS_JSON" > /tmp/project_fields.json

# 2. Fetch all project items (issues)
echo "Fetching project items..."
ITEMS_JSON=$(gh project item-list --owner $ORG $PROJECT_NUMBER --format json)
echo "$ITEMS_JSON" > /tmp/project_items.json

# 3. Map issue numbers to project item IDs

echo "Mapping issue numbers to project item IDs..."
declare -A ISSUE_TO_ITEM_ID

echo "$ITEMS_JSON" | jq -r '.items[] | select(.content.__typename=="Issue") | "\(.content.number) \(.id)"' | while read -r number itemid; do
  ISSUE_TO_ITEM_ID[$number]=$itemid
done

# 4. Parse dependencies from issue bodies and build dependency edges

echo "Parsing dependencies from issue bodies..."
declare -A DEPENDENCIES
MERMAID_EDGES=""

for number in "${!ISSUE_TO_ITEM_ID[@]}"; do
  body=$(gh issue view $number --repo $ORG/$REPO --json body -q '.body // empty')
  echo "Parsing dependencies for issue #$number: body length ${#body}"
  if [[ -n "$body" ]]; then
    deps=$(echo "$body" | grep -oE 'Depends on #[0-9]+' | grep -oE '[0-9]+')
    for dep in $deps; do
      if [[ -n "${ISSUE_TO_ITEM_ID[$dep]:-}" ]]; then
        DEPENDENCIES[$number]+="$dep "
        # For Mermaid
        MERMAID_EDGES+="  $dep --> $number\n"
      fi
    done
  fi
done

# 5. Set project fields (Status, Milestone, Parent issue)
# (Example: set Status to 'Todo' if not set, set Milestone from issue milestone, set Parent issue if dependency found)

# FIX: Use correct key 'fields' instead of 'items' for field extraction
STATUS_FIELD_ID=$(echo "$FIELDS_JSON" | jq -r '.fields[] | select(.name=="Status") | .id')
MILESTONE_FIELD_ID=$(echo "$FIELDS_JSON" | jq -r '.fields[] | select(.name=="Milestone") | .id')
PARENT_FIELD_ID=$(echo "$FIELDS_JSON" | jq -r '.fields[] | select(.name|test("Parent|Dependency")) | .id' | head -n1)

for number in "${!ISSUE_TO_ITEM_ID[@]}"; do
  itemid=${ISSUE_TO_ITEM_ID[$number]}
  echo "Processing issue #$number (item ID: $itemid)"
  # Always set Status to 'Todo'
  gh project item-edit $itemid --field-id $STATUS_FIELD_ID --value "Todo"
  # Set Milestone from issue milestone
  milestone=$(gh issue view $number --repo $ORG/$REPO --json milestone 2>/dev/null | jq -r '.milestone.title // empty')
  echo "  Milestone: $milestone"
  if [[ -n "$milestone" ]]; then
    gh project item-edit $itemid --field-id $MILESTONE_FIELD_ID --value "$milestone"
  fi
  # Set Parent issue/dependency if found
  if [[ -n "${DEPENDENCIES[$number]:-}" && -n "$PARENT_FIELD_ID" ]]; then
    for dep in ${DEPENDENCIES[$number]}; do
      dep_itemid=${ISSUE_TO_ITEM_ID[$dep]:-}
      echo "  Dependency: #$dep (item ID: $dep_itemid)"
      if [[ -n "$dep_itemid" ]]; then
        gh project item-edit $itemid --field-id $PARENT_FIELD_ID --value "$dep_itemid"
      fi
    done
  fi
done

# 6. Generate Mermaid dependency graph and update dashboard
if [[ -n "$MERMAID_EDGES" ]]; then
  echo "Updating Mermaid dependency graph in $DASHBOARD_MD..."
  # Replace <!-- MERMAID_EDGES --> in dashboard.md
  sed -i "/<!-- MERMAID_EDGES -->/r <(echo -e '$MERMAID_EDGES')" "$DASHBOARD_MD"
fi

echo "Project field assignment and dependency mapping complete."
