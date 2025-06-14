const express = require('express');
const { v4: uuidv4 } = require('uuid');
const logger = require('../utils/logger');

class McpServer {
  constructor(reynoldsOrchestrator) {
    this.reynolds = reynoldsOrchestrator;
    this.router = express.Router();
    this.tools = new Map();
    this.resources = new Map();
    this.capabilities = {
      tools: [],
      resources: [],
      prompts: []
    };
    
    this.initializeRoutes();
    this.registerTools();
    this.registerResources();
  }

  async initialize() {
    logger.info('🔧 Initializing Reynolds MCP Server...');
    
    // Initialize MCP protocol handlers
    await this.setupMcpProtocol();
    
    logger.info('✅ Reynolds MCP Server initialized successfully');
  }

  async setupMcpProtocol() {
    // Set up MCP protocol compliance
    this.capabilities = {
      tools: Array.from(this.tools.keys()),
      resources: Array.from(this.resources.keys()),
      prompts: [],
      sampling: {
        maxTokens: 4096,
        temperature: 0.7
      }
    };
  }

  initializeRoutes() {
    // MCP Protocol endpoints
    this.router.get('/capabilities', this.handleCapabilities.bind(this));
    this.router.post('/tools/:toolName', this.handleToolCall.bind(this));
    this.router.get('/tools', this.handleListTools.bind(this));
    this.router.get('/resources/:resourceUri', this.handleResourceAccess.bind(this));
    this.router.get('/resources', this.handleListResources.bind(this));
    
    // Reynolds-specific endpoints
    this.router.post('/orchestrate', this.handleOrchestrate.bind(this));
    this.router.get('/status', this.handleStatus.bind(this));
    this.router.get('/metrics', this.handleMetrics.bind(this));
    
    // WebSocket endpoint for real-time communication
    this.router.get('/events', this.handleEventStream.bind(this));
    
    logger.info('🛣️ MCP routes initialized');
  }

  registerTools() {
    // Core orchestration tools
    this.registerTool('orchestrate_task', {
      description: 'Orchestrate a complex task using parallel agent execution',
      parameters: {
        type: 'object',
        properties: {
          task: {
            type: 'object',
            properties: {
              type: { type: 'string', description: 'Type of task to orchestrate' },
              description: { type: 'string', description: 'Detailed task description' },
              components: { type: 'array', description: 'Task components or items' },
              deadline: { type: 'string', description: 'Task deadline (ISO 8601)' },
              priority: { type: 'string', enum: ['low', 'medium', 'high', 'critical'] }
            },
            required: ['type', 'description']
          },
          strategy: {
            type: 'object',
            properties: {
              approach: { type: 'string', enum: ['parallel_optimized', 'parallel_urgent', 'hybrid_intelligent', 'direct'] },
              maxConcurrency: { type: 'integer', minimum: 1, maximum: 10 }
            }
          }
        },
        required: ['task']
      },
      handler: this.handleOrchestateTask.bind(this)
    });

    this.registerTool('analyze_orchestration_potential', {
      description: 'Analyze whether a task should be orchestrated or executed directly',
      parameters: {
        type: 'object',
        properties: {
          task: {
            type: 'object',
            properties: {
              type: { type: 'string' },
              description: { type: 'string' },
              components: { type: 'array' },
              estimatedHours: { type: 'number' }
            },
            required: ['type', 'description']
          }
        },
        required: ['task']
      },
      handler: this.handleAnalyzeOrchestration.bind(this)
    });

    this.registerTool('get_agent_status', {
      description: 'Get status and health information for all agent pools',
      parameters: {
        type: 'object',
        properties: {
          agentType: { type: 'string', enum: ['devops', 'platform', 'code', 'all'] }
        }
      },
      handler: this.handleGetAgentStatus.bind(this)
    });

    this.registerTool('run_experiment', {
      description: 'Run a comparative experiment between different orchestration strategies',
      parameters: {
        type: 'object',
        properties: {
          scenario: {
            type: 'object',
            properties: {
              name: { type: 'string' },
              task: { type: 'object' },
              strategies: { type: 'array' },
              metrics: { type: 'array' }
            },
            required: ['name', 'task', 'strategies']
          }
        },
        required: ['scenario']
      },
      handler: this.handleRunExperiment.bind(this)
    });

    this.registerTool('get_reynolds_wisdom', {
      description: 'Get Reynolds\' wise and charming perspective on orchestration challenges',
      parameters: {
        type: 'object',
        properties: {
          situation: { type: 'string', description: 'Current situation or challenge' },
          context: { type: 'object', description: 'Additional context' }
        },
        required: ['situation']
      },
      handler: this.handleGetReynoldsWisdom.bind(this)
    });

    logger.info(`🔧 Registered ${this.tools.size} MCP tools`);
  }

  registerResources() {
    // Agent pool resources
    this.registerResource('agent-pools', {
      description: 'Current status of all agent pools',
      mimeType: 'application/json',
      handler: this.handleAgentPoolsResource.bind(this)
    });

    // Orchestration metrics
    this.registerResource('metrics', {
      description: 'Real-time orchestration metrics and performance data',
      mimeType: 'application/json',
      handler: this.handleMetricsResource.bind(this)
    });

    // Loop prevention status
    this.registerResource('loop-prevention', {
      description: 'Loop prevention engine status and confidence reports',
      mimeType: 'application/json',
      handler: this.handleLoopPreventionResource.bind(this)
    });

    // Reynolds personality insights
    this.registerResource('reynolds-personality', {
      description: 'Reynolds personality engine statistics and recent responses',
      mimeType: 'application/json',
      handler: this.handleReynoldsPersonalityResource.bind(this)
    });

    logger.info(`📚 Registered ${this.resources.size} MCP resources`);
  }

  registerTool(name, definition) {
    this.tools.set(name, definition);
    this.capabilities.tools.push({
      name,
      description: definition.description,
      inputSchema: definition.parameters
    });
  }

  registerResource(uri, definition) {
    this.resources.set(uri, definition);
    this.capabilities.resources.push({
      uri,
      description: definition.description,
      mimeType: definition.mimeType
    });
  }

  // Route handlers
  async handleCapabilities(req, res) {
    try {
      const response = {
        capabilities: this.capabilities,
        protocolVersion: '2024-11-05',
        serverInfo: {
          name: 'Reynolds Orchestrator MCP Server',
          version: '1.0.0',
          description: 'Supernatural orchestration with Maximum Effort™'
        }
      };

      res.json(response);
    } catch (error) {
      logger.error('Error handling capabilities request:', error);
      res.status(500).json({ error: 'Internal server error' });
    }
  }

  async handleToolCall(req, res) {
    const { toolName } = req.params;
    const { arguments: args } = req.body;

    try {
      const tool = this.tools.get(toolName);
      if (!tool) {
        return res.status(404).json({
          error: `Tool '${toolName}' not found`,
          availableTools: Array.from(this.tools.keys())
        });
      }

      logger.info(`🔧 Executing tool: ${toolName}`, { arguments: args });

      const result = await tool.handler(args);
      
      res.json({
        content: [
          {
            type: 'text',
            text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
          }
        ]
      });

    } catch (error) {
      logger.error(`Error executing tool '${toolName}':`, error);
      res.status(500).json({
        error: `Tool execution failed: ${error.message}`,
        toolName,
        reynoldsComment: "Even I can't fix everything, but this error definitely needs some Maximum Effort™ attention."
      });
    }
  }

  async handleListTools(req, res) {
    try {
      const tools = Array.from(this.tools.entries()).map(([name, definition]) => ({
        name,
        description: definition.description,
        inputSchema: definition.parameters
      }));

      res.json({ tools });
    } catch (error) {
      logger.error('Error listing tools:', error);
      res.status(500).json({ error: 'Internal server error' });
    }
  }

  async handleResourceAccess(req, res) {
    const { resourceUri } = req.params;

    try {
      const resource = this.resources.get(resourceUri);
      if (!resource) {
        return res.status(404).json({
          error: `Resource '${resourceUri}' not found`,
          availableResources: Array.from(this.resources.keys())
        });
      }

      const data = await resource.handler(req.query);
      
      res.set('Content-Type', resource.mimeType);
      res.json(data);

    } catch (error) {
      logger.error(`Error accessing resource '${resourceUri}':`, error);
      res.status(500).json({ error: 'Resource access failed' });
    }
  }

  async handleListResources(req, res) {
    try {
      const resources = Array.from(this.resources.entries()).map(([uri, definition]) => ({
        uri,
        description: definition.description,
        mimeType: definition.mimeType
      }));

      res.json({ resources });
    } catch (error) {
      logger.error('Error listing resources:', error);
      res.status(500).json({ error: 'Internal server error' });
    }
  }

  // Tool handlers
  async handleOrchestateTask(args) {
    const { task, strategy } = args;
    
    logger.info('🎭 Reynolds orchestrating task via MCP', { 
      taskType: task.type,
      strategy: strategy?.approach 
    });

    const result = await this.reynolds.routeTask(task);
    
    return {
      success: result.success,
      taskId: result.taskId,
      results: result.results,
      metrics: result.metrics,
      reynoldsComment: result.reynoldsComment,
      charmLevel: result.charmLevel,
      timestamp: new Date().toISOString()
    };
  }

  async handleAnalyzeOrchestration(args) {
    const { task } = args;
    
    const analysis = await this.reynolds.analyzeTaskForOrchestration(task);
    
    const personality = await this.reynolds.personality.generateTaskResponse({
      task,
      result: { success: true },
      executionTime: 0,
      strategy: analysis.strategy
    });

    return {
      shouldOrchestrate: analysis.shouldOrchestrate,
      confidence: analysis.confidence,
      strategy: analysis.strategy,
      signals: analysis.signals,
      recommendation: analysis.shouldOrchestrate ? 
        'Parallel orchestration recommended - this task has excellent parallelization potential!' :
        'Direct execution recommended - this task is better handled sequentially.',
      reynoldsAdvice: personality.message
    };
  }

  async handleGetAgentStatus(args) {
    const { agentType = 'all' } = args;
    
    const status = this.reynolds.getAgentPoolStatus();
    
    if (agentType === 'all') {
      return {
        allPools: status,
        summary: {
          totalAgents: status.reduce((sum, pool) => sum + pool.totalAgents, 0),
          healthyAgents: status.reduce((sum, pool) => sum + pool.healthyAgents, 0),
          averageUtilization: status.reduce((sum, pool) => sum + pool.utilization, 0) / status.length
        },
        reynoldsComment: "All agents are standing by, ready for some supernatural coordination. Maximum Effort™ mode is always engaged."
      };
    } else {
      const poolStatus = status.find(pool => pool.name === agentType);
      if (!poolStatus) {
        throw new Error(`Agent pool '${agentType}' not found`);
      }
      return poolStatus;
    }
  }

  async handleRunExperiment(args) {
    const { scenario } = args;
    
    // This would integrate with the experimentation framework
    // For now, return a mock experiment result
    const experimentId = uuidv4();
    
    logger.info(`🧪 Starting experiment: ${scenario.name}`, { experimentId });
    
    return {
      experimentId,
      status: 'running',
      scenario: scenario.name,
      estimatedDuration: '5-10 minutes',
      message: `Experiment '${scenario.name}' started with Maximum Effort™. Reynolds will coordinate the comparative analysis across multiple strategies.`,
      reynoldsComment: "Science! Let's see which approach comes out on top. My money's on parallel execution, but I'm always ready to be surprised."
    };
  }

  async handleGetReynoldsWisdom(args) {
    const { situation, context } = args;
    
    const wisdom = await this.reynolds.personality.generateTaskResponse({
      task: { type: 'wisdom_request', description: situation },
      result: { success: true },
      executionTime: 0,
      strategy: { approach: 'maximum_effort' }
    });

    return {
      situation,
      wisdom: wisdom.message,
      charmLevel: wisdom.charmLevel,
      encouragementFactor: wisdom.encouragementFactor,
      practicalAdvice: this.generatePracticalAdvice(situation),
      timestamp: new Date().toISOString()
    };
  }

  generatePracticalAdvice(situation) {
    const advice = {
      'orchestration challenge': 'Remember the MCP migration lesson: when you see multiple independent tasks, think parallel first, sequential second.',
      'agent coordination': 'Trust your agents - they\'re specialists for a reason. Give them clear tasks and let them work their magic.',
      'performance optimization': 'Measure everything, optimize the bottlenecks, and never sacrifice clarity for premature optimization.',
      'failure recovery': 'Failures are just unexpected learning opportunities. Log them, learn from them, and come back stronger.',
      'team communication': 'Clear communication prevents more bugs than perfect code. Keep everyone in the loop.'
    };

    for (const [key, value] of Object.entries(advice)) {
      if (situation.toLowerCase().includes(key.toLowerCase())) {
        return value;
      }
    }

    return 'When in doubt, apply Maximum Effort™ and trust the process. Orchestration is about coordination, not control.';
  }

  // Resource handlers
  async handleAgentPoolsResource(query) {
    return {
      agentPools: this.reynolds.getAgentPoolStatus(),
      timestamp: new Date().toISOString(),
      metadata: {
        totalPools: this.reynolds.agentPools.size,
        healthStatus: 'operational'
      }
    };
  }

  async handleMetricsResource(query) {
    return {
      performanceMetrics: this.reynolds.performanceMetrics,
      loopPrevention: this.reynolds.loopPrevention.getConfidenceReport(),
      timestamp: new Date().toISOString()
    };
  }

  async handleLoopPreventionResource(query) {
    return this.reynolds.loopPrevention.getSystemStatus();
  }

  async handleReynoldsPersonalityResource(query) {
    return this.reynolds.personality.getPersonalityStats();
  }

  // Additional handlers
  async handleOrchestrate(req, res) {
    try {
      const { task, strategy } = req.body;
      const result = await this.handleOrchestateTask({ task, strategy });
      res.json(result);
    } catch (error) {
      logger.error('Orchestration request failed:', error);
      res.status(500).json({ 
        error: error.message,
        reynoldsComment: "Well, that didn't go as planned. But hey, even supernatural orchestrators have their off days."
      });
    }
  }

  async handleStatus(req, res) {
    try {
      const status = {
        orchestrator: 'operational',
        agentPools: this.reynolds.getAgentPoolStatus(),
        loopPrevention: this.reynolds.loopPrevention.getSystemStatus(),
        personality: 'maximum_effort_engaged',
        uptime: process.uptime(),
        timestamp: new Date().toISOString()
      };

      res.json(status);
    } catch (error) {
      logger.error('Status request failed:', error);
      res.status(500).json({ error: 'Status check failed' });
    }
  }

  async handleMetrics(req, res) {
    try {
      const metrics = await this.handleMetricsResource();
      res.json(metrics);
    } catch (error) {
      logger.error('Metrics request failed:', error);
      res.status(500).json({ error: 'Metrics retrieval failed' });
    }
  }

  async handleEventStream(req, res) {
    // Set up Server-Sent Events for real-time updates
    res.writeHead(200, {
      'Content-Type': 'text/event-stream',
      'Cache-Control': 'no-cache',
      'Connection': 'keep-alive',
      'Access-Control-Allow-Origin': '*'
    });

    const eventId = uuidv4();
    
    // Send initial connection event
    res.write(`event: connected\n`);
    res.write(`id: ${eventId}\n`);
    res.write(`data: ${JSON.stringify({ 
      message: 'Connected to Reynolds Orchestrator event stream',
      timestamp: new Date().toISOString()
    })}\n\n`);

    // Set up event listeners for real-time updates
    const metricsInterval = setInterval(() => {
      if (res.destroyed) {
        clearInterval(metricsInterval);
        return;
      }

      const metrics = {
        agentPools: this.reynolds.getAgentPoolStatus(),
        performanceMetrics: this.reynolds.performanceMetrics,
        timestamp: new Date().toISOString()
      };

      res.write(`event: metrics\n`);
      res.write(`id: ${uuidv4()}\n`);
      res.write(`data: ${JSON.stringify(metrics)}\n\n`);
    }, 5000); // Every 5 seconds

    // Cleanup on client disconnect
    req.on('close', () => {
      clearInterval(metricsInterval);
      logger.debug('Event stream client disconnected');
    });
  }

  getRouter() {
    return this.router;
  }
}

module.exports = McpServer;