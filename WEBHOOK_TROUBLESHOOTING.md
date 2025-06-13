# 🔧 GitHub Webhook Signature Validation Troubleshooting Guide

## 🚨 Current Issue: Signature Validation Failures

Your logs show `GitHub event failed signature validation` because you're using a **JWT token** as a **webhook secret**. These are two completely different things!

## 📋 Quick Fix Steps

### 1. Generate a Proper Webhook Secret

```bash
# Generate a new webhook secret
node generate_webhook_secret.js
```

This will output something like:
```
Generated webhook secret:
a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
```

### 2. Update GitHub Webhook Configuration

1. Go to your GitHub repository/organization settings
2. Navigate to **Settings → Webhooks**
3. Edit your existing webhook
4. **Replace the secret field** with the generated secret (NOT your JWT token)
5. Save the webhook

### 3. Update Container Environment Variable

Set the webhook secret in your Azure Container App:

```bash
# In Azure Container Apps environment variables
NGL_DEVOPS_WEBHOOK_SECRET=a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
```

### 4. Test the Configuration

```bash
# Set the secret locally and test
export NGL_DEVOPS_WEBHOOK_SECRET="your-generated-secret"
node test_webhook_signature.js
```

## 🔍 Understanding the Difference

### JWT Token (for API Authentication)
- **Purpose**: Authenticate your app with GitHub API
- **Location**: Your `jwt-365d.txt` file
- **Format**: `eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...` (very long)
- **Usage**: Making API calls to GitHub

### Webhook Secret (for Signature Validation)
- **Purpose**: Verify webhook payloads are from GitHub
- **Location**: GitHub webhook configuration
- **Format**: Simple hex string like `a1b2c3d4e5f6...`
- **Usage**: HMAC-SHA256 signature validation

## 📊 Enhanced Logging Output

With the new logging, you'll see detailed diagnostics like:

```log
🔍 WEBHOOK SIGNATURE VALIDATION START
📏 Payload length: 8240 bytes
🔑 Secret length: 64 chars
🔑 Secret preview: a1b2c3d4e5...
📝 Incoming signature: sha256=abcdef1234567890...
🔍 Hash from GitHub signature: abcdef1234567890...
🧮 Computed signature: abcdef1234567890...
✅ Webhook signature validation PASSED
🔍 WEBHOOK SIGNATURE VALIDATION END
```

## 🛠️ Debugging Common Issues

### Issue 1: Wrong Secret Type
**Symptom**: Long JWT token in webhook secret field
**Fix**: Use the short hex string from `generate_webhook_secret.js`

### Issue 2: Secret Mismatch
**Symptom**: `❌ WEBHOOK SECRET MISMATCH` in logs
**Fix**: Ensure both GitHub webhook and container have identical secret

### Issue 3: Missing Environment Variable
**Symptom**: `Secret empty: true` in logs
**Fix**: Set `NGL_DEVOPS_WEBHOOK_SECRET` in container environment

### Issue 4: Signature Format Issues
**Symptom**: `Invalid signature format` in logs
**Fix**: GitHub sends `sha256=hash`, ensure webhook is configured correctly

## 🧪 Testing Your Setup

### Local Testing
```bash
# 1. Generate secret
node generate_webhook_secret.js

# 2. Set environment variable
export NGL_DEVOPS_WEBHOOK_SECRET="your-secret"

# 3. Test signature generation
node test_webhook_signature.js

# 4. Test against your app (when deployed)
curl -X POST https://your-app/api/github/webhook \
  -H "Content-Type: application/json" \
  -H "X-Hub-Signature-256: sha256=computed-signature" \
  -d '{"test": "payload"}'
```

### GitHub Webhook Test
1. In GitHub webhook settings, click "Redeliver" on a recent delivery
2. Check your container logs for the new detailed validation output
3. Look for ✅ success or ❌ failure indicators

## 🔧 Container Rebuild Required

After setting the new environment variable:

```bash
# Redeploy your container with the new environment variable
# The logs will now show detailed signature validation information
```

## 📈 Monitoring Success

Successful webhook processing will show:
```log
✅ Webhook signature validation PASSED
info: Processing pull request webhook: opened for PR #123
✅ Pull request webhook processed: True, Actions: 2
```

Failed validation will show:
```log
❌ Webhook signature validation FAILED
🚨 WEBHOOK SECRET MISMATCH - Check your GitHub webhook secret configuration!
```

## 🎯 Next Steps

1. **Generate webhook secret**: `node generate_webhook_secret.js`
2. **Update GitHub webhook**: Replace secret in webhook settings
3. **Update container**: Set `NGL_DEVOPS_WEBHOOK_SECRET` environment variable
4. **Redeploy container**: Apply the environment variable changes
5. **Test webhook**: Trigger a test delivery from GitHub
6. **Monitor logs**: Watch for ✅ or ❌ indicators

## 📞 Support

If issues persist:
1. Check container logs for detailed validation output
2. Verify webhook secret matches between GitHub and container
3. Ensure webhook URL is correct: `/api/github/webhook`
4. Confirm webhook is sending `application/json` content-type

The enhanced logging will show exactly where the validation is failing!