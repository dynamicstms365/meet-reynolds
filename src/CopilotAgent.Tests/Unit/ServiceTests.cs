using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Moq;
using CopilotAgent.Services;
using Shared.Models;

namespace CopilotAgent.Tests.Unit;

[TestFixture]
public class EnvironmentManagerTests
{
    private Mock<IPacCliService> _mockPacCliService;
    private Mock<IPacCliValidator> _mockValidator;
    private Mock<ILogger<EnvironmentManager>> _mockLogger;
    private EnvironmentManager _environmentManager;

    [SetUp]
    public void SetUp()
    {
        _mockPacCliService = new Mock<IPacCliService>();
        _mockValidator = new Mock<IPacCliValidator>();
        _mockLogger = new Mock<ILogger<EnvironmentManager>>();
        
        _environmentManager = new EnvironmentManager(
            _mockPacCliService.Object,
            _mockValidator.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task CreateEnvironmentAsync_Should_Return_Success_When_Command_Succeeds()
    {
        // Arrange
        var spec = new EnvironmentSpec
        {
            Name = "test-environment",
            Type = "Sandbox",
            Region = "UnitedStates",
            Description = "Test environment"
        };

        _mockValidator
            .Setup(v => v.ValidateCommandAsync(It.IsAny<string>()))
            .ReturnsAsync(ValidationResult.CreateSuccess());

        _mockPacCliService
            .Setup(s => s.ExecuteAsync(It.IsAny<string>()))
            .ReturnsAsync(CliResult.CreateSuccess("Environment created successfully\nEnvironment ID: 12345"));

        // Act
        var result = await _environmentManager.CreateEnvironmentAsync(spec);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.EnvironmentId, Is.EqualTo("12345"));
        Assert.That(result.Properties["name"], Is.EqualTo("test-environment"));
    }

    [Test]
    public async Task CreateEnvironmentAsync_Should_Return_Failure_When_Validation_Fails()
    {
        // Arrange
        var spec = new EnvironmentSpec
        {
            Name = "test-environment",
            Type = "Sandbox",
            Region = "UnitedStates"
        };

        _mockValidator
            .Setup(v => v.ValidateCommandAsync(It.IsAny<string>()))
            .ReturnsAsync(ValidationResult.CreateFailure("Command not allowed"));

        // Act
        var result = await _environmentManager.CreateEnvironmentAsync(spec);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Command not allowed"));
    }

    [Test]
    public async Task ListEnvironmentsAsync_Should_Return_Environments_When_Command_Succeeds()
    {
        // Arrange
        var jsonOutput = @"[
            {
                ""environmentId"": ""env1"",
                ""displayName"": ""Development"",
                ""environmentType"": ""Sandbox"",
                ""region"": ""UnitedStates"",
                ""lifecycleStatus"": ""Ready""
            }
        ]";

        _mockValidator
            .Setup(v => v.ValidateCommandAsync(It.IsAny<string>()))
            .ReturnsAsync(ValidationResult.CreateSuccess());

        _mockPacCliService
            .Setup(s => s.ExecuteAsync(It.IsAny<string>()))
            .ReturnsAsync(CliResult.CreateSuccess(jsonOutput));

        // Act
        var result = await _environmentManager.ListEnvironmentsAsync();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Development"));
        Assert.That(result[0].Type, Is.EqualTo("Sandbox"));
    }

    [Test]
    public async Task ValidateEnvironmentAsync_Should_Return_True_When_Environment_Exists()
    {
        // Arrange
        var environmentName = "test-environment";
        
        _mockPacCliService
            .Setup(s => s.ExecuteAsync(It.IsAny<string>()))
            .ReturnsAsync(CliResult.CreateSuccess($"Environment found: {environmentName}"));

        // Act
        var result = await _environmentManager.ValidateEnvironmentAsync(environmentName);

        // Assert
        Assert.That(result, Is.True);
    }
}

[TestFixture]
public class PacCliValidatorTests
{
    private PacCliValidator _validator;
    private Mock<ILogger<PacCliValidator>> _mockLogger;
    private Mock<ISecurityAuditService> _mockAuditService;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<PacCliValidator>>();
        _mockAuditService = new Mock<ISecurityAuditService>();
        _validator = new PacCliValidator(_mockLogger.Object, _mockAuditService.Object);
    }

    [Test]
    public void IsCommandSafe_Should_Return_True_For_Allowed_Commands()
    {
        // Arrange & Act & Assert
        Assert.That(_validator.IsCommandSafe("pac env list"), Is.True);
        Assert.That(_validator.IsCommandSafe("pac auth create --name test"), Is.True);
        Assert.That(_validator.IsCommandSafe("pac solution list"), Is.True);
    }

    [Test]
    public void IsCommandSafe_Should_Return_False_For_Disallowed_Commands()
    {
        // Arrange & Act & Assert
        Assert.That(_validator.IsCommandSafe("pac env delete"), Is.False);
        Assert.That(_validator.IsCommandSafe("rm -rf /"), Is.False);
        Assert.That(_validator.IsCommandSafe("powershell -c 'Get-Process'"), Is.False);
    }

    [Test]
    public async Task ValidateCommandAsync_Should_Return_Success_For_Valid_Commands()
    {
        // Arrange
        var command = "pac env list";

        // Act
        var result = await _validator.ValidateCommandAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task ValidateCommandAsync_Should_Return_Failure_For_Empty_Commands()
    {
        // Arrange
        var command = "";

        // Act
        var result = await _validator.ValidateCommandAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Command cannot be empty"));
    }

    [Test]
    public async Task ValidateCommandAsync_Should_Return_Failure_For_Non_Pac_Commands()
    {
        // Arrange
        var command = "m365 login";

        // Act
        var result = await _validator.ValidateCommandAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Only pac commands are allowed"));
    }
}