const redis = require('redis');
const logger = require('./logger');

class RedisManager {
  constructor() {
    this.client = null;
    this.connected = false;
    this.connectionString = process.env.REDIS_CONNECTION_STRING || 'redis://redis:6379';
    this.retryAttempts = 5;
    this.retryDelay = 2000; // 2 seconds
  }

  async initialize() {
    logger.info('🗄️ Initializing Redis connection...');
    
    try {
      this.client = redis.createClient({
        url: this.connectionString,
        retry_strategy: (options) => {
          if (options.error && options.error.code === 'ECONNREFUSED') {
            logger.error('Redis connection refused');
            return new Error('The Redis server is not available');
          }
          if (options.total_retry_time > 1000 * 60 * 60) {
            logger.error('Redis retry time exhausted');
            return new Error('Retry time exhausted');
          }
          if (options.attempt > this.retryAttempts) {
            logger.error('Redis max retry attempts exceeded');
            return new Error('Max retry attempts exceeded');
          }
          // Exponential backoff
          return Math.min(options.attempt * 100, 3000);
        }
      });

      this.client.on('error', (err) => {
        logger.error('Redis client error:', err);
        this.connected = false;
      });

      this.client.on('connect', () => {
        logger.info('✅ Redis connection established');
        this.connected = true;
      });

      this.client.on('disconnect', () => {
        logger.warn('⚠️ Redis disconnected');
        this.connected = false;
      });

      await this.client.connect();
      
      // Test the connection
      await this.client.ping();
      
      logger.info('✅ Redis connection initialized successfully');
      
    } catch (error) {
      logger.error('❌ Failed to initialize Redis:', error);
      // Don't throw error - Redis is optional for basic functionality
      this.connected = false;
    }
  }

  async set(key, value, expireSeconds = null) {
    if (!this.connected) {
      logger.warn('Redis not connected, skipping set operation');
      return false;
    }

    try {
      const serializedValue = JSON.stringify(value);
      
      if (expireSeconds) {
        await this.client.setEx(key, expireSeconds, serializedValue);
      } else {
        await this.client.set(key, serializedValue);
      }
      
      return true;
    } catch (error) {
      logger.error('Redis set error:', error);
      return false;
    }
  }

  async get(key) {
    if (!this.connected) {
      logger.warn('Redis not connected, skipping get operation');
      return null;
    }

    try {
      const value = await this.client.get(key);
      return value ? JSON.parse(value) : null;
    } catch (error) {
      logger.error('Redis get error:', error);
      return null;
    }
  }

  async delete(key) {
    if (!this.connected) {
      logger.warn('Redis not connected, skipping delete operation');
      return false;
    }

    try {
      await this.client.del(key);
      return true;
    } catch (error) {
      logger.error('Redis delete error:', error);
      return false;
    }
  }

  async exists(key) {
    if (!this.connected) {
      return false;
    }

    try {
      const result = await this.client.exists(key);
      return result === 1;
    } catch (error) {
      logger.error('Redis exists error:', error);
      return false;
    }
  }

  async setHash(key, field, value) {
    if (!this.connected) {
      return false;
    }

    try {
      await this.client.hSet(key, field, JSON.stringify(value));
      return true;
    } catch (error) {
      logger.error('Redis hSet error:', error);
      return false;
    }
  }

  async getHash(key, field) {
    if (!this.connected) {
      return null;
    }

    try {
      const value = await this.client.hGet(key, field);
      return value ? JSON.parse(value) : null;
    } catch (error) {
      logger.error('Redis hGet error:', error);
      return null;
    }
  }

  async getHashAll(key) {
    if (!this.connected) {
      return {};
    }

    try {
      const hash = await this.client.hGetAll(key);
      const result = {};
      
      for (const [field, value] of Object.entries(hash)) {
        try {
          result[field] = JSON.parse(value);
        } catch {
          result[field] = value; // Keep as string if not valid JSON
        }
      }
      
      return result;
    } catch (error) {
      logger.error('Redis hGetAll error:', error);
      return {};
    }
  }

  async increment(key, amount = 1) {
    if (!this.connected) {
      return 0;
    }

    try {
      return await this.client.incrBy(key, amount);
    } catch (error) {
      logger.error('Redis increment error:', error);
      return 0;
    }
  }

  async expire(key, seconds) {
    if (!this.connected) {
      return false;
    }

    try {
      await this.client.expire(key, seconds);
      return true;
    } catch (error) {
      logger.error('Redis expire error:', error);
      return false;
    }
  }

  // Cache orchestration patterns
  async cacheOrchestrationPattern(taskType, pattern) {
    const key = `orchestration:pattern:${taskType}`;
    return await this.set(key, pattern, 3600); // Cache for 1 hour
  }

  async getCachedOrchestrationPattern(taskType) {
    const key = `orchestration:pattern:${taskType}`;
    return await this.get(key);
  }

  // Agent performance caching
  async cacheAgentPerformance(agentId, performance) {
    const key = `agent:performance:${agentId}`;
    return await this.setHash('agent_performance', agentId, performance);
  }

  async getCachedAgentPerformance(agentId) {
    return await this.getHash('agent_performance', agentId);
  }

  // Task execution caching
  async cacheTaskExecution(taskId, execution) {
    const key = `task:execution:${taskId}`;
    return await this.set(key, execution, 1800); // Cache for 30 minutes
  }

  async getCachedTaskExecution(taskId) {
    const key = `task:execution:${taskId}`;
    return await this.get(key);
  }

  // Loop prevention tracking
  async trackEventChain(eventId, chainData) {
    const key = `loop_prevention:chain:${eventId}`;
    return await this.set(key, chainData, 3600); // Keep for 1 hour
  }

  async getEventChain(eventId) {
    const key = `loop_prevention:chain:${eventId}`;
    return await this.get(key);
  }

  isConnected() {
    return this.connected;
  }

  async healthCheck() {
    if (!this.connected) {
      return {
        status: 'disconnected',
        connected: false,
        error: 'Redis client not connected'
      };
    }

    try {
      const start = Date.now();
      await this.client.ping();
      const responseTime = Date.now() - start;
      
      return {
        status: 'healthy',
        connected: true,
        responseTime: `${responseTime}ms`
      };
    } catch (error) {
      return {
        status: 'unhealthy',
        connected: false,
        error: error.message
      };
    }
  }

  async close() {
    if (this.client) {
      try {
        await this.client.quit();
        logger.info('✅ Redis connection closed');
      } catch (error) {
        logger.error('Error closing Redis connection:', error);
      }
    }
  }
}

module.exports = RedisManager;