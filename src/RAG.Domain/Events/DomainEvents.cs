namespace RAG.Domain.Events;

/// <summary>
/// Base interface for domain events following DDD principles
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
    string EventId { get; }
}

public abstract class DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string EventId { get; } = Guid.NewGuid().ToString();
}

public class ChatMessageSent : DomainEvent
{
    public string SessionId { get; }
    public string MessageId { get; }
    public string Content { get; }
    public string Role { get; }

    public ChatMessageSent(string sessionId, string messageId, string content, string role)
    {
        SessionId = sessionId;
        MessageId = messageId;
        Content = content;
        Role = role;
    }
}

public class DocumentProcessed : DomainEvent
{
    public string DocumentId { get; }
    public string FileName { get; }
    public int ChunkCount { get; }

    public DocumentProcessed(string documentId, string fileName, int chunkCount)
    {
        DocumentId = documentId;
        FileName = fileName;
        ChunkCount = chunkCount;
    }
}
