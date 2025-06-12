# MCP Server SSE Authentication

## Overview

The MCP Server SSE (Server-Sent Events) endpoint now requires authentication to establish a connection. This ensures that only authorized clients can access the real-time event stream.

## Authentication Methods

### 1. Bearer Token (Recommended)

Use the GitHub App installation token as a Bearer token in the Authorization header:

```javascript
const token = "your-github-app-installation-token";
const eventSource = new EventSource("https://your-domain.com/mcp/sse", {
  headers: {
    "Authorization": `Bearer ${token}`
  }
});
```

### 2. API Key Query Parameter

Pass the GitHub App installation token as an API key in the query string:

```javascript
const token = "your-github-app-installation-token";
const eventSource = new EventSource(`https://your-domain.com/mcp/sse?api_key=${token}`);
```

### 3. GitHub Token Query Parameter

Use any valid GitHub personal access token or installation token:

```javascript
const githubToken = "your-github-token";
const eventSource = new EventSource(`https://your-domain.com/mcp/sse?github_token=${githubToken}`);
```

## Example Implementation

```javascript
// Example: Connecting with Bearer token authentication
async function connectToMCPStream() {
  try {
    // Get the GitHub App installation token
    const token = await getGitHubAppToken();
    
    // Create EventSource with authentication
    const eventSource = new EventSource("https://your-domain.com/mcp/sse", {
      headers: {
        "Authorization": `Bearer ${token}`
      }
    });

    // Handle connection events
    eventSource.addEventListener('connected', (event) => {
      const data = JSON.parse(event.data);
      console.log('Connected to MCP Server:', data);
    });

    // Handle heartbeat events
    eventSource.addEventListener('heartbeat', (event) => {
      const data = JSON.parse(event.data);
      console.log('Heartbeat:', data.timestamp);
    });

    // Handle errors
    eventSource.addEventListener('error', (event) => {
      const data = JSON.parse(event.data);
      console.error('MCP Server error:', data);
    });

    // Handle connection errors
    eventSource.onerror = (error) => {
      console.error('EventSource error:', error);
      if (eventSource.readyState === EventSource.CLOSED) {
        console.log('Connection closed');
      }
    };

    return eventSource;
  } catch (error) {
    console.error('Failed to connect to MCP Server:', error);
    throw error;
  }
}

// Helper function to get GitHub App token
async function getGitHubAppToken() {
  // This would typically come from your authentication system
  // For example, from a secure API endpoint or environment variable
  return process.env.GITHUB_APP_INSTALLATION_TOKEN;
}
```

## Authentication Validation

The server validates authentication using these methods:

1. **Bearer Token**: Compares against the current GitHub App installation token
2. **API Key**: Uses the same validation as Bearer token
3. **GitHub Token**: Makes a test API call to GitHub to verify token validity

## Error Responses

### 401 Unauthorized

If authentication fails, the server responds with:

```
HTTP/1.1 401 Unauthorized
Content-Type: text/plain

Unauthorized: Invalid or missing authentication
```

### Connection Events

#### Successful Connection

```json
{
  "timestamp": "2023-12-06T12:00:00.000Z",
  "message": "Connected to GitHub Copilot Bot MCP Server",
  "authenticated": true
}
```

#### Heartbeat

```json
{
  "timestamp": "2023-12-06T12:00:30.000Z",
  "status": "alive"
}
```

#### Error Event

```json
{
  "timestamp": "2023-12-06T12:00:00.000Z",
  "error": "Connection error"
}
```

## Security Considerations

1. **Token Security**: Always use HTTPS to protect tokens in transit
2. **Token Rotation**: GitHub App installation tokens have expiration times
3. **CORS**: The server includes appropriate CORS headers for web clients
4. **Logging**: All authentication attempts are logged for security auditing

## Testing Authentication

You can test the authentication using curl:

```bash
# Test with Bearer token
curl -H "Authorization: Bearer YOUR_TOKEN" \
     -H "Accept: text/event-stream" \
     https://your-domain.com/mcp/sse

# Test with API key
curl -H "Accept: text/event-stream" \
     https://your-domain.com/mcp/sse?api_key=YOUR_TOKEN

# Test with GitHub token
curl -H "Accept: text/event-stream" \
     https://your-domain.com/mcp/sse?github_token=YOUR_GITHUB_TOKEN
```

## MCP Tool and Resource Authentication

All MCP endpoints now require authentication:

- `POST /mcp/tools/{toolName}` - Execute MCP tools
- `GET /mcp/resources/{resourceUri}` - Access MCP resources
- `GET /mcp/sse` - Server-Sent Events stream

Use the same authentication methods for all endpoints.