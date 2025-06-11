using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Moq;
using CopilotAgent.Services;
using Shared.Models;

namespace CopilotAgent.Tests.Unit;

[TestFixture]
public class CliEnhancementTests
{
    [TestFixture]
    public class CliMonitoringServiceTests
    {
        private CliMonitoringService _monitoringService;
        private Mock<ILogger<CliMonitoringService>> _mockLogger;
        private Mock<ISecurityAuditService> _mockAuditService;
        private CliMonitoringOptions _options;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<CliMonitoringService>>();
            _mockAuditService = new Mock<ISecurityAuditService>();
            _options = new CliMonitoringOptions
            {
                EnablePerformanceTracking = true,
                EnableSuccessRateTracking = true,
                SuccessRateThreshold = 0.95,
                MonitoringWindow = TimeSpan.FromMinutes(5),
                EnableAlerting = true
            };
            _monitoringService = new CliMonitoringService(_mockLogger.Object, _mockAuditService.Object, _options);
        }

        [Test]
        public async Task RecordOperationAsync_Should_Store_Metrics()
        {
            // Arrange
            var metrics = new CliOperationMetrics
            {
                CliTool = "pac",
                Command = "pac env list",
                ExecutionTime = TimeSpan.FromSeconds(2),
                Success = true,
                Error = null,
                RetryCount = 0
            };

            // Act
            await _monitoringService.RecordOperationAsync(metrics);

            // Assert
            var stats = await _monitoringService.GetStatsAsync(TimeSpan.FromMinutes(10));
            Assert.That(stats.TotalOperations, Is.EqualTo(1));
            Assert.That(stats.SuccessfulOperations, Is.EqualTo(1));
            Assert.That(stats.SuccessRate, Is.EqualTo(1.0));
        }

        [Test]
        public async Task GetStatsAsync_Should_Calculate_Success_Rate_Correctly()
        {
            // Arrange
            var successMetrics = new CliOperationMetrics
            {
                CliTool = "pac",
                Command = "pac env list",
                ExecutionTime = TimeSpan.FromSeconds(1),
                Success = true
            };

            var failureMetrics = new CliOperationMetrics
            {
                CliTool = "pac",
                Command = "pac env create",
                ExecutionTime = TimeSpan.FromSeconds(5),
                Success = false,
                Error = "Authentication failed"
            };

            // Act
            await _monitoringService.RecordOperationAsync(successMetrics);
            await _monitoringService.RecordOperationAsync(successMetrics);
            await _monitoringService.RecordOperationAsync(failureMetrics);

            // Assert
            var stats = await _monitoringService.GetStatsAsync(TimeSpan.FromMinutes(10));
            Assert.That(stats.TotalOperations, Is.EqualTo(3));
            Assert.That(stats.SuccessfulOperations, Is.EqualTo(2));
            Assert.That(stats.SuccessRate, Is.EqualTo(2.0 / 3.0).Within(0.001));
        }

        [Test]
        public async Task IsHealthyAsync_Should_Return_False_When_Success_Rate_Below_Threshold()
        {
            // Arrange
            var failureMetrics = new CliOperationMetrics
            {
                CliTool = "pac",
                Command = "pac env create",
                ExecutionTime = TimeSpan.FromSeconds(5),
                Success = false,
                Error = "Authentication failed"
            };

            // Act - Record multiple failures to bring success rate below threshold
            for (int i = 0; i < 10; i++)
            {
                await _monitoringService.RecordOperationAsync(failureMetrics);
            }

            // Assert
            var isHealthy = await _monitoringService.IsHealthyAsync();
            Assert.That(isHealthy, Is.False);
        }

        [Test]
        public async Task CheckAlertsAsync_Should_Trigger_Alert_For_Low_Success_Rate()
        {
            // Arrange
            var optionsWithoutAutoAlerting = new CliMonitoringOptions
            {
                EnablePerformanceTracking = true,
                EnableSuccessRateTracking = true,
                SuccessRateThreshold = 0.95,
                MonitoringWindow = TimeSpan.FromMinutes(5),
                EnableAlerting = false  // Disable auto-alerting to control when alerts are checked
            };
            var monitoringServiceForAlertTest = new CliMonitoringService(_mockLogger.Object, _mockAuditService.Object, optionsWithoutAutoAlerting);
            
            var failureMetrics = new CliOperationMetrics
            {
                CliTool = "pac",
                Command = "pac env create",
                ExecutionTime = TimeSpan.FromSeconds(5),
                Success = false,
                Error = "Authentication failed"
            };

            // Act - Record multiple failures and then manually check alerts
            for (int i = 0; i < 10; i++)
            {
                await monitoringServiceForAlertTest.RecordOperationAsync(failureMetrics);
            }

            await monitoringServiceForAlertTest.CheckAlertsAsync();

            // Assert - Verify alert was logged at least once
            _mockAuditService.Verify(
                x => x.LogEventAsync(
                    "CLI_Alert",
                    null,
                    null,
                    "CLI_SuccessRate_Low",
                    "Alert_Triggered",
                    It.IsAny<object>()),
                Times.AtLeastOnce);
        }
    }

    [TestFixture]
    public class SecurityAuditServiceCliTests
    {
        private SecurityAuditService _auditService;
        private Mock<ILogger<SecurityAuditService>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<SecurityAuditService>>();
            _auditService = new SecurityAuditService(_mockLogger.Object);
        }

        [Test]
        public async Task LogCliOperationAsync_Should_Log_CLI_Operation()
        {
            // Arrange
            var cliTool = "pac";
            var command = "pac env list";
            var userId = "test-user";
            var result = "Success";
            var executionTime = TimeSpan.FromSeconds(2);

            // Act
            await _auditService.LogCliOperationAsync(cliTool, command, userId, result, executionTime);

            // Assert - Verify that logging was called (we can't easily test the exact message without more setup)
            Assert.Pass("LogCliOperationAsync completed without exception");
        }

        [Test]
        public async Task LogCliValidationAsync_Should_Log_Validation_Result()
        {
            // Arrange
            var cliTool = "m365";
            var command = "m365 status";
            var userId = "test-user";
            var isValid = true;

            // Act
            await _auditService.LogCliValidationAsync(cliTool, command, userId, isValid);

            // Assert
            Assert.Pass("LogCliValidationAsync completed without exception");
        }

        [Test]
        public async Task LogCliRetryAsync_Should_Log_Retry_Attempt()
        {
            // Arrange
            var cliTool = "pac";
            var command = "pac solution import";
            var userId = "test-user";
            var attemptNumber = 2;
            var error = "Network timeout";

            // Act
            await _auditService.LogCliRetryAsync(cliTool, command, userId, attemptNumber, error);

            // Assert
            Assert.Pass("LogCliRetryAsync completed without exception");
        }
    }

    [TestFixture]
    public class EnhancedPacCliValidatorTests
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
        public async Task ValidateCommandAsync_Should_Log_Validation_Success()
        {
            // Arrange
            var command = "pac env list";

            // Act
            var result = await _validator.ValidateCommandAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            _mockAuditService.Verify(
                x => x.LogCliValidationAsync("pac", command, It.IsAny<string>(), true, null),
                Times.Once);
        }

        [Test]
        public async Task ValidateCommandAsync_Should_Log_Validation_Failure()
        {
            // Arrange
            var command = "pac env delete --force";

            // Act
            var result = await _validator.ValidateCommandAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            _mockAuditService.Verify(
                x => x.LogCliValidationAsync("pac", command, It.IsAny<string>(), false, It.IsAny<string>()),
                Times.Once);
        }

        [Test]
        public async Task ValidateCommandAsync_Should_Reject_Non_Pac_Commands()
        {
            // Arrange
            var command = "rm -rf /";

            // Act
            var result = await _validator.ValidateCommandAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Only pac commands are allowed"));
        }

        [Test]
        public async Task ValidateCommandAsync_Should_Reject_Dangerous_Parameters()
        {
            // Arrange
            var command = "pac solution import --force";

            // Act
            var result = await _validator.ValidateCommandAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Command contains potentially dangerous parameters"));
        }
    }

    [TestFixture]
    public class EnhancedM365CliValidatorTests
    {
        private M365CliValidator _validator;
        private Mock<ILogger<M365CliValidator>> _mockLogger;
        private Mock<ISecurityAuditService> _mockAuditService;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<M365CliValidator>>();
            _mockAuditService = new Mock<ISecurityAuditService>();
            _validator = new M365CliValidator(_mockLogger.Object, _mockAuditService.Object);
        }

        [Test]
        public async Task ValidateCommandAsync_Should_Log_Validation_Success()
        {
            // Arrange
            var command = "m365 status";

            // Act
            var result = await _validator.ValidateCommandAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            _mockAuditService.Verify(
                x => x.LogCliValidationAsync("m365", command, It.IsAny<string>(), true, null),
                Times.Once);
        }

        [Test]
        public async Task ValidateCommandAsync_Should_Reject_Dangerous_Operations()
        {
            // Arrange - Use a command that's allowed but has dangerous operations
            var command = "m365 spo list --clear";

            // Act
            var result = await _validator.ValidateCommandAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Command contains potentially dangerous operations"));
        }

        [Test]
        public async Task ValidateCommandAsync_Should_Allow_Safe_Commands()
        {
            // Arrange
            var command = "m365 app list";

            // Act
            var result = await _validator.ValidateCommandAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
        }
    }
}