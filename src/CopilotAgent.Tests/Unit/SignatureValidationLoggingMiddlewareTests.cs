using NUnit.Framework;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Moq;
using CopilotAgent.Middleware;
using CopilotAgent.Services;
using System.Text;

namespace CopilotAgent.Tests.Unit;

[TestFixture]
public class SignatureValidationLoggingMiddlewareTests
{
    private Mock<ILogger<SignatureValidationLoggingMiddleware>> _mockLogger;
    private Mock<IConfiguration> _mockConfiguration;
    private Mock<ISecurityAuditService> _mockAuditService;
    private ServiceCollection _serviceCollection;
    private ServiceProvider _serviceProvider;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<SignatureValidationLoggingMiddleware>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockAuditService = new Mock<ISecurityAuditService>();

        // Create a real service collection for simpler testing
        _serviceCollection = new ServiceCollection();
        _serviceCollection.AddSingleton(_mockConfiguration.Object);
        _serviceCollection.AddSingleton(_mockAuditService.Object);
        _serviceProvider = _serviceCollection.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public async Task InvokeAsync_WithNonWebhookPath_ShouldSkipProcessing()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/other/endpoint";

        var nextCalled = false;
        var middleware = new SignatureValidationLoggingMiddleware(
            async (ctx) => { await Task.CompletedTask; nextCalled = true; },
            _mockLogger.Object,
            _serviceProvider);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.That(nextCalled, Is.True);
        
        // Verify no webhook-specific logging occurred
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("WEBHOOK")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Test]
    public async Task InvokeAsync_WithWebhookPathAndSignatureFailure_ShouldLogFailure()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/github/webhook";
        context.Request.Headers["X-GitHub-Delivery"] = "test-delivery-123";
        context.Request.Headers["X-GitHub-Event"] = "push";
        context.Request.Headers["X-Hub-Signature-256"] = "sha256=invalid-signature";
        context.Request.Headers["User-Agent"] = "GitHub-Hookshot/test";
        context.Request.ContentLength = 1024;
        
        // Setup mock configuration to indicate secret is configured
        _mockConfiguration.Setup(c => c["NGL_DEVOPS_WEBHOOK_SECRET"]).Returns("test-secret");

        var middleware = new SignatureValidationLoggingMiddleware(
            async (ctx) => { 
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsync("Unauthorized: signature validation failed");
            },
            _mockLogger.Object,
            _serviceProvider);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.That(context.Response.StatusCode, Is.EqualTo(401));
        
        // Verify signature validation failure was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("WEBHOOK_SIGNATURE_VALIDATION_FAILED")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify audit service was called
        _mockAuditService.Verify(
            x => x.LogEventAsync(
                "GitHub_Webhook_Signature_Validation_Failed",
                It.IsAny<string>(),
                It.IsAny<string>(),
                "SIGNATURE_VALIDATION_FAILED",
                "FAILED",
                It.IsAny<object>()),
            Times.Once);
    }

    [Test]
    public async Task SignatureValidationMiddleware_ShouldBeConstructable()
    {
        await Task.CompletedTask;
        
        // Arrange & Act
        var middleware = new SignatureValidationLoggingMiddleware(
            async (ctx) => { await Task.CompletedTask; },
            _mockLogger.Object,
            _serviceProvider);

        // Assert
        Assert.That(middleware, Is.Not.Null);
    }
}