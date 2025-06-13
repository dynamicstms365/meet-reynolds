using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using CopilotAgent.Services;
using Shared.Models;

namespace CopilotAgent.Tests.Unit;

[TestFixture]
public class ReynoldsMemeServiceTests
{
    private Mock<ILogger<ReynoldsMemeService>> _mockLogger;
    private Mock<IConfiguration> _mockConfiguration;
    private Mock<IReynoldsTeamsChatService> _mockTeamsChatService;
    private ReynoldsMemeService _memeService;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<ReynoldsMemeService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockTeamsChatService = new Mock<IReynoldsTeamsChatService>();
        
        _memeService = new ReynoldsMemeService(
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockTeamsChatService.Object
        );
    }

    [Test]
    public async Task GetRandomMemeAsync_Should_Return_Meme_When_Available()
    {
        // Act
        var result = await _memeService.GetRandomMemeAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Not.Empty);
        Assert.That(result.Name, Is.Not.Empty);
        Assert.That(result.Url, Is.Not.Empty);
        Assert.That(result.Description, Is.Not.Empty);
    }

    [Test]
    public async Task GetRandomMemeAsync_Should_Return_Category_Specific_Meme()
    {
        // Act
        var result = await _memeService.GetRandomMemeAsync("project-management");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Category, Is.EqualTo("project-management"));
    }

    [Test]
    public async Task GetMemesByCategoryAsync_Should_Return_Filtered_Memes()
    {
        // Act
        var result = await _memeService.GetMemesByCategoryAsync("development");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.GreaterThan(0));
        Assert.That(result.All(m => m.Category == "development"), Is.True);
    }

    [Test]
    public async Task GetAllMemesAsync_Should_Return_All_Available_Memes()
    {
        // Act
        var result = await _memeService.GetAllMemesAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.GreaterThan(0));
        Assert.That(result.All(m => !string.IsNullOrEmpty(m.Id)), Is.True);
    }

    [Test]
    public async Task AddMemeAsync_Should_Add_Meme_Successfully()
    {
        // Arrange
        var newMeme = new MemeItem
        {
            Name = "Test Meme",
            Url = "https://example.com/test.gif",
            Description = "A test meme",
            Category = "test"
        };

        // Act
        var result = await _memeService.AddMemeAsync(newMeme);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(newMeme.Id, Is.Not.Empty); // Should have assigned an ID
    }

    [Test]
    public async Task SendMemeToChannelAsync_Should_Return_Success()
    {
        // Arrange
        var meme = new MemeItem
        {
            Id = "test-meme",
            Name = "Test Meme", 
            Url = "https://example.com/test.gif",
            Description = "A test meme"
        };

        // Act
        var result = await _memeService.SendMemeToChannelAsync("test-channel", meme);

        // Assert
        Assert.That(result, Is.True);
    }
}

[TestFixture]
public class ReynoldsWorkStatusServiceTests
{
    private Mock<ILogger<ReynoldsWorkStatusService>> _mockLogger;
    private ReynoldsWorkStatusService _statusService;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<ReynoldsWorkStatusService>>();
        _statusService = new ReynoldsWorkStatusService(_mockLogger.Object);
    }

    [Test]
    public async Task GetCurrentStatusAsync_Should_Return_Valid_Status()
    {
        // Act
        var result = await _statusService.GetCurrentStatusAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Not.Empty);
        Assert.That(result.CurrentTask, Is.Not.Empty);
    }

    [Test]
    public async Task UpdateCurrentTaskAsync_Should_Update_Status_Successfully()
    {
        // Arrange
        var taskName = "Test Task";
        var description = "Testing Reynolds work tracking";
        var repository = "test-repo";

        // Act
        var result = await _statusService.UpdateCurrentTaskAsync(taskName, description, repository);
        var currentStatus = await _statusService.GetCurrentStatusAsync();

        // Assert
        Assert.That(result, Is.True);
        Assert.That(currentStatus.CurrentTask, Is.EqualTo(taskName));
        Assert.That(currentStatus.TaskDescription, Is.EqualTo(description));
        Assert.That(currentStatus.Repository, Is.EqualTo(repository));
        Assert.That(currentStatus.Status, Is.EqualTo("active"));
    }

    [Test]
    public async Task CompleteCurrentTaskAsync_Should_Mark_Task_Complete()
    {
        // Arrange
        await _statusService.UpdateCurrentTaskAsync("Test Task", "Test Description");

        // Act
        var result = await _statusService.CompleteCurrentTaskAsync();
        var currentStatus = await _statusService.GetCurrentStatusAsync();

        // Assert
        Assert.That(result, Is.True);
        // After completion, should create a new idle status
        Assert.That(currentStatus.Status, Is.Not.EqualTo("completed"));
    }

    [Test]
    public async Task GetRecentActivityAsync_Should_Return_Activity_History()
    {
        // Arrange
        await _statusService.UpdateCurrentTaskAsync("Task 1", "Description 1");
        await _statusService.CompleteCurrentTaskAsync();
        await _statusService.UpdateCurrentTaskAsync("Task 2", "Description 2");

        // Act
        var result = await _statusService.GetRecentActivityAsync(5);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.GreaterThan(0));
    }

    [Test]
    public void GetStatusSummary_Should_Return_Formatted_Summary()
    {
        // Act
        var result = _statusService.GetStatusSummary();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Contains.Substring("Reynolds"));
    }
}