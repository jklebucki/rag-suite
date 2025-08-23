namespace RAG.Orchestrator.Api.Features.Search;

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
