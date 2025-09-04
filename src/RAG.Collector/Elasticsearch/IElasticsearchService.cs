namespace RAG.Collector.Elasticsearch;

/// <summary>
/// Interface for Elasticsearch operations
/// </summary>
public interface IElasticsearchService
{
    /// <summary>
    /// Index a single chunk document
    /// </summary>
    /// <param name="document">Document to index</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> IndexDocumentAsync(ChunkDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Index multiple chunk documents in batch
    /// </summary>
    /// <param name="documents">Documents to index</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of successfully indexed documents</returns>
    Task<int> IndexDocumentsBatchAsync(IList<ChunkDocument> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the index exists and create it if not
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if index is ready</returns>
    Task<bool> EnsureIndexExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete documents for a specific source file
    /// </summary>
    /// <param name="sourceFile">Source file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of deleted documents</returns>
    Task<int> DeleteDocumentsBySourceFileAsync(string sourceFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if Elasticsearch is available
    /// </summary>
    /// <returns>True if available</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Get index statistics
    /// </summary>
    /// <returns>Index statistics</returns>
    Task<IndexStats?> GetIndexStatsAsync();

    /// <summary>
    /// Get a document by ID from a specific index
    /// </summary>
    /// <typeparam name="T">Document type</typeparam>
    /// <param name="indexName">Index name</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document if found, null otherwise</returns>
    Task<T?> GetDocumentByIdAsync<T>(string indexName, string documentId, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Index a document to a custom index
    /// </summary>
    /// <typeparam name="T">Document type</typeparam>
    /// <param name="indexName">Index name</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="document">Document to index</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> IndexDocumentToCustomIndexAsync<T>(string indexName, string documentId, T document, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Ensure a custom index exists
    /// </summary>
    /// <param name="indexName">Index name</param>
    /// <param name="mappingJson">Optional mapping JSON</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if index is ready</returns>
    Task<bool> EnsureCustomIndexExistsAsync(string indexName, string? mappingJson = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Index statistics
/// </summary>
public class IndexStats
{
    public long DocumentCount { get; set; }
    public long IndexSizeBytes { get; set; }
    public string IndexName { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
