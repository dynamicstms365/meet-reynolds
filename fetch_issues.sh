#!/bin/bash

# Create issue analysis directory if it doesn't exist
mkdir -p issue_analysis

# List of issue numbers < 70 from the gh issue list output
issues=(63 55 53 52 51 50 49 48 44 43 42 41 40 39 38 34 33 32 31 30 28 27 26 25 24 23 22 21 20 19 18 17 16 15 14 13 12 11)

echo "Fetching details for ${#issues[@]} issues numbered < 70..."

# Loop through each issue and save to file
for issue in "${issues[@]}"; do
    echo "Fetching issue #$issue..."
    gh issue view $issue > issue_analysis/issue_$issue.txt
    if [ $? -eq 0 ]; then
        echo "✅ Issue #$issue saved to issue_analysis/issue_$issue.txt"
    else
        echo "❌ Failed to fetch issue #$issue"
    fi
done

echo "All issues fetched!"
echo "Files saved in: issue_analysis/"
ls -la issue_analysis/