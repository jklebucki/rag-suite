using System.ComponentModel.DataAnnotations;

namespace RAG.Orchestrator.Api.Models.Configuration;

/// <summary>
/// Configuration class for LLM endpoint settings shared between services
/// </summary>
public class LlmEndpointConfig
{
    public const string SectionName = "Services:LlmService";

    /// <summary>
    /// Base URL of the LLM service (e.g., Ollama)
    /// </summary>
    [Required]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Model name to use for LLM requests
    /// </summary>
    [Required]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    [Range(1, 100000)]
    public int MaxTokens { get; set; } = 3000;

    /// <summary>
    /// Temperature for response randomness (0.0 - 2.0)
    /// </summary>
    [Range(0.0, 2.0)]
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Request timeout in minutes
    /// </summary>
    [Range(1, 120)]
    public int TimeoutMinutes { get; set; } = 15;

    /// <summary>
    /// Maximum context window sent to Ollama as num_ctx
    /// </summary>
    [Range(1, 128000)]
    public int ContextWindow { get; set; } = 98000;

    /// <summary>
    /// Maximum estimated tokens allowed for temporary user attachments in one chat draft
    /// </summary>
    [Range(1, 128000)]
    public int AttachmentContextLimitTokens { get; set; } = 12000;

    /// <summary>
    /// Maximum estimated tokens allowed for one persisted chat session
    /// </summary>
    [Range(1, 128000)]
    public int SessionContextLimitTokens { get; set; } = 9600;

    /// <summary>
    /// Whether the service is Ollama (affects endpoint format)
    /// </summary>
    public bool IsOllama { get; set; } = true;

    /// <summary>
    /// API endpoint for chat completion (default for Ollama)
    /// </summary>
    public string ChatEndpoint { get; set; } = "/api/chat";

    /// <summary>
    /// API endpoint for generation (legacy, deprecated for Ollama)
    /// </summary>
    public string GenerateEndpoint { get; set; } = "/api/generate";

    /// <summary>
    /// Gets the full URL for chat endpoint
    /// </summary>
    public string ChatUrl => $"{Url.TrimEnd('/')}{ChatEndpoint}";

    /// <summary>
    /// Gets the full URL for generate endpoint (legacy)
    /// </summary>
    public string GenerateUrl => $"{Url.TrimEnd('/')}{GenerateEndpoint}";

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Url))
            throw new ArgumentException("LLM Service URL is required", nameof(Url));

        if (string.IsNullOrWhiteSpace(Model))
            throw new ArgumentException("LLM Model name is required", nameof(Model));

        if (!Uri.TryCreate(Url, UriKind.Absolute, out _))
            throw new ArgumentException("LLM Service URL must be a valid absolute URI", nameof(Url));

        if (MaxTokens <= 0)
            throw new ArgumentException("MaxTokens must be greater than 0", nameof(MaxTokens));

        if (Temperature < 0.0 || Temperature > 2.0)
            throw new ArgumentException("Temperature must be between 0.0 and 2.0", nameof(Temperature));

        if (TimeoutMinutes <= 0)
            throw new ArgumentException("TimeoutMinutes must be greater than 0", nameof(TimeoutMinutes));

        if (ContextWindow <= 0 || ContextWindow > 128000)
            throw new ArgumentException("ContextWindow must be between 1 and 128000", nameof(ContextWindow));

        if (AttachmentContextLimitTokens <= 0 || AttachmentContextLimitTokens > ContextWindow)
            throw new ArgumentException("AttachmentContextLimitTokens must be between 1 and ContextWindow", nameof(AttachmentContextLimitTokens));

        if (SessionContextLimitTokens <= 0 || SessionContextLimitTokens > ContextWindow)
            throw new ArgumentException("SessionContextLimitTokens must be between 1 and ContextWindow", nameof(SessionContextLimitTokens));
    }
}
