using Shared.Models;
using System.Diagnostics;

namespace CopilotAgent.Services;

public class PacCliValidator : IPacCliValidator
{
    private readonly HashSet<string> _allowedCommands = new()
    {
        "pac auth list",
        "pac auth create",
        "pac auth select",
        "pac auth clear",
        "pac env list",
        "pac env create",
        "pac env who",
        "pac solution list",
        "pac solution export",
        "pac solution import",
        "pac solution clone",
        "pac application list",
        "pac help"
    };

    private readonly ILogger<PacCliValidator> _logger;

    public PacCliValidator(ILogger<PacCliValidator> logger)
    {
        _logger = logger;
    }

    public bool IsCommandSafe(string command)
    {
        var baseCommand = ExtractBaseCommand(command);
        return _allowedCommands.Contains(baseCommand);
    }

    public async Task<ValidationResult> ValidateCommandAsync(string command)
    {
        await Task.CompletedTask; // Placeholder for async validation

        if (string.IsNullOrWhiteSpace(command))
        {
            return ValidationResult.CreateFailure("Command cannot be empty");
        }

        if (!command.TrimStart().StartsWith("pac "))
        {
            return ValidationResult.CreateFailure("Only pac commands are allowed");
        }

        var baseCommand = ExtractBaseCommand(command);
        if (!IsCommandSafe(command))
        {
            _logger.LogWarning("Unsafe command attempted: {Command}", baseCommand);
            return ValidationResult.CreateFailure($"Command not allowed: {baseCommand}");
        }

        // Check for dangerous parameters
        if (ContainsDangerousParameters(command))
        {
            return ValidationResult.CreateFailure("Command contains potentially dangerous parameters");
        }

        return ValidationResult.CreateSuccess();
    }

    private string ExtractBaseCommand(string command)
    {
        var parts = command.Trim().Split(' ');
        if (parts.Length >= 3)
        {
            return $"{parts[0]} {parts[1]} {parts[2]}";
        }
        if (parts.Length >= 2)
        {
            return $"{parts[0]} {parts[1]}";
        }
        return command.Trim();
    }

    private bool ContainsDangerousParameters(string command)
    {
        var dangerousParams = new[] { "--force", "--delete", "--reset", "--remove" };
        var lowerCommand = command.ToLowerInvariant();
        
        return dangerousParams.Any(param => lowerCommand.Contains(param));
    }
}

public class M365CliValidator : IM365CliValidator
{
    private readonly HashSet<string> _allowedCommands = new()
    {
        "m365 login",
        "m365 logout",
        "m365 status",
        "m365 app list",
        "m365 app get",
        "m365 app add",
        "m365 app set",
        "m365 app permission",
        "m365 spo site",
        "m365 spo list",
        "m365 teams list",
        "m365 teams get",
        "m365 graph get",
        "m365 help"
    };

    private readonly ILogger<M365CliValidator> _logger;

    public M365CliValidator(ILogger<M365CliValidator> logger)
    {
        _logger = logger;
    }

    public bool IsCommandSafe(string command)
    {
        var baseCommand = ExtractBaseCommand(command);
        return _allowedCommands.Any(allowed => baseCommand.StartsWith(allowed));
    }

    public async Task<ValidationResult> ValidateCommandAsync(string command)
    {
        await Task.CompletedTask; // Placeholder for async validation

        if (string.IsNullOrWhiteSpace(command))
        {
            return ValidationResult.CreateFailure("Command cannot be empty");
        }

        if (!command.TrimStart().StartsWith("m365 "))
        {
            return ValidationResult.CreateFailure("Only m365 commands are allowed");
        }

        if (!IsCommandSafe(command))
        {
            var baseCommand = ExtractBaseCommand(command);
            _logger.LogWarning("Unsafe command attempted: {Command}", baseCommand);
            return ValidationResult.CreateFailure($"Command not allowed: {baseCommand}");
        }

        // Check for dangerous operations
        if (ContainsDangerousOperations(command))
        {
            return ValidationResult.CreateFailure("Command contains potentially dangerous operations");
        }

        return ValidationResult.CreateSuccess();
    }

    private string ExtractBaseCommand(string command)
    {
        var parts = command.Trim().Split(' ');
        if (parts.Length >= 3)
        {
            return $"{parts[0]} {parts[1]} {parts[2]}";
        }
        if (parts.Length >= 2)
        {
            return $"{parts[0]} {parts[1]}";
        }
        return command.Trim();
    }

    private bool ContainsDangerousOperations(string command)
    {
        var dangerousOps = new[] { "delete", "remove", "clear", "--force" };
        var lowerCommand = command.ToLowerInvariant();
        
        return dangerousOps.Any(op => lowerCommand.Contains(op));
    }
}

public class PacCliService : IPacCliService
{
    private readonly ILogger<PacCliService> _logger;

    public PacCliService(ILogger<PacCliService> logger)
    {
        _logger = logger;
    }

    public async Task<CliResult> ExecuteAsync(string command)
    {
        try
        {
            _logger.LogInformation("Executing PAC CLI command");

            var processInfo = new ProcessStartInfo
            {
                FileName = "pac",
                Arguments = command.Substring(4), // Remove "pac " prefix
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for process to complete with timeout
            var completed = await WaitForExitAsync(process, TimeSpan.FromSeconds(30));
            
            if (!completed)
            {
                process.Kill();
                return CliResult.CreateFailure("Command timed out");
            }

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();
            var exitCode = process.ExitCode;

            _logger.LogInformation("PAC CLI command completed with exit code: {ExitCode}", exitCode);

            if (exitCode == 0)
            {
                return CliResult.CreateSuccess(output);
            }
            else
            {
                return new CliResult
                {
                    Success = false,
                    Output = output,
                    Error = error,
                    ExitCode = exitCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception executing PAC CLI command");
            return CliResult.CreateFailure(ex.Message);
        }
    }

    private async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}

public class M365CliService : IM365CliService
{
    private readonly ILogger<M365CliService> _logger;

    public M365CliService(ILogger<M365CliService> logger)
    {
        _logger = logger;
    }

    public async Task<CliResult> ExecuteAsync(string command)
    {
        try
        {
            _logger.LogInformation("Executing M365 CLI command");

            var processInfo = new ProcessStartInfo
            {
                FileName = "m365",
                Arguments = command.Substring(5), // Remove "m365 " prefix
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for process to complete with timeout
            var completed = await WaitForExitAsync(process, TimeSpan.FromSeconds(30));
            
            if (!completed)
            {
                process.Kill();
                return CliResult.CreateFailure("Command timed out");
            }

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();
            var exitCode = process.ExitCode;

            _logger.LogInformation("M365 CLI command completed with exit code: {ExitCode}", exitCode);

            if (exitCode == 0)
            {
                return CliResult.CreateSuccess(output);
            }
            else
            {
                return new CliResult
                {
                    Success = false,
                    Output = output,
                    Error = error,
                    ExitCode = exitCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception executing M365 CLI command");
            return CliResult.CreateFailure(ex.Message);
        }
    }

    private async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}