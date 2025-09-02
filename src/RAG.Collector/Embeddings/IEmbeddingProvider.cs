using RAG.Collector.Models;

namespace RAG.Collector.Embeddings;

/// <summary>
/// Interface for text embedding providers
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>
    /// Model name used by this provider
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Maximum number of tokens this model can process
    /// </summary>
    int MaxTokens { get; }

    /// <summary>
    /// Dimension of the embedding vectors
    /// </summary>
    int VectorDimensions { get; }

    /// <summary>
    /// Generate embedding for a text chunk
    /// </summary>
    /// <param name="chunk">Text chunk to embed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding result</returns>
    Task<EmbeddingResult> GenerateEmbeddingAsync(TextChunk chunk, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate embeddings for multiple chunks in batch
    /// </summary>
    /// <param name="chunks">Text chunks to embed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of embedding results</returns>
    Task<IList<EmbeddingResult>> GenerateBatchEmbeddingsAsync(IList<TextChunk> chunks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the provider is available and configured
    /// </summary>
    /// <returns>True if available</returns>
    Task<bool> IsAvailableAsync();
}
