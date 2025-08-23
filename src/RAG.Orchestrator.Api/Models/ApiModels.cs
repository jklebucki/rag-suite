namespace RAG.Orchestrator.Api.Models;

// Search Models
public record SearchRequest(
    string Query,
    SearchFilters? Filters = null,
    int Limit = 20,
    int Offset = 0
);

public record SearchFilters(
    string[]? DocumentType = null,
    DateRange? DateRange = null,
    string[]? Source = null
);

public record DateRange(DateTime From, DateTime To);

public record SearchResult(
    string Id,
    string Title,
    string Content,
    double Score,
    string Source,
    string DocumentType,
    Dictionary<string, object> Metadata,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record SearchResponse(
    SearchResult[] Results,
    int Total,
    int Took,
    string Query
);

// Chat Models
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
    string[]? Context = null
);

public record CreateSessionRequest(string? Title = null);

// Plugin Models
public record PluginInfo(
    string Id,
    string Name,
    string Description,
    string Version,
    bool Enabled,
    string[] Capabilities
);

// Analytics Models
public record UsageStats(
    int TotalQueries,
    int TotalSessions,
    double AvgResponseTime,
    string[] TopQueries,
    Dictionary<string, int> PluginUsage
);
