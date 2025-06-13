using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shared.Models;
using CopilotAgent.Services;

namespace CopilotAgent.Tests.Unit;

[TestFixture]
public class ScopeCreepMonitoringServiceTests
{
    private Mock<ILogger<ScopeCreepMonitoringService>> _mockLogger;
    private Mock<IGitHubIssuesService> _mockGitHubIssuesService;
    private Mock<ISecurityAuditService> _mockAuditService;
    private ScopeCreepMonitoringService _service;
    private ScopeMonitoringOptions _options;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<ScopeCreepMonitoringService>>();
        _mockGitHubIssuesService = new Mock<IGitHubIssuesService>();
        _mockAuditService = new Mock<ISecurityAuditService>();
        
        _options = new ScopeMonitoringOptions
        {
            EnableScopeMonitoring = true,
            ScopeDeviationThreshold = 0.25,
            EnableReynoldsMessages = true,
            EnableRecommendations = true
        };

        _service = new ScopeCreepMonitoringService(
            _mockLogger.Object,
            _mockGitHubIssuesService.Object,
            _mockAuditService.Object,
            _options);
    }

    [Test]
    public async Task AnalyzeProjectScopeAsync_WithinScope_ReturnsNoScopeCreep()
    {
        // Arrange
        var repository = "test/repo";
        var scopeParameters = new ProjectScopeParameters
        {
            ProjectId = "test-project",
            Repository = repository,
            ExpectedIssueCount = 10,
            ExpectedPullRequestCount = 3,
            ExpectedTaskCount = 10,
            ScopeDeviationThreshold = 0.25
        };

        var mockIssues = CreateMockIssues(8); // Less than expected, so no scope creep
        _mockGitHubIssuesService.Setup(x => x.GetIssuesByRepositoryAsync(repository, "all", 50))
            .ReturnsAsync(mockIssues);

        // Act
        var result = await _service.AnalyzeProjectScopeAsync(repository, scopeParameters);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.HasScopeCreep, Is.False);
        Assert.That(result.Repository, Is.EqualTo(repository));
        Assert.That(result.ProjectId, Is.EqualTo("test-project"));
        Assert.That(result.CurrentIssueCount, Is.EqualTo(8));
        Assert.That(result.CreepIndicators, Is.Empty);
    }

    [Test]
    public async Task AnalyzeProjectScopeAsync_ExceedsScope_ReturnsScopeCreep()
    {
        // Arrange
        var repository = "test/repo";
        var scopeParameters = new ProjectScopeParameters
        {
            ProjectId = "test-project",
            Repository = repository,
            ExpectedIssueCount = 10,
            ExpectedPullRequestCount = 3,
            ExpectedTaskCount = 10,
            ScopeDeviationThreshold = 0.25
        };

        var mockIssues = CreateMockIssues(15); // 50% more than expected
        _mockGitHubIssuesService.Setup(x => x.GetIssuesByRepositoryAsync(repository, "all", 50))
            .ReturnsAsync(mockIssues);

        // Act
        var result = await _service.AnalyzeProjectScopeAsync(repository, scopeParameters);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.HasScopeCreep, Is.True);
        Assert.That(result.Repository, Is.EqualTo(repository));
        Assert.That(result.CurrentIssueCount, Is.EqualTo(15));
        Assert.That(result.IssueDeviation, Is.EqualTo(0.5).Within(0.01)); // 50% deviation
        Assert.That(result.CreepIndicators, Is.Not.Empty);
        Assert.That(result.CreepIndicators[0], Does.Contain("Issue count exceeded expected"));
    }

    [Test]
    public async Task CheckForScopeCreepAsync_NoCreep_ReturnsNull()
    {
        // Arrange
        var repository = "test/repo";
        var scopeParameters = new ProjectScopeParameters
        {
            ProjectId = "test-project",
            Repository = repository,
            ExpectedIssueCount = 10,
            ExpectedPullRequestCount = 3,
            ExpectedTaskCount = 10,
            ScopeDeviationThreshold = 0.25
        };

        var mockIssues = CreateMockIssues(8); // Within scope
        _mockGitHubIssuesService.Setup(x => x.GetIssuesByRepositoryAsync(repository, "all", 50))
            .ReturnsAsync(mockIssues);

        // Act
        var result = await _service.CheckForScopeCreepAsync(repository, scopeParameters);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CheckForScopeCreepAsync_WithCreep_ReturnsAlert()
    {
        // Arrange
        var repository = "test/repo";
        var scopeParameters = new ProjectScopeParameters
        {
            ProjectId = "test-project",
            Repository = repository,
            ExpectedIssueCount = 10,
            ExpectedPullRequestCount = 3,
            ExpectedTaskCount = 10,
            ScopeDeviationThreshold = 0.25
        };

        var mockIssues = CreateMockIssues(20); // 100% more than expected - critical level
        _mockGitHubIssuesService.Setup(x => x.GetIssuesByRepositoryAsync(repository, "all", 50))
            .ReturnsAsync(mockIssues);

        // Act
        var result = await _service.CheckForScopeCreepAsync(repository, scopeParameters);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Repository, Is.EqualTo(repository));
        Assert.That(result.ProjectId, Is.EqualTo("test-project"));
        Assert.That(result.Severity, Is.EqualTo(ScopeCreepSeverity.Critical));
        Assert.That(result.Summary, Is.Not.Empty);
        Assert.That(result.ReynoldsMessage, Is.Not.Empty);
        Assert.That(result.Changes, Is.Not.Empty);
        Assert.That(result.Recommendations, Is.Not.Empty);
        Assert.That(result.Metrics.HasScopeCreep, Is.True);
    }

    [Test]
    public async Task IsProjectWithinScopeAsync_WithinScope_ReturnsTrue()
    {
        // Arrange
        var repository = "test/repo";
        var scopeParameters = new ProjectScopeParameters
        {
            ProjectId = "test-project",
            Repository = repository,
            ExpectedIssueCount = 10,
            ExpectedPullRequestCount = 3,
            ExpectedTaskCount = 10,
            ScopeDeviationThreshold = 0.25
        };

        var mockIssues = CreateMockIssues(8);
        _mockGitHubIssuesService.Setup(x => x.GetIssuesByRepositoryAsync(repository, "all", 50))
            .ReturnsAsync(mockIssues);

        // Act
        var result = await _service.IsProjectWithinScopeAsync(repository, scopeParameters);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsProjectWithinScopeAsync_ExceedsScope_ReturnsFalse()
    {
        // Arrange
        var repository = "test/repo";
        var scopeParameters = new ProjectScopeParameters
        {
            ProjectId = "test-project",
            Repository = repository,
            ExpectedIssueCount = 10,
            ExpectedPullRequestCount = 3,
            ExpectedTaskCount = 10,
            ScopeDeviationThreshold = 0.25
        };

        var mockIssues = CreateMockIssues(15); // Exceeds scope
        _mockGitHubIssuesService.Setup(x => x.GetIssuesByRepositoryAsync(repository, "all", 50))
            .ReturnsAsync(mockIssues);

        // Act
        var result = await _service.IsProjectWithinScopeAsync(repository, scopeParameters);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task SendScopeCreepAlertAsync_ValidAlert_LogsAndAudits()
    {
        // Arrange
        var alert = new ScopeCreepAlert
        {
            ProjectId = "test-project",
            Repository = "test/repo",
            Severity = ScopeCreepSeverity.Medium,
            Summary = "Test scope creep detected",
            ReynoldsMessage = "Test Reynolds message",
            Changes = new List<string> { "Issue count increased" },
            Recommendations = new List<string> { "Consider splitting project" }
        };

        // Act
        await _service.SendScopeCreepAlertAsync(alert);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(
                "Scope_Creep_Alert",
                null, // userId
                alert.Repository, // repository
                "Medium", // action
                "Alert_Sent", // result
                alert), // details
            Times.Once);
    }

    [Test]
    public async Task RecordScopeEventAsync_ValidEvent_LogsAndAudits()
    {
        // Arrange
        var repository = "test/repo";
        var eventType = "issue_created";
        var eventData = new Dictionary<string, object>
        {
            { "issue_number", 123 },
            { "title", "New issue" }
        };

        // Act
        await _service.RecordScopeEventAsync(repository, eventType, eventData);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(
                "Scope_Event",
                null, // userId
                repository, // repository
                eventType, // action
                "Recorded", // result
                It.IsAny<object>()), // details
            Times.Once);
    }

    [Test]
    public async Task GetRecentAlertsAsync_WithAlerts_ReturnsFilteredAlerts()
    {
        // Arrange
        var repository = "test/repo";
        var scopeParameters = new ProjectScopeParameters
        {
            ProjectId = "test-project",
            Repository = repository,
            ExpectedIssueCount = 5,
            ExpectedPullRequestCount = 2,
            ExpectedTaskCount = 5,
            ScopeDeviationThreshold = 0.25
        };

        // Create scope creep to generate an alert
        var mockIssues = CreateMockIssues(10); // Double the expected
        _mockGitHubIssuesService.Setup(x => x.GetIssuesByRepositoryAsync(repository, "all", 50))
            .ReturnsAsync(mockIssues);

        // Generate and send an alert
        var alert = await _service.CheckForScopeCreepAsync(repository, scopeParameters);
        if (alert != null)
        {
            await _service.SendScopeCreepAlertAsync(alert);
        }

        // Act
        var recentAlerts = await _service.GetRecentAlertsAsync(repository, TimeSpan.FromHours(1));

        // Assert
        Assert.That(recentAlerts, Is.Not.Null);
        Assert.That(recentAlerts.Count, Is.EqualTo(1));
        Assert.That(recentAlerts[0].Repository, Is.EqualTo(repository));
    }

    private static List<GitHubIssue> CreateMockIssues(int count)
    {
        var issues = new List<GitHubIssue>();
        for (int i = 1; i <= count; i++)
        {
            issues.Add(new GitHubIssue
            {
                Number = i,
                Title = $"Test Issue {i}",
                Body = $"Test body for issue {i}",
                State = "open",
                Author = "test-user",
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                UpdatedAt = DateTime.UtcNow.AddDays(-i),
                Repository = "test/repo"
            });
        }
        return issues;
    }
}