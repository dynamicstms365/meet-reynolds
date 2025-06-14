const winston = require('winston');
const path = require('path');

// Create logs directory if it doesn't exist
const fs = require('fs');
const logsDir = path.join(__dirname, '../../logs');
if (!fs.existsSync(logsDir)) {
  fs.mkdirSync(logsDir, { recursive: true });
}

// Custom format for Reynolds personality
const reynoldsFormat = winston.format.combine(
  winston.format.timestamp({
    format: 'YYYY-MM-DD HH:mm:ss'
  }),
  winston.format.errors({ stack: true }),
  winston.format.json(),
  winston.format.printf(({ timestamp, level, message, ...meta }) => {
    // Add Reynolds charm to error messages
    if (level === 'error' && typeof message === 'string') {
      const reynoldsComments = [
        '🎭 Well, that didn\'t go as planned...',
        '🎭 Even supernatural orchestrators hit bumps...',
        '🎭 Maximum Effort™ debugging mode activated...',
        '🎭 Time for some Reynolds-style problem solving...'
      ];
      
      if (Math.random() < 0.1) { // 10% chance for Reynolds comment
        meta.reynoldsComment = reynoldsComments[Math.floor(Math.random() * reynoldsComments.length)];
      }
    }
    
    const logObject = {
      timestamp,
      level,
      message,
      ...meta
    };
    
    return JSON.stringify(logObject);
  })
);

// Create Winston logger instance
const logger = winston.createLogger({
  level: process.env.LOG_LEVEL || 'info',
  format: reynoldsFormat,
  defaultMeta: { 
    service: 'reynolds-orchestrator',
    version: '1.0.0'
  },
  transports: [
    // Console transport with colorized output for development
    new winston.transports.Console({
      format: winston.format.combine(
        winston.format.colorize(),
        winston.format.timestamp({
          format: 'HH:mm:ss'
        }),
        winston.format.printf(({ timestamp, level, message, ...meta }) => {
          let output = `${timestamp} [${level}] ${message}`;
          
          // Add metadata if present
          if (Object.keys(meta).length > 0) {
            // Filter out internal winston properties
            const cleanMeta = Object.fromEntries(
              Object.entries(meta).filter(([key]) => 
                !['timestamp', 'level', 'message', 'service', 'version'].includes(key)
              )
            );
            
            if (Object.keys(cleanMeta).length > 0) {
              output += ` ${JSON.stringify(cleanMeta)}`;
            }
          }
          
          return output;
        })
      ),
      handleExceptions: true,
      handleRejections: true
    }),
    
    // File transport for all logs
    new winston.transports.File({
      filename: path.join(logsDir, 'reynolds-orchestrator.log'),
      maxsize: 5242880, // 5MB
      maxFiles: 5,
      format: reynoldsFormat
    }),
    
    // Separate file for errors
    new winston.transports.File({
      filename: path.join(logsDir, 'errors.log'),
      level: 'error',
      maxsize: 5242880, // 5MB
      maxFiles: 3,
      format: reynoldsFormat
    }),
    
    // Separate file for orchestration events
    new winston.transports.File({
      filename: path.join(logsDir, 'orchestration.log'),
      level: 'info',
      maxsize: 10485760, // 10MB
      maxFiles: 10,
      format: winston.format.combine(
        reynoldsFormat,
        winston.format((info) => {
          // Only log orchestration-related messages
          if (info.message && (
            info.message.includes('orchestrat') ||
            info.message.includes('agent') ||
            info.message.includes('task') ||
            info.message.includes('parallel') ||
            info.message.includes('🎭') ||
            info.message.includes('🤖') ||
            info.message.includes('⚡')
          )) {
            return info;
          }
          return false;
        })()
      )
    })
  ],
  
  // Handle uncaught exceptions
  exceptionHandlers: [
    new winston.transports.File({
      filename: path.join(logsDir, 'exceptions.log'),
      maxsize: 5242880,
      maxFiles: 2
    })
  ],
  
  // Handle unhandled promise rejections
  rejectionHandlers: [
    new winston.transports.File({
      filename: path.join(logsDir, 'rejections.log'),
      maxsize: 5242880,
      maxFiles: 2
    })
  ]
});

// Add custom logging methods for Reynolds personality
logger.reynolds = {
  charm: (message, meta = {}) => {
    logger.info(`🎭 ${message}`, { ...meta, reynoldsCharm: true });
  },
  
  maxEffort: (message, meta = {}) => {
    logger.info(`💪 Maximum Effort™: ${message}`, { ...meta, maxEffort: true });
  },
  
  orchestration: (message, meta = {}) => {
    logger.info(`🎵 Orchestration: ${message}`, { ...meta, orchestration: true });
  },
  
  agentCoordination: (message, meta = {}) => {
    logger.info(`🤖 Agent Coordination: ${message}`, { ...meta, agentCoordination: true });
  },
  
  loopPrevention: (message, meta = {}) => {
    logger.info(`🛡️ Loop Prevention: ${message}`, { ...meta, loopPrevention: true });
  },
  
  experimentation: (message, meta = {}) => {
    logger.info(`🧪 Experimentation: ${message}`, { ...meta, experimentation: true });
  },
  
  wisdom: (message, meta = {}) => {
    const wisdomQuotes = [
      "Like I always say, with great power comes great electricity bills.",
      "The key to orchestration is knowing when to lead and when to let the agents do their thing.",
      "Maximum Effort™ isn't just about working hard, it's about working smart... and with style.",
      "In orchestration, as in life, timing is everything. And good one-liners don't hurt either."
    ];
    
    const randomWisdom = wisdomQuotes[Math.floor(Math.random() * wisdomQuotes.length)];
    logger.info(`🧠 Reynolds Wisdom: ${message}`, { 
      ...meta, 
      wisdom: true,
      randomWisdom: Math.random() < 0.2 ? randomWisdom : undefined
    });
  }
};

// Performance logging helpers
logger.performance = {
  start: (operation) => {
    const startTime = process.hrtime.bigint();
    return {
      operation,
      startTime,
      end: (meta = {}) => {
        const endTime = process.hrtime.bigint();
        const duration = Number(endTime - startTime) / 1000000; // Convert to milliseconds
        
        logger.info(`⏱️ Performance: ${operation} completed`, {
          ...meta,
          duration: `${duration.toFixed(2)}ms`,
          performance: true
        });
        
        return duration;
      }
    };
  },
  
  measure: async (operation, asyncFn, meta = {}) => {
    const timer = logger.performance.start(operation);
    try {
      const result = await asyncFn();
      timer.end({ ...meta, success: true });
      return result;
    } catch (error) {
      timer.end({ ...meta, success: false, error: error.message });
      throw error;
    }
  }
};

// Structured logging for different event types
logger.event = {
  taskStarted: (taskId, taskType, meta = {}) => {
    logger.info(`📋 Task Started: ${taskType}`, {
      taskId,
      taskType,
      event: 'task_started',
      ...meta
    });
  },
  
  taskCompleted: (taskId, taskType, success, duration, meta = {}) => {
    const emoji = success ? '✅' : '❌';
    logger.info(`${emoji} Task ${success ? 'Completed' : 'Failed'}: ${taskType}`, {
      taskId,
      taskType,
      success,
      duration,
      event: 'task_completed',
      ...meta
    });
  },
  
  agentAssigned: (agentId, taskId, taskType, meta = {}) => {
    logger.info(`🤖 Agent Assigned: ${agentId} → ${taskType}`, {
      agentId,
      taskId,
      taskType,
      event: 'agent_assigned',
      ...meta
    });
  },
  
  orchestrationDecision: (taskId, shouldOrchestrate, confidence, strategy, meta = {}) => {
    const emoji = shouldOrchestrate ? '⚡' : '🎯';
    const decision = shouldOrchestrate ? 'ORCHESTRATE' : 'DIRECT';
    logger.info(`${emoji} Orchestration Decision: ${decision} (${(confidence * 100).toFixed(1)}%)`, {
      taskId,
      decision,
      confidence,
      strategy,
      event: 'orchestration_decision',
      ...meta
    });
  },
  
  parallelExecution: (taskId, subtaskCount, strategy, meta = {}) => {
    logger.info(`🔄 Parallel Execution: ${subtaskCount} subtasks with ${strategy} strategy`, {
      taskId,
      subtaskCount,
      strategy,
      event: 'parallel_execution',
      ...meta
    });
  },
  
  circuitBreakerTriggered: (pattern, contextId, meta = {}) => {
    logger.error(`🚨 Circuit Breaker Triggered: ${pattern}`, {
      pattern,
      contextId,
      event: 'circuit_breaker_triggered',
      severity: 'critical',
      ...meta
    });
  }
};

// Development vs Production logging adjustments
if (process.env.NODE_ENV === 'production') {
  // In production, reduce console verbosity and add more structured logging
  logger.remove(logger.transports.find(t => t.name === 'console'));
  logger.add(new winston.transports.Console({
    level: 'warn',
    format: winston.format.combine(
      winston.format.timestamp(),
      winston.format.errors({ stack: true }),
      winston.format.json()
    )
  }));
  
  // Add monitoring/alerting transports in production
  // These would typically integrate with services like DataDog, New Relic, etc.
} else {
  // Development mode - more verbose logging
  logger.level = 'debug';
}

// Export logger with type information for better IDE support
module.exports = logger;

// Also export specific loggers for organized logging
module.exports.orchestrationLogger = logger.child({ component: 'orchestration' });
module.exports.agentLogger = logger.child({ component: 'agent' });
module.exports.mcpLogger = logger.child({ component: 'mcp' });
module.exports.performanceLogger = logger.child({ component: 'performance' });