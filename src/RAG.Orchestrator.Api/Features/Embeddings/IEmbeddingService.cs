namespace RAG.Orchestrator.Api.Features.Embeddings;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    int EmbeddingDimension { get; }
    Task<bool> IsAvailableAsync();
}
