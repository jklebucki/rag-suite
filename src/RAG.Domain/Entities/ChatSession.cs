using RAG.Domain.Events;

namespace RAG.Domain.Entities;

/// <summary>
/// Chat session aggregate root following DDD principles
/// </summary>
public class ChatSession
{
    private readonly List<ChatMessage> _messages = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public string Id { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private ChatSession() 
    { 
        Id = string.Empty;
        Title = string.Empty;
    } // EF Core

    public ChatSession(string title)
    {
        Id = Guid.NewGuid().ToString();
        Title = title ?? "New Conversation";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public ChatMessage AddMessage(string content, string role)
    {
        var message = new ChatMessage(content, role);
        _messages.Add(message);
        UpdatedAt = DateTime.UtcNow;
        
        _domainEvents.Add(new ChatMessageSent(Id, message.Id, content, role));
        
        return message;
    }

    public void UpdateTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("Title cannot be empty", nameof(newTitle));
            
        Title = newTitle;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public class ChatMessage
{
    public string Id { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public SearchContext? SearchContext { get; private set; }

    private ChatMessage() 
    { 
        Id = string.Empty;
        Content = string.Empty;
        Role = string.Empty;
    } // EF Core

    public ChatMessage(string content, string role)
    {
        Id = Guid.NewGuid().ToString();
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Role = role ?? throw new ArgumentNullException(nameof(role));
        CreatedAt = DateTime.UtcNow;
    }

    public void AttachSearchContext(SearchContext context)
    {
        SearchContext = context;
    }
}

public class SearchContext
{
    public List<DocumentReference> Sources { get; set; } = new();
    public string Query { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public double SearchDuration { get; set; }
}

public class DocumentReference
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public double Relevance { get; set; }
}
