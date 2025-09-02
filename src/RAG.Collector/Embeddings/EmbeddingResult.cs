namespace RAG.Collector.Embeddings;

/// <summary>
/// Result of text embedding operation
/// </summary>
public class EmbeddingResult
{
    /// <summary>
    /// Indicates if the embedding was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The embedding vector
    /// </summary>
    public float[] Vector { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Number of dimensions in the vector
    /// </summary>
    public int Dimensions => Vector.Length;

    /// <summary>
    /// Error message if embedding failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Token count used for the embedding
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// Model used for embedding
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Time taken to generate the embedding
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Create a successful embedding result
    /// </summary>
    public static EmbeddingResult CreateSuccess(float[] vector, string modelName, int tokenCount, TimeSpan duration)
    {
        return new EmbeddingResult
        {
            Success = true,
            Vector = vector,
            ModelName = modelName,
            TokenCount = tokenCount,
            Duration = duration
        };
    }

    /// <summary>
    /// Create a failed embedding result
    /// </summary>
    public static EmbeddingResult CreateFailure(string errorMessage)
    {
        return new EmbeddingResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
