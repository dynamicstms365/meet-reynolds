using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using CopilotAgent.Services;
using Shared.Models;
using System.Net;
using System.Text.Json;
using Moq.Protected;
using System.Text;

namespace CopilotAgent.Tests.Unit;

[TestFixture]
public class GitHubIssuePRSynchronizationTests
{
    private Mock<IGitHubAppAuthService> _mockAuthService;
    private Mock<IGitHubIssuesService> _mockIssuesService;
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private HttpClient _mockHttpClient;
    private Mock<ILogger<GitHubIssuePRSynchronizationService>> _mockLogger;
    private Mock<IConfiguration> _mockConfiguration;
    private GitHubIssuePRSynchronizationService _service;

    [SetUp]
    public void Setup()
    {
        _mockAuthService = new Mock<IGitHubAppAuthService>();
        _mockIssuesService = new Mock<IGitHubIssuesService>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockHttpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockLogger = new Mock<ILogger<GitHubIssuePRSynchronizationService>>();
        _mockConfiguration = new Mock<IConfiguration>();

        _service = new GitHubIssuePRSynchronizationService(
            _mockAuthService.Object,
            _mockIssuesService.Object,
            _mockHttpClient,
            _mockLogger.Object,
            _mockConfiguration.Object);

        // Setup default auth token
        _mockAuthService.Setup(x => x.GetInstallationTokenAsync())
            .ReturnsAsync(new GitHubAppAuthentication { Token = "test-token" });
    }

    [TearDown]
    public void TearDown()
    {
        _mockHttpClient?.Dispose();
    }

    [Test]
    public async Task GetPullRequestsByRepositoryAsync_ShouldReturnPullRequests_WhenSuccessful()
    {
        // Arrange
        var repository = "test-org/test-repo";
        var prResponseJson = JsonSerializer.Serialize(new[]
        {
            new
            {
                node_id = "PR_1",
                number = 1,
                title = "Fix issue #123",
                body = "This PR fixes #123",
                html_url = "https://github.com/test-org/test-repo/pull/1",
                state = "open",
                merged = false,
                created_at = "2024-01-01T10:00:00Z",
                updated_at = "2024-01-01T10:30:00Z",
                merged_at = (string?)null,
                closed_at = (string?)null,
                user = new { login = "developer1" },
                @base = new { 
                    @ref = "main",
                    repo = new { full_name = repository }
                },
                head = new { @ref = "feature-branch" },
                labels = new object[] { },
                assignees = new object[] { }
            }
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(prResponseJson, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.GetPullRequestsByRepositoryAsync(repository);

        // Assert
        Assert.That(result, Is.Not.Null);
        var prs = result.ToList();
        Assert.That(prs.Count, Is.EqualTo(1));
        Assert.That(prs[0].Number, Is.EqualTo(1));
        Assert.That(prs[0].Title, Is.EqualTo("Fix issue #123"));
        Assert.That(prs[0].LinkedIssueNumbers, Contains.Item(123));
    }

    [Test]
    public async Task GetPullRequestAsync_ShouldReturnPullRequest_WhenSuccessful()
    {
        // Arrange
        var repository = "test-org/test-repo";
        var prNumber = 1;
        var prResponseJson = JsonSerializer.Serialize(new
        {
            node_id = "PR_1",
            number = 1,
            title = "Fix issue #123",
            body = "This PR fixes #123 and resolves #456",
            html_url = "https://github.com/test-org/test-repo/pull/1",
            state = "open",
            merged = false,
            created_at = "2024-01-01T10:00:00Z",
            updated_at = "2024-01-01T10:30:00Z",
            merged_at = (string?)null,
            closed_at = (string?)null,
            user = new { login = "developer1" },
            @base = new { 
                @ref = "main",
                repo = new { full_name = repository }
            },
            head = new { @ref = "feature-branch" },
            labels = new object[] { },
            assignees = new object[] { }
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(prResponseJson, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.GetPullRequestAsync(repository, prNumber);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Number, Is.EqualTo(1));
        Assert.That(result.Title, Is.EqualTo("Fix issue #123"));
        Assert.That(result.LinkedIssueNumbers, Has.Length.EqualTo(2));
        Assert.That(result.LinkedIssueNumbers, Contains.Item(123));
        Assert.That(result.LinkedIssueNumbers, Contains.Item(456));
    }

    [Test]
    public async Task FindPullRequestsLinkedToIssueAsync_ShouldReturnLinkedPRs()
    {
        // Arrange
        var repository = "test-org/test-repo";
        var issueNumber = 123;
        
        // Mock PR data with one PR linked to issue #123
        var prResponseJson = JsonSerializer.Serialize(new[]
        {
            new
            {
                node_id = "PR_1",
                number = 1,
                title = "Fix issue #123",
                body = "This PR fixes the bug described in #123",
                html_url = "https://github.com/test-org/test-repo/pull/1",
                state = "open",
                merged = false,
                created_at = "2024-01-01T10:00:00Z",
                updated_at = "2024-01-01T10:30:00Z",
                merged_at = (string?)null,
                closed_at = (string?)null,
                user = new { login = "developer1" },
                @base = new { 
                    @ref = "main",
                    repo = new { full_name = repository }
                },
                head = new { @ref = "feature-branch" },
                labels = new object[] { },
                assignees = new object[] { }
            },
            new
            {
                node_id = "PR_2",
                number = 2,
                title = "Unrelated PR",
                body = "This PR is unrelated to any issues",
                html_url = "https://github.com/test-org/test-repo/pull/2",
                state = "open",
                merged = false,
                created_at = "2024-01-01T11:00:00Z",
                updated_at = "2024-01-01T11:30:00Z",
                merged_at = (string?)null,
                closed_at = (string?)null,
                user = new { login = "developer2" },
                @base = new { 
                    @ref = "main",
                    repo = new { full_name = repository }
                },
                head = new { @ref = "another-feature" },
                labels = new object[] { },
                assignees = new object[] { }
            }
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(prResponseJson, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.FindPullRequestsLinkedToIssueAsync(repository, issueNumber);

        // Assert
        Assert.That(result, Is.Not.Null);
        var linkedPRs = result.ToList();
        Assert.That(linkedPRs.Count, Is.EqualTo(1));
        Assert.That(linkedPRs[0].Number, Is.EqualTo(1));
        Assert.That(linkedPRs[0].LinkedIssueNumbers, Contains.Item(123));
    }

    [Test]
    public async Task FindIssuesLinkedToPullRequestAsync_ShouldReturnLinkedIssues()
    {
        // Arrange
        var repository = "test-org/test-repo";
        var prNumber = 1;

        // Mock PR response
        var prResponseJson = JsonSerializer.Serialize(new
        {
            node_id = "PR_1",
            number = 1,
            title = "Fix multiple issues",
            body = "This PR fixes #123 and closes #456",
            html_url = "https://github.com/test-org/test-repo/pull/1",
            state = "open",
            merged = false,
            created_at = "2024-01-01T10:00:00Z",
            updated_at = "2024-01-01T10:30:00Z",
            merged_at = (string?)null,
            closed_at = (string?)null,
            user = new { login = "developer1" },
            @base = new { 
                @ref = "main",
                repo = new { full_name = repository }
            },
            head = new { @ref = "feature-branch" },
            labels = new object[] { },
            assignees = new object[] { }
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(prResponseJson, Encoding.UTF8, "application/json")
            });

        // Mock issues service
        var issue123 = new GitHubIssue
        {
            Number = 123,
            Title = "First issue",
            State = "open",
            Repository = repository
        };
        var issue456 = new GitHubIssue
        {
            Number = 456,
            Title = "Second issue", 
            State = "open",
            Repository = repository
        };

        _mockIssuesService.Setup(x => x.GetIssueAsync(repository, 123))
            .ReturnsAsync(issue123);
        _mockIssuesService.Setup(x => x.GetIssueAsync(repository, 456))
            .ReturnsAsync(issue456);

        // Act
        var result = await _service.FindIssuesLinkedToPullRequestAsync(repository, prNumber);

        // Assert
        Assert.That(result, Is.Not.Null);
        var linkedIssues = result.ToList();
        Assert.That(linkedIssues.Count, Is.EqualTo(2));
        Assert.That(linkedIssues.Any(i => i.Number == 123), Is.True);
        Assert.That(linkedIssues.Any(i => i.Number == 456), Is.True);
    }

    [Test]
    public async Task SynchronizeIssueWithPRsAsync_ShouldUpdateIssueState_WhenPRIsMerged()
    {
        // Arrange
        var repository = "test-org/test-repo";
        var issueNumber = 123;

        // Mock issue
        var issue = new GitHubIssue
        {
            Number = issueNumber,
            Title = "Test issue",
            State = "open",
            Repository = repository
        };

        // Mock merged PR
        var prResponseJson = JsonSerializer.Serialize(new[]
        {
            new
            {
                node_id = "PR_1",
                number = 1,
                title = "Fix issue #123",
                body = "This PR fixes #123",
                html_url = "https://github.com/test-org/test-repo/pull/1",
                state = "closed",
                merged = true,
                created_at = "2024-01-01T10:00:00Z",
                updated_at = "2024-01-01T10:30:00Z",
                merged_at = "2024-01-01T12:00:00Z",
                closed_at = "2024-01-01T12:00:00Z",
                user = new { login = "developer1" },
                @base = new { 
                    @ref = "main",
                    repo = new { full_name = repository }
                },
                head = new { @ref = "feature-branch" },
                labels = new object[] { },
                assignees = new object[] { }
            }
        });

        _mockIssuesService.Setup(x => x.GetIssueAsync(repository, issueNumber))
            .ReturnsAsync(issue);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(prResponseJson, Encoding.UTF8, "application/json")
            });

        _mockIssuesService.Setup(x => x.UpdateIssueAsync(repository, issueNumber, null, null, "closed", null))
            .ReturnsAsync(new GitHubIssue { Number = issueNumber, State = "closed" });

        _mockIssuesService.Setup(x => x.AddIssueCommentAsync(repository, issueNumber, It.IsAny<string>()))
            .ReturnsAsync(new GitHubComment { Body = "Test comment" });

        // Act
        var result = await _service.SynchronizeIssueWithPRsAsync(repository, issueNumber);

        // Assert
        Assert.That(result, Is.True);
        _mockIssuesService.Verify(x => x.UpdateIssueAsync(repository, issueNumber, null, null, "closed", null), Times.Once);
        _mockIssuesService.Verify(x => x.AddIssueCommentAsync(repository, issueNumber, It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task GenerateSynchronizationReportAsync_ShouldReturnCompleteReport()
    {
        // Arrange
        var repository = "test-org/test-repo";

        // Mock issues
        var issues = new List<GitHubIssue>
        {
            new() { Number = 123, Title = "Issue with PR", State = "open", Repository = repository },
            new() { Number = 456, Title = "Orphaned issue", State = "open", Repository = repository }
        };

        // Mock PRs - one linked to issue #123, one orphaned
        var prResponseJson = JsonSerializer.Serialize(new[]
        {
            new
            {
                node_id = "PR_1",
                number = 1,
                title = "Fix issue #123",
                body = "This PR fixes #123",
                html_url = "https://github.com/test-org/test-repo/pull/1",
                state = "open",
                merged = false,
                created_at = "2024-01-01T10:00:00Z",
                updated_at = "2024-01-01T10:30:00Z",
                merged_at = (string?)null,
                closed_at = (string?)null,
                user = new { login = "developer1" },
                @base = new { 
                    @ref = "main",
                    repo = new { full_name = repository }
                },
                head = new { @ref = "feature-branch" },
                labels = new object[] { },
                assignees = new object[] { }
            },
            new
            {
                node_id = "PR_2",
                number = 2,
                title = "Orphaned PR",
                body = "This PR has no linked issues",
                html_url = "https://github.com/test-org/test-repo/pull/2",
                state = "open",
                merged = false,
                created_at = "2024-01-01T11:00:00Z",
                updated_at = "2024-01-01T11:30:00Z",
                merged_at = (string?)null,
                closed_at = (string?)null,
                user = new { login = "developer2" },
                @base = new { 
                    @ref = "main",
                    repo = new { full_name = repository }
                },
                head = new { @ref = "another-feature" },
                labels = new object[] { },
                assignees = new object[] { }
            }
        });

        _mockIssuesService.Setup(x => x.GetIssuesByRepositoryAsync(repository, "all", 200))
            .ReturnsAsync(issues);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(prResponseJson, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.GenerateSynchronizationReportAsync(repository);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Repository, Is.EqualTo(repository));
        Assert.That(result.Summary.TotalIssues, Is.EqualTo(2));
        Assert.That(result.Summary.TotalPRs, Is.EqualTo(2));
        Assert.That(result.Summary.OrphanedPRs, Is.EqualTo(1));
        Assert.That(result.Summary.OrphanedIssues, Is.EqualTo(1));
        
        var relations = result.IssuePRRelations.ToList();
        Assert.That(relations.Count, Is.EqualTo(1)); // One issue-PR relation
        Assert.That(relations[0].Issue.Number, Is.EqualTo(123));
        
        var orphanedPRs = result.OrphanedPRs.ToList();
        Assert.That(orphanedPRs.Count, Is.EqualTo(1));
        Assert.That(orphanedPRs[0].Number, Is.EqualTo(2));
        
        var orphanedIssues = result.OrphanedIssues.ToList();
        Assert.That(orphanedIssues.Count, Is.EqualTo(1));
        Assert.That(orphanedIssues[0].Number, Is.EqualTo(456));
    }

    [Test]
    public async Task GetPullRequestsByRepositoryAsync_ShouldReturnEmpty_WhenAPICallFails()
    {
        // Arrange
        var repository = "test-org/test-repo";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        // Act
        var result = await _service.GetPullRequestsByRepositoryAsync(repository);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task SynchronizeIssueWithPRsAsync_ShouldReturnTrue_WhenNoRelatedPRs()
    {
        // Arrange
        var repository = "test-org/test-repo";
        var issueNumber = 123;

        var issue = new GitHubIssue
        {
            Number = issueNumber,
            Title = "Test issue",
            State = "open",
            Repository = repository
        };

        _mockIssuesService.Setup(x => x.GetIssueAsync(repository, issueNumber))
            .ReturnsAsync(issue);

        // Return empty PR array
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.SynchronizeIssueWithPRsAsync(repository, issueNumber);

        // Assert
        Assert.That(result, Is.True);
        _mockIssuesService.Verify(x => x.UpdateIssueAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
    }
}