using RAG.Orchestrator.Api.Features.Search;

namespace RAG.Orchestrator.Api.Features.Chat;

public record UserChatMessage(
    string Id,
    string Role,
    string Content,
    DateTime Timestamp,
    SearchResult[]? Sources = null,
    Dictionary<string, object>? Metadata = null
);

public record UserChatSession(
    string Id,
    string UserId,
    string Title,
    UserChatMessage[] Messages,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record UserChatRequest(
    string Message,
    string? SessionId = null,
    bool UseRag = true,
    string[]? Context = null,
    string? Language = null,
    Dictionary<string, object>? Metadata = null
);

public record CreateUserSessionRequest(string? Title = null, string? Language = null);
