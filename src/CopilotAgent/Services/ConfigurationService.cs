using Shared.Models;
using System.Text.Json;

namespace CopilotAgent.Services;

public interface IConfigurationService
{
    AgentConfiguration GetConfiguration();
    Task UpdateConfigurationAsync(AgentConfiguration configuration);
    Task<bool> ValidateConfigurationAsync(AgentConfiguration configuration);
    event EventHandler<AgentConfiguration>? ConfigurationChanged;
}

public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _configurationFile;
    private AgentConfiguration _currentConfiguration;
    private readonly object _configLock = new();
    private readonly FileSystemWatcher _fileWatcher;

    public event EventHandler<AgentConfiguration>? ConfigurationChanged;

    public ConfigurationService(ILogger<ConfigurationService> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _configurationFile = Path.Combine(environment.ContentRootPath, "agent-config.json");
        
        // Load initial configuration
        _currentConfiguration = LoadConfiguration();
        
        // Setup file watcher for runtime configuration updates
        _fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(_configurationFile)!, Path.GetFileName(_configurationFile))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        
        _fileWatcher.Changed += OnConfigurationFileChanged;
        
        _logger.LogInformation("Configuration service initialized with file: {ConfigFile}", _configurationFile);
    }

    public AgentConfiguration GetConfiguration()
    {
        lock (_configLock)
        {
            return _currentConfiguration;
        }
    }

    public async Task UpdateConfigurationAsync(AgentConfiguration configuration)
    {
        try
        {
            // Validate configuration before applying
            var isValid = await ValidateConfigurationAsync(configuration);
            if (!isValid)
            {
                throw new InvalidOperationException("Configuration validation failed");
            }

            // Temporarily disable file watcher to avoid triggering our own change
            _fileWatcher.EnableRaisingEvents = false;

            try
            {
                // Save to file
                var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(_configurationFile, json);

                // Update in-memory configuration
                lock (_configLock)
                {
                    _currentConfiguration = configuration;
                }

                _logger.LogInformation("Configuration updated successfully");
                
                // Notify subscribers
                ConfigurationChanged?.Invoke(this, configuration);
            }
            finally
            {
                // Re-enable file watcher
                _fileWatcher.EnableRaisingEvents = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configuration");
            throw;
        }
    }

    public async Task<bool> ValidateConfigurationAsync(AgentConfiguration configuration)
    {
        await Task.CompletedTask; // Placeholder for async validation

        try
        {
            // Basic validation rules
            if (configuration.IntentRecognition.ConfidenceThreshold < 0 || 
                configuration.IntentRecognition.ConfidenceThreshold > 1)
            {
                _logger.LogWarning("Invalid confidence threshold: {Threshold}", 
                    configuration.IntentRecognition.ConfidenceThreshold);
                return false;
            }

            if (configuration.RetryPolicy.MaxRetries < 0 || configuration.RetryPolicy.MaxRetries > 10)
            {
                _logger.LogWarning("Invalid max retries: {MaxRetries}", configuration.RetryPolicy.MaxRetries);
                return false;
            }

            if (configuration.RetryPolicy.BaseDelayMs < 0 || configuration.RetryPolicy.BaseDelayMs > 60000)
            {
                _logger.LogWarning("Invalid base delay: {BaseDelay}", configuration.RetryPolicy.BaseDelayMs);
                return false;
            }

            if (configuration.Processing.RequestTimeoutMs < 1000 || configuration.Processing.RequestTimeoutMs > 300000)
            {
                _logger.LogWarning("Invalid request timeout: {Timeout}", configuration.Processing.RequestTimeoutMs);
                return false;
            }

            _logger.LogDebug("Configuration validation passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration");
            return false;
        }
    }

    private AgentConfiguration LoadConfiguration()
    {
        try
        {
            if (File.Exists(_configurationFile))
            {
                var json = File.ReadAllText(_configurationFile);
                var config = JsonSerializer.Deserialize<AgentConfiguration>(json);
                if (config != null)
                {
                    _logger.LogInformation("Configuration loaded from file");
                    return config;
                }
            }

            _logger.LogInformation("Using default configuration");
            var defaultConfig = new AgentConfiguration();
            
            // Save default configuration for future reference
            _ = Task.Run(async () => 
            {
                try
                {
                    await UpdateConfigurationAsync(defaultConfig);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save default configuration");
                }
            });

            return defaultConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration, using defaults");
            return new AgentConfiguration();
        }
    }

    private async void OnConfigurationFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Add a small delay to ensure file write is complete
            await Task.Delay(100);
            
            var newConfiguration = LoadConfiguration();
            var isValid = await ValidateConfigurationAsync(newConfiguration);
            
            if (isValid)
            {
                lock (_configLock)
                {
                    _currentConfiguration = newConfiguration;
                }
                
                _logger.LogInformation("Configuration reloaded from file");
                ConfigurationChanged?.Invoke(this, newConfiguration);
            }
            else
            {
                _logger.LogWarning("Invalid configuration detected in file, keeping current configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading configuration from file");
        }
    }

    public void Dispose()
    {
        _fileWatcher?.Dispose();
    }
}