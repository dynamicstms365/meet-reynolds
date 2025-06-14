const EventEmitter = require('events');
const { v4: uuidv4 } = require('uuid');
const logger = require('../utils/logger');

class AgentPool extends EventEmitter {
  constructor(agentType, config) {
    super();
    this.agentType = agentType;
    this.config = {
      minAgents: 1,
      maxAgents: 5,
      defaultAgents: 2,
      capabilities: [],
      healthCheckInterval: 30000,
      loadBalancingStrategy: 'least_busy',
      ...config
    };
    
    this.agents = new Map();
    this.loadBalancer = new LoadBalancer(this.config.loadBalancingStrategy);
    this.healthCheckInterval = null;
    this.metrics = {
      totalRequests: 0,
      successfulRequests: 0,
      failedRequests: 0,
      averageResponseTime: 0,
      currentUtilization: 0
    };
    
    this.initialized = false;
  }

  async initialize() {
    logger.info(`🤖 Initializing ${this.agentType} agent pool...`);
    
    try {
      // Discover and register available agents
      await this.discoverAgents();
      
      // Start health monitoring
      this.startHealthMonitoring();
      
      // Initialize metrics collection
      this.initializeMetrics();
      
      this.initialized = true;
      logger.info(`✅ ${this.agentType} agent pool initialized with ${this.agents.size} agents`);
      
    } catch (error) {
      logger.error(`❌ Failed to initialize ${this.agentType} agent pool:`, error);
      throw error;
    }
  }

  async discoverAgents() {
    // In a real implementation, this would discover agents via service discovery
    // For now, we'll simulate agent registration based on Docker service discovery
    
    const defaultAgentCount = this.config.defaultAgents;
    
    for (let i = 0; i < defaultAgentCount; i++) {
      const agent = await this.createAgentInstance(i);
      this.registerAgent(agent);
    }
  }

  async createAgentInstance(index) {
    const agentId = `${this.agentType}-${index + 1}`;
    
    return {
      id: agentId,
      type: this.agentType,
      status: 'healthy',
      endpoint: `http://${this.agentType}-agent:8080`, // Docker service name
      capabilities: [...this.config.capabilities],
      currentLoad: 0,
      maxLoad: 10,
      successRate: 1.0,
      lastHealthCheck: new Date(),
      metadata: {
        version: '1.0.0',
        startTime: new Date(),
        totalTasksCompleted: 0,
        specializations: this.getAgentSpecializations()
      },
      // Performance metrics
      metrics: {
        averageExecutionTime: 0,
        taskQueue: [],
        resourceUsage: {
          cpu: 0,
          memory: 0
        }
      }
    };
  }

  getAgentSpecializations() {
    switch (this.agentType) {
      case 'devops-polyglot':
        return [
          'github_operations',
          'azure_deployment',
          'docker_orchestration',
          'cicd_pipelines',
          'infrastructure_management'
        ];
      case 'platform-specialist':
        return [
          'power_platform_development',
          'teams_integration',
          'm365_administration',
          'business_process_automation',
          'compliance_management'
        ];
      case 'code-intelligence':
        return [
          'multi_language_development',
          'code_analysis',
          'testing_automation',
          'documentation_generation',
          'refactoring_assistance'
        ];
      default:
        return [];
    }
  }

  registerAgent(agent) {
    this.agents.set(agent.id, agent);
    logger.info(`🤖 Registered agent: ${agent.id}`, {
      type: agent.type,
      capabilities: agent.capabilities,
      endpoint: agent.endpoint
    });
    
    this.emit('agentRegistered', agent);
  }

  unregisterAgent(agentId) {
    const agent = this.agents.get(agentId);
    if (agent) {
      this.agents.delete(agentId);
      logger.warn(`🚫 Unregistered agent: ${agentId}`, { reason: 'health_check_failed' });
      this.emit('agentUnregistered', agent);
    }
  }

  getAllAgents() {
    return Array.from(this.agents.values());
  }

  getHealthyAgents() {
    return this.getAllAgents().filter(agent => agent.status === 'healthy');
  }

  getAgentById(agentId) {
    return this.agents.get(agentId);
  }

  getByCapability(capability) {
    return this.getHealthyAgents().filter(agent => 
      agent.capabilities.includes(capability)
    );
  }

  getLeastBusy() {
    const healthyAgents = this.getHealthyAgents();
    
    if (healthyAgents.length === 0) {
      throw new Error(`No healthy agents available in ${this.agentType} pool`);
    }

    return healthyAgents
      .sort((a, b) => a.currentLoad - b.currentLoad)[0];
  }

  getMostCapable(requiredCapabilities) {
    const healthyAgents = this.getHealthyAgents();
    
    if (!requiredCapabilities || requiredCapabilities.length === 0) {
      return this.getLeastBusy();
    }

    const scoredAgents = healthyAgents.map(agent => ({
      agent,
      score: this.calculateCapabilityScore(agent, requiredCapabilities)
    }));

    return scoredAgents
      .filter(sa => sa.score > 0)
      .sort((a, b) => b.score - a.score)[0]?.agent || this.getLeastBusy();
  }

  calculateCapabilityScore(agent, requiredCapabilities) {
    const matchingCapabilities = requiredCapabilities.filter(cap => 
      agent.capabilities.includes(cap)
    );
    
    if (matchingCapabilities.length === 0) return 0;
    
    // Base score from capability match ratio
    let score = (matchingCapabilities.length / requiredCapabilities.length) * 100;
    
    // Bonus for specializations
    const matchingSpecializations = requiredCapabilities.filter(cap =>
      agent.metadata.specializations.includes(cap)
    );
    score += matchingSpecializations.length * 10;
    
    // Penalty for high load
    score -= agent.currentLoad * 5;
    
    // Bonus for high success rate
    score += agent.successRate * 20;
    
    return Math.max(score, 0);
  }

  async selectOptimalAgent(task) {
    const selectionStrategy = this.config.loadBalancingStrategy;
    
    switch (selectionStrategy) {
      case 'least_busy':
        return this.getLeastBusy();
      case 'most_capable':
        return this.getMostCapable(task.requiredCapabilities);
      case 'round_robin':
        return this.loadBalancer.selectRoundRobin(this.getHealthyAgents());
      case 'weighted':
        return this.loadBalancer.selectWeighted(this.getHealthyAgents(), task);
      default:
        return this.getLeastBusy();
    }
  }

  async assignTask(agentId, task) {
    const agent = this.getAgentById(agentId);
    
    if (!agent) {
      throw new Error(`Agent ${agentId} not found in pool`);
    }
    
    if (agent.status !== 'healthy') {
      throw new Error(`Agent ${agentId} is not healthy (status: ${agent.status})`);
    }
    
    if (agent.currentLoad >= agent.maxLoad) {
      throw new Error(`Agent ${agentId} is at maximum capacity`);
    }

    // Increment load
    agent.currentLoad++;
    agent.metrics.taskQueue.push({
      taskId: task.id,
      assignedAt: new Date(),
      estimatedDuration: task.estimatedDurationMs
    });

    // Update metrics
    this.metrics.totalRequests++;
    
    logger.info(`📋 Task assigned to agent: ${agentId}`, {
      taskId: task.id,
      taskType: task.type,
      agentLoad: agent.currentLoad,
      poolUtilization: this.getUtilization()
    });

    this.emit('taskAssigned', { agent, task });
    
    return agent;
  }

  async completeTask(agentId, taskId, result) {
    const agent = this.getAgentById(agentId);
    
    if (!agent) {
      logger.warn(`Attempted to complete task for unknown agent: ${agentId}`);
      return;
    }

    // Decrement load
    agent.currentLoad = Math.max(0, agent.currentLoad - 1);
    
    // Remove from task queue
    agent.metrics.taskQueue = agent.metrics.taskQueue.filter(t => t.taskId !== taskId);
    
    // Update agent metrics
    agent.metadata.totalTasksCompleted++;
    
    if (result.success) {
      this.metrics.successfulRequests++;
      
      // Update success rate (exponential moving average)
      agent.successRate = agent.successRate * 0.9 + 0.1;
      
      // Update average execution time
      if (result.executionTime) {
        agent.metrics.averageExecutionTime = 
          (agent.metrics.averageExecutionTime + result.executionTime) / 2;
      }
    } else {
      this.metrics.failedRequests++;
      
      // Decrease success rate
      agent.successRate = agent.successRate * 0.9;
      
      // If success rate drops too low, mark as unhealthy
      if (agent.successRate < 0.5) {
        agent.status = 'degraded';
        logger.warn(`🔴 Agent ${agentId} marked as degraded due to low success rate: ${agent.successRate.toFixed(2)}`);
      }
    }

    logger.info(`✅ Task completed by agent: ${agentId}`, {
      taskId,
      success: result.success,
      executionTime: result.executionTime,
      agentLoad: agent.currentLoad,
      successRate: agent.successRate.toFixed(2)
    });

    this.emit('taskCompleted', { agent, taskId, result });
  }

  getUtilization() {
    const totalCapacity = this.getAllAgents().reduce((sum, agent) => sum + agent.maxLoad, 0);
    const currentLoad = this.getAllAgents().reduce((sum, agent) => sum + agent.currentLoad, 0);
    
    return totalCapacity > 0 ? currentLoad / totalCapacity : 0;
  }

  getCapabilities() {
    return this.config.capabilities;
  }

  getPoolStatus() {
    const agents = this.getAllAgents();
    const healthyAgents = this.getHealthyAgents();
    
    return {
      type: this.agentType,
      totalAgents: agents.length,
      healthyAgents: healthyAgents.length,
      utilization: this.getUtilization(),
      capabilities: this.config.capabilities,
      metrics: this.metrics,
      agents: agents.map(agent => ({
        id: agent.id,
        status: agent.status,
        currentLoad: agent.currentLoad,
        successRate: agent.successRate,
        lastHealthCheck: agent.lastHealthCheck
      }))
    };
  }

  startHealthMonitoring() {
    this.healthCheckInterval = setInterval(async () => {
      await this.performHealthChecks();
    }, this.config.healthCheckInterval);
    
    logger.info(`💓 Started health monitoring for ${this.agentType} agents`);
  }

  async performHealthChecks() {
    const agents = this.getAllAgents();
    const healthCheckPromises = agents.map(agent => this.checkAgentHealth(agent));
    
    await Promise.allSettled(healthCheckPromises);
    
    // Update pool metrics
    this.updatePoolMetrics();
  }

  async checkAgentHealth(agent) {
    try {
      // Simulate health check - in real implementation, this would ping the agent
      const isHealthy = await this.pingAgent(agent);
      
      if (isHealthy) {
        if (agent.status !== 'healthy') {
          agent.status = 'healthy';
          logger.info(`💚 Agent ${agent.id} recovered to healthy status`);
          this.emit('agentRecovered', agent);
        }
        agent.lastHealthCheck = new Date();
      } else {
        this.handleUnhealthyAgent(agent);
      }
    } catch (error) {
      logger.error(`Health check failed for agent ${agent.id}:`, error);
      this.handleUnhealthyAgent(agent);
    }
  }

  async pingAgent(agent) {
    // Simulate agent ping - in real implementation, this would be an HTTP health check
    // For now, randomly simulate occasional health issues
    return Math.random() > 0.05; // 95% success rate
  }

  handleUnhealthyAgent(agent) {
    const previousStatus = agent.status;
    agent.status = 'unhealthy';
    
    if (previousStatus !== 'unhealthy') {
      logger.warn(`🔴 Agent ${agent.id} marked as unhealthy`);
      this.emit('agentUnhealthy', agent);
    }
    
    // If agent has been unhealthy for too long, consider removing it
    const timeSinceLastHealthy = Date.now() - agent.lastHealthCheck.getTime();
    if (timeSinceLastHealthy > 300000) { // 5 minutes
      this.unregisterAgent(agent.id);
    }
  }

  updatePoolMetrics() {
    const agents = this.getAllAgents();
    const healthyAgents = this.getHealthyAgents();
    
    this.metrics.currentUtilization = this.getUtilization();
    
    // Calculate pool-wide success rate
    const totalSuccessRate = agents.reduce((sum, agent) => sum + agent.successRate, 0);
    this.metrics.poolSuccessRate = agents.length > 0 ? totalSuccessRate / agents.length : 0;
    
    // Calculate average response time
    const totalResponseTime = agents.reduce((sum, agent) => sum + agent.metrics.averageExecutionTime, 0);
    this.metrics.averageResponseTime = agents.length > 0 ? totalResponseTime / agents.length : 0;
    
    // Emit metrics for monitoring
    this.emit('metrics', {
      poolType: this.agentType,
      totalAgents: agents.length,
      healthyAgents: healthyAgents.length,
      ...this.metrics,
      timestamp: new Date().toISOString()
    });
  }

  initializeMetrics() {
    // Reset metrics
    this.metrics = {
      totalRequests: 0,
      successfulRequests: 0,
      failedRequests: 0,
      averageResponseTime: 0,
      currentUtilization: 0,
      poolSuccessRate: 1.0
    };
  }

  async scaleUp(targetAgents) {
    const currentAgents = this.getAllAgents().length;
    const maxAgents = this.config.maxAgents;
    
    const actualTarget = Math.min(targetAgents, maxAgents);
    
    if (actualTarget <= currentAgents) {
      logger.info(`🔄 No scale up needed for ${this.agentType} pool (current: ${currentAgents}, target: ${actualTarget})`);
      return;
    }
    
    logger.info(`⬆️ Scaling up ${this.agentType} pool from ${currentAgents} to ${actualTarget} agents`);
    
    for (let i = currentAgents; i < actualTarget; i++) {
      try {
        const agent = await this.createAgentInstance(i);
        this.registerAgent(agent);
      } catch (error) {
        logger.error(`Failed to create agent instance ${i}:`, error);
      }
    }
  }

  async scaleDown(targetAgents) {
    const currentAgents = this.getAllAgents().length;
    const minAgents = this.config.minAgents;
    
    const actualTarget = Math.max(targetAgents, minAgents);
    
    if (actualTarget >= currentAgents) {
      logger.info(`🔄 No scale down needed for ${this.agentType} pool (current: ${currentAgents}, target: ${actualTarget})`);
      return;
    }
    
    logger.info(`⬇️ Scaling down ${this.agentType} pool from ${currentAgents} to ${actualTarget} agents`);
    
    // Remove least busy agents first
    const agents = this.getAllAgents().sort((a, b) => a.currentLoad - b.currentLoad);
    const agentsToRemove = agents.slice(0, currentAgents - actualTarget);
    
    for (const agent of agentsToRemove) {
      if (agent.currentLoad === 0) {
        this.unregisterAgent(agent.id);
      }
    }
  }

  async shutdown() {
    logger.info(`🛑 Shutting down ${this.agentType} agent pool...`);
    
    // Stop health monitoring
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval);
    }
    
    // Wait for active tasks to complete
    await this.waitForActiveTasks();
    
    // Clear all agents
    this.agents.clear();
    
    logger.info(`✅ ${this.agentType} agent pool shutdown complete`);
  }

  async waitForActiveTasks() {
    const maxWaitTime = 30000; // 30 seconds
    const startTime = Date.now();
    
    while (Date.now() - startTime < maxWaitTime) {
      const activeTasks = this.getAllAgents().reduce((sum, agent) => sum + agent.currentLoad, 0);
      
      if (activeTasks === 0) {
        break;
      }
      
      logger.info(`⏳ Waiting for ${activeTasks} active tasks to complete...`);
      await new Promise(resolve => setTimeout(resolve, 1000));
    }
  }
}

// Load balancer helper class
class LoadBalancer {
  constructor(strategy) {
    this.strategy = strategy;
    this.roundRobinIndex = 0;
  }

  selectRoundRobin(agents) {
    if (agents.length === 0) return null;
    
    const agent = agents[this.roundRobinIndex % agents.length];
    this.roundRobinIndex++;
    
    return agent;
  }

  selectWeighted(agents, task) {
    if (agents.length === 0) return null;
    
    // Calculate weights based on inverse load and success rate
    const weightedAgents = agents.map(agent => ({
      agent,
      weight: (1 / (agent.currentLoad + 1)) * agent.successRate
    }));
    
    const totalWeight = weightedAgents.reduce((sum, wa) => sum + wa.weight, 0);
    const randomValue = Math.random() * totalWeight;
    
    let currentWeight = 0;
    for (const wa of weightedAgents) {
      currentWeight += wa.weight;
      if (randomValue <= currentWeight) {
        return wa.agent;
      }
    }
    
    return weightedAgents[0].agent;
  }
}

module.exports = AgentPool;