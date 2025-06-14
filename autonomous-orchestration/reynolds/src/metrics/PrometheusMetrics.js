const client = require('prom-client');
const logger = require('../utils/logger');

class PrometheusMetrics {
  constructor() {
    // Initialize Prometheus metrics for loop prevention and event monitoring
    this.registry = new client.Registry();
    
    // Enable default system metrics collection
    client.collectDefaultMetrics({ register: this.registry });
    
    this.initializeLoopPreventionMetrics();
    this.initializeEventTrackingMetrics();
    this.initializeConfidenceMetrics();
    this.initializeReynoldsMetrics();
  }

  initializeLoopPreventionMetrics() {
    // Event chain metrics
    this.eventChainDepthGauge = new client.Gauge({
      name: 'reynolds_loop_prevention_chain_depth',
      help: 'Current depth of event chains',
      labelNames: ['event_type', 'execution_id'],
      registers: [this.registry]
    });

    this.eventChainSizeGauge = new client.Gauge({
      name: 'reynolds_loop_prevention_chain_size',
      help: 'Total number of events in tracking',
      registers: [this.registry]
    });

    // Circuit breaker metrics
    this.circuitBreakerCounter = new client.Counter({
      name: 'reynolds_loop_prevention_circuit_breaker_total',
      help: 'Total number of circuit breaker triggers',
      labelNames: ['pattern_type', 'severity'],
      registers: [this.registry]
    });

    this.circuitBreakerOpenGauge = new client.Gauge({
      name: 'reynolds_loop_prevention_circuit_breaker_open',
      help: 'Number of open circuit breakers',
      labelNames: ['pattern_type'],
      registers: [this.registry]
    });

    // Loop detection metrics
    this.loopDetectionCounter = new client.Counter({
      name: 'reynolds_loop_prevention_loop_detected_total',
      help: 'Total number of loops detected',
      labelNames: ['pattern_type', 'severity'],
      registers: [this.registry]
    });

    this.preventedExecutionsCounter = new client.Counter({
      name: 'reynolds_loop_prevention_executions_prevented_total',
      help: 'Total number of executions prevented by loop detection',
      labelNames: ['pattern_type'],
      registers: [this.registry]
    });
  }

  initializeEventTrackingMetrics() {
    // Event tracking metrics
    this.eventGenerationCounter = new client.Counter({
      name: 'reynolds_events_generated_total',
      help: 'Total number of events generated',
      labelNames: ['event_type', 'parent_exists'],
      registers: [this.registry]
    });

    this.activeExecutionsGauge = new client.Gauge({
      name: 'reynolds_active_executions',
      help: 'Number of currently active executions',
      registers: [this.registry]
    });

    this.executionDurationHistogram = new client.Histogram({
      name: 'reynolds_execution_duration_seconds',
      help: 'Duration of executions in seconds',
      labelNames: ['execution_type', 'success'],
      buckets: [0.1, 0.5, 1, 2, 5, 10, 30, 60, 300, 600],
      registers: [this.registry]
    });

    this.eventProcessingTime = new client.Histogram({
      name: 'reynolds_event_processing_duration_seconds',
      help: 'Time taken to process events',
      labelNames: ['event_type'],
      buckets: [0.001, 0.005, 0.01, 0.05, 0.1, 0.5, 1],
      registers: [this.registry]
    });
  }

  initializeConfidenceMetrics() {
    // Confidence tracking metrics
    this.confidenceGauge = new client.Gauge({
      name: 'reynolds_loop_prevention_confidence',
      help: 'Current confidence level (0-1)',
      labelNames: ['event_id', 'execution_id'],
      registers: [this.registry]
    });

    this.averageConfidenceGauge = new client.Gauge({
      name: 'reynolds_loop_prevention_average_confidence',
      help: 'Average confidence level across all events',
      registers: [this.registry]
    });

    this.confidenceThresholdGauge = new client.Gauge({
      name: 'reynolds_loop_prevention_confidence_threshold',
      help: 'Configured confidence threshold (99.9%)',
      registers: [this.registry]
    });

    this.confidenceViolationsCounter = new client.Counter({
      name: 'reynolds_loop_prevention_confidence_violations_total',
      help: 'Number of times confidence dropped below threshold',
      registers: [this.registry]
    });
  }

  initializeReynoldsMetrics() {
    // Reynolds Issue Obsession™ specific metrics
    this.reynoldsObsessionCounter = new client.Counter({
      name: 'reynolds_issue_obsession_patterns_total',
      help: 'Total Reynolds Issue Obsession™ patterns detected',
      labelNames: ['obsession_type', 'severity'],
      registers: [this.registry]
    });

    this.reynoldsObsessionActiveGauge = new client.Gauge({
      name: 'reynolds_issue_obsession_active',
      help: 'Number of active Reynolds Issue Obsession™ patterns',
      labelNames: ['obsession_type'],
      registers: [this.registry]
    });

    // Pattern-specific metrics
    this.rapidFirePatternsCounter = new client.Counter({
      name: 'reynolds_rapid_fire_patterns_total',
      help: 'Total rapid fire patterns detected',
      labelNames: ['severity'],
      registers: [this.registry]
    });

    this.recursivePatternsCounter = new client.Counter({
      name: 'reynolds_recursive_patterns_total',
      help: 'Total recursive patterns detected',
      labelNames: ['severity'],
      registers: [this.registry]
    });

    this.fanOutPatternsCounter = new client.Counter({
      name: 'reynolds_fan_out_patterns_total',
      help: 'Total fan-out patterns detected',
      labelNames: ['severity'],
      registers: [this.registry]
    });
  }

  // Event tracking methods
  recordEventGeneration(eventType, hasParent = false) {
    this.eventGenerationCounter
      .labels(eventType, hasParent ? 'true' : 'false')
      .inc();
  }

  updateEventChainDepth(eventType, executionId, depth) {
    this.eventChainDepthGauge
      .labels(eventType, executionId || 'unknown')
      .set(depth);
  }

  updateEventChainSize(size) {
    this.eventChainSizeGauge.set(size);
  }

  updateActiveExecutions(count) {
    this.activeExecutionsGauge.set(count);
  }

  recordExecutionDuration(executionType, durationSeconds, success) {
    this.executionDurationHistogram
      .labels(executionType, success ? 'true' : 'false')
      .observe(durationSeconds);
  }

  recordEventProcessingTime(eventType, durationSeconds) {
    this.eventProcessingTime
      .labels(eventType)
      .observe(durationSeconds);
  }

  // Confidence tracking methods
  updateConfidence(eventId, executionId, confidence) {
    this.confidenceGauge
      .labels(eventId, executionId || 'unknown')
      .set(confidence);
  }

  updateAverageConfidence(averageConfidence) {
    this.averageConfidenceGauge.set(averageConfidence);
  }

  updateConfidenceThreshold(threshold) {
    this.confidenceThresholdGauge.set(threshold);
  }

  recordConfidenceViolation() {
    this.confidenceViolationsCounter.inc();
  }

  // Circuit breaker methods
  recordCircuitBreakerTrigger(patternType, severity = 'warning') {
    this.circuitBreakerCounter
      .labels(patternType, severity)
      .inc();
  }

  updateCircuitBreakerStatus(patternType, isOpen) {
    this.circuitBreakerOpenGauge
      .labels(patternType)
      .set(isOpen ? 1 : 0);
  }

  // Loop detection methods
  recordLoopDetection(patternType, severity = 'warning') {
    this.loopDetectionCounter
      .labels(patternType, severity)
      .inc();
  }

  recordPreventedExecution(patternType) {
    this.preventedExecutionsCounter
      .labels(patternType)
      .inc();
  }

  // Reynolds Issue Obsession™ methods
  recordReynoldsObsession(obsessionType, severity = 'info') {
    this.reynoldsObsessionCounter
      .labels(obsessionType, severity)
      .inc();
  }

  updateActiveReynoldsObsession(obsessionType, count) {
    this.reynoldsObsessionActiveGauge
      .labels(obsessionType)
      .set(count);
  }

  // Pattern-specific recording methods
  recordRapidFirePattern(severity = 'warning') {
    this.rapidFirePatternsCounter
      .labels(severity)
      .inc();
  }

  recordRecursivePattern(severity = 'warning') {
    this.recursivePatternsCounter
      .labels(severity)
      .inc();
  }

  recordFanOutPattern(severity = 'warning') {
    this.fanOutPatternsCounter
      .labels(severity)
      .inc();
  }

  // System health metrics
  getSystemHealthMetrics() {
    return {
      totalEvents: this.eventGenerationCounter.get(),
      activeExecutions: this.activeExecutionsGauge.get(),
      averageConfidence: this.averageConfidenceGauge.get(),
      circuitBreakersOpen: this.getTotalOpenCircuitBreakers(),
      loopsDetected: this.getTotalLoopsDetected(),
      reynoldsObsessionActive: this.getTotalActiveObsessions()
    };
  }

  getTotalOpenCircuitBreakers() {
    try {
      const metrics = this.circuitBreakerOpenGauge.get();
      return metrics.values?.reduce((sum, metric) => sum + metric.value, 0) || 0;
    } catch (error) {
      logger.error('Error getting circuit breaker metrics:', error);
      return 0;
    }
  }

  getTotalLoopsDetected() {
    try {
      const metrics = this.loopDetectionCounter.get();
      return metrics.values?.reduce((sum, metric) => sum + metric.value, 0) || 0;
    } catch (error) {
      logger.error('Error getting loop detection metrics:', error);
      return 0;
    }
  }

  getTotalActiveObsessions() {
    try {
      const metrics = this.reynoldsObsessionActiveGauge.get();
      return metrics.values?.reduce((sum, metric) => sum + metric.value, 0) || 0;
    } catch (error) {
      logger.error('Error getting Reynolds obsession metrics:', error);
      return 0;
    }
  }

  // Export metrics for Prometheus scraping
  getMetrics() {
    return this.registry.metrics();
  }

  // Clear metrics (useful for testing)
  clearMetrics() {
    this.registry.clear();
    logger.info('🧹 Prometheus metrics cleared');
  }
}

module.exports = PrometheusMetrics;