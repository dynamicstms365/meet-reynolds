#!/usr/bin/env bash
# Verifies GitHub App authentication using org/repo variables and secrets
# Usage: ./scripts/verify-github-app-auth.sh

set -euo pipefail

# Read from environment or prompt for values
APP_ID="${NGL_DEVOPS_APP_ID:-}"  # Should be set in env or exported
PEM_PATH="${NGL_DEVOPS_BOT_PEM_PATH:-}"  # Path to PEM file

if [[ -z "$APP_ID" ]]; then
  echo "Error: NGL_DEVOPS_APP_ID environment variable is not set."
  exit 1
fi
if [[ -z "$PEM_PATH" ]]; then
  echo "Error: NGL_DEVOPS_BOT_PEM_PATH environment variable (path to PEM file) is not set."
  exit 1
fi
if [[ ! -f "$PEM_PATH" ]]; then
  echo "Error: PEM file not found at $PEM_PATH"
  exit 1
fi

# Install jq and openssl if not present
if ! command -v jq &>/dev/null; then
  echo "jq not found, installing..."
  sudo apt-get update && sudo apt-get install -y jq
fi
if ! command -v openssl &>/dev/null; then
  echo "openssl not found, installing..."
  sudo apt-get update && sudo apt-get install -y openssl
fi

# Generate JWT for GitHub App
ISSUED_AT=$(date +%s)
EXPIRES_AT=$((ISSUED_AT + 540))

HEADER_BASE64=$(echo -n '{"alg":"RS256","typ":"JWT"}' | openssl base64 -A | tr '+/' '-_' | tr -d '=')
PAYLOAD_BASE64=$(echo -n '{"iat":'$ISSUED_AT',"exp":'$EXPIRES_AT',"iss":'$APP_ID'}' | openssl base64 -A | tr '+/' '-_' | tr -d '=')
HEADER_PAYLOAD="$HEADER_BASE64.$PAYLOAD_BASE64"

SIGNATURE=$(echo -n "$HEADER_PAYLOAD" | openssl dgst -sha256 -sign "$PEM_PATH" | openssl base64 -A | tr '+/' '-_' | tr -d '=')
JWT="$HEADER_PAYLOAD.$SIGNATURE"

# Get installation ID for the app in this org
ORG="dynamicstms365"
INSTALLATION_ID=$(curl -s -H "Authorization: Bearer $JWT" -H "Accept: application/vnd.github+json" \
  "https://api.github.com/orgs/$ORG/installation" | jq -r '.id')

if [[ "$INSTALLATION_ID" == "null" || -z "$INSTALLATION_ID" ]]; then
  echo "Error: Could not retrieve installation ID for org $ORG."
  exit 1
fi

echo "GitHub App installation ID for $ORG: $INSTALLATION_ID"

# Exchange JWT for installation access token
ACCESS_TOKEN=$(curl -s -X POST -H "Authorization: Bearer $JWT" -H "Accept: application/vnd.github+json" \
  "https://api.github.com/app/installations/$INSTALLATION_ID/access_tokens" | jq -r '.token')

if [[ "$ACCESS_TOKEN" == "null" || -z "$ACCESS_TOKEN" ]]; then
  echo "Error: Could not retrieve access token."
  exit 1
fi

echo "GitHub App access token acquired."

# Test the token by listing repo info
REPO="copilot-powerplatform"
REPO_INFO=$(curl -s -H "Authorization: token $ACCESS_TOKEN" -H "Accept: application/vnd.github+json" \
  "https://api.github.com/repos/$ORG/$REPO")

if echo "$REPO_INFO" | jq -e '.id' >/dev/null; then
  echo "Success: Authenticated as GitHub App. Repo info:"
  echo "$REPO_INFO" | jq '{id, name, full_name, private, permissions}'
else
  echo "Error: Auth token did not work for repo API call."
  echo "$REPO_INFO"
  exit 1
fi
