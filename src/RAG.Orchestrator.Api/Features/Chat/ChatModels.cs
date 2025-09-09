using RAG.Abstractions.Search;

namespace RAG.Orchestrator.Api.Features.Chat;

public record ChatMessage(
    string Id,
    string Role,
    string Content,
    DateTime Timestamp,
    SearchResult[]? Sources = null,
    Dictionary<string, object>? Metadata = null
);

public record ChatSession(
    string Id,
    string Title,
    ChatMessage[] Messages,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ChatRequest(
    string Message,
    string? SessionId = null,
    bool UseRag = true,
    bool UseDocumentSearch = true,
    string[]? Context = null
);

public record CreateSessionRequest(string? Title = null, string? Language = null);
