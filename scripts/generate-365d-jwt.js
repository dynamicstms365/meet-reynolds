const jwt = require('jsonwebtoken');
const fs = require('fs');

if (process.argv.length < 4) {
  console.error('Usage: node generate-365d-jwt.js <APP_ID> <PRIVATE_KEY_FILE>');
  process.exit(1);
}

const appId = process.argv[2];
const privateKey = fs.readFileSync(process.argv[3], 'utf8');
const now = Math.floor(Date.now() / 1000);
const payload = {
  iss: parseInt(appId),
  iat: now,
  exp: now + 365 * 24 * 60 * 60 // 365 days
};

const token = jwt.sign(payload, privateKey, { algorithm: 'RS256' });
console.log(token);
