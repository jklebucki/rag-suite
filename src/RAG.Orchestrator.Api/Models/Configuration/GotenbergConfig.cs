using System.ComponentModel.DataAnnotations;

namespace RAG.Orchestrator.Api.Models.Configuration;

/// <summary>
/// Configuration class for Gotenberg PDF conversion service
/// </summary>
public class GotenbergConfig
{
    public const string SectionName = "Services:Gotenberg";

    /// <summary>
    /// Base URL of the Gotenberg service
    /// </summary>
    [Required]
    public string Url { get; set; } = "http://localhost:3000";

    /// <summary>
    /// Request timeout in minutes
    /// </summary>
    [Range(1, 60)]
    public int TimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Enable debug route for troubleshooting
    /// </summary>
    public bool EnableDebugRoute { get; set; } = true;

    /// <summary>
    /// Disable web security for Chromium
    /// </summary>
    public bool DisableWebSecurity { get; set; } = true;

    /// <summary>
    /// Allow file access from files for Chromium
    /// </summary>
    public bool AllowFileAccessFromFiles { get; set; } = true;

    /// <summary>
    /// Disable extension check for LibreOffice
    /// </summary>
    public bool DisableExtensionCheck { get; set; } = true;
}