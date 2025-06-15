using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using CopilotAgent.Middleware;
using System.IO;
using System.Threading.Tasks;

namespace CopilotAgent.Tests.Unit;

/// <summary>
/// Tests to validate webhook secret configuration is working correctly
/// </summary>
[TestFixture]
public class WebhookSecretConfigurationTests
{
    private IServiceProvider _serviceProvider;
    private DefaultHttpContext _httpContext;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        // Create configuration with webhook secret
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "NGL_DEVOPS_WEBHOOK_SECRET", "96328478de1391f4633f28221ef6d62d8fa42b57cea159ff65360e88015507dd" }
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        _serviceProvider = services.BuildServiceProvider();

        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = "/api/github/webhook";
        _httpContext.Request.Headers["X-GitHub-Event"] = "push";
        _httpContext.Request.Headers["X-GitHub-Delivery"] = "test-delivery-id";
        _httpContext.Request.Headers["X-Hub-Signature-256"] = "sha256=test-signature";
        _httpContext.Request.Headers["User-Agent"] = "GitHub-Hookshot/test";
        _httpContext.Request.Headers["Content-Type"] = "application/json";
        _httpContext.Response.Body = new MemoryStream();
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Test]
    public void Configuration_ShouldHaveWebhookSecret()
    {
        // Arrange
        var configuration = _serviceProvider.GetRequiredService<IConfiguration>();

        // Act
        var webhookSecret = configuration["NGL_DEVOPS_WEBHOOK_SECRET"];

        // Assert
        Assert.That(webhookSecret, Is.Not.Null.And.Not.Empty);
        Assert.That(webhookSecret, Is.EqualTo("96328478de1391f4633f28221ef6d62d8fa42b57cea159ff65360e88015507dd"));
    }

    [Test]
    public async Task WebhookLoggingMiddleware_ShouldNotLogSecretNotConfiguredError_WhenSecretIsPresent()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<WebhookLoggingMiddleware>>();
        var middleware = new WebhookLoggingMiddleware(
            context => Task.CompletedTask, 
            logger, 
            _serviceProvider);

        // Capture log output
        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        var memoryLogger = new TestLogger<WebhookLoggingMiddleware>();
        
        var testServices = new ServiceCollection();
        testServices.AddSingleton<ILogger<WebhookLoggingMiddleware>>(memoryLogger);
        testServices.AddSingleton<IConfiguration>(_serviceProvider.GetRequiredService<IConfiguration>());
        var testServiceProvider = testServices.BuildServiceProvider();

        var testMiddleware = new WebhookLoggingMiddleware(
            context => Task.CompletedTask,
            memoryLogger,
            testServiceProvider);

        // Act
        await testMiddleware.InvokeAsync(_httpContext);

        // Assert
        var errorMessages = memoryLogger.LoggedMessages.Where(m => m.Contains("WEBHOOK_SECRET_NOT_CONFIGURED"));
        Assert.That(errorMessages, Is.Empty, "Should not log webhook secret not configured error when secret is present");

        var infoMessages = memoryLogger.LoggedMessages.Where(m => m.Contains("WEBHOOK_REQUEST_RECEIVED"));
        Assert.That(infoMessages, Is.Not.Empty, "Should log webhook request received message");
    }

    [Test]
    public void Environment_ShouldResolveWebhookSecretFromConfiguration()
    {
        // Arrange
        var configuration = _serviceProvider.GetRequiredService<IConfiguration>();

        // Act - Simulate the same logic used in Program.cs
        var webhookSecret = configuration["NGL_DEVOPS_WEBHOOK_SECRET"] ??
                           System.Environment.GetEnvironmentVariable("NGL_DEVOPS_WEBHOOK_SECRET");

        // Assert
        Assert.That(webhookSecret, Is.Not.Null.And.Not.Empty);
        Assert.That(webhookSecret, Is.EqualTo("96328478de1391f4633f28221ef6d62d8fa42b57cea159ff65360e88015507dd"));
    }
}

/// <summary>
/// Test logger implementation to capture log messages for testing
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    public List<string> LoggedMessages { get; } = new List<string>();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullDisposable.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        LoggedMessages.Add(formatter(state, exception));
    }

    private class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new NullDisposable();
        public void Dispose() { }
    }
}