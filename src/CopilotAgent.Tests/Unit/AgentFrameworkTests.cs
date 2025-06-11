using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Moq;
using CopilotAgent.Services;
using Shared.Models;
using System.Text.Json;

namespace CopilotAgent.Tests.Unit;

[TestFixture]
public class IntentRecognitionServiceTests
{
    private Mock<ILogger<IntentRecognitionService>> _mockLogger;
    private IntentRecognitionService _intentService;
    private AgentConfiguration _testConfig;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<IntentRecognitionService>>();
        _intentService = new IntentRecognitionService(_mockLogger.Object);
        _testConfig = new AgentConfiguration();
    }

    [Test]
    public async Task AnalyzeIntentAsync_Should_Recognize_Environment_Intent()
    {
        // Arrange
        var message = "create a new environment for testing";

        // Act
        var result = await _intentService.AnalyzeIntentAsync(message, _testConfig);

        // Assert
        Assert.That(result.Type, Is.EqualTo(IntentType.EnvironmentManagement));
        Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.5));
        Assert.That(result.ProcessingTimeMs, Is.GreaterThan(0));
    }

    [Test]
    public async Task AnalyzeIntentAsync_Should_Recognize_CLI_Intent()
    {
        // Arrange
        var message = "run pac env list command";

        // Act
        var result = await _intentService.AnalyzeIntentAsync(message, _testConfig);

        // Assert
        Assert.That(result.Type, Is.EqualTo(IntentType.CliExecution));
        Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.5));
    }

    [Test]
    public async Task AnalyzeIntentAsync_Should_Recognize_Code_Generation_Intent()
    {
        // Arrange
        var message = "generate a blazor component for user management";

        // Act
        var result = await _intentService.AnalyzeIntentAsync(message, _testConfig);

        // Assert
        Assert.That(result.Type, Is.EqualTo(IntentType.CodeGeneration));
        Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.5));
    }

    [Test]
    public async Task AnalyzeIntentAsync_Should_Recognize_Knowledge_Intent()
    {
        // Arrange
        var message = "how to create a Power Platform environment?";

        // Act
        var result = await _intentService.AnalyzeIntentAsync(message, _testConfig);

        // Assert
        Assert.That(result.Type, Is.EqualTo(IntentType.KnowledgeQuery));
        Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.5));
    }

    [Test]
    public async Task AnalyzeIntentAsync_Should_Return_General_For_Low_Confidence()
    {
        // Arrange
        var message = "hello world";
        _testConfig.IntentRecognition.ConfidenceThreshold = 0.9; // High threshold

        // Act
        var result = await _intentService.AnalyzeIntentAsync(message, _testConfig);

        // Assert
        Assert.That(result.Type, Is.EqualTo(IntentType.General));
        Assert.That(result.Confidence, Is.LessThan(0.9));
    }
}

[TestFixture]
public class RetryServiceTests
{
    private Mock<ILogger<RetryService>> _mockLogger;
    private Mock<IConfigurationService> _mockConfigService;
    private RetryService _retryService;
    private AgentConfiguration _testConfig;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<RetryService>>();
        _mockConfigService = new Mock<IConfigurationService>();
        _testConfig = new AgentConfiguration
        {
            RetryPolicy = new RetryPolicyConfig
            {
                MaxRetries = 2,
                BaseDelayMs = 100,
                BackoffMultiplier = 2.0,
                RetriableExceptions = new[] { "TimeoutException", "HttpRequestException" }
            }
        };
        
        _mockConfigService.Setup(c => c.GetConfiguration()).Returns(_testConfig);
        _retryService = new RetryService(_mockLogger.Object, _mockConfigService.Object);
    }

    [Test]
    public async Task ExecuteWithRetryAsync_Should_Succeed_On_First_Attempt()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task<string>>(async () =>
        {
            callCount++;
            await Task.CompletedTask;
            return "success";
        });

        // Act
        var result = await _retryService.ExecuteWithRetryAsync(operation);

        // Assert
        Assert.That(result, Is.EqualTo("success"));
        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteWithRetryAsync_Should_Retry_On_Retriable_Exception()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task<string>>(async () =>
        {
            callCount++;
            await Task.CompletedTask;
            
            if (callCount <= 2)
                throw new TimeoutException("Timeout");
                
            return "success";
        });

        // Act
        var result = await _retryService.ExecuteWithRetryAsync(operation);

        // Assert
        Assert.That(result, Is.EqualTo("success"));
        Assert.That(callCount, Is.EqualTo(3));
    }

    [Test]
    public void ExecuteWithRetryAsync_Should_Throw_RetryExhaustedException_After_Max_Retries()
    {
        // Arrange
        var operation = new Func<Task<string>>(async () =>
        {
            await Task.CompletedTask;
            throw new TimeoutException("Persistent timeout");
        });

        // Act & Assert
        Assert.ThrowsAsync<RetryExhaustedException>(async () =>
            await _retryService.ExecuteWithRetryAsync(operation));
    }

    [Test]
    public void ExecuteWithRetryAsync_Should_Not_Retry_Non_Retriable_Exception()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task<string>>(async () =>
        {
            callCount++;
            await Task.CompletedTask;
            throw new ArgumentException("Invalid argument");
        });

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _retryService.ExecuteWithRetryAsync(operation));
        
        Assert.That(callCount, Is.EqualTo(1));
    }
}

[TestFixture]
public class TelemetryServiceTests
{
    private Mock<ILogger<TelemetryService>> _mockLogger;
    private Mock<IConfigurationService> _mockConfigService;
    private TelemetryService _telemetryService;
    private AgentConfiguration _testConfig;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<TelemetryService>>();
        _mockConfigService = new Mock<IConfigurationService>();
        _testConfig = new AgentConfiguration
        {
            Telemetry = new TelemetryConfig
            {
                EnableDetailedLogging = true,
                EnablePerformanceMetrics = true
            }
        };
        
        _mockConfigService.Setup(c => c.GetConfiguration()).Returns(_testConfig);
        _telemetryService = new TelemetryService(_mockLogger.Object, _mockConfigService.Object);
    }

    [Test]
    public void RecordRequestProcessed_Should_Update_Metrics()
    {
        // Arrange
        var requestId = "test-request";
        var intentType = IntentType.EnvironmentManagement;
        var success = true;
        var processingTime = 1500.0;

        // Act
        _telemetryService.RecordRequestProcessed(requestId, intentType, success, processingTime);

        // Assert
        var metrics = _telemetryService.GetMetrics();
        Assert.That(metrics.TotalRequests, Is.EqualTo(1));
        Assert.That(metrics.SuccessfulRequests, Is.EqualTo(1));
        Assert.That(metrics.FailedRequests, Is.EqualTo(0));
        Assert.That(metrics.AverageProcessingTimeMs, Is.EqualTo(processingTime));
        Assert.That(metrics.IntentCounts[intentType], Is.EqualTo(1));
    }

    [Test]
    public void RecordRequestProcessed_Should_Track_Failed_Requests()
    {
        // Arrange & Act
        _telemetryService.RecordRequestProcessed("req1", IntentType.General, false, 500.0);
        _telemetryService.RecordRequestProcessed("req2", IntentType.General, true, 300.0);

        // Assert
        var metrics = _telemetryService.GetMetrics();
        Assert.That(metrics.TotalRequests, Is.EqualTo(2));
        Assert.That(metrics.SuccessfulRequests, Is.EqualTo(1));
        Assert.That(metrics.FailedRequests, Is.EqualTo(1));
        Assert.That(metrics.AccuracyRate, Is.EqualTo(0.5));
    }

    [Test]
    public void RecordError_Should_Track_Recent_Errors()
    {
        // Arrange
        var operation = "TestOperation";
        var exception = new Exception("Test error");

        // Act
        _telemetryService.RecordError(operation, exception);

        // Assert
        var metrics = _telemetryService.GetMetrics();
        Assert.That(metrics.RecentErrors.Count, Is.EqualTo(1));
        Assert.That(metrics.RecentErrors[0], Does.Contain("TestOperation"));
        Assert.That(metrics.RecentErrors[0], Does.Contain("Test error"));
    }

    [Test]
    public void ResetMetrics_Should_Clear_All_Metrics()
    {
        // Arrange
        _telemetryService.RecordRequestProcessed("req1", IntentType.General, true, 500.0);
        _telemetryService.RecordError("op1", new Exception("error"));

        // Act
        _telemetryService.ResetMetrics();

        // Assert
        var metrics = _telemetryService.GetMetrics();
        Assert.That(metrics.TotalRequests, Is.EqualTo(0));
        Assert.That(metrics.SuccessfulRequests, Is.EqualTo(0));
        Assert.That(metrics.FailedRequests, Is.EqualTo(0));
        Assert.That(metrics.RecentErrors.Count, Is.EqualTo(0));
        Assert.That(metrics.IntentCounts.Count, Is.EqualTo(0));
    }

    [Test]
    public void GetMetrics_Should_Calculate_Agent_Status_Based_On_Performance()
    {
        // Arrange - Record high accuracy and good performance
        for (int i = 0; i < 10; i++)
        {
            _telemetryService.RecordRequestProcessed($"req{i}", IntentType.General, true, 1000.0);
        }

        // Act
        var metrics = _telemetryService.GetMetrics();

        // Assert
        Assert.That(metrics.Status, Is.EqualTo(AgentStatus.Healthy));
        Assert.That(metrics.AccuracyRate, Is.EqualTo(1.0)); // 100% accuracy
    }
}

[TestFixture]
public class ConfigurationServiceTests
{
    private Mock<ILogger<ConfigurationService>> _mockLogger;
    private Mock<IHostEnvironment> _mockEnvironment;
    private ConfigurationService? _configService;
    private string _tempConfigFile;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<ConfigurationService>>();
        _mockEnvironment = new Mock<IHostEnvironment>();
        
        _tempConfigFile = Path.GetTempFileName();
        var tempDir = Path.GetDirectoryName(_tempConfigFile)!;
        _mockEnvironment.Setup(e => e.ContentRootPath).Returns(tempDir);
        
        // Create a test configuration file
        var defaultConfig = new AgentConfiguration();
        var json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(tempDir, "agent-config.json"), json);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            var configPath = Path.Combine(Path.GetDirectoryName(_tempConfigFile)!, "agent-config.json");
            if (File.Exists(configPath))
                File.Delete(configPath);
                
            if (File.Exists(_tempConfigFile))
                File.Delete(_tempConfigFile);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Test]
    public async Task ValidateConfigurationAsync_Should_Accept_Valid_Configuration()
    {
        // Arrange
        _configService = new ConfigurationService(_mockLogger.Object, _mockEnvironment.Object);
        var validConfig = new AgentConfiguration
        {
            IntentRecognition = new IntentRecognitionConfig { ConfidenceThreshold = 0.8 },
            RetryPolicy = new RetryPolicyConfig { MaxRetries = 3, BaseDelayMs = 1000 },
            Processing = new ProcessingConfig { RequestTimeoutMs = 30000 }
        };

        // Act
        var isValid = await _configService.ValidateConfigurationAsync(validConfig);

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public async Task ValidateConfigurationAsync_Should_Reject_Invalid_Configuration()
    {
        // Arrange
        _configService = new ConfigurationService(_mockLogger.Object, _mockEnvironment.Object);
        var invalidConfig = new AgentConfiguration
        {
            IntentRecognition = new IntentRecognitionConfig { ConfidenceThreshold = 2.0 }, // Invalid: > 1.0
            RetryPolicy = new RetryPolicyConfig { MaxRetries = -1 }, // Invalid: negative
            Processing = new ProcessingConfig { RequestTimeoutMs = 500 } // Invalid: too low
        };

        // Act
        var isValid = await _configService.ValidateConfigurationAsync(invalidConfig);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void GetConfiguration_Should_Return_Current_Configuration()
    {
        // Arrange
        _configService = new ConfigurationService(_mockLogger.Object, _mockEnvironment.Object);

        // Act
        var config = _configService.GetConfiguration();

        // Assert
        Assert.That(config, Is.Not.Null);
        Assert.That(config.IntentRecognition, Is.Not.Null);
        Assert.That(config.RetryPolicy, Is.Not.Null);
        Assert.That(config.Telemetry, Is.Not.Null);
    }
}