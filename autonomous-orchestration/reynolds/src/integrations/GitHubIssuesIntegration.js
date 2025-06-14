const axios = require('axios');
const logger = require('../utils/logger');

class GitHubIssuesIntegration {
  constructor() {
    this.token = process.env.GITHUB_TOKEN;
    this.owner = process.env.GITHUB_OWNER;
    this.repo = process.env.GITHUB_REPO;
    this.baseUrl = 'https://api.github.com';
    this.healthy = false;
    
    this.client = axios.create({
      baseURL: this.baseUrl,
      timeout: 10000,
      headers: {
        'Authorization': `token ${this.token}`,
        'Accept': 'application/vnd.github.v3+json',
        'User-Agent': 'Reynolds-Orchestrator/1.0.0'
      }
    });
  }

  async initialize() {
    if (!this.token || !this.owner || !this.repo) {
      logger.warn('⚠️ GitHub integration not configured (missing GITHUB_TOKEN, GITHUB_OWNER, or GITHUB_REPO)');
      return;
    }

    try {
      logger.info('🐙 Initializing GitHub Issues integration...');
      
      // Test connection by getting repository info
      await this.testConnection();
      
      this.healthy = true;
      logger.info('✅ GitHub Issues integration initialized successfully');
      
    } catch (error) {
      logger.error('❌ Failed to initialize GitHub integration:', error);
      this.healthy = false;
    }
  }

  async testConnection() {
    try {
      const response = await this.client.get(`/repos/${this.owner}/${this.repo}`);
      logger.debug('GitHub connection test successful:', response.data.name);
      return true;
    } catch (error) {
      logger.error('GitHub connection test failed:', error);
      throw error;
    }
  }

  async createIssue(issueData) {
    if (!this.healthy) {
      logger.warn('GitHub integration not healthy, skipping issue creation');
      return null;
    }

    try {
      const payload = {
        title: issueData.title,
        body: issueData.body || issueData.description,
        labels: issueData.labels || ['orchestration', 'automated'],
        assignees: issueData.assignees || []
      };

      if (issueData.milestone) {
        payload.milestone = issueData.milestone;
      }

      const response = await this.client.post(`/repos/${this.owner}/${this.repo}/issues`, payload);
      
      logger.info(`📋 Created GitHub issue: #${response.data.number} - ${response.data.title}`);
      
      return {
        number: response.data.number,
        id: response.data.id,
        url: response.data.html_url,
        title: response.data.title,
        state: response.data.state
      };

    } catch (error) {
      logger.error('Failed to create GitHub issue:', error);
      return null;
    }
  }

  async updateIssue(issueNumber, updates) {
    if (!this.healthy) {
      logger.warn('GitHub integration not healthy, skipping issue update');
      return null;
    }

    try {
      const response = await this.client.patch(`/repos/${this.owner}/${this.repo}/issues/${issueNumber}`, updates);
      
      logger.info(`📝 Updated GitHub issue: #${issueNumber}`);
      
      return {
        number: response.data.number,
        id: response.data.id,
        url: response.data.html_url,
        title: response.data.title,
        state: response.data.state
      };

    } catch (error) {
      logger.error(`Failed to update GitHub issue #${issueNumber}:`, error);
      return null;
    }
  }

  async closeIssue(issueNumber, closingComment = null) {
    if (!this.healthy) {
      logger.warn('GitHub integration not healthy, skipping issue closure');
      return null;
    }

    try {
      // Add closing comment if provided
      if (closingComment) {
        await this.addComment(issueNumber, closingComment);
      }

      // Close the issue
      const response = await this.updateIssue(issueNumber, { state: 'closed' });
      
      logger.info(`✅ Closed GitHub issue: #${issueNumber}`);
      
      return response;

    } catch (error) {
      logger.error(`Failed to close GitHub issue #${issueNumber}:`, error);
      return null;
    }
  }

  async addComment(issueNumber, comment) {
    if (!this.healthy) {
      logger.warn('GitHub integration not healthy, skipping comment addition');
      return null;
    }

    try {
      const response = await this.client.post(`/repos/${this.owner}/${this.repo}/issues/${issueNumber}/comments`, {
        body: comment
      });
      
      logger.debug(`💬 Added comment to GitHub issue #${issueNumber}`);
      
      return {
        id: response.data.id,
        url: response.data.html_url,
        body: response.data.body
      };

    } catch (error) {
      logger.error(`Failed to add comment to GitHub issue #${issueNumber}:`, error);
      return null;
    }
  }

  async createMasterTrackingIssue(task, strategy) {
    const issueBody = this.generateMasterIssueBody(task, strategy);
    
    return await this.createIssue({
      title: `🎭 Reynolds Orchestration: ${task.type} - ${task.description}`,
      body: issueBody,
      labels: ['orchestration', 'master-tracking', 'reynolds', strategy.approach],
      assignees: []
    });
  }

  async createSubtaskIssue(subtask, parentIssue) {
    const issueBody = this.generateSubtaskIssueBody(subtask, parentIssue);
    
    return await this.createIssue({
      title: `🤖 Subtask: ${subtask.type} - ${subtask.description}`,
      body: issueBody,
      labels: ['orchestration', 'subtask', subtask.suggestedAgent, subtask.priority],
      assignees: []
    });
  }

  generateMasterIssueBody(task, strategy) {
    return `
# 🎭 Reynolds Orchestration Tracking

**Task Type:** ${task.type}
**Description:** ${task.description}
**Strategy:** ${strategy.approach}
**Started:** ${new Date().toISOString()}

## Task Details
${task.components ? `**Components:** ${task.components.length} items` : ''}
${task.deadline ? `**Deadline:** ${task.deadline}` : ''}
${task.priority ? `**Priority:** ${task.priority}` : ''}

## Orchestration Strategy
- **Approach:** ${strategy.approach}
- **Max Concurrency:** ${strategy.maxConcurrency || 'Adaptive'}
- **Reason:** ${strategy.reason || 'Parallel execution optimized for efficiency'}

## Execution Progress
This issue will be updated with progress as subtasks are completed.

---
*This issue was created automatically by Reynolds Orchestrator with Maximum Effort™*
*🎭 "With great power comes great responsibility... and excellent project tracking."*
    `.trim();
  }

  generateSubtaskIssueBody(subtask, parentIssue) {
    return `
# 🤖 Subtask Execution

**Parent Task:** ${parentIssue ? `#${parentIssue.number}` : 'N/A'}
**Agent Type:** ${subtask.suggestedAgent}
**Priority:** ${subtask.priority}
**Can Parallelize:** ${subtask.canParallelize ? 'Yes' : 'No'}

## Subtask Details
${subtask.items ? `**Items to Process:** ${subtask.items.map(item => `\n- ${item.name || item}`).join('')}` : ''}
${subtask.estimatedDurationMs ? `**Estimated Duration:** ${Math.round(subtask.estimatedDurationMs / 1000)}s` : ''}

## Required Capabilities
${subtask.requiredCapabilities ? subtask.requiredCapabilities.map(cap => `- ${cap}`).join('\n') : 'Standard capabilities'}

## Dependencies
${subtask.dependencies && subtask.dependencies.length > 0 ? 
  subtask.dependencies.map(dep => `- ${dep}`).join('\n') : 
  'No dependencies - can execute immediately'}

---
*This subtask is part of the Reynolds Orchestration system*
*🤖 Agent coordination with supernatural efficiency*
    `.trim();
  }

  async updateProgressComment(issueNumber, progress) {
    const progressComment = this.generateProgressComment(progress);
    return await this.addComment(issueNumber, progressComment);
  }

  generateProgressComment(progress) {
    const { completed, total, successRate, executionTime } = progress;
    const percentage = Math.round((completed / total) * 100);
    
    return `
## 📊 Progress Update

**Completion:** ${completed}/${total} (${percentage}%)
**Success Rate:** ${Math.round(successRate * 100)}%
**Execution Time:** ${Math.round(executionTime / 1000)}s

${this.generateProgressBar(percentage)}

${percentage === 100 ? 
  '🎉 **Task completed!** Reynolds strikes again with supernatural efficiency.' :
  `⚡ **In progress...** Maximum Effort™ mode engaged. ${total - completed} tasks remaining.`}

---
*Updated automatically by Reynolds Orchestrator*
    `.trim();
  }

  generateProgressBar(percentage) {
    const filled = Math.round(percentage / 5);
    const empty = 20 - filled;
    const bar = '█'.repeat(filled) + '░'.repeat(empty);
    return `\`${bar}\` ${percentage}%`;
  }

  async createFailureReport(subtask, agent, error) {
    const failureBody = `
# 🚨 Subtask Execution Failure

**Agent:** ${agent.id} (${agent.type})
**Subtask:** ${subtask.type}
**Error:** ${error.message}

## Failure Details
- **Timestamp:** ${new Date().toISOString()}
- **Agent Load:** ${agent.currentLoad}/${agent.maxLoad}
- **Agent Success Rate:** ${Math.round(agent.successRate * 100)}%

## Error Information
\`\`\`
${error.stack || error.message}
\`\`\`

## Recovery Actions
This failure has been logged for pattern analysis and will inform future orchestration decisions.

---
*Failure report generated by Reynolds Loop Prevention Engine*
*🛡️ "Learning from failures is just another form of Maximum Effort™"*
    `.trim();

    return await this.createIssue({
      title: `🚨 Failure Report: ${subtask.type} - ${agent.id}`,
      body: failureBody,
      labels: ['failure', 'analysis', 'reynolds', agent.type],
      assignees: []
    });
  }

  isHealthy() {
    return this.healthy;
  }

  async getRepositoryStats() {
    if (!this.healthy) {
      return null;
    }

    try {
      const response = await this.client.get(`/repos/${this.owner}/${this.repo}`);
      return {
        name: response.data.name,
        fullName: response.data.full_name,
        openIssues: response.data.open_issues_count,
        stargazers: response.data.stargazers_count,
        language: response.data.language,
        lastUpdated: response.data.updated_at
      };
    } catch (error) {
      logger.error('Failed to get repository stats:', error);
      return null;
    }
  }

  async searchOrchestrationIssues(query = '') {
    if (!this.healthy) {
      return [];
    }

    try {
      const searchQuery = `repo:${this.owner}/${this.repo} label:orchestration ${query}`;
      const response = await this.client.get(`/search/issues?q=${encodeURIComponent(searchQuery)}`);
      
      return response.data.items.map(issue => ({
        number: issue.number,
        title: issue.title,
        state: issue.state,
        url: issue.html_url,
        createdAt: issue.created_at,
        updatedAt: issue.updated_at,
        labels: issue.labels.map(label => label.name)
      }));

    } catch (error) {
      logger.error('Failed to search orchestration issues:', error);
      return [];
    }
  }
}

module.exports = GitHubIssuesIntegration;