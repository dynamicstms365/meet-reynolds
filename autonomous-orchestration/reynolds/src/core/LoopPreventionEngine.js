const { v4: uuidv4 } = require('uuid');
const EventEmitter = require('events');
const logger = require('../utils/logger');
const PrometheusMetrics = require('../metrics/PrometheusMetrics');

class LoopPreventionEngine extends EventEmitter {
  constructor(config = {}) {
    super();
    
    this.config = {
      confidenceThreshold: 0.999, // 99.9% confidence requirement
      maxChainDepth: 10, // Maximum event chain depth
      maxExecutionTime: 300000, // 5 minutes max execution time
      patternDetectionWindow: 3600000, // 1 hour window for pattern detection
      reynoldsObsessionThreshold: 5, // Threshold for Reynolds Issue Obsession™
      rapidFireThreshold: 20, // Events per 10 seconds
      recursivePatternThreshold: 3, // Max recursive patterns
      fanOutThreshold: 10, // Max child events
      metricsEnabled: true,
      ...config
    };

    this.eventChain = new Map(); // eventId -> { parentEvent, timestamp, type, data }
    this.activeExecutions = new Map(); // executionId -> { startTime, events, status }
    this.circuitBreakers = new Map(); // pattern -> { count, lastTriggered, isOpen }
    this.reynoldsObsessionPatterns = new Map(); // pattern -> { count, severity, lastDetected }
    
    // Initialize Prometheus metrics if enabled
    this.metrics = this.config.metricsEnabled ? new PrometheusMetrics() : null;
    if (this.metrics) {
      this.metrics.updateConfidenceThreshold(this.config.confidenceThreshold);
    }
    
    // Cleanup intervals
    this.cleanupInterval = setInterval(() => this.cleanupOldEvents(), 60000); // Every minute
    this.metricsUpdateInterval = setInterval(() => this.updateMetrics(), 15000); // Every 15 seconds
    
    logger.info('🛡️ Loop Prevention Engine initialized with 99.9% confidence tracking and Reynolds Issue Obsession™ detection');
  }

  generateEventId(parentEventId = null, eventType = 'generated') {
    const startTime = Date.now();
    const eventId = uuidv4();
    const timestamp = Date.now();
    
    // Create event chain entry
    this.eventChain.set(eventId, {
      parentEvent: parentEventId,
      timestamp,
      children: [],
      type: eventType,
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
    
    // Record metrics
    if (this.metrics) {
      this.metrics.recordEventGeneration(eventType, parentEventId !== null);
      const processingTime = (Date.now() - startTime) / 1000;
      this.metrics.recordEventProcessingTime(eventType, processingTime);
    }
    
    // Validate chain integrity
    this.validateEventChain(eventId);
    
    logger.debug(`🔗 Generated event ID: ${eventId}`, {
      parentEvent: parentEventId,
      eventType,
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
    if (event.metadata.depth > this.config.maxChainDepth) {
      this.triggerCircuitBreaker('max_depth_exceeded', eventId);
      throw new Error(`🚨 MAX CHAIN DEPTH EXCEEDED: Event ${eventId} exceeds maximum depth of ${this.config.maxChainDepth}`);
    }
    
    // Check execution patterns and Reynolds Issue Obsession™
    this.validateExecutionPatterns(eventId);
    this.detectReynoldsObsessionPatterns(eventId);
    
    // Update metrics
    if (this.metrics) {
      this.metrics.updateEventChainDepth(event.type, event.executionId, event.metadata.depth);
      this.metrics.updateEventChainSize(this.eventChain.size);
    }
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
      if (depth > this.config.maxChainDepth + 1) {
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
    if (executionTime > this.config.maxExecutionTime) {
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
      if (path.length > this.config.maxChainDepth) {
        logger.warn(`⚠️ Execution path truncated at ${this.config.maxChainDepth} events`);
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
    
    // Record metrics for tracking confidence
    if (this.metrics) {
      const event = this.eventChain.get(eventId);
      if (event) {
        this.metrics.updateConfidence(eventId, executionId, event.metadata.confidence);
      }
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
    if (executionTime > this.config.maxExecutionTime) {
      this.triggerCircuitBreaker('execution_timeout', executionId);
      throw new Error(`🚨 EXECUTION TIMEOUT: Execution ${executionId} exceeded maximum time of ${this.config.maxExecutionTime}ms`);
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
    const executionConfidence = this.calculateExecutionConfidence(execution);
    
    // Record execution metrics
    if (this.metrics) {
      const executionTimeSeconds = executionTime / 1000;
      this.metrics.recordExecutionDuration(
        execution.metadata.taskType || 'unknown',
        executionTimeSeconds,
        result.success
      );
    }
    
    logger.info(`✅ Completed execution tracking: ${executionId}`, {
      status: execution.status,
      executionTime,
      eventCount,
      confidence: executionConfidence
    });
    
    // Archive completed execution (remove from active)
    this.activeExecutions.delete(executionId);
    
    // Update pattern learning
    this.learnFromExecution(execution);
    
    // Emit completion event
    this.emit('executionCompleted', {
      executionId,
      success: result.success,
      executionTime,
      confidence: executionConfidence
    });
  }

  calculateExecutionConfidence(execution) {
    const baseConfidence = 1.0;
    let confidence = baseConfidence;
    
    // Factor in execution time
    const executionTime = execution.endTime - execution.startTime;
    const timeRatio = executionTime / this.config.maxExecutionTime;
    confidence -= timeRatio * 0.2; // Up to 20% penalty for long executions
    
    // Factor in event chain complexity
    const eventComplexity = execution.events.length / this.config.maxChainDepth;
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
    
    const detected = recentEvents > this.config.rapidFireThreshold;
    const severity = recentEvents > (this.config.rapidFireThreshold * 2.5) ? 'critical' : 'warning';
    
    // Record metrics
    if (detected && this.metrics) {
      this.metrics.recordRapidFirePattern(severity);
    }
    
    return {
      detected,
      type: 'rapid_fire',
      severity,
      details: { eventsInWindow: recentEvents, timeWindow, threshold: this.config.rapidFireThreshold }
    };
  }

  detectRecursivePattern(eventId) {
    const event = this.eventChain.get(eventId);
    const executionPath = event.executionPath || [];
    
    // Look for repeating sequences in the execution path
    if (executionPath.length < 4) return { detected: false };
    
    let recursiveCount = 0;
    
    for (let i = 0; i < executionPath.length - 2; i++) {
      for (let j = i + 2; j < executionPath.length; j++) {
        if (executionPath[i].type === executionPath[j].type) {
          const repetitionGap = j - i;
          if (repetitionGap <= this.config.recursivePatternThreshold) {
            recursiveCount++;
            
            const detected = recursiveCount >= this.config.recursivePatternThreshold;
            const severity = recursiveCount > (this.config.recursivePatternThreshold * 2) ? 'critical' : 'warning';
            
            // Record metrics
            if (detected && this.metrics) {
              this.metrics.recordRecursivePattern(severity);
            }
            
            return {
              detected,
              type: 'recursive',
              severity,
              details: {
                repeatingType: executionPath[i].type,
                gap: repetitionGap,
                recursiveCount,
                threshold: this.config.recursivePatternThreshold
              }
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
    
    const detected = childrenCount > this.config.fanOutThreshold;
    const severity = childrenCount > (this.config.fanOutThreshold * 2) ? 'critical' : 'warning';
    
    // Record metrics
    if (detected && this.metrics) {
      this.metrics.recordFanOutPattern(severity);
    }
    
    return {
      detected,
      type: 'fan_out',
      severity,
      details: { childrenCount, threshold: this.config.fanOutThreshold }
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
    
    // Record metrics
    if (this.metrics) {
      this.metrics.recordLoopDetection(pattern.type, pattern.severity);
    }
    
    // Emit event for monitoring
    this.emit('suspiciousPattern', { pattern, eventId });
  }

  detectReynoldsObsessionPatterns(eventId) {
    const event = this.eventChain.get(eventId);
    
    // Reynolds Issue Obsession™: Detect patterns that indicate excessive focus on specific tasks
    const obsessionPatterns = [
      this.detectTaskRepetitionObsession(event),
      this.detectMaximumEffortOverdrive(event),
      this.detectSupernaturalPerfectionismPattern(event)
    ];
    
    for (const obsessionPattern of obsessionPatterns) {
      if (obsessionPattern.detected) {
        this.handleReynoldsObsession(obsessionPattern, eventId);
      }
    }
  }

  detectTaskRepetitionObsession(event) {
    const now = Date.now();
    const windowStart = now - this.config.patternDetectionWindow;
    
    // Count how many times Reynolds has focused on the same task type
    let repetitionCount = 0;
    for (const [id, evt] of this.eventChain.entries()) {
      if (evt.timestamp >= windowStart &&
          evt.executionMetadata?.taskType === event.executionMetadata?.taskType &&
          evt.executionMetadata?.agentType === 'reynolds') {
        repetitionCount++;
      }
    }
    
    const detected = repetitionCount > this.config.reynoldsObsessionThreshold;
    const severity = repetitionCount > (this.config.reynoldsObsessionThreshold * 2) ? 'critical' : 'warning';
    
    return {
      detected,
      type: 'task_repetition_obsession',
      severity,
      details: {
        repetitionCount,
        taskType: event.executionMetadata?.taskType,
        threshold: this.config.reynoldsObsessionThreshold,
        reynoldsQuote: "Maximum Effort™ doesn't mean doing the same thing over and over... or does it?"
      }
    };
  }

  detectMaximumEffortOverdrive(event) {
    const now = Date.now();
    const shortWindow = 300000; // 5 minutes
    const windowStart = now - shortWindow;
    
    // Count rapid succession of "maximum effort" patterns
    let maxEffortEvents = 0;
    for (const [id, evt] of this.eventChain.entries()) {
      if (evt.timestamp >= windowStart &&
          (evt.type?.includes('maximum_effort') ||
           evt.executionMetadata?.strategy?.includes('maximum_effort'))) {
        maxEffortEvents++;
      }
    }
    
    const detected = maxEffortEvents > 10; // More than 10 max effort events in 5 minutes
    const severity = maxEffortEvents > 20 ? 'critical' : 'warning';
    
    return {
      detected,
      type: 'maximum_effort_overdrive',
      severity,
      details: {
        maxEffortEvents,
        timeWindow: shortWindow,
        reynoldsQuote: "Even I need to pace myself sometimes. Maximum Effort™ has its limits."
      }
    };
  }

  detectSupernaturalPerfectionismPattern(event) {
    // Check for excessive re-work patterns
    const executionPath = event.executionPath || [];
    let reworkCount = 0;
    
    // Look for repeated similar events in the same execution
    const eventTypes = executionPath.map(e => e.type);
    const uniqueTypes = new Set(eventTypes);
    
    // If we have significantly more events than unique types, it's rework
    if (eventTypes.length > uniqueTypes.size * 1.5) {
      reworkCount = eventTypes.length - uniqueTypes.size;
    }
    
    const detected = reworkCount > 5; // More than 5 rework events
    const severity = reworkCount > 10 ? 'critical' : 'warning';
    
    return {
      detected,
      type: 'supernatural_perfectionism',
      severity,
      details: {
        reworkCount,
        totalEvents: eventTypes.length,
        uniqueEvents: uniqueTypes.size,
        reynoldsQuote: "Perfection is overrated. Sometimes 'good enough' is supernaturally efficient."
      }
    };
  }

  handleReynoldsObsession(obsessionPattern, eventId) {
    logger.warn(`🎭 Reynolds Issue Obsession™ detected: ${obsessionPattern.type}`, {
      eventId,
      severity: obsessionPattern.severity,
      details: obsessionPattern.details
    });
    
    // Track the obsession pattern
    const existingPattern = this.reynoldsObsessionPatterns.get(obsessionPattern.type) || {
      count: 0,
      severity: 'info',
      lastDetected: 0
    };
    
    existingPattern.count++;
    existingPattern.severity = obsessionPattern.severity;
    existingPattern.lastDetected = Date.now();
    
    this.reynoldsObsessionPatterns.set(obsessionPattern.type, existingPattern);
    
    // Record metrics
    if (this.metrics) {
      this.metrics.recordReynoldsObsession(obsessionPattern.type, obsessionPattern.severity);
      this.metrics.updateActiveReynoldsObsession(obsessionPattern.type, existingPattern.count);
    }
    
    // Emit Reynolds-specific event
    this.emit('reynoldsObsession', {
      pattern: obsessionPattern,
      eventId,
      reynoldsWisdom: obsessionPattern.details.reynoldsQuote
    });
    
    // Trigger intervention if critical
    if (obsessionPattern.severity === 'critical') {
      this.triggerReynoldsIntervention(obsessionPattern.type, eventId);
    }
  }

  triggerReynoldsIntervention(obsessionType, eventId) {
    logger.error(`🚨 REYNOLDS INTERVENTION REQUIRED: ${obsessionType}`, {
      eventId,
      intervention: 'supernatural_reality_check',
      message: "Time for a reality check. Even Reynolds needs to step back sometimes."
    });
    
    // This could integrate with the Reynolds personality system
    this.emit('reynoldsIntervention', {
      obsessionType,
      eventId,
      interventionType: 'supernatural_reality_check'
    });
  }

  assessPatternRisk(event) {
    const now = Date.now();
    const windowStart = now - this.config.patternDetectionWindow;
    
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
    
    // Factor in Reynolds obsession patterns
    const reynoldsRisk = this.calculateReynoldsObsessionRisk(event);
    
    // Combined risk calculation
    let baseRisk = 0;
    if (repetitionRatio > 0.8) baseRisk = 1.0; // Very high risk
    else if (repetitionRatio > 0.6) baseRisk = 0.7; // High risk
    else if (repetitionRatio > 0.4) baseRisk = 0.4; // Medium risk
    else if (repetitionRatio > 0.2) baseRisk = 0.2; // Low risk
    
    // Combine with Reynolds-specific risks
    return Math.min(1.0, baseRisk + reynoldsRisk * 0.3);
  }

  calculateReynoldsObsessionRisk(event) {
    let obsessionRisk = 0;
    
    for (const [type, pattern] of this.reynoldsObsessionPatterns.entries()) {
      const timeSinceDetection = Date.now() - pattern.lastDetected;
      const recentFactor = Math.max(0, 1 - (timeSinceDetection / this.config.patternDetectionWindow));
      
      if (pattern.severity === 'critical') {
        obsessionRisk += 0.4 * recentFactor;
      } else if (pattern.severity === 'warning') {
        obsessionRisk += 0.2 * recentFactor;
      }
    }
    
    return Math.min(1.0, obsessionRisk);
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
    
    // Determine severity based on pattern type and frequency
    const severity = this.getCircuitBreakerSeverity(patternType, circuitBreaker.count);
    
    // Record metrics
    if (this.metrics) {
      this.metrics.recordCircuitBreakerTrigger(patternType, severity);
      this.metrics.updateCircuitBreakerStatus(patternType, true);
      this.metrics.recordPreventedExecution(patternType);
    }
    
    logger.error(`🚨 CIRCUIT BREAKER TRIGGERED: ${patternType}`, {
      contextId,
      triggerCount: circuitBreaker.count,
      severity,
      timestamp: new Date(now).toISOString()
    });
    
    // Auto-reset circuit breaker after 5 minutes
    setTimeout(() => {
      this.resetCircuitBreaker(patternType);
    }, 300000);
    
    // Emit circuit breaker event
    this.emit('circuitBreakerTriggered', {
      patternType,
      contextId,
      severity,
      count: circuitBreaker.count
    });
    
    throw new Error(`🚨 CIRCUIT BREAKER OPEN: ${patternType} pattern detected and prevented`);
  }

  getCircuitBreakerSeverity(patternType, count) {
    // Determine severity based on pattern type and occurrence count
    const criticalPatterns = ['circular_reference', 'max_depth_exceeded', 'execution_timeout'];
    
    if (criticalPatterns.includes(patternType)) {
      return 'critical';
    }
    
    if (count > 5) return 'critical';
    if (count > 3) return 'warning';
    return 'info';
  }

  resetCircuitBreaker(patternType) {
    const circuitBreaker = this.circuitBreakers.get(patternType);
    if (circuitBreaker) {
      circuitBreaker.isOpen = false;
      
      // Update metrics
      if (this.metrics) {
        this.metrics.updateCircuitBreakerStatus(patternType, false);
      }
      
      logger.info(`🔄 Circuit breaker reset: ${patternType}`);
      
      // Emit reset event
      this.emit('circuitBreakerReset', { patternType });
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

  updateMetrics() {
    if (!this.metrics) return;
    
    try {
      // Update basic metrics
      this.metrics.updateEventChainSize(this.eventChain.size);
      this.metrics.updateActiveExecutions(this.activeExecutions.size);
      
      // Update confidence metrics
      const report = this.getConfidenceReport();
      this.metrics.updateAverageConfidence(report.averageConfidence);
      
      // Update circuit breaker status
      for (const [type, breaker] of this.circuitBreakers.entries()) {
        this.metrics.updateCircuitBreakerStatus(type, breaker.isOpen);
      }
      
      // Update Reynolds obsession metrics
      for (const [type, pattern] of this.reynoldsObsessionPatterns.entries()) {
        const isActive = Date.now() - pattern.lastDetected < 300000; // Active in last 5 minutes
        this.metrics.updateActiveReynoldsObsession(type, isActive ? pattern.count : 0);
      }
      
    } catch (error) {
      logger.error('Error updating metrics:', error);
    }
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
    
    // Also cleanup old Reynolds obsession patterns
    let obsessionsCleaned = 0;
    for (const [type, pattern] of this.reynoldsObsessionPatterns.entries()) {
      if (pattern.lastDetected < cutoffTime) {
        this.reynoldsObsessionPatterns.delete(type);
        obsessionsCleaned++;
      }
    }
    
    if (cleanedCount > 0 || obsessionsCleaned > 0) {
      logger.debug(`🧹 Cleaned up ${cleanedCount} old events and ${obsessionsCleaned} old obsession patterns from tracking`);
    }
    
    // Update metrics after cleanup
    this.updateMetrics();
  }

  getConfidenceReport() {
    const activeExecutions = Array.from(this.activeExecutions.values());
    const recentEvents = Array.from(this.eventChain.values())
      .filter(event => Date.now() - event.timestamp < 3600000) // Last hour
      .sort((a, b) => b.timestamp - a.timestamp);
    
    const confidenceScores = recentEvents.map(event => event.metadata?.confidence || 1.0);
    const averageConfidence = confidenceScores.length > 0 ?
      confidenceScores.reduce((sum, conf) => sum + conf, 0) / confidenceScores.length : 1.0;
    
    const meetsThreshold = averageConfidence >= this.config.confidenceThreshold;
    
    // Update metrics
    if (this.metrics) {
      this.metrics.updateAverageConfidence(averageConfidence);
      if (!meetsThreshold) {
        this.metrics.recordConfidenceViolation();
      }
    }
    
    return {
      averageConfidence,
      meetsThreshold,
      confidenceThreshold: this.config.confidenceThreshold,
      activeExecutions: activeExecutions.length,
      recentEvents: recentEvents.length,
      circuitBreakers: Array.from(this.circuitBreakers.entries()).map(([type, breaker]) => ({
        type,
        isOpen: breaker.isOpen,
        triggerCount: breaker.count
      })),
      reynoldsObsessions: Array.from(this.reynoldsObsessionPatterns.entries()).map(([type, pattern]) => ({
        type,
        count: pattern.count,
        severity: pattern.severity,
        lastDetected: new Date(pattern.lastDetected).toISOString()
      })),
      timestamp: new Date().toISOString()
    };
  }

  getSystemStatus() {
    const report = this.getConfidenceReport();
    const integrity = this.assessSystemIntegrity();
    
    // Determine overall status
    let status = 'healthy';
    if (!report.meetsThreshold || integrity === 'compromised') {
      status = 'unhealthy';
    } else if (integrity === 'degraded' || report.openCircuitBreakers > 0) {
      status = 'degraded';
    }
    
    const systemStatus = {
      status,
      confidence: report.averageConfidence,
      confidenceThreshold: report.confidenceThreshold,
      activeExecutions: report.activeExecutions,
      recentEvents: report.recentEvents,
      openCircuitBreakers: report.circuitBreakers.filter(cb => cb.isOpen).length,
      activeReynoldsObsessions: report.reynoldsObsessions.filter(obs =>
        Date.now() - new Date(obs.lastDetected).getTime() < 300000 // Active in last 5 minutes
      ).length,
      systemIntegrity: integrity,
      lastUpdated: new Date().toISOString()
    };
    
    // Update metrics
    if (this.metrics) {
      this.metrics.updateActiveExecutions(systemStatus.activeExecutions);
    }
    
    return systemStatus;
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
    
    try {
      // Clear intervals
      if (this.cleanupInterval) {
        clearInterval(this.cleanupInterval);
      }
      if (this.metricsUpdateInterval) {
        clearInterval(this.metricsUpdateInterval);
      }
      
      // Generate final metrics report
      if (this.metrics) {
        const finalReport = this.getSystemStatus();
        logger.info('📊 Final system status:', finalReport);
        
        // Clear metrics
        this.metrics.clearMetrics();
      }
      
      // Final cleanup
      this.eventChain.clear();
      this.activeExecutions.clear();
      this.circuitBreakers.clear();
      this.reynoldsObsessionPatterns.clear();
      
      // Remove all listeners
      this.removeAllListeners();
      
      logger.info('✅ Loop Prevention Engine shutdown complete');
      
    } catch (error) {
      logger.error('Error during Loop Prevention Engine shutdown:', error);
    }
  }
}

module.exports = LoopPreventionEngine;