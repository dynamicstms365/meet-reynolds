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
        _logger.LogInformation("🔍 WEBHOOK SIGNATURE VALIDATION START");
        _logger.LogInformation("📏 Payload length: {PayloadLength} bytes", payload?.Length ?? 0);
        _logger.LogInformation("🔑 Secret length: {SecretLength} chars", secret?.Length ?? 0);
        _logger.LogInformation("🔑 Secret preview: {SecretPreview}...", secret?.Substring(0, Math.Min(10, secret?.Length ?? 0)));
        _logger.LogInformation("📝 Incoming signature: {Signature}", signature);

        if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
        {
            _logger.LogError("❌ Missing required parameters for signature validation");
            _logger.LogError("   - Payload empty: {PayloadEmpty}", string.IsNullOrEmpty(payload));
            _logger.LogError("   - Signature empty: {SignatureEmpty}", string.IsNullOrEmpty(signature));
            _logger.LogError("   - Secret empty: {SecretEmpty}", string.IsNullOrEmpty(secret));
            return false;
        }

        // GitHub sends signature as "sha256=<hash>"
        if (!signature.StartsWith("sha256="))
        {
            _logger.LogError("❌ Invalid signature format - must start with 'sha256=': {Signature}", signature);
            return false;
        }

        var hashFromSignature = signature.Substring(7); // Remove "sha256=" prefix
        _logger.LogInformation("🔍 Hash from GitHub signature: {HashFromSignature}", hashFromSignature);

        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

            _logger.LogInformation("🧮 Computed signature: {ComputedSignature}", computedSignature);
            _logger.LogInformation("🔍 GitHub signature:  {GitHubSignature}", hashFromSignature.ToLowerInvariant());

            var isValid = string.Equals(hashFromSignature, computedSignature, StringComparison.OrdinalIgnoreCase);
            
            if (isValid)
            {
                _logger.LogInformation("✅ Webhook signature validation PASSED");
            }
            else
            {
                _logger.LogError("❌ Webhook signature validation FAILED");
                _logger.LogError("   Expected: {Expected}", computedSignature);
                _logger.LogError("   Received: {Received}", hashFromSignature.ToLowerInvariant());
                _logger.LogError("   🚨 WEBHOOK SECRET MISMATCH - Check your GitHub webhook secret configuration!");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Exception during webhook signature validation");
            return false;
        }
        finally
        {
            _logger.LogInformation("🔍 WEBHOOK SIGNATURE VALIDATION END");
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