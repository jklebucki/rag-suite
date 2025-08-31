namespace RAG.Application.DTOs;

public record ChatSessionDto
{
    public required string SessionId { get; init; }
    public required string UserId { get; init; }
    public required string Context { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime LastActivityAt { get; init; }
    public required int MessageCount { get; init; }
    public List<ChatMessageDto> Messages { get; init; } = new();
}

public record ChatMessageDto
{
    public required string MessageId { get; init; }
    public required string Content { get; init; }
    public required bool IsFromUser { get; init; }
    public required DateTime Timestamp { get; init; }
    public List<ContextItemDto>? Context { get; init; }
}

public record ContextItemDto
{
    public required string Source { get; init; }
    public required string Content { get; init; }
    public required double Score { get; init; }
}
