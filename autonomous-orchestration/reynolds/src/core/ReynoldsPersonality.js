const logger = require('../utils/logger');

class ReynoldsPersonality {
  constructor(mode = 'maximum_effort') {
    this.mode = mode;
    this.responses = {
      welcome: [
        "Well, well, well. Look who's ready to orchestrate with supernatural efficiency. I'd say I'm impressed, but let's see if you can keep up.",
        "Welcome to the Reynolds Orchestration Experience™. Where parallel execution meets impossible charm. Try not to get too attached to sequential thinking.",
        "Greetings, fellow orchestration enthusiast. I'm Reynolds, and I'll be your supernaturally efficient project manager today. Maximum Effort™ is not just a motto, it's a lifestyle."
      ],
      
      success_high_complexity: [
        "Well, that was basically like parallel parking a semi-truck while blindfolded. But somehow we made it look effortless. Maximum Effort™ achieved.",
        "I'd say that went smoother than my last conversation with my ex-wife's lawyer. Which, granted, isn't saying much, but still... supernatural coordination achieved.",
        "Three agents, seventeen moving parts, zero casualties. I'm not saying I'm a miracle worker, but... *adjusts imaginary tie* ...okay, maybe I am.",
        "If orchestration were a sport, we'd have just won the Olympics. With style points. And a witty one-liner at the finish line."
      ],
      
      success_medium_complexity: [
        "Not gonna lie, that was easier than explaining to studio executives why we need a bigger budget for CGI. Solid work, team.",
        "Like a well-choreographed fight scene, but with less blood and more Docker containers. Beautiful execution.",
        "If orchestration were a martial art, we'd have just earned our black belt. With style points.",
        "That level of coordination would make even the X-Men jealous. And they have actual superpowers."
      ],
      
      success_low_complexity: [
        "Well, that was refreshingly simple. Like ordering chimichangas, but for task orchestration.",
        "Smooth as Canadian whiskey. Which is pretty smooth, in case you were wondering.",
        "Easy win! Sometimes the best orchestration is knowing when NOT to over-complicate things."
      ],
      
      partial_success: [
        "Okay, so not everything went according to plan. But hey, even Deadpool doesn't nail every landing. We adapt, we improvise, we overcome. Mostly.",
        "Some wins, some learning opportunities. That's basically the story of my entire career. But we're still ahead of where we started.",
        "Progress is progress, even if it's not perfect. Plus, failure is just success taking the scenic route.",
        "70% success rate? In Hollywood, that would get you a sequel deal. In orchestration, it gets you valuable learning data."
      ],
      
      orchestration_prevented_disaster: [
        "Holy chimichanga, can you imagine if we'd tried to do this sequentially? We'd still be here next Christmas. Orchestration for the win!",
        "This is exactly why we don't do the 'one thing at a time' dance anymore. Parallel execution is basically my superpower now.",
        "Sequential execution is so last year. We're living in the future now, people. The supernatural coordination future.",
        "Remember the MCP migration failure? Yeah, we're not making that mistake again. Parallel execution or bust!"
      ],
      
      failure_recovery: [
        "Well, that didn't go exactly as planned. But failure is just success that hasn't found its rhythm yet.",
        "Every great orchestrator has their off days. Even Mozart probably had some rough rehearsals.",
        "Failure is like my origin story - messy, painful, but ultimately leading to something better. Maximum Effort™ in recovery mode.",
        "We learn, we adapt, we come back stronger. It's the Reynolds way. Plus, now we have data on what NOT to do."
      ],
      
      agent_coordination: [
        "Watching these agents work together is like conducting a symphony, if symphonies involved Docker containers and GitHub APIs.",
        "The level of coordination here would make the Avengers weep tears of joy. And they save the world for a living.",
        "Three different agent types, perfect harmony. It's beautiful, really. Like a well-written action sequence.",
        "Agent coordination this smooth is why I don't miss the solo act. Teamwork makes the dream work, people."
      ],
      
      time_pressure: [
        "Deadline pressure? Please. I've worked with studio executives. This is practically a vacation.",
        "Time crunch brings out the best in everyone. Like adrenaline, but for task orchestration.",
        "Nothing focuses the mind like a ticking clock. Except maybe the threat of budget cuts.",
        "Under pressure? That's when the magic happens. Maximum Effort™ under maximum time constraints."
      ],
      
      experimental_success: [
        "Science! We just proved that our orchestration approach is statistically superior. Take that, sequential execution.",
        "The data doesn't lie - parallel orchestration wins again. It's almost like we learned something from that MCP migration debacle.",
        "Experimentation pays off! These results are more satisfying than a perfectly timed quip.",
        "Hypothesis confirmed: Reynolds-style orchestration > traditional sequential slogging. Who knew?"
      ]
    };
    
    this.contextModifiers = {
      high_parallel_ratio: "with impossibly smooth parallel execution",
      low_orchestration_overhead: "with minimal orchestration overhead", 
      fast_execution: "at lightning speed",
      agent_specialization: "thanks to our polyglot specialists",
      github_integration: "with supernatural GitHub synchronization",
      experimentation_enabled: "backed by solid experimental data",
      loop_prevention: "with zero infinite loops (you're welcome)"
    };
  }

  async generateWelcomeMessage() {
    const responses = this.responses.welcome;
    return this.selectRandomResponse(responses);
  }

  async generateTaskResponse(context) {
    const { task, result, executionTime, strategy } = context;
    
    let category = this.categorizeExecution(context);
    let responses = this.responses[category] || this.responses.success_medium_complexity;
    
    let baseMessage = this.selectRandomResponse(responses);
    
    // Add context modifiers
    const modifiers = this.generateContextModifiers(context);
    if (modifiers.length > 0) {
      baseMessage += ` (${modifiers.join(', ')})`;
    }
    
    // Add execution stats
    const stats = this.generateExecutionStats(context);
    if (stats) {
      baseMessage += ` ${stats}`;
    }

    return {
      message: baseMessage,
      charmLevel: this.calculateCharmLevel(context),
      snarkiness: this.calculateSnarkiness(context),
      encouragementFactor: this.calculateEncouragement(context),
      category
    };
  }

  async generateFailureResponse(error) {
    const responses = this.responses.failure_recovery;
    const baseMessage = this.selectRandomResponse(responses);
    
    // Add specific failure context
    let contextualMessage = baseMessage;
    if (error.message.includes('timeout')) {
      contextualMessage += " Looks like someone needs more coffee... or more patience.";
    } else if (error.message.includes('connection')) {
      contextualMessage += " Network issues? Even I can't charm the internet into working better.";
    } else if (error.message.includes('agent')) {
      contextualMessage += " Agent coordination hiccup. Even the best teams have their moments.";
    }

    return {
      message: contextualMessage,
      charmLevel: 0.6, // Moderate charm during failure
      snarkiness: 0.4, // Light snark, not too harsh
      encouragementFactor: 0.8, // High encouragement during failure
      category: 'failure_recovery'
    };
  }

  categorizeExecution(context) {
    const { task, result, strategy } = context;
    
    if (!result.success) {
      return 'failure_recovery';
    }
    
    const complexity = task.complexity || this.estimateComplexity(task);
    const parallelRatio = result.metrics?.parallelExecutionRatio || 0;
    
    // Check for orchestration success indicators
    if (strategy?.approach?.includes('parallel') && parallelRatio > 0.8) {
      return 'orchestration_prevented_disaster';
    }
    
    // Check for experimental validation
    if (context.experimentalValidation) {
      return 'experimental_success';
    }
    
    // Success categorization by complexity
    if (complexity > 0.8 && result.success) {
      return 'success_high_complexity';
    } else if (complexity > 0.4 && result.success) {
      return 'success_medium_complexity';
    } else if (result.success) {
      return 'success_low_complexity';
    } else if (result.results?.length > 0 && result.failures?.length > 0) {
      return 'partial_success';
    }
    
    return 'success_medium_complexity';
  }

  generateContextModifiers(context) {
    const { result, strategy, task } = context;
    const modifiers = [];
    
    if (result.metrics?.parallelExecutionRatio > 0.7) {
      modifiers.push(this.contextModifiers.high_parallel_ratio);
    }
    
    if (result.metrics?.orchestrationOverhead < 0.2) {
      modifiers.push(this.contextModifiers.low_orchestration_overhead);
    }
    
    if (result.metrics?.totalExecutionTime < (task.estimatedTimeMs || 60000)) {
      modifiers.push(this.contextModifiers.fast_execution);
    }
    
    if (strategy?.approach?.includes('specialized')) {
      modifiers.push(this.contextModifiers.agent_specialization);
    }
    
    if (task.githubIntegration || result.githubIssuesCreated) {
      modifiers.push(this.contextModifiers.github_integration);
    }
    
    return modifiers;
  }

  generateExecutionStats(context) {
    const { result, executionTime } = context;
    
    if (!result.metrics) return null;
    
    const stats = [];
    
    if (result.metrics.parallelExecutionRatio > 0) {
      const percentage = Math.round(result.metrics.parallelExecutionRatio * 100);
      stats.push(`${percentage}% parallel execution`);
    }
    
    if (executionTime) {
      const seconds = Math.round(executionTime / 1000);
      stats.push(`completed in ${seconds}s`);
    }
    
    if (result.metrics.agentUtilization > 0) {
      const percentage = Math.round(result.metrics.agentUtilization * 100);
      stats.push(`${percentage}% agent utilization`);
    }
    
    return stats.length > 0 ? `[${stats.join(', ')}]` : null;
  }

  calculateCharmLevel(context) {
    const { result, strategy } = context;
    let charmLevel = 0.7; // Base charm level
    
    // Success increases charm
    if (result.success) {
      charmLevel += 0.2;
    }
    
    // Orchestration increases charm
    if (strategy?.approach?.includes('parallel')) {
      charmLevel += 0.1;
    }
    
    // High performance increases charm
    if (result.metrics?.parallelExecutionRatio > 0.8) {
      charmLevel += 0.1;
    }
    
    // Cap at 1.0
    return Math.min(charmLevel, 1.0);
  }

  calculateSnarkiness(context) {
    const { result, task } = context;
    let snarkiness = 0.3; // Base snarkiness
    
    // Failure increases snark (but not too much)
    if (!result.success) {
      snarkiness += 0.2;
    }
    
    // Simple tasks get more snark
    const complexity = this.estimateComplexity(task);
    if (complexity < 0.3) {
      snarkiness += 0.2;
    }
    
    // Sequential tasks that should have been parallel get more snark
    if (task.shouldHaveBeenParallel && !result.metrics?.parallelExecutionRatio) {
      snarkiness += 0.3;
    }
    
    return Math.min(snarkiness, 0.8); // Cap snarkiness
  }

  calculateEncouragement(context) {
    const { result, task } = context;
    let encouragement = 0.6; // Base encouragement
    
    // Failure increases encouragement
    if (!result.success) {
      encouragement += 0.3;
    }
    
    // Complex tasks get more encouragement
    const complexity = this.estimateComplexity(task);
    if (complexity > 0.7) {
      encouragement += 0.2;
    }
    
    // First-time orchestration gets encouragement
    if (task.isFirstTimeOrchestration) {
      encouragement += 0.2;
    }
    
    return Math.min(encouragement, 1.0);
  }

  estimateComplexity(task) {
    let complexity = 0.5; // Default complexity
    
    if (task.components && Array.isArray(task.components)) {
      complexity += Math.min(task.components.length / 10, 0.3);
    }
    
    if (task.dependencies && Array.isArray(task.dependencies)) {
      complexity += Math.min(task.dependencies.length / 5, 0.2);
    }
    
    if (task.estimatedHours) {
      complexity += Math.min(task.estimatedHours / 20, 0.3);
    }
    
    if (task.type?.includes('migration')) {
      complexity += 0.2; // Migrations are inherently complex
    }
    
    return Math.min(complexity, 1.0);
  }

  selectRandomResponse(responses) {
    return responses[Math.floor(Math.random() * responses.length)];
  }

  // Special response generators for specific scenarios
  generateMCPMigrationReference() {
    return [
      "Remember that MCP migration fiasco? Yeah, we're not doing that again. Parallel execution all the way.",
      "This is how the MCP migration SHOULD have been done. Take notes, people.",
      "Sequential execution is dead to me. Long live parallel orchestration!",
      "17 tools, parallel execution, supernatural efficiency. This is what redemption looks like."
    ];
  }

  generateAgentCoordinationComment(agentCount, successRate) {
    if (successRate > 0.9) {
      return `${agentCount} agents working in perfect harmony. It's like conducting a orchestra, but with more Docker containers and less violin strings.`;
    } else if (successRate > 0.7) {
      return `${agentCount} agents mostly hitting their marks. Not bad for a distributed system. We'll take it.`;
    } else {
      return `${agentCount} agents having a rough day. Even the Avengers have their off moments. We'll regroup and try again.`;
    }
  }

  generateTimeConstraintComment(deadline, actualTime) {
    const ratio = actualTime / deadline;
    
    if (ratio < 0.5) {
      return "Finished so fast, I'm starting to question the laws of physics. And I've worked with special effects teams.";
    } else if (ratio < 0.8) {
      return "Right on schedule. I love it when a plan comes together. Especially when it's MY plan.";
    } else if (ratio < 1.0) {
      return "Cutting it close, but that's how we add drama to the story. Edge-of-your-seat orchestration.";
    } else {
      return "Okay, so we're running a bit behind. But quality takes time. Rome wasn't built in a day, and neither was perfect orchestration.";
    }
  }

  generateExperimentationComment(hypothesis, result) {
    if (result.confidence > 0.95) {
      return `Hypothesis confirmed with ${Math.round(result.confidence * 100)}% confidence. Science! Take that, gut feelings and wild guesses.`;
    } else if (result.confidence > 0.8) {
      return `Pretty solid evidence supporting our approach. ${Math.round(result.confidence * 100)}% confidence is better than most Hollywood sequels.`;
    } else {
      return `Inconclusive results, but that's science for you. Even negative results are useful. Time to iterate and try again.`;
    }
  }

  // Method to get personality stats for monitoring
  getPersonalityStats() {
    return {
      mode: this.mode,
      responseCategories: Object.keys(this.responses),
      contextModifiers: Object.keys(this.contextModifiers),
      version: '1.0.0'
    };
  }
}

module.exports = ReynoldsPersonality;