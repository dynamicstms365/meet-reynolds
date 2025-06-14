const { spawn } = require('child_process');
const fs = require('fs').promises;
const path = require('path');

class AnalysisTools {
  constructor() {
    this.supportedLanguages = [
      'javascript', 'typescript', 'python', 'java', 'csharp', 'go', 
      'rust', 'php', 'ruby', 'swift', 'kotlin', 'scala'
    ];
  }

  async analyzeCodeQuality(projectPath, language) {
    const tools = this.getQualityTools(language);
    const results = {};

    for (const tool of tools) {
      try {
        results[tool.name] = await this.runTool(tool, projectPath);
      } catch (error) {
        results[tool.name] = { error: error.message };
      }
    }

    return {
      language,
      tools: Object.keys(results),
      results,
      summary: this.generateQualitySummary(results)
    };
  }

  async performSecurityScan(projectPath, language) {
    const securityTools = this.getSecurityTools(language);
    const vulnerabilities = [];

    for (const tool of securityTools) {
      try {
        const result = await this.runTool(tool, projectPath);
        if (result.vulnerabilities) {
          vulnerabilities.push(...result.vulnerabilities);
        }
      } catch (error) {
        console.warn(`Security tool ${tool.name} failed:`, error.message);
      }
    }

    return {
      language,
      vulnerabilities,
      severity: this.calculateSeverity(vulnerabilities),
      recommendations: this.generateSecurityRecommendations(vulnerabilities)
    };
  }

  async runStaticAnalysis(projectPath, language) {
    const analysisTools = this.getStaticAnalysisTools(language);
    const issues = [];

    for (const tool of analysisTools) {
      try {
        const result = await this.runTool(tool, projectPath);
        if (result.issues) {
          issues.push(...result.issues);
        }
      } catch (error) {
        console.warn(`Static analysis tool ${tool.name} failed:`, error.message);
      }
    }

    return {
      language,
      totalIssues: issues.length,
      issues: this.categorizeIssues(issues),
      metrics: this.calculateMetrics(issues),
      suggestions: this.generateSuggestions(issues)
    };
  }

  getQualityTools(language) {
    const toolMap = {
      javascript: [
        { name: 'eslint', command: 'eslint', args: ['--format', 'json'] },
        { name: 'jshint', command: 'jshint', args: ['--reporter', 'json'] }
      ],
      typescript: [
        { name: 'eslint', command: 'eslint', args: ['--format', 'json'] },
        { name: 'tsc', command: 'tsc', args: ['--noEmit'] }
      ],
      python: [
        { name: 'flake8', command: 'flake8', args: ['--format', 'json'] },
        { name: 'pylint', command: 'pylint', args: ['--output-format', 'json'] },
        { name: 'mypy', command: 'mypy', args: ['--output', 'json'] }
      ],
      java: [
        { name: 'checkstyle', command: 'checkstyle', args: ['-f', 'json'] },
        { name: 'pmd', command: 'pmd', args: ['-f', 'json'] }
      ],
      go: [
        { name: 'golangci-lint', command: 'golangci-lint', args: ['run', '--out-format', 'json'] },
        { name: 'go-vet', command: 'go', args: ['vet', '-json'] }
      ]
    };

    return toolMap[language] || [];
  }

  getSecurityTools(language) {
    const securityMap = {
      javascript: [
        { name: 'npm-audit', command: 'npm', args: ['audit', '--json'] },
        { name: 'snyk', command: 'snyk', args: ['test', '--json'] }
      ],
      python: [
        { name: 'bandit', command: 'bandit', args: ['-f', 'json'] },
        { name: 'safety', command: 'safety', args: ['check', '--json'] }
      ],
      java: [
        { name: 'spotbugs', command: 'spotbugs', args: ['-output', 'json'] }
      ],
      go: [
        { name: 'gosec', command: 'gosec', args: ['-fmt', 'json'] }
      ]
    };

    return securityMap[language] || [];
  }

  getStaticAnalysisTools(language) {
    const analysisMap = {
      javascript: [
        { name: 'sonarjs', command: 'sonar-scanner', args: ['-Dsonar.language=js'] }
      ],
      python: [
        { name: 'sonar-scanner', command: 'sonar-scanner', args: ['-Dsonar.language=py'] }
      ],
      java: [
        { name: 'sonar-scanner', command: 'sonar-scanner', args: ['-Dsonar.language=java'] }
      ]
    };

    return analysisMap[language] || [];
  }

  async runTool(tool, projectPath) {
    return new Promise((resolve, reject) => {
      const process = spawn(tool.command, [...tool.args, projectPath], {
        cwd: projectPath,
        stdio: ['pipe', 'pipe', 'pipe']
      });

      let stdout = '';
      let stderr = '';

      process.stdout.on('data', (data) => {
        stdout += data.toString();
      });

      process.stderr.on('data', (data) => {
        stderr += data.toString();
      });

      process.on('close', (code) => {
        try {
          const result = JSON.parse(stdout || '{}');
          resolve(result);
        } catch (error) {
          resolve({ raw: stdout, stderr, code });
        }
      });

      process.on('error', (error) => {
        reject(error);
      });
    });
  }

  generateQualitySummary(results) {
    const totalIssues = Object.values(results).reduce((sum, result) => {
      return sum + (result.issues?.length || 0);
    }, 0);

    return {
      totalIssues,
      score: Math.max(0, 100 - totalIssues * 2),
      grade: totalIssues < 5 ? 'A' : totalIssues < 15 ? 'B' : totalIssues < 30 ? 'C' : 'D'
    };
  }

  calculateSeverity(vulnerabilities) {
    const severities = vulnerabilities.map(v => v.severity || 'low');
    const high = severities.filter(s => s === 'high').length;
    const medium = severities.filter(s => s === 'medium').length;
    
    if (high > 0) return 'high';
    if (medium > 0) return 'medium';
    return 'low';
  }

  categorizeIssues(issues) {
    return {
      errors: issues.filter(i => i.severity === 'error'),
      warnings: issues.filter(i => i.severity === 'warning'),
      info: issues.filter(i => i.severity === 'info')
    };
  }

  calculateMetrics(issues) {
    return {
      complexity: 'medium',
      maintainability: issues.length < 10 ? 'high' : 'medium',
      testability: 'medium',
      duplication: 'low'
    };
  }

  generateSuggestions(issues) {
    const suggestions = [];
    
    if (issues.some(i => i.rule?.includes('complexity'))) {
      suggestions.push('Consider breaking down complex functions');
    }
    
    if (issues.some(i => i.rule?.includes('unused'))) {
      suggestions.push('Remove unused variables and imports');
    }
    
    if (issues.some(i => i.rule?.includes('naming'))) {
      suggestions.push('Follow consistent naming conventions');
    }
    
    return suggestions;
  }

  generateSecurityRecommendations(vulnerabilities) {
    const recommendations = [];
    
    if (vulnerabilities.some(v => v.type === 'dependency')) {
      recommendations.push('Update vulnerable dependencies');
    }
    
    if (vulnerabilities.some(v => v.type === 'injection')) {
      recommendations.push('Implement proper input validation');
    }
    
    if (vulnerabilities.some(v => v.type === 'authentication')) {
      recommendations.push('Strengthen authentication mechanisms');
    }
    
    return recommendations;
  }
}

module.exports = AnalysisTools;