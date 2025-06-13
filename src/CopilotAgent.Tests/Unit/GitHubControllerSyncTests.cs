using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using CopilotAgent.Controllers;
using CopilotAgent.Services;
using Shared.Models;

namespace CopilotAgent.Tests.Unit;

[TestFixture]
public class GitHubControllerSyncTests
{
    private Mock<IGitHubAppAuthService> _mockAuthService;
    private Mock<ISecurityAuditService> _mockAuditService;
    private Mock<IGitHubWebhookValidator> _mockWebhookValidator;
    private Mock<IGitHubWorkflowOrchestrator> _mockWorkflowOrchestrator;
    private Mock<IGitHubSemanticSearchService> _mockSemanticSearchService;
    private Mock<IGitHubDiscussionsService> _mockDiscussionsService;
    private Mock<IGitHubIssuesService> _mockIssuesService;
    private Mock<IGitHubIssuePRSynchronizationService> _mockSyncService;
    private Mock<ILogger<GitHubController>> _mockLogger;
    private Mock<IConfiguration> _mockConfiguration;
    private GitHubController _controller;

    [SetUp]
    public void Setup()
    {
        _mockAuthService = new Mock<IGitHubAppAuthService>();
        _mockAuditService = new Mock<ISecurityAuditService>();
        _mockWebhookValidator = new Mock<IGitHubWebhookValidator>();
        _mockWorkflowOrchestrator = new Mock<IGitHubWorkflowOrchestrator>();
        _mockSemanticSearchService = new Mock<IGitHubSemanticSearchService>();
        _mockDiscussionsService = new Mock<IGitHubDiscussionsService>();
        _mockIssuesService = new Mock<IGitHubIssuesService>();
        _mockSyncService = new Mock<IGitHubIssuePRSynchronizationService>();
        _mockLogger = new Mock<ILogger<GitHubController>>();
        _mockConfiguration = new Mock<IConfiguration>();

        _controller = new GitHubController(
            _mockAuthService.Object,
            _mockAuditService.Object,
            _mockWebhookValidator.Object,
            _mockWorkflowOrchestrator.Object,
            _mockSemanticSearchService.Object,
            _mockDiscussionsService.Object,
            _mockIssuesService.Object,
            _mockSyncService.Object,
            _mockLogger.Object,
            _mockConfiguration.Object);
    }

    [Test]
    public async Task GetSynchronizationReport_ReturnsReport_WhenSuccessful()
    {
        // Arrange
        var repository = "owner/repo";
        var expectedReport = new IssuePRSynchronizationReport
        {
            Repository = repository,
            Summary = new IssuePRSynchronizationSummary
            {
                TotalIssues = 10,
                TotalPRs = 5,
                SynchronizedRelations = 3,
                NeedsUpdateRelations = 2
            }
        };

        _mockSyncService.Setup(s => s.GenerateSynchronizationReportAsync(repository))
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GetSynchronizationReport(repository);

        // Assert
        Assert.That(result, Is.TypeOf<ActionResult<IssuePRSynchronizationReport>>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        
        var report = okResult.Value as IssuePRSynchronizationReport;
        Assert.That(report, Is.Not.Null);
        Assert.That(report.Repository, Is.EqualTo(repository));
        Assert.That(report.Summary.TotalIssues, Is.EqualTo(10));
        Assert.That(report.Summary.TotalPRs, Is.EqualTo(5));
    }

    [Test]
    public async Task SynchronizeIssue_ReturnsSuccess_WhenSynchronizationSucceeds()
    {
        // Arrange
        var repository = "owner/repo";
        var issueNumber = 123;

        _mockSyncService.Setup(s => s.SynchronizeIssueWithPRsAsync(repository, issueNumber))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SynchronizeIssue(repository, issueNumber);

        // Assert
        Assert.That(result, Is.TypeOf<ActionResult<object>>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        
        dynamic response = okResult.Value;
        var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
        Assert.That(responseJson, Does.Contain("\"success\":true"));
        Assert.That(responseJson, Does.Contain($"\"issueNumber\":{issueNumber}"));
        Assert.That(responseJson, Does.Contain($"\"repository\":\"{repository}\""));
    }

    [Test]
    public async Task SynchronizeIssue_ReturnsFalse_WhenSynchronizationFails()
    {
        // Arrange
        var repository = "owner/repo";
        var issueNumber = 123;

        _mockSyncService.Setup(s => s.SynchronizeIssueWithPRsAsync(repository, issueNumber))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.SynchronizeIssue(repository, issueNumber);

        // Assert
        Assert.That(result, Is.TypeOf<ActionResult<object>>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        
        dynamic response = okResult.Value;
        var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
        Assert.That(responseJson, Does.Contain("\"success\":false"));
        Assert.That(responseJson, Does.Contain($"\"issueNumber\":{issueNumber}"));
        Assert.That(responseJson, Does.Contain($"\"repository\":\"{repository}\""));
    }

    [Test]
    public async Task SynchronizeAllIssues_ReturnsCount_WhenSuccessful()
    {
        // Arrange
        var repository = "owner/repo";
        var expectedCount = 5;

        _mockSyncService.Setup(s => s.SynchronizeAllIssuesWithPRsAsync(repository))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _controller.SynchronizeAllIssues(repository);

        // Assert
        Assert.That(result, Is.TypeOf<ActionResult<object>>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        
        dynamic response = okResult.Value;
        var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
        Assert.That(responseJson, Does.Contain("\"success\":true"));
        Assert.That(responseJson, Does.Contain($"\"synchronizedCount\":{expectedCount}"));
        Assert.That(responseJson, Does.Contain($"\"repository\":\"{repository}\""));
    }

    [Test]
    public async Task GetPullRequests_ReturnsPRs_WhenSuccessful()
    {
        // Arrange
        var repository = "owner/repo";
        var expectedPRs = new List<GitHubPullRequest>
        {
            new GitHubPullRequest { Number = 1, Title = "First PR", State = "open" },
            new GitHubPullRequest { Number = 2, Title = "Second PR", State = "closed" }
        };

        _mockSyncService.Setup(s => s.GetPullRequestsByRepositoryAsync(repository, "all", 100))
            .ReturnsAsync(expectedPRs);

        // Act
        var result = await _controller.GetPullRequests(repository);

        // Assert
        Assert.That(result, Is.TypeOf<ActionResult<IEnumerable<GitHubPullRequest>>>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        
        var prs = okResult.Value as IEnumerable<GitHubPullRequest>;
        Assert.That(prs, Is.Not.Null);
        Assert.That(prs.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetPullRequest_ReturnsPR_WhenSuccessful()
    {
        // Arrange
        var repository = "owner/repo";
        var prNumber = 123;
        var expectedPR = new GitHubPullRequest 
        { 
            Number = prNumber, 
            Title = "Test PR", 
            State = "open",
            Repository = repository
        };

        _mockSyncService.Setup(s => s.GetPullRequestAsync(repository, prNumber))
            .ReturnsAsync(expectedPR);

        // Act
        var result = await _controller.GetPullRequest(repository, prNumber);

        // Assert
        Assert.That(result, Is.TypeOf<ActionResult<GitHubPullRequest>>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        
        var pr = okResult.Value as GitHubPullRequest;
        Assert.That(pr, Is.Not.Null);
        Assert.That(pr.Number, Is.EqualTo(prNumber));
        Assert.That(pr.Title, Is.EqualTo("Test PR"));
    }

    [Test]
    public async Task GetLinkedPullRequests_ReturnsPRs_WhenSuccessful()
    {
        // Arrange
        var repository = "owner/repo";
        var issueNumber = 123;
        var expectedPRs = new List<GitHubPullRequest>
        {
            new GitHubPullRequest { Number = 1, Title = "Fix issue #123", State = "open" }
        };

        _mockSyncService.Setup(s => s.FindPullRequestsLinkedToIssueAsync(repository, issueNumber))
            .ReturnsAsync(expectedPRs);

        // Act
        var result = await _controller.GetLinkedPullRequests(repository, issueNumber);

        // Assert
        Assert.That(result, Is.TypeOf<ActionResult<IEnumerable<GitHubPullRequest>>>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        
        var prs = okResult.Value as IEnumerable<GitHubPullRequest>;
        Assert.That(prs, Is.Not.Null);
        Assert.That(prs.Count(), Is.EqualTo(1));
        Assert.That(prs.First().Number, Is.EqualTo(1));
    }

    [Test]
    public async Task GetLinkedIssues_ReturnsIssues_WhenSuccessful()
    {
        // Arrange
        var repository = "owner/repo";
        var prNumber = 123;
        var expectedIssues = new List<GitHubIssue>
        {
            new GitHubIssue { Number = 1, Title = "Test issue", State = "open" }
        };

        _mockSyncService.Setup(s => s.FindIssuesLinkedToPullRequestAsync(repository, prNumber))
            .ReturnsAsync(expectedIssues);

        // Act
        var result = await _controller.GetLinkedIssues(repository, prNumber);

        // Assert
        Assert.That(result, Is.TypeOf<ActionResult<IEnumerable<GitHubIssue>>>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        
        var issues = okResult.Value as IEnumerable<GitHubIssue>;
        Assert.That(issues, Is.Not.Null);
        Assert.That(issues.Count(), Is.EqualTo(1));
        Assert.That(issues.First().Number, Is.EqualTo(1));
    }

    [Test]
    public async Task GetSynchronizationReport_ReturnsError_WhenExceptionThrown()
    {
        // Arrange
        var repository = "owner/repo";
        _mockSyncService.Setup(s => s.GenerateSynchronizationReportAsync(repository))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetSynchronizationReport(repository);

        // Assert
        Assert.That(result, Is.TypeOf<ActionResult<IssuePRSynchronizationReport>>());
        var statusResult = result.Result as ObjectResult;
        Assert.That(statusResult, Is.Not.Null);
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task SynchronizeIssue_ReturnsError_WhenExceptionThrown()
    {
        // Arrange
        var repository = "owner/repo";
        var issueNumber = 123;
        _mockSyncService.Setup(s => s.SynchronizeIssueWithPRsAsync(repository, issueNumber))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.SynchronizeIssue(repository, issueNumber);

        // Assert
        Assert.That(result, Is.TypeOf<ActionResult<object>>());
        var statusResult = result.Result as ObjectResult;
        Assert.That(statusResult, Is.Not.Null);
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }
}