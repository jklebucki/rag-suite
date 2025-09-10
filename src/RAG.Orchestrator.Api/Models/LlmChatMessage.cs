namespace RAG.Orchestrator.Api.Models;

public class LlmChatMessage
{
    public string Role { get; set; } = string.Empty; // "system", "user", "assistant"
    public string Content { get; set; } = string.Empty;
}