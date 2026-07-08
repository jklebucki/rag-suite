namespace RAG.Orchestrator.Api.Features.Settings;

public record LlmSettingsRequest
{
    public string Url { get; init; } = string.Empty;
    public int MaxTokens { get; init; }
    public double Temperature { get; init; }
    public string Model { get; init; } = string.Empty;
    public bool IsOllama { get; init; }
    public int TimeoutMinutes { get; init; }
    public int ContextWindow { get; init; } = 98000;
    public int AttachmentContextLimitTokens { get; init; } = 12000;
    public int SessionContextLimitTokens { get; init; } = 9600;
    public int DocumentSearchLimit { get; init; } = 4;
    public string ChatEndpoint { get; init; } = "/api/chat";
    public string GenerateEndpoint { get; init; } = "/api/generate";
}

public record LlmSettingsResponse
{
    public string Url { get; init; } = string.Empty;
    public int MaxTokens { get; init; }
    public double Temperature { get; init; }
    public string Model { get; init; } = string.Empty;
    public bool IsOllama { get; init; }
    public int TimeoutMinutes { get; init; }
    public int ContextWindow { get; init; } = 98000;
    public int AttachmentContextLimitTokens { get; init; } = 12000;
    public int SessionContextLimitTokens { get; init; } = 9600;
    public int DocumentSearchLimit { get; init; } = 4;
    public string ChatEndpoint { get; init; } = "/api/chat";
    public string GenerateEndpoint { get; init; } = "/api/generate";
}
