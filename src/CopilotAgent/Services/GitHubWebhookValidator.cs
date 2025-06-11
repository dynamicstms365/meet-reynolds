using System.Security.Cryptography;
using System.Text;

namespace CopilotAgent.Services;

public interface IGitHubWebhookValidator
{
    bool ValidateSignature(string payload, string signature, string secret);
    bool IsValidGitHubIP(string ipAddress);
}

public class GitHubWebhookValidator : IGitHubWebhookValidator
{
    private readonly ILogger<GitHubWebhookValidator> _logger;
    
    // GitHub's webhook IP ranges (as of 2024)
    private readonly string[] _gitHubIpRanges = {
        "192.30.252.0/22",
        "185.199.108.0/22", 
        "140.82.112.0/20",
        "143.55.64.0/20",
        "2a0a:a440::/29",
        "2606:50c0::/32"
    };

    public GitHubWebhookValidator(ILogger<GitHubWebhookValidator> logger)
    {
        _logger = logger;
    }

    public bool ValidateSignature(string payload, string signature, string secret)
    {
        if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("Missing required parameters for signature validation");
            return false;
        }

        // GitHub sends signature as "sha256=<hash>"
        if (!signature.StartsWith("sha256="))
        {
            _logger.LogWarning("Invalid signature format: {Signature}", signature);
            return false;
        }

        var hashFromSignature = signature.Substring(7); // Remove "sha256=" prefix

        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

            var isValid = string.Equals(hashFromSignature, computedSignature, StringComparison.OrdinalIgnoreCase);
            
            if (!isValid)
            {
                _logger.LogWarning("Webhook signature validation failed");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating webhook signature");
            return false;
        }
    }

    public bool IsValidGitHubIP(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return false;

        // In production, you'd implement proper CIDR matching
        // This is a simplified version for demonstration
        return _gitHubIpRanges.Any(range => ipAddress.StartsWith(range.Split('/')[0].Substring(0, 10)));
    }
}