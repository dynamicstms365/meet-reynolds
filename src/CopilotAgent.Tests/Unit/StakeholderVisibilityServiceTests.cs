using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using CopilotAgent.Services;
using Shared.Models;

namespace CopilotAgent.Tests.Unit;

[TestFixture]
public class StakeholderVisibilityServiceTests
{
    private Mock<ILogger<StakeholderVisibilityService>> _mockLogger = null!;
    private Mock<ISecurityAuditService> _mockAuditService = null!;
    private StakeholderVisibilityService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<StakeholderVisibilityService>>();
        _mockAuditService = new Mock<ISecurityAuditService>();
        _service = new StakeholderVisibilityService(_mockLogger.Object, _mockAuditService.Object);
    }

    [Test]
    public async Task CreateStakeholderAsync_ShouldCreateStakeholder_WhenValidInput()
    {
        // Arrange
        var stakeholder = new StakeholderConfiguration
        {
            Name = "Test Stakeholder",
            Email = "test@example.com",
            Repositories = new[] { "owner/repo" }
        };

        // Act
        var result = await _service.CreateStakeholderAsync(stakeholder);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Not.Empty);
        Assert.That(result.Name, Is.EqualTo("Test Stakeholder"));
        Assert.That(result.Email, Is.EqualTo("test@example.com"));
        Assert.That(result.IsActive, Is.True);
        Assert.That(result.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
        Assert.That(result.UpdatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));

        // Verify audit log was called
        _mockAuditService.Verify(x => x.LogEventAsync(
            "Stakeholder_Created",
            null, null, "CreateStakeholder", "SUCCESS",
            It.IsAny<object>()), Times.Once);
    }

    [Test]
    public async Task GetStakeholderAsync_ShouldReturnStakeholder_WhenExists()
    {
        // Arrange
        var stakeholder = new StakeholderConfiguration
        {
            Name = "Test Stakeholder",
            Email = "test@example.com"
        };
        var created = await _service.CreateStakeholderAsync(stakeholder);

        // Act
        var result = await _service.GetStakeholderAsync(created.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(created.Id));
        Assert.That(result.Name, Is.EqualTo("Test Stakeholder"));
    }

    [Test]
    public async Task GetStakeholderAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _service.GetStakeholderAsync("non-existent-id");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateStakeholderAsync_ShouldUpdateStakeholder_WhenExists()
    {
        // Arrange
        var stakeholder = new StakeholderConfiguration
        {
            Name = "Test Stakeholder",
            Email = "test@example.com"
        };
        var created = await _service.CreateStakeholderAsync(stakeholder);

        var updatedStakeholder = new StakeholderConfiguration
        {
            Name = "Updated Stakeholder",
            Email = "updated@example.com"
        };

        // Act
        var result = await _service.UpdateStakeholderAsync(created.Id, updatedStakeholder);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(created.Id));
        Assert.That(result.Name, Is.EqualTo("Updated Stakeholder"));
        Assert.That(result.Email, Is.EqualTo("updated@example.com"));
        Assert.That(result.CreatedAt, Is.EqualTo(created.CreatedAt));
        Assert.That(result.UpdatedAt, Is.GreaterThan(created.UpdatedAt));

        _mockAuditService.Verify(x => x.LogEventAsync(
            "Stakeholder_Updated",
            null, null, "UpdateStakeholder", "SUCCESS",
            It.IsAny<object>()), Times.Once);
    }

    [Test]
    public void UpdateStakeholderAsync_ShouldThrowException_WhenNotExists()
    {
        // Arrange
        var stakeholder = new StakeholderConfiguration
        {
            Name = "Test Stakeholder"
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateStakeholderAsync("non-existent-id", stakeholder));
        
        Assert.That(ex?.Message, Does.Contain("not found"));
    }

    [Test]
    public async Task DeleteStakeholderAsync_ShouldReturnTrue_WhenExists()
    {
        // Arrange
        var stakeholder = new StakeholderConfiguration
        {
            Name = "Test Stakeholder",
            Email = "test@example.com"
        };
        var created = await _service.CreateStakeholderAsync(stakeholder);

        // Act
        var result = await _service.DeleteStakeholderAsync(created.Id);

        // Assert
        Assert.That(result, Is.True);

        // Verify stakeholder is deleted
        var deletedStakeholder = await _service.GetStakeholderAsync(created.Id);
        Assert.That(deletedStakeholder, Is.Null);

        _mockAuditService.Verify(x => x.LogEventAsync(
            "Stakeholder_Deleted",
            null, null, "DeleteStakeholder", "SUCCESS",
            It.IsAny<object>()), Times.Once);
    }

    [Test]
    public async Task DeleteStakeholderAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Act
        var result = await _service.DeleteStakeholderAsync("non-existent-id");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetAllStakeholdersAsync_ShouldReturnActiveStakeholders()
    {
        // Arrange
        var stakeholder1 = new StakeholderConfiguration
        {
            Name = "Stakeholder 1",
            Email = "test1@example.com",
            IsActive = true
        };
        var stakeholder2 = new StakeholderConfiguration
        {
            Name = "Stakeholder 2",
            Email = "test2@example.com",
            IsActive = true
        };
        var stakeholder3 = new StakeholderConfiguration
        {
            Name = "Stakeholder 3",
            Email = "test3@example.com",
            IsActive = false
        };

        await _service.CreateStakeholderAsync(stakeholder1);
        await _service.CreateStakeholderAsync(stakeholder2);
        await _service.CreateStakeholderAsync(stakeholder3);

        // Act
        var result = await _service.GetAllStakeholdersAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2)); // Only active stakeholders
        Assert.That(result.All(s => s.IsActive), Is.True);
    }

    [Test]
    public async Task GetStakeholdersByRepositoryAsync_ShouldReturnFilteredStakeholders()
    {
        // Arrange
        var stakeholder1 = new StakeholderConfiguration
        {
            Name = "Stakeholder 1",
            Email = "test1@example.com",
            Repositories = new[] { "owner/repo1", "owner/repo2" },
            IsActive = true
        };
        var stakeholder2 = new StakeholderConfiguration
        {
            Name = "Stakeholder 2",
            Email = "test2@example.com",
            Repositories = new[] { "owner/repo2", "owner/repo3" },
            IsActive = true
        };
        var stakeholder3 = new StakeholderConfiguration
        {
            Name = "Stakeholder 3",
            Email = "test3@example.com",
            Repositories = new[] { "owner/repo3" },
            IsActive = true
        };

        await _service.CreateStakeholderAsync(stakeholder1);
        await _service.CreateStakeholderAsync(stakeholder2);
        await _service.CreateStakeholderAsync(stakeholder3);

        // Act
        var result = await _service.GetStakeholdersByRepositoryAsync("owner/repo2");

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2)); // stakeholder1 and stakeholder2
        Assert.That(result.All(s => s.Repositories.Contains("owner/repo2")), Is.True);
    }
}