namespace RAG.Collector.Config;

/// <summary>
/// Configuration options for the RAG Collector service
/// </summary>
public class CollectorOptions
{
    public const string SectionName = "Collector";

    /// <summary>
    /// Source folders to scan for documents (UNC paths supported)
    /// </summary>
    public List<string> SourceFolders { get; set; } = new();

    /// <summary>
    /// File extensions to process (e.g., .pdf, .docx, .xlsx, .pptx, .txt, .csv, .md)
    /// </summary>
    public List<string> FileExtensions { get; set; } = new() { ".pdf", ".docx", ".xlsx", ".pptx", ".txt", ".csv", ".md" };

    /// <summary>
    /// Size of text chunks in characters
    /// </summary>
    public int ChunkSize { get; set; } = 1200;

    /// <summary>
    /// Overlap between chunks in characters
    /// </summary>
    public int ChunkOverlap { get; set; } = 200;

    /// <summary>
    /// Elasticsearch URL
    /// </summary>
    public string ElasticsearchUrl { get; set; } = "http://localhost:9200";

    /// <summary>
    /// Elasticsearch username for authentication
    /// </summary>
    public string? ElasticsearchUsername { get; set; }

    /// <summary>
    /// Elasticsearch password for authentication
    /// </summary>
    public string? ElasticsearchPassword { get; set; }

    /// <summary>
    /// Elasticsearch API key for authentication (alternative to username/password)
    /// </summary>
    public string? ElasticsearchApiKey { get; set; }

    /// <summary>
    /// Allow self-signed TLS certificates for Elasticsearch
    /// </summary>
    public bool AllowSelfSignedCert { get; set; } = false;

    /// <summary>
    /// Index name for storing document chunks
    /// </summary>
    public string IndexName { get; set; } = "rag-chunks";

    /// <summary>
    /// Embedding model name
    /// </summary>
    public string EmbeddingModelName { get; set; } = "intfloat/multilingual-e5-small";

    /// <summary>
    /// Processing interval in minutes
    /// </summary>
    public int ProcessingIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Batch size for bulk operations to Elasticsearch
    /// </summary>
    public int BulkBatchSize { get; set; } = 200;
}
