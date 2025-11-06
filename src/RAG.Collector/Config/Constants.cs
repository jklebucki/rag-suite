namespace RAG.Collector.Config;

/// <summary>
/// Constants used throughout the RAG Collector application
/// </summary>
public static class Constants
{
    /// <summary>
    /// Default embedding vector dimensions for multilingual-e5-small model
    /// </summary>
    public const int DefaultEmbeddingDimensions = 768;

    /// <summary>
    /// Default maximum tokens for embedding models
    /// </summary>
    public const int DefaultMaxTokens = 512;

    /// <summary>
    /// Progress reporting interval (log every N files)
    /// </summary>
    public const int ProgressLogInterval = 50;

    /// <summary>
    /// Yield control interval during file enumeration (yield every N files)
    /// </summary>
    public const int EnumerationYieldInterval = 100;

    /// <summary>
    /// Delay between batch embedding requests (milliseconds)
    /// </summary>
    public const int EmbeddingRequestDelayMs = 50;

    /// <summary>
    /// Health check timeout (seconds)
    /// </summary>
    public const int HealthCheckTimeoutSeconds = 5;

    /// <summary>
    /// Content hash length for display (first N characters)
    /// </summary>
    public const int ContentHashDisplayLength = 12;

    /// <summary>
    /// Elasticsearch file metadata index name
    /// </summary>
    public const string FileMetadataIndexName = "rag-file-metadata";

    /// <summary>
    /// Default Elasticsearch index name
    /// </summary>
    public const string DefaultIndexName = "rag-chunks";

    /// <summary>
    /// Embedding service endpoint path
    /// </summary>
    public const string EmbeddingEndpoint = "/embed";

    /// <summary>
    /// Characters per token estimation (rough approximation)
    /// </summary>
    public const double CharactersPerToken = 4.0;

    /// <summary>
    /// Minimum paragraph length to consider splitting by sentences
    /// </summary>
    public const int MinParagraphLengthForSentenceSplit = 800;

    /// <summary>
    /// Conservative sentence grouping threshold
    /// </summary>
    public const int SentenceGroupingThreshold = 600;

    /// <summary>
    /// Percentage of chunk to search for break point (80% = search in last 20%)
    /// </summary>
    public const double BreakPointSearchPercentage = 0.8;
}

