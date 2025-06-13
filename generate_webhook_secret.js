const crypto = require('crypto');

function generateWebhookSecret() {
  // Generate a strong random secret for webhook validation
  const secret = crypto.randomBytes(32).toString('hex');
  
  console.log('GitHub Webhook Secret Generator');
  console.log('=================================');
  console.log('');
  console.log('Generated webhook secret:');
  console.log(secret);
  console.log('');
  console.log('IMPORTANT: This is different from your GitHub App JWT token!');
  console.log('');
  console.log('Instructions:');
  console.log('1. Copy the secret above');
  console.log('2. In your GitHub repository/organization settings:');
  console.log('   - Go to Settings → Webhooks');
  console.log('   - Edit your webhook');
  console.log('   - Paste this secret in the "Secret" field');
  console.log('3. Set this environment variable in your container:');
  console.log(`   NGL_DEVOPS_WEBHOOK_SECRET="${secret}"`);
  console.log('');
  console.log('Note: Your JWT token in jwt-365d.txt is for GitHub API authentication');
  console.log('      This webhook secret is for webhook signature validation');
  
  return secret;
}

generateWebhookSecret();