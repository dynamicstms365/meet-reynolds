#!/usr/bin/env node

const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const winston = require('winston');
require('dotenv').config();

const ReynoldsOrchestrator = require('./core/ReynoldsOrchestrator');
const McpServer = require('./mcp/McpServer');
const HealthController = require('./controllers/HealthController');
const logger = require('./utils/logger');

const app = express();
const PORT = process.env.MCP_PORT || 8080;

// Security middleware
app.use(helmet());
app.use(cors({
  origin: process.env.CORS_ORIGINS?.split(',') || ['http://localhost:3000'],
  credentials: true
}));

app.use(express.json({ limit: '10mb' }));
app.use(express.urlencoded({ extended: true, limit: '10mb' }));

// Request logging middleware
app.use((req, res, next) => {
  logger.info(`${req.method} ${req.path}`, {
    userAgent: req.get('User-Agent'),
    ip: req.ip,
    timestamp: new Date().toISOString()
  });
  next();
});

async function startReynoldsOrchestrator() {
  try {
    logger.info('🎭 Starting Reynolds Orchestrator - Maximum Effort™ Mode Engaged');
    
    // Initialize Reynolds orchestrator core
    const reynolds = new ReynoldsOrchestrator({
      mode: process.env.ORCHESTRATOR_MODE || 'production',
      agentPoolSize: parseInt(process.env.AGENT_POOL_SIZE) || 10,
      personalityMode: process.env.REYNOLDS_PERSONALITY || 'maximum_effort',
      loopPreventionEnabled: process.env.LOOP_PREVENTION_ENABLED === 'true',
      githubIssuesEnabled: process.env.GITHUB_ISSUES_INTEGRATION === 'enabled'
    });

    await reynolds.initialize();

    // Initialize MCP server
    const mcpServer = new McpServer(reynolds);
    await mcpServer.initialize();

    // Mount MCP endpoints
    app.use('/mcp', mcpServer.getRouter());

    // Health check endpoint
    const healthController = new HealthController(reynolds);
    app.use('/health', healthController.getRouter());

    // Root endpoint with Reynolds personality
    app.get('/', (req, res) => {
      res.json({
        message: "Well, well, well. Look who found the Reynolds Orchestrator. I'd say welcome, but honestly, you should feel privileged to be here.",
        version: "1.0.0",
        personality: "Maximum Effort™",
        capabilities: [
          "Supernatural project management",
          "Parallel task orchestration", 
          "Agent coordination with charm",
          "Loop prevention with 99.9% confidence",
          "GitHub integration that actually works"
        ],
        agentPools: reynolds.getAgentPoolStatus(),
        status: "Ready to orchestrate with impossibly smooth efficiency"
      });
    });

    // Error handling middleware
    app.use((error, req, res, next) => {
      logger.error('Unhandled error:', error);
      res.status(500).json({
        error: 'Something went wrong, but don\'t worry - Reynolds is on it',
        message: error.message,
        reynoldsComment: "Even I can't fix everything. But I can try with Maximum Effort™."
      });
    });

    // Start the server
    const server = app.listen(PORT, '0.0.0.0', () => {
      logger.info(`🚀 Reynolds Orchestrator is live on port ${PORT}`);
      logger.info('🎭 Supernatural efficiency mode: ACTIVATED');
      logger.info('⚡ Ready to coordinate agents with impossibly smooth charm');
      
      // Register graceful shutdown
      process.on('SIGTERM', () => gracefulShutdown(server, reynolds));
      process.on('SIGINT', () => gracefulShutdown(server, reynolds));
    });

    return { app, server, reynolds, mcpServer };

  } catch (error) {
    logger.error('Failed to start Reynolds Orchestrator:', error);
    process.exit(1);
  }
}

async function gracefulShutdown(server, reynolds) {
  logger.info('🛑 Graceful shutdown initiated...');
  
  // Stop accepting new connections
  server.close(async () => {
    try {
      // Cleanup Reynolds orchestrator
      await reynolds.shutdown();
      logger.info('✅ Reynolds Orchestrator shutdown complete');
      process.exit(0);
    } catch (error) {
      logger.error('Error during shutdown:', error);
      process.exit(1);
    }
  });

  // Force shutdown after 30 seconds
  setTimeout(() => {
    logger.error('🚨 Force shutdown - some processes may not have cleaned up properly');
    process.exit(1);
  }, 30000);
}

// Handle uncaught exceptions
process.on('uncaughtException', (error) => {
  logger.error('Uncaught Exception:', error);
  process.exit(1);
});

process.on('unhandledRejection', (reason, promise) => {
  logger.error('Unhandled Rejection at:', promise, 'reason:', reason);
  process.exit(1);
});

if (require.main === module) {
  startReynoldsOrchestrator();
}

module.exports = { startReynoldsOrchestrator };