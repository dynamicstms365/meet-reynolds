const { v4: uuidv4 } = require('uuid');
const logger = require('../utils/logger');

class LoopPreventionEngine {
  constructor() {
    this.eventChain = new Map(); // eventId -> { parentEvent, timestamp, type, data }
    this.activeExecutions = new Map(); // executionId -> { startTime, events, status }
    this.circuitBreakers = new Map(); // pattern -> { count, lastTriggered, isOpen }
    this.confidenceThreshold = 0.999; // 99.9% confidence requirement
    this.maxChainDepth = 10; // Maximum event chain depth
    this.maxExecutionTime = 300000; // 5 minutes max execution time
    this.patternDetectionWindow = 3600000; // 1 hour window for pattern detection
    
    // Cleanup intervals
    this.cleanupInterval = setInterval(() => this.cleanupOldEvents(), 60000); // Every minute
    
    logger.info('🛡️ Loop Prevention Engine initialized with 99.9% confidence tracking');
  }

  generateEventId(parentEventId = null) {
    const eventId = uuidv4();
    const timestamp = Date.now();
    
    // Create event chain entry
    this.eventChain.set(eventId, {
      parentEvent: parentEventId,
      timestamp,
      children: [],
      type: 'generated',
      executionPath: this.buildExecutionPath(parentEventId),
      metadata: {
        depth: this.calculateChainDepth(parentEventId),
        confidence: this.calculateConfidence(parentEventId)
      }
    });
    
    // Add to parent's children if applicable
    if (parentEventId && this.eventChain.has(parentEventId)) {
      this.eventChain.get(parentEventId).children.push(eventId);
    }
    
    // Validate chain integrity
    this.validateEventChain(eventId);
    
    logger.debug(`🔗 Generated event ID: ${eventId}`, {
      parentEvent: parentEventId,
      chainDepth: this.eventChain.get(eventId).metadata.depth,
      confidence: this.eventChain.get(eventId).metadata.confidence
    });
    
    return eventId;
  }

  validateEventChain(eventId) {
    const event = this.eventChain.get(eventId);
    if (!event) return;
    
    // Check for circular references
    if (this.detectCircularReference(eventId)) {
      this.triggerCircuitBreaker('circular_reference', eventId);
      throw new Error(`🚨 CIRCULAR REFERENCE DETECTED: Event ${eventId} creates a circular dependency`);
    }
    
    // Check chain depth
    if (event.metadata.depth > this.maxChainDepth) {
      this.triggerCircuitBreaker('max_depth_exceeded', eventId);
      throw new Error(`🚨 MAX CHAIN DEPTH EXCEEDED: Event ${eventId} exceeds maximum depth of ${this.maxChainDepth}`);
    }
    
    // Check execution patterns
    this.validateExecutionPatterns(eventId);
  }

  detectCircularReference(eventId, visited = new Set()) {
    if (visited.has(eventId)) {
      logger.error(`🔄 Circular reference detected in event chain: ${eventId}`);
      return true;
    }
    
    visited.add(eventId);
    const event = this.eventChain.get(eventId);
    
    if (!event || !event.parentEvent) {
      return false;
    }
    
    return this.detectCircularReference(event.parentEvent, visited);
  }

  calculateChainDepth(parentEventId) {
    if (!parentEventId) return 0;
    
    let depth = 0;
    let currentEventId = parentEventId;
    
    while (currentEventId && this.eventChain.has(currentEventId)) {
      depth++;
      const event = this.eventChain.get(currentEventId);
      currentEventId = event.parentEvent;
      
      // Safety check to prevent infinite loops
      if (depth > this.maxChainDepth + 1) {
        logger.error(`🚨 Potential infinite loop detected while calculating chain depth for event: ${parentEventId}`);
        break;
      }
    }
    
    return depth;
  }

  calculateConfidence(parentEventId) {
    if (!parentEventId) return 1.0;
    
    const event = this.eventChain.get(parentEventId);
    if (!event) return 0.0;
    
    // Base confidence starts high and decreases with chain depth and complexity
    let confidence = 1.0;
    
    // Decrease confidence based on chain depth
    confidence -= (event.metadata?.depth || 0) * 0.02; // 2% per level
    
    // Decrease confidence based on execution time
    const executionTime = Date.now() - event.timestamp;
    if (executionTime > this.maxExecutionTime) {
      confidence -= 0.5; // 50% penalty for long executions
    }
    
    // Decrease confidence if similar patterns detected recently
    const patternRisk = this.assessPatternRisk(event);
    confidence -= patternRisk * 0.3; // Up to 30% penalty for risky patterns
    
    // Ensure confidence is within bounds
    return Math.max(0, Math.min(1, confidence));
  }

  buildExecutionPath(parentEventId) {
    const path = [];
    let currentEventId = parentEventId;
    
    while (currentEventId && this.eventChain.has(currentEventId)) {
      const event = this.eventChain.get(currentEventId);
      path.unshift({
        eventId: currentEventId,
        timestamp: event.timestamp,
        type: event.type
      });
      currentEventId = event.parentEvent;
      
      // Safety check
      if (path.length > this.maxChainDepth) {
        logger.warn(`⚠️ Execution path truncated at ${this.maxChainDepth} events`);
        break;
      }
    }
    
    return path;
  }

  startExecution(executionId, eventId, metadata = {}) {
    const execution = {
      executionId,
      rootEventId: eventId,
      startTime: Date.now(),
      events: [eventId],
      status: 'running',
      metadata: {
        taskType: metadata.taskType,
        agentType: metadata.agentType,
        strategy: metadata.strategy,
        ...metadata
      }
    };
    
    this.activeExecutions.set(executionId, execution);
    
    // Update event with execution context
    if (this.eventChain.has(eventId)) {
      this.eventChain.get(eventId).executionId = executionId;
      this.eventChain.get(eventId).executionMetadata = execution.metadata;
    }
    
    logger.info(`⚡ Started execution tracking: ${executionId}`, {
      eventId,
      metadata: execution.metadata
    });
  }

  addEventToExecution(executionId, eventId) {
    const execution = this.activeExecutions.get(executionId);
    if (!execution) {
      logger.warn(`⚠️ Attempted to add event to unknown execution: ${executionId}`);
      return;
    }
    
    execution.events.push(eventId);
    
    // Check for execution time limits
    const executionTime = Date.now() - execution.startTime;
    if (executionTime > this.maxExecutionTime) {
      this.triggerCircuitBreaker('execution_timeout', executionId);
      throw new Error(`🚨 EXECUTION TIMEOUT: Execution ${executionId} exceeded maximum time of ${this.maxExecutionTime}ms`);
    }
    
    // Update event with execution context
    if (this.eventChain.has(eventId)) {
      this.eventChain.get(eventId).executionId = executionId;
    }
  }

  completeExecution(executionId, result = {}) {
    const execution = this.activeExecutions.get(executionId);
    if (!execution) {
      logger.warn(`⚠️ Attempted to complete unknown execution: ${executionId}`);
      return;
    }
    
    execution.status = result.success ? 'completed' : 'failed';
    execution.endTime = Date.now();
    execution.result = result;
    
    const executionTime = execution.endTime - execution.startTime;
    const eventCount = execution.events.length;
    
    logger.info(`✅ Completed execution tracking: ${executionId}`, {
      status: execution.status,
      executionTime,
      eventCount,
      confidence: this.calculateExecutionConfidence(execution)
    });
    
    // Archive completed execution (remove from active)
    this.activeExecutions.delete(executionId);
    
    // Update pattern learning
    this.learnFromExecution(execution);
  }

  calculateExecutionConfidence(execution) {
    const baseConfidence = 1.0;
    let confidence = baseConfidence;
    
    // Factor in execution time
    const executionTime = execution.endTime - execution.startTime;
    const timeRatio = executionTime / this.maxExecutionTime;
    confidence -= timeRatio * 0.2; // Up to 20% penalty for long executions
    
    // Factor in event chain complexity
    const eventComplexity = execution.events.length / this.maxChainDepth;
    confidence -= eventComplexity * 0.1; // Up to 10% penalty for complex chains
    
    // Factor in result success
    if (execution.result && !execution.result.success) {
      confidence -= 0.3; // 30% penalty for failed executions
    }
    
    return Math.max(0, confidence);
  }

  validateExecutionPatterns(eventId) {
    const event = this.eventChain.get(eventId);
    if (!event) return;
    
    // Check for suspicious patterns
    const suspiciousPatterns = [
      this.detectRapidFirePattern(eventId),
      this.detectRecursivePattern(eventId),
      this.detectFanOutPattern(eventId)
    ];
    
    for (const pattern of suspiciousPatterns) {
      if (pattern.detected) {
        this.handleSuspiciousPattern(pattern, eventId);
      }
    }
  }

  detectRapidFirePattern(eventId) {
    const event = this.eventChain.get(eventId);
    const now = Date.now();
    const timeWindow = 10000; // 10 seconds
    
    // Count recent events in the same execution path
    let recentEvents = 0;
    for (const [id, evt] of this.eventChain.entries()) {
      if (now - evt.timestamp < timeWindow && 
          evt.executionId === event.executionId) {
        recentEvents++;
      }
    }
    
    return {
      detected: recentEvents > 20, // More than 20 events in 10 seconds
      type: 'rapid_fire',
      severity: recentEvents > 50 ? 'critical' : 'warning',
      details: { eventsInWindow: recentEvents, timeWindow }
    };
  }

  detectRecursivePattern(eventId) {
    const event = this.eventChain.get(eventId);
    const executionPath = event.executionPath || [];
    
    // Look for repeating sequences in the execution path
    if (executionPath.length < 4) return { detected: false };
    
    for (let i = 0; i < executionPath.length - 2; i++) {
      for (let j = i + 2; j < executionPath.length; j++) {
        if (executionPath[i].type === executionPath[j].type) {
          const repetitionGap = j - i;
          if (repetitionGap <= 3) { // Suspicious if same type repeats within 3 steps
            return {
              detected: true,
              type: 'recursive',
              severity: 'warning',
              details: { repeatingType: executionPath[i].type, gap: repetitionGap }
            };
          }
        }
      }
    }
    
    return { detected: false };
  }

  detectFanOutPattern(eventId) {
    const event = this.eventChain.get(eventId);
    const childrenCount = event.children?.length || 0;
    
    return {
      detected: childrenCount > 10, // More than 10 child events
      type: 'fan_out',
      severity: childrenCount > 20 ? 'critical' : 'warning',
      details: { childrenCount }
    };
  }

  handleSuspiciousPattern(pattern, eventId) {
    logger.warn(`🔍 Suspicious pattern detected: ${pattern.type}`, {
      eventId,
      severity: pattern.severity,
      details: pattern.details
    });
    
    if (pattern.severity === 'critical') {
      this.triggerCircuitBreaker(pattern.type, eventId);
    }
    
    // Emit event for monitoring
    this.emit('suspiciousPattern', { pattern, eventId });
  }

  assessPatternRisk(event) {
    const now = Date.now();
    const windowStart = now - this.patternDetectionWindow;
    
    // Count similar events in the detection window
    let similarEvents = 0;
    let totalEvents = 0;
    
    for (const [id, evt] of this.eventChain.entries()) {
      if (evt.timestamp >= windowStart) {
        totalEvents++;
        if (evt.type === event.type && 
            evt.executionMetadata?.taskType === event.executionMetadata?.taskType) {
          similarEvents++;
        }
      }
    }
    
    // Calculate risk based on repetition ratio
    if (totalEvents === 0) return 0;
    
    const repetitionRatio = similarEvents / totalEvents;
    
    // Higher risk for higher repetition ratios
    if (repetitionRatio > 0.8) return 1.0; // Very high risk
    if (repetitionRatio > 0.6) return 0.7; // High risk
    if (repetitionRatio > 0.4) return 0.4; // Medium risk
    if (repetitionRatio > 0.2) return 0.2; // Low risk
    
    return 0; // No significant risk
  }

  triggerCircuitBreaker(patternType, contextId) {
    const now = Date.now();
    const circuitBreaker = this.circuitBreakers.get(patternType) || {
      count: 0,
      lastTriggered: 0,
      isOpen: false
    };
    
    circuitBreaker.count++;
    circuitBreaker.lastTriggered = now;
    circuitBreaker.isOpen = true;
    
    this.circuitBreakers.set(patternType, circuitBreaker);
    
    logger.error(`🚨 CIRCUIT BREAKER TRIGGERED: ${patternType}`, {
      contextId,
      triggerCount: circuitBreaker.count,
      timestamp: new Date(now).toISOString()
    });
    
    // Auto-reset circuit breaker after 5 minutes
    setTimeout(() => {
      this.resetCircuitBreaker(patternType);
    }, 300000);
    
    throw new Error(`🚨 CIRCUIT BREAKER OPEN: ${patternType} pattern detected and prevented`);
  }

  resetCircuitBreaker(patternType) {
    const circuitBreaker = this.circuitBreakers.get(patternType);
    if (circuitBreaker) {
      circuitBreaker.isOpen = false;
      logger.info(`🔄 Circuit breaker reset: ${patternType}`);
    }
  }

  isCircuitBreakerOpen(patternType) {
    const circuitBreaker = this.circuitBreakers.get(patternType);
    return circuitBreaker ? circuitBreaker.isOpen : false;
  }

  learnFromExecution(execution) {
    // Learn patterns from completed executions for future detection
    const pattern = {
      taskType: execution.metadata.taskType,
      agentType: execution.metadata.agentType,
      strategy: execution.metadata.strategy,
      executionTime: execution.endTime - execution.startTime,
      eventCount: execution.events.length,
      success: execution.result?.success || false,
      timestamp: execution.endTime
    };
    
    // Store pattern for future analysis
    // In a real implementation, this would be persisted to a database
    logger.debug('📚 Learning from execution pattern', pattern);
  }

  cleanupOldEvents() {
    const now = Date.now();
    const maxAge = 3600000; // 1 hour
    const cutoffTime = now - maxAge;
    
    let cleanedCount = 0;
    
    for (const [eventId, event] of this.eventChain.entries()) {
      if (event.timestamp < cutoffTime) {
        this.eventChain.delete(eventId);
        cleanedCount++;
      }
    }
    
    if (cleanedCount > 0) {
      logger.debug(`🧹 Cleaned up ${cleanedCount} old events from chain`);
    }
  }

  getConfidenceReport() {
    const activeExecutions = Array.from(this.activeExecutions.values());
    const recentEvents = Array.from(this.eventChain.values())
      .filter(event => Date.now() - event.timestamp < 3600000) // Last hour
      .sort((a, b) => b.timestamp - a.timestamp);
    
    const confidenceScores = recentEvents.map(event => event.metadata?.confidence || 1.0);
    const averageConfidence = confidenceScores.length > 0 ? 
      confidenceScores.reduce((sum, conf) => sum + conf, 0) / confidenceScores.length : 1.0;
    
    return {
      averageConfidence,
      meetsThreshold: averageConfidence >= this.confidenceThreshold,
      activeExecutions: activeExecutions.length,
      recentEvents: recentEvents.length,
      circuitBreakers: Array.from(this.circuitBreakers.entries()).map(([type, breaker]) => ({
        type,
        isOpen: breaker.isOpen,
        triggerCount: breaker.count
      })),
      timestamp: new Date().toISOString()
    };
  }

  getSystemStatus() {
    const report = this.getConfidenceReport();
    
    return {
      status: report.meetsThreshold ? 'healthy' : 'degraded',
      confidence: report.averageConfidence,
      activeExecutions: report.activeExecutions,
      recentEvents: report.recentEvents,
      openCircuitBreakers: report.circuitBreakers.filter(cb => cb.isOpen).length,
      systemIntegrity: this.assessSystemIntegrity()
    };
  }

  assessSystemIntegrity() {
    const openBreakers = Array.from(this.circuitBreakers.values())
      .filter(breaker => breaker.isOpen).length;
    
    if (openBreakers > 3) return 'compromised';
    if (openBreakers > 1) return 'degraded';
    
    const report = this.getConfidenceReport();
    if (report.averageConfidence < 0.8) return 'degraded';
    if (report.averageConfidence < this.confidenceThreshold) return 'warning';
    
    return 'healthy';
  }

  shutdown() {
    logger.info('🛑 Shutting down Loop Prevention Engine...');
    
    if (this.cleanupInterval) {
      clearInterval(this.cleanupInterval);
    }
    
    // Final cleanup
    this.eventChain.clear();
    this.activeExecutions.clear();
    this.circuitBreakers.clear();
    
    logger.info('✅ Loop Prevention Engine shutdown complete');
  }
}

module.exports = LoopPreventionEngine;