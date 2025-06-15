const config = {
  azureOpenAIKey: process.env.AZURE_OPENAI_API_KEY,
  azureOpenAIEndpoint: process.env.AZURE_OPENAI_ENDPOINT,
  azureOpenAIDeploymentName: process.env.AZURE_OPENAI_DEPLOYMENT_NAME,
  
  // Reynolds' backend orchestration configuration - Maximum Effort™
  backendOrchestrationUrl: process.env.BACKEND_ORCHESTRATION_URL || "http://localhost:5000",
  backendAuthToken: process.env.BACKEND_AUTH_TOKEN || "dev-token",
  
  // Reynolds' coordination settings
  maxRetryAttempts: parseInt(process.env.MAX_RETRY_ATTEMPTS || "3"),
  coordinationTimeoutMs: parseInt(process.env.COORDINATION_TIMEOUT_MS || "30000"),
  
  // Development settings
  isDevelopment: process.env.NODE_ENV === "development",
  enableDetailedLogging: process.env.ENABLE_DETAILED_LOGGING === "true"
};

export default config;
