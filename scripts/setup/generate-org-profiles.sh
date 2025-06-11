#!/usr/bin/env bash
# generate-org-profiles.sh
# Generates GitHub org user interaction profiles as markdown in docs/org-profiles/
# Requirements: gh CLI, jq

ORG="$1"
if [ -z "$ORG" ]; then
  echo "Usage: $0 <github-org>"
  exit 1
fi

OUTDIR="$(dirname "$0")/../../docs/org-profiles"
mkdir -p "$OUTDIR"

# Get all members
MEMBERS=$(gh api orgs/$ORG/members --paginate | jq -r '.[].login')

echo "Generating profiles for org: $ORG"

DASHBOARD="$OUTDIR/dashboard.md"
MEMBER_TABLE=""
MERMAID_EDGES=""
MOST_ACTIVE=""
TOP_REVIEWERS=""
EXPERTISE_HEATMAP=""

# Temporary files for metrics
TMP_ACTIVITY="$OUTDIR/.activity.tmp"
> "$TMP_ACTIVITY"

for USER in $MEMBERS; do
  echo "Processing $USER..."
  # Get user details
  USERDATA=$(gh api users/$USER)
  NAME=$(echo "$USERDATA" | jq -r '.name // .login')
  BIO=$(echo "$USERDATA" | jq -r '.bio // ""')
  # Get recent events (activity)
  EVENTS=$(gh api users/$USER/events --paginate | jq -r '.[].type' | sort | uniq -c | sort -nr)
  # Generate profile markdown
  PROFILE="$OUTDIR/$USER.md"
  echo "# $NAME ($USER)

**Bio:** $BIO

**Recent Activity Types:**
" > "$PROFILE"
  echo '```' >> "$PROFILE"
  echo "$EVENTS" >> "$PROFILE"
  echo '```' >> "$PROFILE"
  echo -e "\n_Last updated: $(date -u)_" >> "$PROFILE"

  # Collect activity for dashboard
  ACTIVITY_COUNT=$(echo "$EVENTS" | awk '{sum+=$1} END {print sum}')
  echo -e "$USER\t$NAME\t$ACTIVITY_COUNT" >> "$TMP_ACTIVITY"
  # Placeholder for collaboration edges (future: parse PR reviews, etc.)
  # Example: MERMAID_EDGES+="  $USER --> otheruser\n"
done

# Generate member table
MEMBER_TABLE="| User | Name | Activity Count |\n|------|------|----------------|\n"
while IFS=$'\t' read -r USER NAME COUNT; do
  MEMBER_TABLE+="| $USER | $NAME | $COUNT |\n"
done < <(sort -k3 -nr "$TMP_ACTIVITY")

# Most active members
MOST_ACTIVE=$(sort -k3 -nr "$TMP_ACTIVITY" | head -3 | awk -F'\t' '{print $2 " (" $1 ")"}')

# Write dashboard
sed -e "/<!-- MEMBER_TABLE -->/r /dev/stdin" -e "/<!-- MEMBER_TABLE -->/d" "$DASHBOARD" <<< "$MEMBER_TABLE" | \
  sed -e "/<!-- MERMAID_EDGES -->/r /dev/stdin" -e "/<!-- MERMAID_EDGES -->/d" <<< "$MERMAID_EDGES" | \
  sed -e "s/<!-- MOST_ACTIVE -->/$MOST_ACTIVE/" \
  sed -e "s/<!-- TOP_REVIEWERS -->/TBD/" \
  sed -e "s/<!-- EXPERTISE_HEATMAP -->/TBD/" \
  > "$DASHBOARD.tmp" && mv "$DASHBOARD.tmp" "$DASHBOARD"

rm "$TMP_ACTIVITY"

echo "All profiles generated in $OUTDIR."
