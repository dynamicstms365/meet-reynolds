const { Pool } = require('pg');
const logger = require('./logger');

class DatabaseManager {
  constructor() {
    this.pool = null;
    this.connected = false;
    this.connectionString = process.env.POSTGRES_CONNECTION_STRING || 
      'postgresql://reynolds:password@postgres:5432/orchestration';
    
    this.retryAttempts = 5;
    this.retryDelay = 5000; // 5 seconds
  }

  async initialize() {
    logger.info('🗄️ Initializing PostgreSQL connection...');
    
    try {
      this.pool = new Pool({
        connectionString: this.connectionString,
        max: 20,
        idleTimeoutMillis: 30000,
        connectionTimeoutMillis: 2000,
      });

      // Test connection
      await this.testConnection();
      
      // Create tables if they don't exist
      await this.createTables();
      
      this.connected = true;
      logger.info('✅ PostgreSQL connection established');
      
    } catch (error) {
      logger.error('❌ Failed to initialize database:', error);
      throw error;
    }
  }

  async testConnection() {
    const client = await this.pool.connect();
    try {
      const result = await client.query('SELECT NOW()');
      logger.debug('Database connection test successful:', result.rows[0]);
    } finally {
      client.release();
    }
  }

  async createTables() {
    const client = await this.pool.connect();
    
    try {
      await client.query('BEGIN');
      
      // Tasks table
      await client.query(`
        CREATE TABLE IF NOT EXISTS tasks (
          id UUID PRIMARY KEY,
          type VARCHAR(100) NOT NULL,
          description TEXT,
          status VARCHAR(50) DEFAULT 'pending',
          created_at TIMESTAMP DEFAULT NOW(),
          started_at TIMESTAMP,
          completed_at TIMESTAMP,
          metadata JSONB,
          result JSONB
        )
      `);

      // Subtasks table
      await client.query(`
        CREATE TABLE IF NOT EXISTS subtasks (
          id UUID PRIMARY KEY,
          parent_task_id UUID REFERENCES tasks(id),
          type VARCHAR(100) NOT NULL,
          description TEXT,
          agent_type VARCHAR(50),
          agent_id VARCHAR(100),
          status VARCHAR(50) DEFAULT 'pending',
          created_at TIMESTAMP DEFAULT NOW(),
          started_at TIMESTAMP,
          completed_at TIMESTAMP,
          metadata JSONB,
          result JSONB
        )
      `);

      // Orchestration patterns table
      await client.query(`
        CREATE TABLE IF NOT EXISTS orchestration_patterns (
          id SERIAL PRIMARY KEY,
          task_type VARCHAR(100) NOT NULL,
          agent_type VARCHAR(50),
          strategy VARCHAR(50),
          success_rate DECIMAL(5,4),
          avg_execution_time INTEGER,
          total_executions INTEGER DEFAULT 1,
          created_at TIMESTAMP DEFAULT NOW(),
          updated_at TIMESTAMP DEFAULT NOW()
        )
      `);

      // Failure patterns table
      await client.query(`
        CREATE TABLE IF NOT EXISTS failure_patterns (
          id SERIAL PRIMARY KEY,
          subtask_type VARCHAR(100) NOT NULL,
          agent_id VARCHAR(100),
          error_message TEXT,
          error_pattern VARCHAR(200),
          occurrence_count INTEGER DEFAULT 1,
          first_seen TIMESTAMP DEFAULT NOW(),
          last_seen TIMESTAMP DEFAULT NOW(),
          metadata JSONB
        )
      `);

      // Agent performance table
      await client.query(`
        CREATE TABLE IF NOT EXISTS agent_performance (
          id SERIAL PRIMARY KEY,
          agent_id VARCHAR(100) NOT NULL,
          agent_type VARCHAR(50) NOT NULL,
          task_type VARCHAR(100),
          execution_time INTEGER,
          success BOOLEAN,
          resource_usage JSONB,
          timestamp TIMESTAMP DEFAULT NOW()
        )
      `);

      // Orchestration decisions table
      await client.query(`
        CREATE TABLE IF NOT EXISTS orchestration_decisions (
          id SERIAL PRIMARY KEY,
          task_id UUID,
          should_orchestrate BOOLEAN,
          confidence DECIMAL(5,4),
          strategy VARCHAR(50),
          signals JSONB,
          timestamp TIMESTAMP DEFAULT NOW()
        )
      `);

      // Create indexes for better performance
      await client.query(`
        CREATE INDEX IF NOT EXISTS idx_tasks_status ON tasks(status);
        CREATE INDEX IF NOT EXISTS idx_tasks_type ON tasks(type);
        CREATE INDEX IF NOT EXISTS idx_subtasks_parent ON subtasks(parent_task_id);
        CREATE INDEX IF NOT EXISTS idx_subtasks_agent ON subtasks(agent_id);
        CREATE INDEX IF NOT EXISTS idx_patterns_task_type ON orchestration_patterns(task_type);
        CREATE INDEX IF NOT EXISTS idx_failures_agent ON failure_patterns(agent_id);
        CREATE INDEX IF NOT EXISTS idx_performance_agent ON agent_performance(agent_id);
        CREATE INDEX IF NOT EXISTS idx_performance_timestamp ON agent_performance(timestamp);
      `);

      await client.query('COMMIT');
      logger.info('✅ Database tables created/verified');
      
    } catch (error) {
      await client.query('ROLLBACK');
      logger.error('❌ Failed to create database tables:', error);
      throw error;
    } finally {
      client.release();
    }
  }

  async logTask(task, result = null) {
    if (!this.connected) return;
    
    try {
      const query = `
        INSERT INTO tasks (id, type, description, status, started_at, completed_at, metadata, result)
        VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
        ON CONFLICT (id) DO UPDATE SET
          status = $4,
          completed_at = $6,
          result = $8
      `;
      
      const values = [
        task.id,
        task.type,
        task.description,
        result ? (result.success ? 'completed' : 'failed') : 'running',
        task.startTime ? new Date(task.startTime) : new Date(),
        result ? new Date() : null,
        JSON.stringify(task),
        result ? JSON.stringify(result) : null
      ];
      
      await this.pool.query(query, values);
      
    } catch (error) {
      logger.error('Failed to log task to database:', error);
    }
  }

  async logSubtask(subtask, agentId, result = null) {
    if (!this.connected) return;
    
    try {
      const query = `
        INSERT INTO subtasks (id, parent_task_id, type, description, agent_type, agent_id, status, started_at, completed_at, metadata, result)
        VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11)
        ON CONFLICT (id) DO UPDATE SET
          agent_id = $6,
          status = $7,
          completed_at = $9,
          result = $11
      `;
      
      const values = [
        subtask.id,
        subtask.parentTaskId,
        subtask.type,
        subtask.description,
        subtask.suggestedAgent,
        agentId,
        result ? (result.success ? 'completed' : 'failed') : 'running',
        new Date(),
        result ? new Date() : null,
        JSON.stringify(subtask),
        result ? JSON.stringify(result) : null
      ];
      
      await this.pool.query(query, values);
      
    } catch (error) {
      logger.error('Failed to log subtask to database:', error);
    }
  }

  async updateOrchestrationPattern(taskType, agentType, strategy, executionTime, success) {
    if (!this.connected) return;
    
    try {
      // First, try to update existing pattern
      const updateQuery = `
        UPDATE orchestration_patterns 
        SET 
          success_rate = (success_rate * total_executions + $5) / (total_executions + 1),
          avg_execution_time = (avg_execution_time * total_executions + $4) / (total_executions + 1),
          total_executions = total_executions + 1,
          updated_at = NOW()
        WHERE task_type = $1 AND agent_type = $2 AND strategy = $3
        RETURNING id
      `;
      
      const result = await this.pool.query(updateQuery, [
        taskType, agentType, strategy, executionTime, success ? 1 : 0
      ]);
      
      // If no existing pattern, create new one
      if (result.rows.length === 0) {
        const insertQuery = `
          INSERT INTO orchestration_patterns (task_type, agent_type, strategy, success_rate, avg_execution_time)
          VALUES ($1, $2, $3, $4, $5)
        `;
        
        await this.pool.query(insertQuery, [
          taskType, agentType, strategy, success ? 1 : 0, executionTime
        ]);
      }
      
    } catch (error) {
      logger.error('Failed to update orchestration pattern:', error);
    }
  }

  async logFailurePattern(failureData) {
    if (!this.connected) return;
    
    try {
      const errorPattern = this.extractErrorPattern(failureData.error);
      
      // Try to update existing failure pattern
      const updateQuery = `
        UPDATE failure_patterns 
        SET 
          occurrence_count = occurrence_count + 1,
          last_seen = NOW(),
          metadata = $5
        WHERE subtask_type = $1 AND agent_id = $2 AND error_pattern = $3
        RETURNING id
      `;
      
      const result = await this.pool.query(updateQuery, [
        failureData.subtaskType,
        failureData.agentId,
        errorPattern,
        failureData.error,
        JSON.stringify(failureData)
      ]);
      
      // If no existing pattern, create new one
      if (result.rows.length === 0) {
        const insertQuery = `
          INSERT INTO failure_patterns (subtask_type, agent_id, error_message, error_pattern, metadata)
          VALUES ($1, $2, $3, $4, $5)
        `;
        
        await this.pool.query(insertQuery, [
          failureData.subtaskType,
          failureData.agentId,
          failureData.error,
          errorPattern,
          JSON.stringify(failureData)
        ]);
      }
      
    } catch (error) {
      logger.error('Failed to log failure pattern:', error);
    }
  }

  extractErrorPattern(errorMessage) {
    // Extract common error patterns for learning
    const patterns = [
      { pattern: 'timeout', regex: /(timeout|timed out)/i },
      { pattern: 'connection', regex: /(connection|connect)/i },
      { pattern: 'authentication', regex: /(auth|unauthorized|forbidden)/i },
      { pattern: 'not_found', regex: /(not found|404|missing)/i },
      { pattern: 'validation', regex: /(validation|invalid|schema)/i },
      { pattern: 'permission', regex: /(permission|access denied)/i },
      { pattern: 'resource', regex: /(resource|memory|cpu|disk)/i }
    ];
    
    for (const { pattern, regex } of patterns) {
      if (regex.test(errorMessage)) {
        return pattern;
      }
    }
    
    return 'unknown';
  }

  async logAgentPerformance(agentId, agentType, taskType, executionTime, success, resourceUsage = {}) {
    if (!this.connected) return;
    
    try {
      const query = `
        INSERT INTO agent_performance (agent_id, agent_type, task_type, execution_time, success, resource_usage)
        VALUES ($1, $2, $3, $4, $5, $6)
      `;
      
      await this.pool.query(query, [
        agentId,
        agentType,
        taskType,
        executionTime,
        success,
        JSON.stringify(resourceUsage)
      ]);
      
    } catch (error) {
      logger.error('Failed to log agent performance:', error);
    }
  }

  async logOrchestrationDecision(taskId, shouldOrchestrate, confidence, strategy, signals) {
    if (!this.connected) return;
    
    try {
      const query = `
        INSERT INTO orchestration_decisions (task_id, should_orchestrate, confidence, strategy, signals)
        VALUES ($1, $2, $3, $4, $5)
      `;
      
      await this.pool.query(query, [
        taskId,
        shouldOrchestrate,
        confidence,
        strategy,
        JSON.stringify(signals)
      ]);
      
    } catch (error) {
      logger.error('Failed to log orchestration decision:', error);
    }
  }

  async findSimilarPatterns(task) {
    if (!this.connected) return 0.5;
    
    try {
      const query = `
        SELECT AVG(success_rate) as avg_success_rate, COUNT(*) as pattern_count
        FROM orchestration_patterns 
        WHERE task_type = $1 OR task_type ILIKE $2
      `;
      
      const result = await this.pool.query(query, [
        task.type,
        `%${task.type.split('_')[0]}%`
      ]);
      
      if (result.rows.length > 0 && result.rows[0].pattern_count > 0) {
        return parseFloat(result.rows[0].avg_success_rate) || 0.5;
      }
      
      return 0.5;
      
    } catch (error) {
      logger.error('Failed to find similar patterns:', error);
      return 0.5;
    }
  }

  async getAgentPerformanceStats(agentId, timeWindow = '24 hours') {
    if (!this.connected) return null;
    
    try {
      const query = `
        SELECT 
          COUNT(*) as total_tasks,
          AVG(execution_time) as avg_execution_time,
          COUNT(*) FILTER (WHERE success = true) as successful_tasks,
          COUNT(*) FILTER (WHERE success = false) as failed_tasks,
          AVG(CASE WHEN success THEN execution_time END) as avg_success_time,
          MAX(timestamp) as last_activity
        FROM agent_performance 
        WHERE agent_id = $1 AND timestamp > NOW() - INTERVAL '${timeWindow}'
      `;
      
      const result = await this.pool.query(query, [agentId]);
      
      if (result.rows.length > 0) {
        const stats = result.rows[0];
        return {
          totalTasks: parseInt(stats.total_tasks),
          averageExecutionTime: parseFloat(stats.avg_execution_time) || 0,
          successfulTasks: parseInt(stats.successful_tasks),
          failedTasks: parseInt(stats.failed_tasks),
          successRate: stats.total_tasks > 0 ? stats.successful_tasks / stats.total_tasks : 0,
          averageSuccessTime: parseFloat(stats.avg_success_time) || 0,
          lastActivity: stats.last_activity
        };
      }
      
      return null;
      
    } catch (error) {
      logger.error('Failed to get agent performance stats:', error);
      return null;
    }
  }

  async getOrchestrationInsights(timeWindow = '7 days') {
    if (!this.connected) return {};
    
    try {
      // Get orchestration vs direct execution statistics
      const orchestrationQuery = `
        SELECT 
          should_orchestrate,
          AVG(confidence) as avg_confidence,
          COUNT(*) as decision_count
        FROM orchestration_decisions 
        WHERE timestamp > NOW() - INTERVAL '${timeWindow}'
        GROUP BY should_orchestrate
      `;
      
      const orchestrationResult = await this.pool.query(orchestrationQuery);
      
      // Get most successful patterns
      const patternsQuery = `
        SELECT 
          task_type,
          strategy,
          AVG(success_rate) as avg_success_rate,
          AVG(avg_execution_time) as avg_time,
          SUM(total_executions) as total_executions
        FROM orchestration_patterns
        WHERE updated_at > NOW() - INTERVAL '${timeWindow}'
        GROUP BY task_type, strategy
        ORDER BY avg_success_rate DESC, total_executions DESC
        LIMIT 10
      `;
      
      const patternsResult = await this.pool.query(patternsQuery);
      
      // Get failure patterns
      const failuresQuery = `
        SELECT 
          error_pattern,
          COUNT(*) as occurrence_count,
          COUNT(DISTINCT agent_id) as affected_agents
        FROM failure_patterns
        WHERE last_seen > NOW() - INTERVAL '${timeWindow}'
        GROUP BY error_pattern
        ORDER BY occurrence_count DESC
        LIMIT 5
      `;
      
      const failuresResult = await this.pool.query(failuresQuery);
      
      return {
        orchestrationDecisions: orchestrationResult.rows,
        successfulPatterns: patternsResult.rows,
        failurePatterns: failuresResult.rows,
        timeWindow,
        generatedAt: new Date().toISOString()
      };
      
    } catch (error) {
      logger.error('Failed to get orchestration insights:', error);
      return {};
    }
  }

  async cleanup() {
    if (!this.connected) return;
    
    try {
      // Clean up old records to prevent database bloat
      const cleanupQueries = [
        "DELETE FROM agent_performance WHERE timestamp < NOW() - INTERVAL '30 days'",
        "DELETE FROM orchestration_decisions WHERE timestamp < NOW() - INTERVAL '30 days'",
        "DELETE FROM tasks WHERE created_at < NOW() - INTERVAL '90 days' AND status IN ('completed', 'failed')",
        "DELETE FROM subtasks WHERE created_at < NOW() - INTERVAL '90 days' AND status IN ('completed', 'failed')"
      ];
      
      for (const query of cleanupQueries) {
        const result = await this.pool.query(query);
        logger.debug(`Cleanup query executed: ${result.rowCount} rows affected`);
      }
      
      logger.info('✅ Database cleanup completed');
      
    } catch (error) {
      logger.error('Failed to cleanup database:', error);
    }
  }

  isConnected() {
    return this.connected;
  }

  async reconnect() {
    if (this.pool) {
      await this.pool.end();
    }
    
    this.connected = false;
    
    for (let attempt = 1; attempt <= this.retryAttempts; attempt++) {
      try {
        logger.info(`🔄 Attempting database reconnection (${attempt}/${this.retryAttempts})...`);
        await this.initialize();
        logger.info('✅ Database reconnection successful');
        return;
      } catch (error) {
        logger.error(`❌ Reconnection attempt ${attempt} failed:`, error);
        
        if (attempt < this.retryAttempts) {
          await new Promise(resolve => setTimeout(resolve, this.retryDelay));
        }
      }
    }
    
    throw new Error('Failed to reconnect to database after maximum attempts');
  }

  async close() {
    if (this.pool) {
      await this.pool.end();
      this.connected = false;
      logger.info('✅ Database connection closed');
    }
  }

  // Health check method
  async healthCheck() {
    try {
      const result = await this.pool.query('SELECT 1 as health_check');
      return {
        status: 'healthy',
        connected: this.connected,
        timestamp: new Date().toISOString()
      };
    } catch (error) {
      return {
        status: 'unhealthy',
        connected: false,
        error: error.message,
        timestamp: new Date().toISOString()
      };
    }
  }
}

module.exports = DatabaseManager;