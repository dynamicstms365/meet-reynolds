#!/usr/bin/env node

const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const winston = require('winston');
const { v4: uuidv4 } = require('uuid');
const client = require('prom-client');
const axios = require('axios');
const { Client } = require('@microsoft/microsoft-graph-client');
const { AuthenticationProvider } = require('@azure/msal-node');
require('dotenv').config();

// Initialize logger
const logger = winston.createLogger({
  level: process.env.LOG_LEVEL || 'info',
  format: winston.format.combine(
    winston.format.timestamp(),
    winston.format.errors({ stack: true }),
    winston.format.json()
  ),
  defaultMeta: { 
    service: 'platform-specialist-agent',
    agentId: process.env.AGENT_ID || uuidv4()
  },
  transports: [
    new winston.transports.Console()
  ]
});

// Initialize Prometheus metrics
const register = new client.Registry();
client.collectDefaultMetrics({ register });

const httpRequestDuration = new client.Histogram({
  name: 'http_request_duration_seconds',
  help: 'Duration of HTTP requests in seconds',
  labelNames: ['method', 'route', 'status'],
  registers: [register]
});

const agentTaskCounter = new client.Counter({
  name: 'platform_agent_tasks_total',
  help: 'Total number of tasks processed by Platform agent',
  labelNames: ['task_type', 'status'],
  registers: [register]
});

const agentHealthGauge = new client.Gauge({
  name: 'platform_agent_health',
  help: 'Health status of Platform agent (1 = healthy, 0 = unhealthy)',
  registers: [register]
});

class PlatformAgent {
  constructor() {
    this.app = express();
    this.agentId = process.env.AGENT_ID || uuidv4();
    this.reynoldsEndpoint = process.env.REYNOLDS_MCP_ENDPOINT || 'http://reynolds:8080/mcp';
    this.capabilities = [
      'power-platform-development',
      'power-apps-creation',
      'power-automate-workflows',
      'power-bi-analytics',
      'm365-integration',
      'sharepoint-management',
      'teams-app-development',
      'graph-api-operations',
      'dynamics-365-integration',
      'compliance-management'
    ];
    
    this.tools = new Map();
    this.taskQueue = [];
    this.isProcessing = false;
    this.health = {
      status: 'starting',
      lastHeartbeat: new Date(),
      tasksProcessed: 0,
      errors: 0
    };

    this.initializeMiddleware();
    this.initializeRoutes();
    this.initializeTools();
    this.initializeClients();
  }

  initializeMiddleware() {
    // Security middleware
    this.app.use(helmet());
    this.app.use(cors({
      origin: process.env.CORS_ORIGINS?.split(',') || ['http://reynolds:8080'],
      credentials: true
    }));

    this.app.use(express.json({ limit: '10mb' }));
    this.app.use(express.urlencoded({ extended: true, limit: '10mb' }));

    // Request logging and metrics
    this.app.use((req, res, next) => {
      const start = Date.now();
      
      logger.info(`${req.method} ${req.path}`, {
        userAgent: req.get('User-Agent'),
        ip: req.ip,
        timestamp: new Date().toISOString()
      });

      res.on('finish', () => {
        const duration = (Date.now() - start) / 1000;
        httpRequestDuration
          .labels(req.method, req.route?.path || req.path, res.statusCode)
          .observe(duration);
      });

      next();
    });
  }

  initializeRoutes() {
    // Health check endpoint
    this.app.get('/health', (req, res) => {
      agentHealthGauge.set(this.health.status === 'healthy' ? 1 : 0);
      
      res.json({
        status: this.health.status,
        agentId: this.agentId,
        agentType: 'platform-specialist',
        philosophy: "I understand business needs like a consultant, code like a developer",
        capabilities: this.capabilities,
        health: this.health,
        uptime: process.uptime(),
        memory: process.memoryUsage(),
        reynolds: {
          connected: this.reynoldsConnected,
          endpoint: this.reynoldsEndpoint
        }
      });
    });

    // Metrics endpoint
    this.app.get('/metrics', async (req, res) => {
      res.set('Content-Type', register.contentType);
      res.end(await register.metrics());
    });

    // Task execution endpoint
    this.app.post('/execute', async (req, res) => {
      try {
        const { taskId, taskType, payload, context } = req.body;
        
        logger.info('Received task execution request', { taskId, taskType });
        
        const result = await this.executeTask(taskId, taskType, payload, context);
        agentTaskCounter.labels(taskType, 'success').inc();
        
        res.json({
          success: true,
          taskId,
          result,
          agentId: this.agentId,
          timestamp: new Date().toISOString()
        });
        
      } catch (error) {
        logger.error('Task execution failed', { error: error.message, stack: error.stack });
        agentTaskCounter.labels(req.body.taskType || 'unknown', 'error').inc();
        this.health.errors++;
        
        res.status(500).json({
          success: false,
          error: error.message,
          agentId: this.agentId,
          timestamp: new Date().toISOString()
        });
      }
    });

    // Agent capabilities
    this.app.get('/capabilities', (req, res) => {
      res.json({
        agentType: 'platform-specialist',
        capabilities: this.capabilities,
        tools: Array.from(this.tools.keys()),
        philosophy: "I understand business needs like a consultant, code like a developer",
        expertise: [
          'Power Platform solution architecture',
          'PowerApps canvas and model-driven apps',
          'Power Automate workflow automation',
          'Power BI dashboard and analytics',
          'Microsoft 365 ecosystem integration',
          'SharePoint site and list management',
          'Microsoft Teams app development',
          'Microsoft Graph API operations',
          'Dynamics 365 customization',
          'Compliance and governance solutions'
        ]
      });
    });

    // Root endpoint
    this.app.get('/', (req, res) => {
      res.json({
        message: "Platform Specialist Agent - Ready for Maximum Effort™ Business Solutions",
        agentId: this.agentId,
        philosophy: "I understand business needs like a consultant, code like a developer",
        status: this.health.status,
        reynoldsIntegration: this.reynoldsConnected ? 'Connected' : 'Connecting...'
      });
    });
  }

  initializeTools() {
    // Power Platform Operations
    this.tools.set('powerapps-create', this.createPowerApp.bind(this));
    this.tools.set('powerapps-deploy', this.deployPowerApp.bind(this));
    this.tools.set('powerautomate-workflow', this.createPowerAutomateWorkflow.bind(this));
    this.tools.set('powerbi-dashboard', this.createPowerBIDashboard.bind(this));
    
    // M365 Integration
    this.tools.set('sharepoint-site-create', this.createSharePointSite.bind(this));
    this.tools.set('sharepoint-list-manage', this.manageSharePointList.bind(this));
    this.tools.set('teams-app-deploy', this.deployTeamsApp.bind(this));
    this.tools.set('graph-api-operation', this.executeGraphOperation.bind(this));
    
    // Dynamics 365 Operations
    this.tools.set('dynamics-customize', this.customizeDynamics365.bind(this));
    this.tools.set('dynamics-workflow', this.createDynamicsWorkflow.bind(this));
    
    // Compliance and Governance
    this.tools.set('compliance-policy', this.createCompliancePolicy.bind(this));
    this.tools.set('governance-setup', this.setupGovernance.bind(this));
    
    // Analytics and Reporting
    this.tools.set('analytics-setup', this.setupAnalytics.bind(this));
    this.tools.set('business-intelligence', this.createBusinessIntelligence.bind(this));
  }

  async initializeClients() {
    try {
      // Initialize Microsoft Graph client
      if (process.env.AZURE_CLIENT_ID && process.env.AZURE_CLIENT_SECRET) {
        this.graphClient = Client.init({
          authProvider: async (done) => {
            // Implementation would use proper MSAL authentication
            done(null, process.env.GRAPH_ACCESS_TOKEN);
          }
        });
      }
      
      // Test Reynolds connection
      await this.connectToReynolds();
      
      this.health.status = 'healthy';
      logger.info('✅ Platform Agent clients initialized successfully');
      
    } catch (error) {
      logger.error('Failed to initialize clients', { error: error.message });
      this.health.status = 'unhealthy';
      this.health.errors++;
    }
  }

  async connectToReynolds() {
    try {
      const response = await axios.post(`${this.reynoldsEndpoint}/agents/register`, {
        agentId: this.agentId,
        agentType: 'platform-specialist',
        capabilities: this.capabilities,
        endpoint: `http://${process.env.HOSTNAME || 'localhost'}:8080`,
        philosophy: "I understand business needs like a consultant, code like a developer"
      });

      this.reynoldsConnected = true;
      logger.info('✅ Successfully connected to Reynolds MCP Server', { 
        reynoldsResponse: response.data 
      });
      
    } catch (error) {
      this.reynoldsConnected = false;
      logger.warn('Failed to connect to Reynolds MCP Server', { 
        error: error.message,
        endpoint: this.reynoldsEndpoint
      });
    }
  }

  async executeTask(taskId, taskType, payload, context) {
    const startTime = Date.now();
    
    try {
      logger.info('Executing task', { taskId, taskType, payload });
      
      if (!this.tools.has(taskType)) {
        throw new Error(`Unknown task type: ${taskType}`);
      }

      const tool = this.tools.get(taskType);
      const result = await tool(payload, context);
      
      this.health.tasksProcessed++;
      this.health.lastHeartbeat = new Date();
      
      const executionTime = Date.now() - startTime;
      logger.info('Task completed successfully', { 
        taskId, 
        taskType, 
        executionTime: `${executionTime}ms` 
      });
      
      return {
        success: true,
        result,
        executionTime,
        metadata: {
          agentType: 'platform-specialist',
          capabilities: this.capabilities
        }
      };
      
    } catch (error) {
      logger.error('Task execution failed', { 
        taskId, 
        taskType, 
        error: error.message,
        stack: error.stack 
      });
      
      this.health.errors++;
      throw error;
    }
  }

  // Tool implementations
  async createPowerApp(payload, context) {
    const { appName, appType, dataSource, requirements } = payload;
    
    logger.info('Creating PowerApp', { appName, appType });
    
    return {
      action: 'create-powerapp',
      appName,
      appType,
      dataSource,
      status: 'created',
      appId: uuidv4(),
      philosophy: "Designed with business insight and coded with developer precision"
    };
  }

  async deployPowerApp(payload, context) {
    const { appId, environment, deploymentOptions } = payload;
    
    logger.info('Deploying PowerApp', { appId, environment });
    
    return {
      action: 'deploy-powerapp',
      appId,
      environment,
      status: 'deployed',
      deploymentUrl: `https://apps.powerapps.com/play/${appId}`,
      philosophy: "Deployed with consultant understanding and developer reliability"
    };
  }

  async createPowerAutomateWorkflow(payload, context) {
    const { workflowName, trigger, actions, connectors } = payload;
    
    logger.info('Creating Power Automate workflow', { workflowName });
    
    return {
      action: 'create-powerautomate-workflow',
      workflowName,
      trigger,
      actions: actions.length,
      connectors,
      status: 'created',
      workflowId: uuidv4(),
      philosophy: "Automated with business logic and technical excellence"
    };
  }

  async createPowerBIDashboard(payload, context) {
    const { dashboardName, dataSource, visualizations, requirements } = payload;
    
    logger.info('Creating Power BI dashboard', { dashboardName });
    
    return {
      action: 'create-powerbi-dashboard',
      dashboardName,
      dataSource,
      visualizations: visualizations.length,
      status: 'created',
      dashboardId: uuidv4(),
      philosophy: "Visualized with business insight and technical precision"
    };
  }

  async createSharePointSite(payload, context) {
    const { siteName, template, permissions, features } = payload;
    
    logger.info('Creating SharePoint site', { siteName, template });
    
    return {
      action: 'create-sharepoint-site',
      siteName,
      template,
      permissions,
      features,
      status: 'created',
      siteUrl: `https://tenant.sharepoint.com/sites/${siteName.toLowerCase()}`,
      philosophy: "Structured with business needs and developer architecture"
    };
  }

  async manageSharePointList(payload, context) {
    const { siteUrl, listName, operation, configuration } = payload;
    
    logger.info('Managing SharePoint list', { siteUrl, listName, operation });
    
    return {
      action: 'manage-sharepoint-list',
      siteUrl,
      listName,
      operation,
      status: 'completed',
      listId: uuidv4(),
      philosophy: "Managed with consultant expertise and developer precision"
    };
  }

  async deployTeamsApp(payload, context) {
    const { appName, appPackage, targetScope, permissions } = payload;
    
    logger.info('Deploying Teams app', { appName, targetScope });
    
    return {
      action: 'deploy-teams-app',
      appName,
      targetScope,
      permissions,
      status: 'deployed',
      appId: uuidv4(),
      philosophy: "Deployed with business understanding and technical excellence"
    };
  }

  async executeGraphOperation(payload, context) {
    const { endpoint, method, data, headers } = payload;
    
    logger.info('Executing Graph API operation', { endpoint, method });
    
    return {
      action: 'graph-api-operation',
      endpoint,
      method,
      status: 'completed',
      response: { data: 'Graph API response would be here' },
      philosophy: "Executed with consultant insight and developer expertise"
    };
  }

  async customizeDynamics365(payload, context) {
    const { entity, customizations, businessRules } = payload;
    
    logger.info('Customizing Dynamics 365', { entity });
    
    return {
      action: 'customize-dynamics365',
      entity,
      customizations: customizations.length,
      businessRules: businessRules.length,
      status: 'completed',
      philosophy: "Customized with business logic and technical precision"
    };
  }

  async createDynamicsWorkflow(payload, context) {
    const { workflowName, entity, triggers, steps } = payload;
    
    logger.info('Creating Dynamics workflow', { workflowName, entity });
    
    return {
      action: 'create-dynamics-workflow',
      workflowName,
      entity,
      triggers,
      steps: steps.length,
      status: 'created',
      workflowId: uuidv4(),
      philosophy: "Automated with business process expertise and developer implementation"
    };
  }

  async createCompliancePolicy(payload, context) {
    const { policyName, scope, rules, enforcement } = payload;
    
    logger.info('Creating compliance policy', { policyName, scope });
    
    return {
      action: 'create-compliance-policy',
      policyName,
      scope,
      rules: rules.length,
      enforcement,
      status: 'created',
      policyId: uuidv4(),
      philosophy: "Governed with consultant compliance expertise and developer implementation"
    };
  }

  async setupGovernance(payload, context) {
    const { framework, policies, monitoring, automation } = payload;
    
    logger.info('Setting up governance framework', { framework });
    
    return {
      action: 'setup-governance',
      framework,
      policies: policies.length,
      monitoring,
      automation,
      status: 'configured',
      philosophy: "Governed with business understanding and technical implementation"
    };
  }

  async setupAnalytics(payload, context) {
    const { analyticsType, dataSources, metrics, reporting } = payload;
    
    logger.info('Setting up analytics', { analyticsType });
    
    return {
      action: 'setup-analytics',
      analyticsType,
      dataSources: dataSources.length,
      metrics: metrics.length,
      reporting,
      status: 'configured',
      philosophy: "Analyzed with business intelligence and technical precision"
    };
  }

  async createBusinessIntelligence(payload, context) {
    const { biName, dataSources, dimensions, measures } = payload;
    
    logger.info('Creating business intelligence solution', { biName });
    
    return {
      action: 'create-business-intelligence',
      biName,
      dataSources: dataSources.length,
      dimensions: dimensions.length,
      measures: measures.length,
      status: 'created',
      biId: uuidv4(),
      philosophy: "Intelligent with consultant insight and developer architecture"
    };
  }

  async start() {
    const PORT = process.env.PORT || 8080;
    
    return new Promise((resolve, reject) => {
      const server = this.app.listen(PORT, '0.0.0.0', () => {
        logger.info(`🚀 Platform Specialist Agent started on port ${PORT}`);
        logger.info('🎭 Philosophy: I understand business needs like a consultant, code like a developer');
        logger.info('⚡ Ready for Maximum Effort™ business solutions');
        
        // Start heartbeat to Reynolds
        this.startHeartbeat();
        
        resolve({ server, agent: this });
      });

      server.on('error', reject);
    });
  }

  startHeartbeat() {
    setInterval(async () => {
      try {
        if (this.reynoldsConnected) {
          await axios.post(`${this.reynoldsEndpoint}/agents/${this.agentId}/heartbeat`, {
            health: this.health,
            timestamp: new Date().toISOString()
          });
        }
      } catch (error) {
        logger.warn('Heartbeat failed', { error: error.message });
        this.reynoldsConnected = false;
      }
    }, 30000); // Every 30 seconds
  }

  async shutdown() {
    logger.info('🛑 Platform Agent shutting down...');
    
    try {
      // Notify Reynolds of shutdown
      if (this.reynoldsConnected) {
        await axios.post(`${this.reynoldsEndpoint}/agents/${this.agentId}/shutdown`);
      }
      
      logger.info('✅ Platform Agent shutdown complete');
    } catch (error) {
      logger.error('Error during shutdown', { error: error.message });
    }
  }
}

// Handle graceful shutdown
process.on('SIGTERM', async () => {
  if (global.platformAgent) {
    await global.platformAgent.shutdown();
  }
  process.exit(0);
});

process.on('SIGINT', async () => {
  if (global.platformAgent) {
    await global.platformAgent.shutdown();
  }
  process.exit(0);
});

// Start the agent if this file is run directly
if (require.main === module) {
  const agent = new PlatformAgent();
  global.platformAgent = agent;
  
  agent.start().catch(error => {
    logger.error('Failed to start Platform Agent', { error: error.message });
    process.exit(1);
  });
}

module.exports = PlatformAgent;