const crypto = require('crypto');

function testWebhookSignature() {
  // Test payload (similar to what GitHub sends)
  const testPayload = JSON.stringify({
    "action": "opened",
    "number": 1,
    "pull_request": {
      "id": 1,
      "number": 1,
      "title": "Test PR",
      "body": "Test description"
    },
    "repository": {
      "id": 123456789,
      "name": "test-repo",
      "full_name": "user/test-repo"
    }
  });

  // Your webhook secret (use the one you generated)
  const webhookSecret = process.env.NGL_DEVOPS_WEBHOOK_SECRET || 'your-webhook-secret-here';
  
  if (webhookSecret === 'your-webhook-secret-here') {
    console.error('❌ Please set NGL_DEVOPS_WEBHOOK_SECRET environment variable');
    console.error('   Run: export NGL_DEVOPS_WEBHOOK_SECRET="your-actual-secret"');
    process.exit(1);
  }

  // Generate the signature like GitHub does
  const hmac = crypto.createHmac('sha256', webhookSecret);
  hmac.update(testPayload, 'utf8');
  const expectedSignature = 'sha256=' + hmac.digest('hex');

  console.log('Webhook Signature Test');
  console.log('=====================');
  console.log('');
  console.log('Test payload length:', testPayload.length, 'bytes');
  console.log('Webhook secret length:', webhookSecret.length, 'chars');
  console.log('Webhook secret preview:', webhookSecret.substring(0, 10) + '...');
  console.log('');
  console.log('Expected signature:', expectedSignature);
  console.log('');
  console.log('To test this against your application:');
  console.log('1. Send a POST request to your webhook endpoint');
  console.log('2. Include this signature in the X-Hub-Signature-256 header');
  console.log('3. Use this exact payload as the request body');
  console.log('');
  console.log('Example curl command:');
  console.log('curl -X POST http://your-app/api/github/webhook \\');
  console.log('  -H "Content-Type: application/json" \\');
  console.log(`  -H "X-Hub-Signature-256: ${expectedSignature}" \\`);
  console.log(`  -d '${testPayload}'`);
}

// Also provide a function to validate a signature
function validateSignature(payload, signature, secret) {
  if (!signature.startsWith('sha256=')) {
    return { valid: false, error: 'Signature must start with sha256=' };
  }

  const hmac = crypto.createHmac('sha256', secret);
  hmac.update(payload, 'utf8');
  const expectedSignature = 'sha256=' + hmac.digest('hex');
  
  const valid = crypto.timingSafeEqual(
    Buffer.from(signature),
    Buffer.from(expectedSignature)
  );

  return {
    valid,
    expected: expectedSignature,
    received: signature,
    error: valid ? null : 'Signature mismatch'
  };
}

// If running directly
if (require.main === module) {
  testWebhookSignature();
}

module.exports = { testWebhookSignature, validateSignature };