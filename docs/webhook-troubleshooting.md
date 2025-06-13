# Webhook Event Logging and Signature Validation Troubleshooting Guide

## Overview

This guide provides information on debugging webhook event processing and signature validation failures in the GitHub Copilot Agent.

## Enhanced Webhook Logging

The system now provides detailed logging for webhook events to aid in debugging:

### 1. Incoming Webhook Request Logging (Middleware)

**Log Format:** `WEBHOOK_REQUEST_RECEIVED`
```
WEBHOOK_REQUEST_RECEIVED: Event={EventType}, DeliveryId={DeliveryId}, Signature={HasSignature}, UserAgent={UserAgent}, ContentType={ContentType}, RemoteIP={RemoteIP}
```

**Example:**
```
WEBHOOK_REQUEST_RECEIVED: Event=pull_request, DeliveryId=12345678-1234-1234-1234-123456789012, Signature=present, UserAgent=GitHub-Hookshot/abc123, ContentType=application/json, RemoteIP=192.30.252.1
```

### 2. Signature Validation Failure Logging (Enhanced Middleware)

**Log Format:** `WEBHOOK_SIGNATURE_VALIDATION_FAILED`
```
WEBHOOK_SIGNATURE_VALIDATION_FAILED: DeliveryId={DeliveryId}, EventType={EventType}, HasSignature={HasSignature}, UserAgent={UserAgent}, ContentLength={ContentLength}, RemoteIP={RemoteIP}, StatusCode={StatusCode}, Response={Response}
```

**Example:**
```
WEBHOOK_SIGNATURE_VALIDATION_FAILED: DeliveryId=12345678-1234-1234-1234-123456789012, EventType=pull_request, HasSignature=yes, UserAgent=GitHub-Hookshot/abc123, ContentLength=1024, RemoteIP=192.30.252.1, StatusCode=401, Response=Unauthorized
```

**Troubleshooting Log Format:** `WEBHOOK_TROUBLESHOOTING`
```
WEBHOOK_TROUBLESHOOTING: [Specific guidance based on failure type]
```

**Examples:**
- `WEBHOOK_TROUBLESHOOTING: Missing X-Hub-Signature-256 header. Ensure your GitHub webhook is configured to include a secret and signature.`
- `WEBHOOK_TROUBLESHOOTING: No webhook secret configured in NGL_DEVOPS_WEBHOOK_SECRET. This must match the secret configured in your GitHub webhook settings.`
- `WEBHOOK_TROUBLESHOOTING: Webhook secret is configured but signature validation failed. Verify the secret exactly matches the one in GitHub webhook settings.`

### 3. Webhook Event Processing Logging

**Log Format:** `WEBHOOK_EVENT_RECEIVED`
```
WEBHOOK_EVENT_RECEIVED: Type={EventType}, Action={Action}, Repository={Repository}, Sender={Sender}, Installation={InstallationId}, DeliveryId={DeliveryId}, UserAgent={UserAgent}
```

**Example:**
```
WEBHOOK_EVENT_RECEIVED: Type=pull_request, Action=opened, Repository=org/repo, Sender=username, Installation=12345, DeliveryId=12345678-1234-1234-1234-123456789012, UserAgent=GitHub-Hookshot/abc123
```

### 4. Event-Specific Metadata Logging

**Pull Request Events:**
```
WEBHOOK_PR_METADATA: PR_Number={Number}, State={State}, Draft={Draft}, User={User}, CreatedAt={CreatedAt}, UpdatedAt={UpdatedAt}
```

**Issue Events:**
```
WEBHOOK_ISSUE_METADATA: Issue_Number={Number}, State={State}, User={User}, CreatedAt={CreatedAt}, UpdatedAt={UpdatedAt}
```

**Workflow Run Events:**
```
WEBHOOK_WORKFLOW_METADATA: Workflow_Id={Id}, Name={Name}, Status={Status}, Conclusion={Conclusion}, CreatedAt={CreatedAt}, UpdatedAt={UpdatedAt}
```

### 5. Unhandled Event Logging

**Log Format:** `WEBHOOK_EVENT_UNHANDLED`
```
WEBHOOK_EVENT_UNHANDLED: Type={EventType}, DeliveryId={DeliveryId}, UserAgent={UserAgent}. This event type is not explicitly handled but was received.
```

## Signature Validation Troubleshooting

### Common Signature Validation Failures

#### 1. Missing Webhook Secret Configuration

**Symptoms:**
- Log: `WEBHOOK_SECRET_NOT_CONFIGURED: No webhook secret found in configuration. This will cause all signature validations to fail.`
- Error: `GitHub event failed signature validation.`

**Solution:**
- Ensure the `NGL_DEVOPS_WEBHOOK_SECRET` environment variable is set
- Verify the secret matches the one configured in your GitHub App/Repository webhook settings

#### 2. Missing Signature Header

**Symptoms:**
- Log: `WEBHOOK_SIGNATURE_MISSING: No X-Hub-Signature-256 header found in request. This will cause signature validation to fail.`

**Solution:**
- Check that your webhook is configured to send the `X-Hub-Signature-256` header
- Verify that your reverse proxy/load balancer is not stripping security headers

#### 3. Invalid Signature Format

**Symptoms:**
- Log: `WEBHOOK_SIGNATURE_INVALID_FORMAT: Signature header does not start with 'sha256='. Format: {SignatureFormat}`

**Solution:**
- Ensure your webhook is configured to use SHA-256 signatures
- Check that middleware/proxies are not modifying the signature header

#### 4. Secret Mismatch

**Symptoms:**
- All headers and format are correct, but validation still fails

**Solution:**
- Verify that the webhook secret in your environment exactly matches the one configured in GitHub
- Check for leading/trailing whitespace in the secret configuration
- Ensure the secret is being read from the correct configuration source

### Debug Steps for Signature Validation

1. **Check Configuration:**
   ```bash
   # Verify webhook secret is configured (without exposing the value)
   echo "Secret configured: $([ -n "$NGL_DEVOPS_WEBHOOK_SECRET" ] && echo "YES" || echo "NO")"
   ```

2. **Examine Request Headers:**
   Look for these log entries to verify the incoming request:
   - `WEBHOOK_REQUEST_RECEIVED` - Shows all incoming headers
   - `WEBHOOK_SECRET_STATUS` - Confirms secret configuration

3. **Test Webhook Delivery:**
   Use GitHub's webhook delivery testing feature:
   - Go to your repository/app webhook settings
   - Click "Recent Deliveries"
   - Click "Redeliver" on a failed delivery
   - Check both GitHub's delivery log and your application logs

4. **Validate Payload Size:**
   - Check `WEBHOOK_REQUEST_SIZE` logs
   - Large payloads (>1MB) may cause issues with some configurations

### Manual Signature Validation Testing

If you need to manually test signature validation:

```csharp
// Example test method (for development/testing only)
public bool TestSignatureValidation(string payload, string signature, string secret)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();
    
    var providedSignature = signature.StartsWith("sha256=") 
        ? signature.Substring(7) 
        : signature;
    
    return string.Equals(providedSignature, computedSignature, StringComparison.OrdinalIgnoreCase);
}
```

### Log Level Configuration

For enhanced debugging, set appropriate log levels:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "CopilotAgent.Services.OctokitWebhookEventProcessor": "Information",
      "CopilotAgent.Middleware.WebhookLoggingMiddleware": "Information",
      "Octokit.Webhooks": "Debug"
    }
  }
}
```

### Performance Considerations

- Enhanced logging adds minimal overhead to webhook processing
- Debug-level header extraction uses reflection and should only be enabled during troubleshooting
- Consider log retention policies for high-volume webhook scenarios

## Support Information

When reporting webhook issues, include:

1. **DeliveryId** from the logs
2. **Event type and action**
3. **Repository and installation information**
4. **Timestamp of the event**
5. **Any error messages from both application logs and GitHub's delivery log**
6. **Configuration status** (secret configured: yes/no)

This information will help identify whether the issue is with:
- Configuration
- Network/infrastructure
- Application processing
- GitHub's webhook delivery