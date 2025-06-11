using Shared.Models;

namespace CopilotAgent.Services;

public interface IRetryService
{
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, RetryPolicyConfig? config = null);
    Task ExecuteWithRetryAsync(Func<Task> operation, RetryPolicyConfig? config = null);
}

public class RetryService : IRetryService
{
    private readonly ILogger<RetryService> _logger;
    private readonly IConfigurationService _configurationService;

    public RetryService(ILogger<RetryService> logger, IConfigurationService configurationService)
    {
        _logger = logger;
        _configurationService = configurationService;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, RetryPolicyConfig? config = null)
    {
        config ??= _configurationService.GetConfiguration().RetryPolicy;
        
        var attempt = 0;
        var delay = config.BaseDelayMs;

        while (true)
        {
            try
            {
                var result = await operation();
                
                if (attempt > 0)
                {
                    _logger.LogInformation("Operation succeeded after {Attempts} attempts", attempt + 1);
                }
                
                return result;
            }
            catch (Exception ex) when (attempt < config.MaxRetries && IsRetriableException(ex, config))
            {
                attempt++;
                
                _logger.LogWarning(ex, "Operation failed on attempt {Attempt}/{MaxAttempts}, retrying in {Delay}ms", 
                    attempt, config.MaxRetries + 1, delay);

                await Task.Delay(delay);
                
                // Calculate next delay with exponential backoff
                delay = Math.Min(
                    (int)(delay * config.BackoffMultiplier), 
                    config.MaxDelayMs
                );
            }
            catch (Exception ex)
            {
                if (attempt >= config.MaxRetries)
                {
                    _logger.LogError(ex, "Operation failed after {MaxAttempts} attempts", config.MaxRetries + 1);
                    throw new RetryExhaustedException($"Operation failed after {config.MaxRetries + 1} attempts", ex);
                }
                
                if (!IsRetriableException(ex, config))
                {
                    _logger.LogError(ex, "Operation failed with non-retriable exception: {ExceptionType}", ex.GetType().Name);
                    throw;
                }
                
                throw; // This shouldn't be reached, but keeps compiler happy
            }
        }
    }

    public async Task ExecuteWithRetryAsync(Func<Task> operation, RetryPolicyConfig? config = null)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true; // Return dummy value for generic method
        }, config);
    }

    private static bool IsRetriableException(Exception exception, RetryPolicyConfig config)
    {
        var exceptionTypeName = exception.GetType().Name;
        return config.RetriableExceptions.Contains(exceptionTypeName) ||
               config.RetriableExceptions.Contains(exception.GetType().FullName ?? "");
    }
}

public class RetryExhaustedException : Exception
{
    public RetryExhaustedException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}