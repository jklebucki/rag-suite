namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public sealed class GeneratedArtifactEntity
{
    public string Id { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public string AssistantMessageId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string StorageKey { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
