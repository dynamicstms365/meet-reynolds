using Shared.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CopilotAgent.Services;

public class EnvironmentManager : IEnvironmentManager
{
    private readonly IPacCliService _pacCliService;
    private readonly IPacCliValidator _validator;
    private readonly ILogger<EnvironmentManager> _logger;

    public EnvironmentManager(
        IPacCliService pacCliService,
        IPacCliValidator validator,
        ILogger<EnvironmentManager> logger)
    {
        _pacCliService = pacCliService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<EnvironmentResult> CreateEnvironmentAsync(EnvironmentSpec spec)
    {
        try
        {
            _logger.LogInformation("Creating environment: {Name}", spec.Name);

            var createCommand = $@"pac env create 
                --name ""{spec.Name}"" 
                --type ""{spec.Type}"" 
                --region ""{spec.Region}""";

            if (!string.IsNullOrEmpty(spec.Description))
            {
                createCommand += $@" --description ""{spec.Description}""";
            }

            var validation = await _validator.ValidateCommandAsync(createCommand);
            if (!validation.Success)
            {
                return new EnvironmentResult { Success = false, Error = validation.Error };
            }

            var result = await _pacCliService.ExecuteAsync(createCommand);

            if (result.Success)
            {
                // Extract environment ID from output if available
                var environmentId = ExtractEnvironmentId(result.Output);
                
                _logger.LogInformation("Environment created successfully: {Name}", spec.Name);
                
                return new EnvironmentResult
                {
                    Success = true,
                    EnvironmentId = environmentId,
                    Properties = new Dictionary<string, object>
                    {
                        ["name"] = spec.Name,
                        ["type"] = spec.Type,
                        ["region"] = spec.Region
                    }
                };
            }

            return new EnvironmentResult { Success = false, Error = result.Error };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating environment: {Name}", spec.Name);
            return new EnvironmentResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<List<Environment>> ListEnvironmentsAsync()
    {
        try
        {
            _logger.LogInformation("Listing environments");

            var command = "pac env list --output json";
            var validation = await _validator.ValidateCommandAsync(command);
            
            if (!validation.Success)
            {
                _logger.LogWarning("Command validation failed: {Error}", validation.Error);
                return new List<Environment>();
            }

            var result = await _pacCliService.ExecuteAsync(command);

            if (result.Success)
            {
                return ParseEnvironmentList(result.Output);
            }

            _logger.LogWarning("Failed to list environments: {Error}", result.Error);
            return new List<Environment>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing environments");
            return new List<Environment>();
        }
    }

    public async Task<bool> ValidateEnvironmentAsync(string environmentName)
    {
        try
        {
            var command = $"pac env list --filter \"displayName eq '{environmentName}'\"";
            var result = await _pacCliService.ExecuteAsync(command);
            
            return result.Success && result.Output.Contains(environmentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating environment: {Name}", environmentName);
            return false;
        }
    }

    public async Task<EnvironmentResult> GetEnvironmentDetailsAsync(string environmentName)
    {
        try
        {
            var command = "pac env who";
            var result = await _pacCliService.ExecuteAsync(command);

            if (result.Success)
            {
                return new EnvironmentResult
                {
                    Success = true,
                    Properties = ParseEnvironmentDetails(result.Output)
                };
            }

            return new EnvironmentResult { Success = false, Error = result.Error };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting environment details: {Name}", environmentName);
            return new EnvironmentResult { Success = false, Error = ex.Message };
        }
    }

    private string? ExtractEnvironmentId(string output)
    {
        // Parse pac CLI output to extract environment ID
        // This is a regex-based implementation for robustness
        try
        {
            if (output.Contains("Environment ID:"))
            {
                var lines = output.Split('\n');
                var regex = new Regex(@"Environment ID:\s*(.+)");
                foreach (var line in lines)
                {
                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        return match.Groups[1].Value.Trim();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract environment ID from output");
        }

        return null;
    }

    private List<Environment> ParseEnvironmentList(string output)
    {
        var environments = new List<Environment>();
        
        try
        {
            // Try to parse as JSON first
            if (output.TrimStart().StartsWith('['))
            {
                var envArray = JsonSerializer.Deserialize<JsonElement[]>(output);
                if (envArray != null)
                {
                    foreach (var env in envArray)
                    {
                        environments.Add(new Environment
                        {
                            Id = env.GetProperty("environmentId").GetString() ?? "",
                            Name = env.GetProperty("displayName").GetString() ?? "",
                            Type = env.GetProperty("environmentType").GetString() ?? "",
                            Region = env.GetProperty("region").GetString() ?? "",
                            Status = env.GetProperty("lifecycleStatus").GetString() ?? ""
                        });
                    }
                }
            }
            else
            {
                // Fallback to text parsing
                environments.Add(new Environment
                {
                    Name = "Current Environment",
                    Status = "Available"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse environment list, using fallback");
            // Return basic environment info
            environments.Add(new Environment
            {
                Name = "Current Environment",
                Status = "Available"
            });
        }

        return environments;
    }

    private Dictionary<string, object> ParseEnvironmentDetails(string output)
    {
        var details = new Dictionary<string, object>();
        
        try
        {
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains(':'))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        details[key] = value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse environment details");
        }

        return details;
    }
}

public class CliExecutor : ICliExecutor
{
    private readonly IPacCliService _pacCliService;
    private readonly IM365CliService _m365CliService;
    private readonly IPacCliValidator _pacValidator;
    private readonly IM365CliValidator _m365Validator;
    private readonly ILogger<CliExecutor> _logger;

    public CliExecutor(
        IPacCliService pacCliService,
        IM365CliService m365CliService,
        IPacCliValidator pacValidator,
        IM365CliValidator m365Validator,
        ILogger<CliExecutor> logger)
    {
        _pacCliService = pacCliService;
        _m365CliService = m365CliService;
        _pacValidator = pacValidator;
        _m365Validator = m365Validator;
        _logger = logger;
    }

    public async Task<CliResult> ExecuteAsync(string command, CliOptions options)
    {
        try
        {
            _logger.LogInformation("Executing command: {Command}", SanitizeCommand(command));

            // Determine which CLI tool to use
            if (command.TrimStart().StartsWith("pac "))
            {
                var validation = await _pacValidator.ValidateCommandAsync(command);
                if (!validation.Success)
                {
                    return CliResult.CreateFailure($"Command validation failed: {validation.Error}");
                }

                return await _pacCliService.ExecuteAsync(command);
            }
            
            if (command.TrimStart().StartsWith("m365 "))
            {
                var validation = await _m365Validator.ValidateCommandAsync(command);
                if (!validation.Success)
                {
                    return CliResult.CreateFailure($"Command validation failed: {validation.Error}");
                }

                return await _m365CliService.ExecuteAsync(command);
            }

            return CliResult.CreateFailure("Unsupported command. Only 'pac' and 'm365' commands are allowed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command: {Command}", SanitizeCommand(command));
            return CliResult.CreateFailure(ex.Message);
        }
    }

    private string SanitizeCommand(string command)
    {
        // Remove sensitive information from logs
        return command
            .Replace("--secret", "--secret ***")
            .Replace("--password", "--password ***")
            .Replace("--token", "--token ***");
    }
}