const { createAppAuth } = require("@octokit/auth-app");

async function generateToken() {
  try {
    const auth = createAppAuth({
      appId: "1247205",
      privateKey: process.env.PRIVATE_KEY,
      installationId: "auto" // auto-detect installation
    });

    const appAuthentication = await auth({
      type: "installation",
    });

    console.log("GitHub App Installation Token:");
    console.log(appAuthentication.token);
  } catch (error) {
    console.error("Error generating token:", error.message);
    process.exit(1);
  }
}

generateToken();
