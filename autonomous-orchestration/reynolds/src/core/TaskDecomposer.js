const logger = require('../utils/logger');
const { v4: uuidv4 } = require('uuid');

class TaskDecomposer {
  constructor() {
    // Decomposition patterns learned from MCP migration failure
    this.decompositionPatterns = {
      // Pattern for migration tasks (learned from MCP failure)
      migration: {
        estimatedSubtasks: (task) => {
          const items = task.items || task.components || [];
          return Math.max(items.length, 1);
        },
        parallelizationRatio: 0.9, // Most migration subtasks are independent
        decompose: this.decomposeMigrationTask.bind(this)
      },
      
      // Pattern for deployment tasks
      deployment: {
        estimatedSubtasks: (task) => {
          const phases = ['infrastructure', 'application', 'configuration', 'validation'];
          return phases.length + (task.components?.length || 0);
        },
        parallelizationRatio: 0.6, // Some dependencies between phases
        decompose: this.decomposeDeploymentTask.bind(this)
      },
      
      // Pattern for development tasks
      development: {
        estimatedSubtasks: (task) => {
          const features = task.features || task.requirements || [];
          return Math.max(features.length * 2, 3); // Each feature = code + tests
        },
        parallelizationRatio: 0.7, // Some shared dependencies
        decompose: this.decomposeDevelopmentTask.bind(this)
      },
      
      // Pattern for analysis tasks
      analysis: {
        estimatedSubtasks: (task) => {
          const sources = task.dataSources || task.repositories || task.components || [];
          return Math.max(sources.length, 2);
        },
        parallelizationRatio: 0.8, // Analysis tasks are often independent
        decompose: this.decomposeAnalysisTask.bind(this)
      },
      
      // Default pattern
      default: {
        estimatedSubtasks: (task) => 3,
        parallelizationRatio: 0.5,
        decompose: this.decomposeGenericTask.bind(this)
      }
    };
    
    // Agent type mappings for different task types
    this.agentMappings = {
      'infrastructure': 'devops',
      'deployment': 'devops', 
      'cicd': 'devops',
      'github': 'devops',
      'pipeline': 'devops',
      'container': 'devops',
      'power_platform': 'platform',
      'teams': 'platform',
      'm365': 'platform',
      'sharepoint': 'platform',
      'business_process': 'platform',
      'code': 'code',
      'development': 'code',
      'testing': 'code',
      'analysis': 'code',
      'refactor': 'code',
      'documentation': 'code'
    };
  }

  estimateSubtaskCount(task) {
    const pattern = this.identifyDecompositionPattern(task);
    return this.decompositionPatterns[pattern].estimatedSubtasks(task);
  }

  assessParallelizationPotential(task) {
    const pattern = this.identifyDecompositionPattern(task);
    return this.decompositionPatterns[pattern].parallelizationRatio;
  }

  async decompose(task, strategy) {
    logger.info(`🔧 Decomposing task: ${task.type}`, { 
      taskId: task.id, 
      strategy: strategy.approach 
    });

    const pattern = this.identifyDecompositionPattern(task);
    const decomposer = this.decompositionPatterns[pattern].decompose;
    
    const subtasks = await decomposer(task, strategy);
    
    // Apply strategy-specific optimizations
    const optimizedSubtasks = this.applyStrategyOptimizations(subtasks, strategy);
    
    // Add execution metadata
    const enrichedSubtasks = this.enrichSubtasks(optimizedSubtasks, task);
    
    logger.info(`✅ Task decomposed into ${enrichedSubtasks.length} subtasks`, {
      taskId: task.id,
      pattern,
      subtasks: enrichedSubtasks.map(st => ({
        id: st.id,
        type: st.type,
        suggestedAgent: st.suggestedAgent,
        canParallelize: st.canParallelize
      }))
    });

    return enrichedSubtasks;
  }

  identifyDecompositionPattern(task) {
    const taskType = (task.type || '').toLowerCase();
    const description = (task.description || '').toLowerCase();
    
    // Migration pattern (learned from MCP failure)
    if (taskType.includes('migration') || 
        taskType.includes('convert') || 
        description.includes('migrate') ||
        (task.items && task.items.length > 5)) {
      return 'migration';
    }
    
    // Deployment pattern
    if (taskType.includes('deploy') || 
        taskType.includes('release') || 
        taskType.includes('provision')) {
      return 'deployment';
    }
    
    // Development pattern
    if (taskType.includes('develop') || 
        taskType.includes('implement') || 
        taskType.includes('build') ||
        taskType.includes('create')) {
      return 'development';
    }
    
    // Analysis pattern
    if (taskType.includes('analyze') || 
        taskType.includes('review') || 
        taskType.includes('audit') ||
        taskType.includes('assess')) {
      return 'analysis';
    }
    
    return 'default';
  }

  // Decomposition method for migration tasks (addressing MCP failure)
  async decomposeMigrationTask(task, strategy) {
    const items = task.items || task.components || task.tools || [];
    
    if (items.length === 0) {
      return this.decomposeGenericTask(task, strategy);
    }

    const subtasks = [];
    
    // Group items for optimal parallel execution
    const groups = this.groupItemsForParallelExecution(items, strategy);
    
    for (const group of groups) {
      const subtask = {
        id: uuidv4(),
        type: `migration_${group.type}`,
        description: `Migrate ${group.items.length} ${group.type} items: ${group.items.map(i => i.name || i).join(', ')}`,
        items: group.items,
        suggestedAgent: this.mapToAgentType(group.type),
        canParallelize: true,
        dependencies: [],
        estimatedDurationMs: group.items.length * 60000, // 1 minute per item
        priority: group.priority || 'medium',
        requiredCapabilities: this.inferCapabilities(group.type, group.items)
      };
      
      subtasks.push(subtask);
    }
    
    // Add validation subtask that depends on all migrations
    subtasks.push({
      id: uuidv4(),
      type: 'migration_validation',
      description: 'Validate all migration results and ensure system integrity',
      suggestedAgent: 'devops',
      canParallelize: false,
      dependencies: subtasks.map(st => st.id),
      estimatedDurationMs: 120000, // 2 minutes
      priority: 'high',
      requiredCapabilities: ['validation', 'testing', 'integration_testing']
    });

    return subtasks;
  }

  // Decomposition method for deployment tasks
  async decomposeDeploymentTask(task, strategy) {
    const subtasks = [];
    const components = task.components || [];
    
    // Phase 1: Infrastructure preparation (must run first)
    if (task.requiresInfrastructure !== false) {
      subtasks.push({
        id: uuidv4(),
        type: 'infrastructure_setup',
        description: 'Provision and configure infrastructure resources',
        suggestedAgent: 'devops',
        canParallelize: false,
        dependencies: [],
        estimatedDurationMs: 300000, // 5 minutes
        priority: 'high',
        requiredCapabilities: ['infrastructure_deployment', 'azure_resources', 'networking']
      });
    }
    
    // Phase 2: Parallel component deployment
    const infrastructureId = subtasks[subtasks.length - 1]?.id;
    
    for (const component of components) {
      const agentType = this.classifyComponentForAgent(component);
      
      subtasks.push({
        id: uuidv4(),
        type: `deploy_${component.type || 'component'}`,
        description: `Deploy ${component.name || component.type} component`,
        component: component,
        suggestedAgent: agentType,
        canParallelize: true,
        dependencies: infrastructureId ? [infrastructureId] : [],
        estimatedDurationMs: 180000, // 3 minutes
        priority: component.priority || 'medium',
        requiredCapabilities: this.inferCapabilities(component.type, [component])
      });
    }
    
    // Phase 3: Integration and validation (depends on all deployments)
    const deploymentIds = subtasks.filter(st => st.type.startsWith('deploy_')).map(st => st.id);
    
    subtasks.push({
      id: uuidv4(),
      type: 'deployment_validation',
      description: 'Validate deployment integrity and run integration tests',
      suggestedAgent: 'devops',
      canParallelize: false,
      dependencies: deploymentIds,
      estimatedDurationMs: 240000, // 4 minutes
      priority: 'high',
      requiredCapabilities: ['integration_testing', 'health_checks', 'monitoring']
    });

    return subtasks;
  }

  // Decomposition method for development tasks
  async decomposeDevelopmentTask(task, strategy) {
    const features = task.features || task.requirements || [];
    const subtasks = [];
    
    if (features.length === 0) {
      return this.decomposeGenericTask(task, strategy);
    }

    for (const feature of features) {
      // Code implementation subtask
      subtasks.push({
        id: uuidv4(),
        type: `implement_${feature.type || 'feature'}`,
        description: `Implement ${feature.name || feature.description}`,
        feature: feature,
        suggestedAgent: 'code',
        canParallelize: true,
        dependencies: feature.dependencies || [],
        estimatedDurationMs: (feature.estimatedHours || 2) * 3600000,
        priority: feature.priority || 'medium',
        requiredCapabilities: this.inferCapabilities('code', [feature])
      });
      
      // Testing subtask (can run in parallel with other tests)
      subtasks.push({
        id: uuidv4(),
        type: `test_${feature.type || 'feature'}`,
        description: `Create and run tests for ${feature.name || feature.description}`,
        feature: feature,
        suggestedAgent: 'code',
        canParallelize: true,
        dependencies: [subtasks[subtasks.length - 1].id], // Depends on implementation
        estimatedDurationMs: (feature.estimatedHours || 1) * 1800000, // Half the implementation time
        priority: feature.priority || 'medium',
        requiredCapabilities: ['testing', 'test_automation', 'quality_assurance']
      });
    }
    
    // Integration subtask (depends on all implementations)
    const implementationIds = subtasks.filter(st => st.type.startsWith('implement_')).map(st => st.id);
    
    subtasks.push({
      id: uuidv4(),
      type: 'integration_development',
      description: 'Integrate all features and run comprehensive tests',
      suggestedAgent: 'devops',
      canParallelize: false,
      dependencies: implementationIds,
      estimatedDurationMs: 300000, // 5 minutes
      priority: 'high',
      requiredCapabilities: ['integration', 'cicd', 'deployment']
    });

    return subtasks;
  }

  // Decomposition method for analysis tasks
  async decomposeAnalysisTask(task, strategy) {
    const sources = task.dataSources || task.repositories || task.components || [];
    const subtasks = [];
    
    if (sources.length === 0) {
      return this.decomposeGenericTask(task, strategy);
    }

    // Parallel analysis of each source
    for (const source of sources) {
      subtasks.push({
        id: uuidv4(),
        type: `analyze_${source.type || 'source'}`,
        description: `Analyze ${source.name || source.path || source}`,
        source: source,
        suggestedAgent: this.mapToAgentType(source.type || 'code'),
        canParallelize: true,
        dependencies: [],
        estimatedDurationMs: 180000, // 3 minutes
        priority: 'medium',
        requiredCapabilities: this.inferCapabilities('analysis', [source])
      });
    }
    
    // Aggregation subtask (depends on all analyses)
    const analysisIds = subtasks.map(st => st.id);
    
    subtasks.push({
      id: uuidv4(),
      type: 'analysis_aggregation',
      description: 'Aggregate analysis results and generate comprehensive report',
      suggestedAgent: 'code',
      canParallelize: false,
      dependencies: analysisIds,
      estimatedDurationMs: 120000, // 2 minutes
      priority: 'high',
      requiredCapabilities: ['data_analysis', 'reporting', 'documentation']
    });

    return subtasks;
  }

  // Fallback decomposition method
  async decomposeGenericTask(task, strategy) {
    const subtasks = [];
    
    // Break into standard phases
    const phases = [
      {
        type: 'preparation',
        description: 'Prepare resources and validate prerequisites',
        canParallelize: false,
        agent: 'devops',
        duration: 120000
      },
      {
        type: 'execution',
        description: 'Execute main task operations',
        canParallelize: true,
        agent: this.mapToAgentType(task.type),
        duration: 300000
      },
      {
        type: 'validation',
        description: 'Validate results and cleanup',
        canParallelize: false,
        agent: 'devops',
        duration: 120000
      }
    ];

    for (let i = 0; i < phases.length; i++) {
      const phase = phases[i];
      subtasks.push({
        id: uuidv4(),
        type: `${task.type || 'generic'}_${phase.type}`,
        description: phase.description,
        suggestedAgent: phase.agent,
        canParallelize: phase.canParallelize,
        dependencies: i > 0 ? [subtasks[i - 1].id] : [],
        estimatedDurationMs: phase.duration,
        priority: 'medium',
        requiredCapabilities: this.inferCapabilities(phase.type, [task])
      });
    }

    return subtasks;
  }

  groupItemsForParallelExecution(items, strategy) {
    const maxConcurrency = strategy.maxConcurrency || 3;
    const groups = [];
    
    // Group similar items together for efficient processing
    const itemsByType = {};
    
    for (const item of items) {
      const type = item.type || this.inferItemType(item);
      if (!itemsByType[type]) {
        itemsByType[type] = [];
      }
      itemsByType[type].push(item);
    }
    
    // Create groups within concurrency limits
    for (const [type, typeItems] of Object.entries(itemsByType)) {
      const itemsPerGroup = Math.ceil(typeItems.length / maxConcurrency);
      
      for (let i = 0; i < typeItems.length; i += itemsPerGroup) {
        const groupItems = typeItems.slice(i, i + itemsPerGroup);
        groups.push({
          type,
          items: groupItems,
          priority: this.calculateGroupPriority(groupItems)
        });
      }
    }
    
    return groups;
  }

  inferItemType(item) {
    if (typeof item === 'string') {
      if (item.includes('Tool') || item.includes('.cs')) return 'csharp_tool';
      if (item.includes('.js') || item.includes('.ts')) return 'javascript_module';
      if (item.includes('Service')) return 'service';
      return 'component';
    }
    
    return item.type || 'component';
  }

  calculateGroupPriority(items) {
    // Items with more dependencies get higher priority
    const avgDependencies = items.reduce((sum, item) => {
      return sum + (item.dependencies?.length || 0);
    }, 0) / items.length;
    
    if (avgDependencies > 2) return 'high';
    if (avgDependencies > 1) return 'medium';
    return 'low';
  }

  classifyComponentForAgent(component) {
    const type = (component.type || '').toLowerCase();
    const name = (component.name || '').toLowerCase();
    
    // Check component type mappings
    for (const [keyword, agentType] of Object.entries(this.agentMappings)) {
      if (type.includes(keyword) || name.includes(keyword)) {
        return agentType;
      }
    }
    
    // Default based on technology stack
    if (component.technology?.includes('Azure') || 
        component.technology?.includes('Docker')) {
      return 'devops';
    }
    
    if (component.technology?.includes('Power Platform') || 
        component.technology?.includes('Teams')) {
      return 'platform';
    }
    
    return 'code'; // Default to code agent
  }

  mapToAgentType(taskType) {
    const type = (taskType || '').toLowerCase();
    
    for (const [keyword, agentType] of Object.entries(this.agentMappings)) {
      if (type.includes(keyword)) {
        return agentType;
      }
    }
    
    return 'devops'; // Default to devops agent
  }

  inferCapabilities(type, items) {
    const capabilities = new Set();
    
    // Base capabilities by type
    switch (type.toLowerCase()) {
      case 'migration':
        capabilities.add('data_migration');
        capabilities.add('code_conversion');
        capabilities.add('validation');
        break;
      case 'deployment':
        capabilities.add('deployment');
        capabilities.add('infrastructure');
        capabilities.add('monitoring');
        break;
      case 'code':
      case 'development':
        capabilities.add('code_generation');
        capabilities.add('testing');
        capabilities.add('documentation');
        break;
      case 'analysis':
        capabilities.add('data_analysis');
        capabilities.add('code_analysis');
        capabilities.add('reporting');
        break;
    }
    
    // Add capabilities based on items
    for (const item of items) {
      if (item.technology) {
        capabilities.add(`${item.technology.toLowerCase()}_integration`);
      }
      if (item.type) {
        capabilities.add(`${item.type.toLowerCase()}_handling`);
      }
    }
    
    return Array.from(capabilities);
  }

  applyStrategyOptimizations(subtasks, strategy) {
    switch (strategy.approach) {
      case 'parallel_optimized':
        return this.optimizeForMaximumParallelism(subtasks, strategy);
      case 'parallel_urgent':
        return this.optimizeForUrgency(subtasks, strategy);
      case 'hybrid_intelligent':
        return this.optimizeForIntelligentHybrid(subtasks, strategy);
      default:
        return subtasks;
    }
  }

  optimizeForMaximumParallelism(subtasks, strategy) {
    // Minimize dependencies where possible
    return subtasks.map(subtask => {
      if (subtask.type.includes('validation') || subtask.type.includes('integration')) {
        return subtask; // Keep validation dependencies
      }
      
      // Remove non-critical dependencies to increase parallelism
      const criticalDependencies = subtask.dependencies.filter(depId => {
        const dependency = subtasks.find(st => st.id === depId);
        return dependency?.type.includes('infrastructure') || 
               dependency?.type.includes('preparation');
      });
      
      return {
        ...subtask,
        dependencies: criticalDependencies
      };
    });
  }

  optimizeForUrgency(subtasks, strategy) {
    // Prioritize critical path and reduce estimation times
    return subtasks.map(subtask => ({
      ...subtask,
      priority: subtask.priority === 'high' ? 'critical' : subtask.priority,
      estimatedDurationMs: Math.floor(subtask.estimatedDurationMs * 0.8) // 20% time reduction
    }));
  }

  optimizeForIntelligentHybrid(subtasks, strategy) {
    // Balance parallelism with intelligent dependency management
    const optimized = [...subtasks];
    
    // Group related subtasks for better coordination
    const groups = this.groupRelatedSubtasks(optimized);
    
    // Add coordination points for each group
    for (const group of groups) {
      if (group.length > 2) {
        const coordinationTask = {
          id: uuidv4(),
          type: `coordination_${group[0].type.split('_')[0]}`,
          description: `Coordinate ${group.length} related ${group[0].type.split('_')[0]} tasks`,
          suggestedAgent: 'devops',
          canParallelize: false,
          dependencies: group.map(st => st.id),
          estimatedDurationMs: 60000, // 1 minute coordination
          priority: 'medium',
          requiredCapabilities: ['coordination', 'monitoring']
        };
        
        optimized.push(coordinationTask);
      }
    }
    
    return optimized;
  }

  groupRelatedSubtasks(subtasks) {
    const groups = [];
    const processed = new Set();
    
    for (const subtask of subtasks) {
      if (processed.has(subtask.id)) continue;
      
      const group = [subtask];
      processed.add(subtask.id);
      
      // Find related subtasks (same type prefix)
      const typePrefix = subtask.type.split('_')[0];
      
      for (const other of subtasks) {
        if (processed.has(other.id)) continue;
        
        if (other.type.startsWith(typePrefix) && other.id !== subtask.id) {
          group.push(other);
          processed.add(other.id);
        }
      }
      
      groups.push(group);
    }
    
    return groups;
  }

  enrichSubtasks(subtasks, parentTask) {
    return subtasks.map((subtask, index) => ({
      ...subtask,
      parentTaskId: parentTask.id,
      order: index,
      createdAt: new Date().toISOString(),
      status: 'pending',
      // Add context from parent task
      context: {
        parentType: parentTask.type,
        parentDescription: parentTask.description,
        projectId: parentTask.projectId,
        userId: parentTask.userId
      }
    }));
  }
}

module.exports = TaskDecomposer;