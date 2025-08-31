namespace RAG.Orchestrator.Api.Features.Health;

public record ServiceStatus(string Name, string Status, string? Message = null, object? Details = null);

public record SystemHealthResponse(
    ServiceStatus Api,
    ServiceStatus Llm,
    ServiceStatus Elasticsearch,
    ServiceStatus VectorStore,
    DateTime Timestamp
);
