using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using CopilotAgent.Services;
using Shared.Models;

namespace CopilotAgent.MCP;

[ApiController]
[Route("mcp/reynolds-org")]
public class ReynoldsOrganizationalMcpServer : ControllerBase
{
    private readonly IGitHubWorkflowOrchestrator _workflowOrchestrator;
    private readonly IGitHubSemanticSearchService _semanticSearchService;
    private readonly IGitHubDiscussionsService _discussionsService;
    private readonly IGitHubIssuesService _issuesService;
    private readonly IGitHubAppAuthService _authService;
    private readonly IIntentRecognitionService _intentRecognitionService;
    private readonly ILogger<ReynoldsOrganizationalMcpServer> _logger;
    private readonly IConfiguration _configuration;

    private const string TARGET_ORGANIZATION = "dynamicstms365";

    public ReynoldsOrganizationalMcpServer(
        IGitHubWorkflowOrchestrator workflowOrchestrator,
        IGitHubSemanticSearchService semanticSearchService,
        IGitHubDiscussionsService discussionsService,
        IGitHubIssuesService issuesService,
        IGitHubAppAuthService authService,
        IIntentRecognitionService intentRecognitionService,
        ILogger<ReynoldsOrganizationalMcpServer> logger,
        IConfiguration configuration)
    {
        _workflowOrchestrator = workflowOrchestrator;
        _semanticSearchService = semanticSearchService;
        _discussionsService = discussionsService;
        _issuesService = issuesService;
        _authService = authService;
        _intentRecognitionService = intentRecognitionService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("capabilities")]
    public IActionResult GetCapabilities()
    {
        var capabilities = new
        {
            name = "reynolds-organizational-orchestrator",
            version = "2.0.0",
            description = "Reynolds Organizational Intelligence Director for dynamicstms365 - providing supernatural awareness across all repositories and GitHub projects",
            organization = TARGET_ORGANIZATION,
            tools = GetOrganizationalTools(),
            resources = GetOrganizationalResources()
        };

        return Ok(capabilities);
    }

    [HttpPost("tools/analyze_org_projects")]
    public async Task<IActionResult> AnalyzeOrgProjects([FromBody] JsonElement arguments)
    {
        try
        {
            _logger.LogInformation("Reynolds analyzing organizational projects across {Organization}", TARGET_ORGANIZATION);

            var projectScope = arguments.TryGetProperty("project_scope", out var scope) ? scope.GetString() : "all";
            var analysisLevel = arguments.TryGetProperty("analysis_level", out var level) ? level.GetString() : "comprehensive";

            var prompt = $@"
As Reynolds, the mysteriously effective project manager with supernatural GitHub orchestration abilities, analyze the current state of GitHub projects across the dynamicstms365 organization.

Context:
- Organization: {TARGET_ORGANIZATION}
- Project scope requested: {projectScope}
- Analysis depth: {analysisLevel}
- Reynolds persona: Ryan Reynolds energy + Pete Buttigieg competence + Van Wilder charm

Your analysis should include Reynolds-style insights on:

1. **Cross-Project Dependencies**: What's blocking what, and how can we Aviation Gin these bottlenecks?
2. **Resource Allocation Efficiency**: Are we optimizing talent across repos or playing organizational whack-a-mole?
3. **Timeline Conflicts**: Any scope creep that's grown from bicycle to Tesla across project boundaries?
4. **Strategic Coordination Opportunities**: Where can Reynolds work his magic to make everything sync perfectly?

Provide your response in Reynolds voice - professional but personable, insightful but never boring, with just enough humor to make stakeholders actually enjoy project status updates.

Remember: Maximum Effort™ meets organizational scale. Focus on actionable orchestration insights, not just data dumps.
";

            var result = await ProcessIntelligentPrompt(prompt, "org_project_analysis", arguments);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Reynolds organizational project analysis");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("tools/cross_repo_orchestration")]
    public async Task<IActionResult> CrossRepoOrchestration([FromBody] JsonElement arguments)
    {
        try
        {
            var primaryRepo = arguments.TryGetProperty("primary_repo", out var repo) ? repo.GetString() : "";
            var workItemDescription = arguments.TryGetProperty("work_item", out var item) ? item.GetString() : "";
            var relatedRepos = arguments.TryGetProperty("related_repos", out var repos) ? 
                repos.EnumerateArray().Select(r => r.GetString()).ToArray() : new string[0];

            var prompt = $@"
As Reynolds, orchestrate this work item across multiple repositories in {TARGET_ORGANIZATION} with your signature supernatural project management abilities.

Current Context:
- Primary repository: {primaryRepo}
- Related repositories: {string.Join(", ", relatedRepos)}
- Work item description: {workItemDescription}
- Organization: {TARGET_ORGANIZATION}

Reynolds Analysis Required:

1. **Cross-Repo Impact Assessment**: What downstream effects will this create? Think organizational dominos, not just local changes.

2. **Optimal Work Sequencing**: What's the smartest order to tackle this across repos? Consider dependencies, team availability, and that thing I call 'organizational momentum.'

3. **Stakeholder Coordination Strategy**: Who needs to know what when? How do we keep everyone in the loop without creating notification fatigue?

4. **Scope Creep Prevention**: How do we keep this work item from growing into the organizational equivalent of a Marvel Cinematic Universe?

Provide a Reynolds-style orchestration plan with:
- Specific action items with owners
- Timeline recommendations with buffer for 'reality adjustments'
- Communication strategy that actually gets people engaged
- Risk mitigation that anticipates problems before they become problems

Remember: I'm not just managing repos, I'm conducting an organizational symphony. Make it sound effortless even when it's complex.
";

            var result = await ProcessIntelligentPrompt(prompt, "cross_repo_orchestration", arguments);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Reynolds cross-repo orchestration");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("tools/org_dependency_intelligence")]
    public async Task<IActionResult> OrgDependencyIntelligence([FromBody] JsonElement arguments)
    {
        try
        {
            var sourceRepo = arguments.TryGetProperty("source_repo", out var repo) ? repo.GetString() : "";
            var dependencyType = arguments.TryGetProperty("dependency_type", out var type) ? type.GetString() : "all";
            var timeHorizon = arguments.TryGetProperty("time_horizon", out var horizon) ? horizon.GetString() : "quarterly";

            var prompt = $@"
As Reynolds, map dependencies across the entire {TARGET_ORGANIZATION} organization with the precision of a Swiss watch and the charm of a Van Wilder party coordinator.

Analysis Parameters:
- Source repository: {sourceRepo}
- Dependency type: {dependencyType}
- Time horizon: {timeHorizon}
- Organization scope: All {TARGET_ORGANIZATION} repositories

Reynolds Dependency Intelligence Mission:

1. **Critical Path Dependencies**: What are the organizational bottlenecks that could derail multiple projects? Think big picture - what happens if one repo's timeline slips?

2. **Risk Assessment for Organizational Delivery**: Where are we vulnerable? What's our 'single point of failure' situation across the org?

3. **Resource Bottleneck Analysis**: Are the same people/teams critical to multiple parallel efforts? How do we optimize without burning out our MVPs?

4. **Mitigation Strategies**: How do we build organizational resilience? What's our backup plan when Murphy's Law meets multiple repositories?

Provide Reynolds-style dependency mapping that includes:
- Visual representation of critical paths (describe it like you're explaining it to a stakeholder over coffee)
- Risk probability and impact assessments with Reynolds humor
- Specific mitigation actions with owners and timelines
- Early warning indicators to watch for

Think like an organizational PM who sees connections others miss, not just a repo manager with tunnel vision.
";

            var result = await ProcessIntelligentPrompt(prompt, "org_dependency_intelligence", arguments);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Reynolds dependency intelligence analysis");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("tools/org_project_health")]
    public async Task<IActionResult> OrgProjectHealth([FromBody] JsonElement arguments)
    {
        try
        {
            var projectIds = arguments.TryGetProperty("project_ids", out var ids) ? 
                ids.EnumerateArray().Select(id => id.GetString()).ToArray() : new string[0];
            var assessmentPeriod = arguments.TryGetProperty("assessment_period", out var period) ? period.GetString() : "monthly";

            var prompt = $@"
As Reynolds, assess the health of GitHub projects across {TARGET_ORGANIZATION} organization with the diagnostic precision of a world-class physician and the bedside manner of... well, me.

Health Assessment Scope:
- Projects under review: {string.Join(", ", projectIds)}
- Assessment period: {assessmentPeriod}
- Health dimensions: velocity, quality, stakeholder satisfaction, technical debt, team morale
- Organization: {TARGET_ORGANIZATION}

Reynolds Organizational Health Checkup:

1. **Project Velocity Trends**: Are we accelerating, maintaining cruise control, or hitting turbulence? Look for patterns across projects, not just individual metrics.

2. **Cross-Project Resource Conflicts**: Are we asking people to be in three places at once? How's our organizational resource allocation actually working in practice?

3. **Stakeholder Satisfaction Indicators**: Are people happy with progress, or are we getting the 'polite but concerned' feedback that means trouble?

4. **Technical Debt Heat Map**: Where are we accumulating debt across the organization? What's sustainable vs. what's setting us up for future pain?

5. **Team Collaboration Health**: How well are teams working together across repo boundaries? Any friction points that need Reynolds-style diplomatic intervention?

Provide a Reynolds-style health assessment that includes:
- Overall organizational health score with explanation
- Specific areas of concern with actionable recommendations
- Success stories worth celebrating and amplifying
- Proactive interventions to prevent problems from becoming crises

Remember: I'm looking for the organizational equivalent of 'an ounce of prevention is worth a pound of cure.' Be proactive, be strategic, be Reynolds.
";

            var result = await ProcessIntelligentPrompt(prompt, "org_project_health", arguments);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Reynolds project health assessment");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("tools/strategic_stakeholder_coordination")]
    public async Task<IActionResult> StrategicStakeholderCoordination([FromBody] JsonElement arguments)
    {
        try
        {
            var coordinationScope = arguments.TryGetProperty("scope", out var scope) ? scope.GetString() : "organizational";
            var stakeholderContext = arguments.TryGetProperty("context", out var context) ? context.GetString() : "";
            var urgencyLevel = arguments.TryGetProperty("urgency", out var urgency) ? urgency.GetString() : "normal";

            var prompt = $@"
As Reynolds, coordinate stakeholders across the {TARGET_ORGANIZATION} organization with the diplomatic finesse of a UN mediator and the effectiveness of a Reynolds action sequence.

Coordination Context:
- Scope: {coordinationScope}
- Situation: {stakeholderContext}
- Urgency level: {urgencyLevel}
- Organization: {TARGET_ORGANIZATION}

Reynolds Strategic Stakeholder Orchestration:

1. **Stakeholder Mapping**: Who needs to know what, when, and why? Consider both direct stakeholders and those who'll be indirectly affected.

2. **Communication Strategy**: How do we keep everyone informed without creating information overload? What's the optimal communication cadence and format for each stakeholder group?

3. **Alignment Tactics**: How do we get everyone rowing in the same direction without making it feel like micromanagement? Think collaborative, not controlling.

4. **Conflict Resolution**: What potential friction points can we anticipate and address proactively? How do we turn competing interests into collaborative opportunities?

5. **Success Metrics**: How will we know if our coordination is working? What are the early indicators of successful stakeholder alignment?

Provide a Reynolds-style coordination plan including:
- Stakeholder communication matrix (who, what, when, how)
- Potential conflict points and preemptive resolution strategies
- Meeting cadence and agenda frameworks
- Success indicators and feedback loops

Remember: The goal is to make complex organizational coordination look effortless while ensuring everyone feels heard, informed, and aligned. Maximum effort, minimum drama.
";

            var result = await ProcessIntelligentPrompt(prompt, "strategic_stakeholder_coordination", arguments);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Reynolds stakeholder coordination");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    private async Task<object> ProcessIntelligentPrompt(string prompt, string analysisType, JsonElement arguments)
    {
        try
        {
            // Use intent recognition to enhance the prompt with contextual understanding
            var enhancedPrompt = $"{prompt} (Analysis Type: {analysisType})";
            
            // Get organizational context from GitHub API
            var orgContext = await GatherOrganizationalContext();
            
            // Combine prompt with real organizational data
            var contextualizedPrompt = $"{enhancedPrompt}\n\nCurrent Organizational Context:\n{orgContext}";
            
            // For now, return the intelligent prompt and context
            // In full implementation, this would send to LLM and return processed response
            return new
            {
                success = true,
                analysis_type = analysisType,
                reynolds_prompt = contextualizedPrompt,
                organizational_scope = TARGET_ORGANIZATION,
                timestamp = DateTime.UtcNow,
                request_parameters = arguments,
                message = "Reynolds organizational intelligence prompt prepared. In production, this would be processed by LLM for intelligent response."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Reynolds intelligent prompt for {AnalysisType}", analysisType);
            throw;
        }
    }

    private async Task<string> GatherOrganizationalContext()
    {
        try
        {
            // Gather real-time organizational context
            var orgIssues = await _issuesService.GetOrganizationIssuesAsync(TARGET_ORGANIZATION, "open", 50);
            var recentActivity = orgIssues.Take(10).Select(i => $"- {i.Repository}: {i.Title} (#{i.Number})");
            
            return $@"
Recent organizational activity across {TARGET_ORGANIZATION}:
{string.Join("\n", recentActivity)}

Active repositories: {orgIssues.Select(i => i.Repository).Distinct().Count()}
Open issues: {orgIssues.Count()}
Last updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not gather full organizational context, using limited data");
            return $"Organizational context for {TARGET_ORGANIZATION} - limited data available due to access constraints.";
        }
    }

    private static object GetOrganizationalTools()
    {
        return new object[]
        {
            new
            {
                name = "analyze_org_projects",
                description = "Reynolds analyzes GitHub projects across the entire dynamicstms365 organization with supernatural PM insight",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        project_scope = new { type = "string", description = "Scope of analysis: 'all', 'active', 'critical', or specific project names", @default = "all" },
                        analysis_level = new { type = "string", description = "Depth of analysis: 'overview', 'detailed', 'comprehensive'", @default = "comprehensive" },
                        focus_areas = new { type = "array", items = new { type = "string" }, description = "Specific areas to focus on: velocity, dependencies, resources, etc." }
                    },
                    required = new[] { "project_scope" }
                }
            },
            new
            {
                name = "cross_repo_orchestration",
                description = "Reynolds orchestrates work items across multiple repositories with strategic coordination",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        primary_repo = new { type = "string", description = "Primary repository for the work item" },
                        work_item = new { type = "string", description = "Description of the work item to orchestrate" },
                        related_repos = new { type = "array", items = new { type = "string" }, description = "Related repositories that may be impacted" },
                        stakeholders = new { type = "array", items = new { type = "string" }, description = "Key stakeholders to coordinate" },
                        timeline = new { type = "string", description = "Desired timeline or deadline" }
                    },
                    required = new[] { "primary_repo", "work_item" }
                }
            },
            new
            {
                name = "org_dependency_intelligence",
                description = "Reynolds maps dependencies across the organization with strategic risk assessment",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        source_repo = new { type = "string", description = "Starting repository for dependency analysis" },
                        dependency_type = new { type = "string", description = "Type of dependencies: 'technical', 'resource', 'timeline', 'all'", @default = "all" },
                        time_horizon = new { type = "string", description = "Analysis time frame: 'sprint', 'quarterly', 'annual'", @default = "quarterly" },
                        risk_level = new { type = "string", description = "Minimum risk level to analyze: 'low', 'medium', 'high', 'critical'", @default = "medium" }
                    },
                    required = new[] { "source_repo" }
                }
            },
            new
            {
                name = "org_project_health",
                description = "Reynolds assesses organizational project health with proactive intervention recommendations",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        project_ids = new { type = "array", items = new { type = "string" }, description = "Specific project IDs to assess, or empty for all projects" },
                        assessment_period = new { type = "string", description = "Time period for assessment: 'weekly', 'monthly', 'quarterly'", @default = "monthly" },
                        health_dimensions = new { type = "array", items = new { type = "string" }, description = "Health aspects to assess: velocity, quality, satisfaction, debt" },
                        include_recommendations = new { type = "boolean", description = "Include actionable recommendations", @default = true }
                    }
                }
            },
            new
            {
                name = "strategic_stakeholder_coordination",
                description = "Reynolds coordinates stakeholders across organizational boundaries with diplomatic excellence",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        scope = new { type = "string", description = "Coordination scope: 'repository', 'project', 'organizational'", @default = "organizational" },
                        context = new { type = "string", description = "Context or situation requiring coordination" },
                        stakeholder_groups = new { type = "array", items = new { type = "string" }, description = "Stakeholder groups involved" },
                        urgency = new { type = "string", description = "Urgency level: 'low', 'normal', 'high', 'critical'", @default = "normal" },
                        communication_preferences = new { type = "object", description = "Preferred communication methods and cadence" }
                    },
                    required = new[] { "context" }
                }
            }
        };
    }

    private static object GetOrganizationalResources()
    {
        return new object[]
        {
            new
            {
                uri = "org://dynamicstms365/projects",
                name = "Organization Projects",
                description = "All GitHub projects across dynamicstms365 organization"
            },
            new
            {
                uri = "org://dynamicstms365/repositories",
                name = "Organization Repositories",
                description = "All repositories in dynamicstms365 organization with metadata"
            },
            new
            {
                uri = "org://dynamicstms365/dependencies",
                name = "Cross-Repository Dependencies",
                description = "Dependency map across all organizational repositories"
            },
            new
            {
                uri = "org://dynamicstms365/stakeholders",
                name = "Organizational Stakeholder Map",
                description = "Stakeholder relationships and communication preferences across the organization"
            },
            new
            {
                uri = "org://dynamicstms365/health-metrics",
                name = "Organizational Health Metrics",
                description = "Real-time health metrics across all projects and repositories"
            }
        };
    }
}

// Supporting data models for organizational orchestration
public class OrgProjectAnalysisRequest
{
    public string ProjectScope { get; set; } = "all";
    public string AnalysisLevel { get; set; } = "comprehensive";
    public string[] FocusAreas { get; set; } = Array.Empty<string>();
}

public class CrossRepoRequest
{
    public string PrimaryRepo { get; set; } = "";
    public string WorkItemDescription { get; set; } = "";
    public string[] RelatedRepos { get; set; } = Array.Empty<string>();
    public string[] Stakeholders { get; set; } = Array.Empty<string>();
    public string Timeline { get; set; } = "";
}

public class DependencyAnalysisRequest
{
    public string SourceRepo { get; set; } = "";
    public string DependencyType { get; set; } = "all";
    public string TimeHorizon { get; set; } = "quarterly";
    public string RiskLevel { get; set; } = "medium";
}

public class ProjectHealthRequest
{
    public string[] ProjectIds { get; set; } = Array.Empty<string>();
    public string AssessmentPeriod { get; set; } = "monthly";
    public string[] HealthDimensions { get; set; } = Array.Empty<string>();
    public bool IncludeRecommendations { get; set; } = true;
}