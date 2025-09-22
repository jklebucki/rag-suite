namespace RAG.Orchestrator.Api.Models;

public record LlmSettings
{
    public string Url { get; init; } = string.Empty;
    public int MaxTokens { get; init; }
    public double Temperature { get; init; }
    public string Model { get; init; } = string.Empty;
    public bool IsOllama { get; init; }
    public int TimeoutMinutes { get; init; }
    public string ChatEndpoint { get; init; } = "/api/chat";
    public string GenerateEndpoint { get; init; } = "/api/generate";
}