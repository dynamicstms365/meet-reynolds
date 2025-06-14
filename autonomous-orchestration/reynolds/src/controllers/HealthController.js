const express = require('express');
const logger = require('../utils/logger');

class HealthController {
  constructor(reynoldsOrchestrator) {
    this.reynolds = reynoldsOrchestrator;
    this.router = express.Router();
    this.initializeRoutes();
  }

  initializeRoutes() {
    this.router.get('/', this.getOverallHealth.bind(this));
    this.router.get('/detailed', this.getDetailedHealth.bind(this));
    this.router.get('/agents', this.getAgentHealth.bind(this));
    this.router.get('/loop-prevention', this.getLoopPreventionHealth.bind(this));
    this.router.get('/database', this.getDatabaseHealth.bind(this));
    this.router.get('/redis', this.getRedisHealth.bind(this));
    this.router.get('/metrics', this.getHealthMetrics.bind(this));
  }

  async getOverallHealth(req, res) {
    try {
      const health = {
        status: 'healthy',
        timestamp: new Date().toISOString(),
        uptime: process.uptime(),
        version: '1.0.0',
        components: {}
      };

      // Check orchestrator
      health.components.orchestrator = {
        status: this.reynolds.initialized ? 'healthy' : 'unhealthy',
        initialized: this.reynolds.initialized
      };

      // Check agent pools
      const agentPools = this.reynolds.getAgentPoolStatus();
      const totalAgents = agentPools.reduce((sum, pool) => sum + pool.totalAgents, 0);
      const healthyAgents = agentPools.reduce((sum, pool) => sum + pool.healthyAgents, 0);
      
      health.components.agents = {
        status: healthyAgents > 0 ? 'healthy' : 'degraded',
        totalAgents,
        healthyAgents,
        pools: agentPools.length
      };

      // Check loop prevention
      const loopPreventionStatus = this.reynolds.loopPrevention.getSystemStatus();
      health.components.loopPrevention = {
        status: loopPreventionStatus.status,
        confidence: loopPreventionStatus.confidence,
        activeExecutions: loopPreventionStatus.activeExecutions
      };

      // Check database
      if (this.reynolds.database) {
        const dbHealth = await this.reynolds.database.healthCheck();
        health.components.database = dbHealth;
      } else {
        health.components.database = { status: 'not_configured' };
      }

      // Check Redis
      if (this.reynolds.redis) {
        const redisHealth = await this.reynolds.redis.healthCheck();
        health.components.redis = redisHealth;
      } else {
        health.components.redis = { status: 'not_configured' };
      }

      // Determine overall status
      const componentStatuses = Object.values(health.components).map(c => c.status);
      if (componentStatuses.includes('unhealthy')) {
        health.status = 'unhealthy';
      } else if (componentStatuses.includes('degraded')) {
        health.status = 'degraded';
      }

      // Add Reynolds personality response
      health.reynoldsComment = this.generateHealthComment(health.status);

      const statusCode = health.status === 'healthy' ? 200 : 
                        health.status === 'degraded' ? 207 : 503;

      res.status(statusCode).json(health);

    } catch (error) {
      logger.error('Health check failed:', error);
      res.status(500).json({
        status: 'error',
        error: error.message,
        timestamp: new Date().toISOString(),
        reynoldsComment: "Well, that's not supposed to happen. Even I need a health check sometimes."
      });
    }
  }

  async getDetailedHealth(req, res) {
    try {
      const detailedHealth = {
        timestamp: new Date().toISOString(),
        uptime: process.uptime(),
        memory: process.memoryUsage(),
        cpu: process.cpuUsage(),
        orchestrator: {
          initialized: this.reynolds.initialized,
          shuttingDown: this.reynolds.shuttingDown,
          performanceMetrics: this.reynolds.performanceMetrics,
          activeExecutions: this.reynolds.activeExecutions.size
        },
        agentPools: this.reynolds.getAgentPoolStatus(),
        loopPrevention: this.reynolds.loopPrevention.getConfidenceReport(),
        personality: this.reynolds.personality.getPersonalityStats()
      };

      // Add database details if available
      if (this.reynolds.database && this.reynolds.database.isConnected()) {
        detailedHealth.database = await this.reynolds.database.healthCheck();
      }

      // Add Redis details if available
      if (this.reynolds.redis && this.reynolds.redis.isConnected()) {
        detailedHealth.redis = await this.reynolds.redis.healthCheck();
      }

      res.json(detailedHealth);

    } catch (error) {
      logger.error('Detailed health check failed:', error);
      res.status(500).json({
        error: error.message,
        timestamp: new Date().toISOString()
      });
    }
  }

  async getAgentHealth(req, res) {
    try {
      const agentHealth = {
        timestamp: new Date().toISOString(),
        pools: this.reynolds.getAgentPoolStatus()
      };

      // Add detailed agent information
      for (const pool of agentHealth.pools) {
        const poolInstance = this.reynolds.agentPools.get(pool.type);
        if (poolInstance) {
          pool.detailedAgents = poolInstance.getAllAgents().map(agent => ({
            id: agent.id,
            status: agent.status,
            currentLoad: agent.currentLoad,
            maxLoad: agent.maxLoad,
            successRate: agent.successRate,
            lastHealthCheck: agent.lastHealthCheck,
            capabilities: agent.capabilities,
            totalTasksCompleted: agent.metadata.totalTasksCompleted
          }));
        }
      }

      // Calculate summary statistics
      const summary = {
        totalPools: agentHealth.pools.length,
        totalAgents: agentHealth.pools.reduce((sum, pool) => sum + pool.totalAgents, 0),
        healthyAgents: agentHealth.pools.reduce((sum, pool) => sum + pool.healthyAgents, 0),
        averageUtilization: agentHealth.pools.reduce((sum, pool) => sum + pool.utilization, 0) / agentHealth.pools.length,
        overallStatus: agentHealth.pools.every(pool => pool.healthyAgents > 0) ? 'healthy' : 'degraded'
      };

      agentHealth.summary = summary;
      agentHealth.reynoldsComment = `${summary.healthyAgents} agents standing by and ready for some supernatural coordination. Maximum Effort™ mode is ${summary.overallStatus === 'healthy' ? 'fully' : 'partially'} engaged.`;

      res.json(agentHealth);

    } catch (error) {
      logger.error('Agent health check failed:', error);
      res.status(500).json({
        error: error.message,
        timestamp: new Date().toISOString()
      });
    }
  }

  async getLoopPreventionHealth(req, res) {
    try {
      const loopPreventionHealth = {
        timestamp: new Date().toISOString(),
        systemStatus: this.reynolds.loopPrevention.getSystemStatus(),
        confidenceReport: this.reynolds.loopPrevention.getConfidenceReport(),
        thresholds: {
          confidenceThreshold: this.reynolds.loopPrevention.confidenceThreshold,
          maxChainDepth: this.reynolds.loopPrevention.maxChainDepth,
          maxExecutionTime: this.reynolds.loopPrevention.maxExecutionTime
        }
      };

      // Add Reynolds wisdom about loop prevention
      loopPreventionHealth.reynoldsWisdom = "Loop prevention is like wearing a seatbelt - you don't think about it until you need it. And trust me, with 99.9% confidence tracking, we're not taking any chances.";

      res.json(loopPreventionHealth);

    } catch (error) {
      logger.error('Loop prevention health check failed:', error);
      res.status(500).json({
        error: error.message,
        timestamp: new Date().toISOString()
      });
    }
  }

  async getDatabaseHealth(req, res) {
    try {
      if (!this.reynolds.database) {
        return res.json({
          status: 'not_configured',
          message: 'Database is not configured',
          timestamp: new Date().toISOString()
        });
      }

      const dbHealth = await this.reynolds.database.healthCheck();
      dbHealth.timestamp = new Date().toISOString();

      res.json(dbHealth);

    } catch (error) {
      logger.error('Database health check failed:', error);
      res.status(500).json({
        status: 'unhealthy',
        error: error.message,
        timestamp: new Date().toISOString()
      });
    }
  }

  async getRedisHealth(req, res) {
    try {
      if (!this.reynolds.redis) {
        return res.json({
          status: 'not_configured',
          message: 'Redis is not configured',
          timestamp: new Date().toISOString()
        });
      }

      const redisHealth = await this.reynolds.redis.healthCheck();
      redisHealth.timestamp = new Date().toISOString();

      res.json(redisHealth);

    } catch (error) {
      logger.error('Redis health check failed:', error);
      res.status(500).json({
        status: 'unhealthy',
        error: error.message,
        timestamp: new Date().toISOString()
      });
    }
  }

  async getHealthMetrics(req, res) {
    try {
      const metrics = {
        timestamp: new Date().toISOString(),
        uptime: process.uptime(),
        memory: process.memoryUsage(),
        orchestration: this.reynolds.performanceMetrics,
        agentUtilization: this.reynolds.calculateCurrentAgentUtilization(),
        loopPrevention: this.reynolds.loopPrevention.getConfidenceReport(),
        systemLoad: {
          activeExecutions: this.reynolds.activeExecutions.size,
          totalAgentPools: this.reynolds.agentPools.size
        }
      };

      // Add historical performance if database is available
      if (this.reynolds.database && this.reynolds.database.isConnected()) {
        try {
          metrics.historical = await this.reynolds.database.getOrchestrationInsights('24 hours');
        } catch (error) {
          logger.warn('Failed to get historical metrics:', error);
        }
      }

      res.json(metrics);

    } catch (error) {
      logger.error('Health metrics check failed:', error);
      res.status(500).json({
        error: error.message,
        timestamp: new Date().toISOString()
      });
    }
  }

  generateHealthComment(status) {
    const comments = {
      healthy: [
        "All systems operational and running with supernatural efficiency. Maximum Effort™ mode is fully engaged.",
        "Everything's running smoother than a well-choreographed action sequence. I'd say I'm impressed with myself, but that would be redundant.",
        "Health status: Supernaturally excellent. Like my healing factor, but for software systems.",
        "All green lights across the board. Even I'm impressed, and that's saying something."
      ],
      degraded: [
        "Some components are having a rough day, but nothing Reynolds can't handle with a little Maximum Effort™.",
        "Partial systems operational. It's like having one hand tied behind my back - challenging, but not impossible.",
        "We're running at reduced capacity, but still better than most systems at full power. That's just how we roll.",
        "Some minor hiccups, but nothing that can't be fixed with a little supernatural intervention."
      ],
      unhealthy: [
        "Well, this is awkward. Some systems are down, but don't worry - I've dealt with worse. Recovery mode: activated.",
        "Houston, we have problems. But hey, every good story needs a crisis to overcome. Maximum Effort™ recovery in progress.",
        "Systems are struggling, but failure is just success taking the scenic route. Time for some heroic troubleshooting.",
        "This is why we have backup plans for our backup plans. Reynolds-style problem solving: engaged."
      ]
    };

    const statusComments = comments[status] || comments.unhealthy;
    return statusComments[Math.floor(Math.random() * statusComments.length)];
  }

  getRouter() {
    return this.router;
  }
}

module.exports = HealthController;