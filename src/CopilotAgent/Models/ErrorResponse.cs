using System.ComponentModel.DataAnnotations;

namespace CopilotAgent.Models;

/// <summary>
/// Standard error response model for API endpoints
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error type or code
    /// </summary>
    [Required]
    public string Error { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed error message
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional error details and context
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}