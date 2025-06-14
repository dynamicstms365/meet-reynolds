const EventEmitter = require('events');
const { v4: uuidv4 } = require('uuid');
const logger = require('../utils/logger');
const TaskDecomposer = require('./TaskDecomposer');
const AgentPool = require('./AgentPool');
const LoopPreventionEngine = require('./LoopPreventionEngine');
const ReynoldsPersonality = require('./ReynoldsPersonality');
const GitHubIssuesIntegration = require('../integrations/GitHubIssuesIntegration');
const DatabaseManager = require('../utils/DatabaseManager');
const RedisManager = require('../utils/RedisManager');

class ReynoldsOrchestrator extends EventEmitter {
  constructor(config) {
    super();
    this.config = {
      mode: 'production',
      agentPoolSize: 10,
      personalityMode: 'maximum_effort',
      loopPreventionEnabled: true,
      githubIssuesEnabled: true,
      metricsEnabled: true,
      ...config
    };

    // Core components
    this.taskDecomposer = new TaskDecomposer();
    
    // Initialize loop prevention with enhanced configuration
    const loopPreventionConfig = {
      confidenceThreshold: 0.999, // 99.9% requirement
      maxChainDepth: 10,
      maxExecutionTime: 300000, // 5 minutes
      reynoldsObsessionThreshold: 5,
      rapidFireThreshold: 20,
      recursivePatternThreshold: 3,
      fanOutThreshold: 10,
      metricsEnabled: this.config.metricsEnabled
    };
    
    this.loopPrevention = new LoopPreventionEngine(loopPreventionConfig);
    this.personality = new ReynoldsPersonality(this.config.personalityMode);
    this.agentPools = new Map();
    this.activeExecutions = new Map();
    this.performanceMetrics = {
      tasksExecuted: 0,
      parallelExecutionRatio: 0,
      averageOrchestrationTime: 0,
      agentUtilization: 0,
      successRate: 0
    };

    // Integrations
    this.githubIntegration = null;
    this.database = null;
    this.redis = null;

    // State management
    this.initialized = false;
    this.shuttingDown = false;
  }

  async initialize() {
    try {
      logger.info('🎭 Initializing Reynolds Orchestrator...');

      // Initialize database connection
      this.database = new DatabaseManager();
      await this.database.initialize();

      // Initialize Redis connection
      this.redis = new RedisManager();
      await this.redis.initialize();

      // Initialize GitHub integration if enabled
      if (this.config.githubIssuesEnabled) {
        this.githubIntegration = new GitHubIssuesIntegration();
        await this.githubIntegration.initialize();
      }

      // Initialize agent pools
      await this.initializeAgentPools();

      // Start health monitoring
      this.startHealthMonitoring();

      // Start performance metrics collection
      this.startMetricsCollection();

      // Set up loop prevention event listeners
      this.setupLoopPreventionIntegration();

      this.initialized = true;
      logger.info('✅ Reynolds Orchestrator initialized successfully');
      
      // Reynolds personality check-in
      const welcomeMessage = await this.personality.generateWelcomeMessage();
      logger.info(`🎭 Reynolds says: "${welcomeMessage}"`);
      
      // Log initial system status
      const systemStatus = this.loopPrevention.getSystemStatus();
      logger.info('🛡️ Loop Prevention System Status:', systemStatus);

    } catch (error) {
      logger.error('❌ Failed to initialize Reynolds Orchestrator:', error);
      throw error;
    }
  }

  async initializeAgentPools() {
    logger.info('🤖 Initializing agent pools...');

    // DevOps Agent Pool
    const devopsPool = new AgentPool('devops-polyglot', {
      minAgents: 1,
      maxAgents: 5,
      defaultAgents: 3,
      capabilities: [
        'github_operations',
        'cicd_management', 
        'infrastructure_deployment',
        'security_scanning',
        'container_orchestration'
      ]
    });

    // Platform Agent Pool
    const platformPool = new AgentPool('platform-specialist', {
      minAgents: 1,
      maxAgents: 3,
      defaultAgents: 2,
      capabilities: [
        'power_platform_deployment',
        'teams_integration',
        'm365_management',
        'business_process_automation',
        'compliance_validation'
      ]
    });

    // Code Intelligence Agent Pool
    const codePool = new AgentPool('code-intelligence', {
      minAgents: 1,
      maxAgents: 5,
      defaultAgents: 3,
      capabilities: [
        'code_generation',
        'code_analysis',
        'testing_automation',
        'documentation_generation',
        'refactoring_assistance'
      ]
    });

    this.agentPools.set('devops', devopsPool);
    this.agentPools.set('platform', platformPool);
    this.agentPools.set('code', codePool);

    logger.info(`✅ Initialized ${this.agentPools.size} agent pools`);
  }

  async routeTask(task) {
    try {
      if (!this.initialized) {
        throw new Error('Reynolds Orchestrator not initialized');
      }

      const taskId = uuidv4();
      task.id = taskId;
      task.startTime = Date.now();

      logger.info(`🎯 New task received: ${task.type}`, { taskId, task });

      // Apply orchestration decision matrix (lessons from MCP migration failure)
      const analysisResult = await this.analyzeTaskForOrchestration(task);
      
      let result;
      if (analysisResult.shouldOrchestrate) {
        logger.info(`⚡ Orchestrating task with ${analysisResult.strategy.approach} strategy`, { taskId });
        result = await this.executeOrchestrated(task, analysisResult.strategy);
      } else {
        logger.info(`🎯 Executing task directly`, { taskId });
        result = await this.executeDirect(task);
      }

      // Update metrics
      this.updatePerformanceMetrics(task, result, analysisResult);

      // Generate Reynolds personality response
      const personalityResponse = await this.personality.generateTaskResponse({
        task,
        result,
        executionTime: Date.now() - task.startTime,
        strategy: analysisResult.strategy
      });

      result.reynoldsComment = personalityResponse.message;
      result.charmLevel = personalityResponse.charmLevel;

      return result;

    } catch (error) {
      logger.error('❌ Task execution failed:', error);
      
      const failureResponse = await this.personality.generateFailureResponse(error);
      
      return {
        success: false,
        error: error.message,
        reynoldsComment: failureResponse.message,
        taskId: task.id
      };
    }
  }

  async analyzeTaskForOrchestration(task) {
    // Decision matrix framework from the MCP migration reflection
    const signals = {
      independentSubtasks: await this.countIndependentSubtasks(task),
      similarPatterns: await this.detectSimilarPatterns(task),
      externalResourcesAvailable: await this.checkExternalResources(),
      timeConstraints: task.deadline ? this.calculateTimeConstraints(task) : null,
      parallelizationPotential: await this.assessParallelization(task),
      complexity: this.calculateTaskComplexity(task)
    };

    // Orchestration decision logic
    const shouldOrchestrate = 
      signals.independentSubtasks >= 3 &&
      signals.similarPatterns > 0.7 &&
      signals.externalResourcesAvailable &&
      signals.parallelizationPotential > 0.6;

    const strategy = shouldOrchestrate ? 
      await this.planOrchestrationStrategy(signals) : 
      { approach: 'direct', reason: 'Below orchestration threshold' };

    const confidence = this.calculateConfidence(signals);

    logger.info('🧠 Task analysis complete', {
      taskId: task.id,
      signals,
      shouldOrchestrate,
      confidence,
      strategy: strategy.approach
    });

    return {
      shouldOrchestrate,
      strategy,
      confidence,
      signals
    };
  }

  async executeOrchestrated(task, strategy) {
    const executionId = uuidv4();
    this.activeExecutions.set(executionId, {
      taskId: task.id,
      strategy,
      startTime: Date.now(),
      status: 'decomposing'
    });

    try {
      // Create GitHub issues for tracking (Reynolds Issue Obsession™)
      let trackingIssues = {};
      if (this.githubIntegration) {
        trackingIssues = await this.createTrackingIssues(task, strategy);
      }

      // Decompose task into parallel subtasks
      this.activeExecutions.get(executionId).status = 'decomposing';
      const subtasks = await this.taskDecomposer.decompose(task, strategy);
      
      logger.info(`🔧 Decomposed task into ${subtasks.length} subtasks`, {
        taskId: task.id,
        executionId,
        subtasks: subtasks.map(st => ({ id: st.id, type: st.type, agent: st.suggestedAgent }))
      });

      // Execute in parallel with intelligent agent selection
      this.activeExecutions.get(executionId).status = 'executing';
      const executionPromises = subtasks.map(async (subtask) => {
        const agent = await this.selectOptimalAgent(subtask);
        const eventId = this.loopPrevention.generateEventId(null, `subtask_${subtask.type}`);

        // Start execution tracking in loop prevention
        this.loopPrevention.startExecution(executionId, eventId, {
          taskType: subtask.type,
          agentType: agent.type || 'unknown',
          strategy: strategy.approach,
          parentTaskId: task.id
        });

        try {
          const result = await this.executeSubtask(subtask, agent, {
            eventId,
            parentTask: task.id,
            executionId
          });

          // Update GitHub issue with progress
          if (trackingIssues[subtask.id] && this.githubIntegration) {
            await this.updateIssueProgress(trackingIssues[subtask.id], result);
          }

          return { subtask, result, agent: agent.id, success: true };

        } catch (error) {
          // Log failure pattern for learning
          await this.logFailurePattern(subtask, agent, error);
          return { subtask, result: null, agent: agent.id, success: false, error };
        }
      });

      const results = await Promise.allSettled(executionPromises);

      // Aggregate results with Reynolds charm
      this.activeExecutions.get(executionId).status = 'aggregating';
      const aggregatedResult = await this.aggregateResults(results, task, strategy);

      // Close tracking issues
      if (this.githubIntegration) {
        await this.closeTrackingIssues(trackingIssues, aggregatedResult);
      }

      this.activeExecutions.delete(executionId);
      return aggregatedResult;

    } catch (error) {
      this.activeExecutions.delete(executionId);
      throw error;
    }
  }

  async executeDirect(task) {
    const agent = await this.selectOptimalAgent(task);
    const eventId = this.loopPrevention.generateEventId();

    return await this.executeSubtask(task, agent, {
      eventId,
      parentTask: task.id
    });
  }

  async selectOptimalAgent(task) {
    const agentType = this.classifyTask(task);
    const pool = this.agentPools.get(agentType);

    if (!pool) {
      throw new Error(`No agent pool found for task type: ${agentType}`);
    }

    // Intelligent selection based on:
    // 1. Current load
    // 2. Historical success with similar tasks
    // 3. Capability match
    // 4. Health status

    const availableAgents = pool.getHealthyAgents();
    if (availableAgents.length === 0) {
      throw new Error(`No healthy agents available in ${agentType} pool`);
    }

    const scoredAgents = await Promise.all(
      availableAgents.map(async (agent) => ({
        agent,
        score: await this.calculateAgentScore(agent, task)
      }))
    );

    const selectedAgent = scoredAgents
      .sort((a, b) => b.score - a.score)[0]
      .agent;

    logger.info(`🤖 Selected agent: ${selectedAgent.id}`, {
      taskType: task.type,
      agentPool: agentType,
      score: scoredAgents.find(sa => sa.agent === selectedAgent)?.score
    });

    return selectedAgent;
  }

  classifyTask(task) {
    // Task classification logic to determine appropriate agent pool
    const taskType = task.type?.toLowerCase() || '';
    const taskDescription = (task.description || '').toLowerCase();

    // DevOps patterns
    if (taskType.includes('deploy') || 
        taskType.includes('cicd') || 
        taskType.includes('infrastructure') ||
        taskDescription.includes('github') ||
        taskDescription.includes('pipeline') ||
        taskDescription.includes('container')) {
      return 'devops';
    }

    // Platform patterns
    if (taskType.includes('power') || 
        taskType.includes('platform') || 
        taskType.includes('teams') ||
        taskType.includes('m365') ||
        taskDescription.includes('business process') ||
        taskDescription.includes('power platform')) {
      return 'platform';
    }

    // Code patterns
    if (taskType.includes('code') || 
        taskType.includes('develop') || 
        taskType.includes('test') ||
        taskType.includes('refactor') ||
        taskDescription.includes('programming') ||
        taskDescription.includes('software')) {
      return 'code';
    }

    // Default to devops for unclassified tasks
    return 'devops';
  }

  async executeSubtask(subtask, agent, context) {
    // This would integrate with the MCP client to call agent tools
    // For now, return a mock implementation
    return {
      success: true,
      data: `Subtask ${subtask.id} executed by agent ${agent.id}`,
      executionTime: Math.random() * 5000 + 1000, // 1-6 seconds
      agent: agent.id
    };
  }

  async calculateAgentScore(agent, task) {
    // Agent scoring algorithm considering multiple factors
    let score = 0;

    // Base capability match (40% weight)
    const capabilityMatch = this.calculateCapabilityMatch(agent, task);
    score += capabilityMatch * 40;

    // Current load (30% weight) - lower load = higher score
    const loadScore = Math.max(0, 100 - agent.currentLoad);
    score += loadScore * 0.3;

    // Historical success rate (20% weight)
    const historicalSuccess = agent.successRate || 0.8;
    score += historicalSuccess * 20;

    // Health status (10% weight)
    const healthScore = agent.status === 'healthy' ? 100 : 0;
    score += healthScore * 0.1;

    return score;
  }

  calculateCapabilityMatch(agent, task) {
    // Calculate how well agent capabilities match task requirements
    if (!task.requiredCapabilities || !agent.capabilities) {
      return 0.5; // Default score if no capability data
    }

    const matches = task.requiredCapabilities.filter(req => 
      agent.capabilities.includes(req)
    ).length;

    return (matches / task.requiredCapabilities.length) * 100;
  }

  async countIndependentSubtasks(task) {
    // Analyze task to count independent subtasks
    return this.taskDecomposer.estimateSubtaskCount(task);
  }

  async detectSimilarPatterns(task) {
    // Check for similar patterns in historical data
    if (this.database) {
      return await this.database.findSimilarPatterns(task);
    }
    return 0.5; // Default similarity score
  }

  async checkExternalResources() {
    // Check availability of external resources (GitHub, agents, etc.)
    const resources = [
      this.agentPools.size > 0,
      this.githubIntegration?.isHealthy() || false,
      this.database?.isConnected() || false,
      this.redis?.isConnected() || false
    ];

    return resources.filter(Boolean).length / resources.length > 0.5;
  }

  calculateTimeConstraints(task) {
    if (!task.deadline) return null;
    
    const now = Date.now();
    const deadline = new Date(task.deadline).getTime();
    const timeAvailable = deadline - now;

    return {
      timeAvailable,
      urgency: timeAvailable < 3600000 ? 'high' : timeAvailable < 86400000 ? 'medium' : 'low'
    };
  }

  async assessParallelization(task) {
    // Assess how much of the task can be parallelized
    return this.taskDecomposer.assessParallelizationPotential(task);
  }

  calculateTaskComplexity(task) {
    // Calculate task complexity based on various factors
    let complexity = 0;

    // Factor in number of components
    if (task.components && Array.isArray(task.components)) {
      complexity += task.components.length * 0.1;
    }

    // Factor in dependencies
    if (task.dependencies && Array.isArray(task.dependencies)) {
      complexity += task.dependencies.length * 0.15;
    }

    // Factor in estimated effort
    if (task.estimatedHours) {
      complexity += Math.min(task.estimatedHours / 10, 1); // Cap at 1.0
    }

    return Math.min(complexity, 1.0); // Normalize to 0-1 scale
  }

  async planOrchestrationStrategy(signals) {
    // Plan orchestration strategy based on analysis signals
    if (signals.parallelizationPotential > 0.8 && signals.independentSubtasks > 5) {
      return {
        approach: 'parallel_optimized',
        reason: 'High parallelization potential with many independent subtasks',
        maxConcurrency: Math.min(signals.independentSubtasks, 5)
      };
    } else if (signals.timeConstraints?.urgency === 'high') {
      return {
        approach: 'parallel_urgent',
        reason: 'Time constraints require maximum parallel execution',
        maxConcurrency: this.config.agentPoolSize
      };
    } else {
      return {
        approach: 'hybrid_intelligent',
        reason: 'Balanced approach with intelligent dependency management',
        maxConcurrency: 3
      };
    }
  }

  calculateConfidence(signals) {
    // Calculate confidence in orchestration decision
    const weights = {
      independentSubtasks: 0.3,
      similarPatterns: 0.2,
      externalResourcesAvailable: 0.2,
      parallelizationPotential: 0.3
    };

    let confidence = 0;
    
    confidence += Math.min(signals.independentSubtasks / 5, 1) * weights.independentSubtasks;
    confidence += signals.similarPatterns * weights.similarPatterns;
    confidence += (signals.externalResourcesAvailable ? 1 : 0) * weights.externalResourcesAvailable;
    confidence += signals.parallelizationPotential * weights.parallelizationPotential;

    return confidence;
  }

  async aggregateResults(results, task, strategy) {
    const successResults = results
      .filter(r => r.status === 'fulfilled' && r.value.success)
      .map(r => r.value);
    
    const failures = results
      .filter(r => r.status === 'rejected' || (r.status === 'fulfilled' && !r.value.success))
      .map(r => r.status === 'rejected' ? r.reason : r.value.error);

    const totalExecutionTime = Date.now() - task.startTime;

    return {
      success: failures.length === 0,
      taskId: task.id,
      results: successResults,
      failures: failures,
      metrics: {
        totalExecutionTime,
        parallelExecutionRatio: successResults.length / results.length,
        orchestrationOverhead: this.calculateOrchestrationOverhead(task, totalExecutionTime),
        agentUtilization: this.calculateCurrentAgentUtilization(),
        strategy: strategy.approach
      }
    };
  }

  calculateOrchestrationOverhead(task, totalTime) {
    // Estimate what direct execution would have taken
    const estimatedDirectTime = task.estimatedHours ? task.estimatedHours * 3600000 : totalTime * 2;
    return Math.max(0, (totalTime - estimatedDirectTime) / estimatedDirectTime);
  }

  calculateCurrentAgentUtilization() {
    let totalAgents = 0;
    let busyAgents = 0;

    for (const pool of this.agentPools.values()) {
      const agents = pool.getAllAgents();
      totalAgents += agents.length;
      busyAgents += agents.filter(a => a.currentLoad > 0).length;
    }

    return totalAgents > 0 ? busyAgents / totalAgents : 0;
  }

  updatePerformanceMetrics(task, result, analysis) {
    this.performanceMetrics.tasksExecuted++;
    
    if (result.success) {
      this.performanceMetrics.successRate = 
        (this.performanceMetrics.successRate * (this.performanceMetrics.tasksExecuted - 1) + 1) / 
        this.performanceMetrics.tasksExecuted;
    } else {
      this.performanceMetrics.successRate = 
        (this.performanceMetrics.successRate * (this.performanceMetrics.tasksExecuted - 1)) / 
        this.performanceMetrics.tasksExecuted;
    }

    if (result.metrics) {
      this.performanceMetrics.parallelExecutionRatio = 
        (this.performanceMetrics.parallelExecutionRatio + (result.metrics.parallelExecutionRatio || 0)) / 2;
      
      this.performanceMetrics.agentUtilization = 
        (this.performanceMetrics.agentUtilization + (result.metrics.agentUtilization || 0)) / 2;
    }
  }

  startHealthMonitoring() {
    setInterval(async () => {
      try {
        await this.performHealthChecks();
      } catch (error) {
        logger.error('Health check failed:', error);
      }
    }, 30000); // Every 30 seconds
  }

  async performHealthChecks() {
    // Check agent pool health
    for (const [poolName, pool] of this.agentPools) {
      const healthyAgents = pool.getHealthyAgents();
      const totalAgents = pool.getAllAgents();
      
      if (healthyAgents.length / totalAgents.length < 0.5) {
        logger.warn(`⚠️ Agent pool ${poolName} health degraded: ${healthyAgents.length}/${totalAgents.length} healthy`);
      }
    }

    // Check database connection
    if (this.database && !this.database.isConnected()) {
      logger.warn('⚠️ Database connection lost');
      await this.database.reconnect();
    }

    // Check Redis connection
    if (this.redis && !this.redis.isConnected()) {
      logger.warn('⚠️ Redis connection lost');
      await this.redis.reconnect();
    }
  }

  startMetricsCollection() {
    setInterval(() => {
      this.publishMetrics();
    }, 60000); // Every minute
  }

  publishMetrics() {
    const metrics = {
      ...this.performanceMetrics,
      timestamp: new Date().toISOString(),
      activeExecutions: this.activeExecutions.size,
      agentPools: Array.from(this.agentPools.entries()).map(([name, pool]) => ({
        name,
        totalAgents: pool.getAllAgents().length,
        healthyAgents: pool.getHealthyAgents().length,
        utilization: pool.getUtilization()
      }))
    };

    this.emit('metrics', metrics);
    logger.debug('📊 Metrics published', metrics);
  }

  getAgentPoolStatus() {
    return Array.from(this.agentPools.entries()).map(([name, pool]) => ({
      name,
      totalAgents: pool.getAllAgents().length,
      healthyAgents: pool.getHealthyAgents().length,
      capabilities: pool.getCapabilities(),
      utilization: pool.getUtilization()
    }));
  }

  async createTrackingIssues(task, strategy) {
    // Create GitHub issues for tracking orchestration progress
    if (!this.githubIntegration) return {};

    const masterIssue = await this.githubIntegration.createIssue({
      title: `🎭 Reynolds Orchestration: ${task.type}`,
      body: `**Task ID**: ${task.id}\n**Strategy**: ${strategy.approach}\n**Status**: In Progress\n\n${task.description || 'No description provided'}`,
      labels: ['reynolds-orchestration', 'in-progress']
    });

    return { master: masterIssue };
  }

  async updateIssueProgress(issue, result) {
    if (!this.githubIntegration) return;

    await this.githubIntegration.updateIssue(issue.number, {
      body: issue.body + `\n\n**Update**: ${result.success ? '✅ Completed' : '❌ Failed'} at ${new Date().toISOString()}`
    });
  }

  async closeTrackingIssues(issues, result) {
    if (!this.githubIntegration) return;

    for (const issue of Object.values(issues)) {
      await this.githubIntegration.updateIssue(issue.number, {
        state: 'closed',
        labels: result.success ? ['reynolds-orchestration', 'completed'] : ['reynolds-orchestration', 'failed']
      });
    }
  }

  async logFailurePattern(subtask, agent, error) {
    // Log failure patterns for learning
    const failureData = {
      subtaskType: subtask.type,
      agentId: agent.id,
      error: error.message,
      timestamp: new Date().toISOString()
    };

    if (this.database) {
      await this.database.logFailurePattern(failureData);
    }

    logger.warn('❌ Failure pattern logged', failureData);
  }

  async shutdown() {
    logger.info('🛑 Shutting down Reynolds Orchestrator...');
    this.shuttingDown = true;

    // Stop health monitoring and metrics collection
    clearInterval(this.healthMonitorInterval);
    clearInterval(this.metricsInterval);

    // Shutdown loop prevention engine
    if (this.loopPrevention) {
      this.loopPrevention.shutdown();
    }

    // Wait for active executions to complete (with timeout)
    const shutdownPromise = this.waitForActiveExecutions();
    const timeoutPromise = new Promise(resolve => setTimeout(resolve, 25000));
    
    await Promise.race([shutdownPromise, timeoutPromise]);

    // Close connections
    if (this.database) {
      await this.database.close();
    }
    if (this.redis) {
      await this.redis.close();
    }

    logger.info('✅ Reynolds Orchestrator shutdown complete');
  }

  async waitForActiveExecutions() {
    while (this.activeExecutions.size > 0) {
      logger.info(`⏳ Waiting for ${this.activeExecutions.size} active executions to complete...`);
      await new Promise(resolve => setTimeout(resolve, 1000));
    }
  }
}

module.exports = ReynoldsOrchestrator;