using RAG.Collector.Models;
using RAG.Collector.Embeddings;
using RAG.Collector.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RAG.Collector.Config;

namespace RAG.Collector.Indexing;

/// <summary>
/// Service for orchestrating the indexing pipeline: embedding generation + Elasticsearch indexing
/// </summary>
public class IndexingService
{
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<IndexingService> _logger;
    private readonly CollectorOptions _options;

    public IndexingService(
        IEmbeddingProvider embeddingProvider,
        IElasticsearchService elasticsearchService,
        ILogger<IndexingService> logger,
        IOptions<CollectorOptions> options)
    {
        _embeddingProvider = embeddingProvider;
        _elasticsearchService = elasticsearchService;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Index a single chunk
    /// </summary>
    /// <param name="chunk">Text chunk to index</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    public async Task<bool> IndexChunkAsync(TextChunk chunk, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting indexing process for chunk {ChunkId}", chunk.Id);

            // Generate embedding
            var embeddingResult = await _embeddingProvider.GenerateEmbeddingAsync(chunk, cancellationToken);
            
            if (!embeddingResult.Success)
            {
                _logger.LogError("Failed to generate embedding for chunk {ChunkId}: {Error}", 
                    chunk.Id, embeddingResult.ErrorMessage);
                return false;
            }

            // Create Elasticsearch document
            var document = ChunkDocument.FromTextChunk(chunk, embeddingResult.Vector, embeddingResult.ModelName!);

            // Index document
            var indexResult = await _elasticsearchService.IndexDocumentAsync(document, cancellationToken);
            
            if (indexResult)
            {
                _logger.LogDebug("Successfully indexed chunk {ChunkId} with {Dimensions}D embedding", 
                    chunk.Id, embeddingResult.Dimensions);
                return true;
            }

            _logger.LogError("Failed to index chunk {ChunkId} in Elasticsearch", chunk.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error indexing chunk {ChunkId}", chunk.Id);
            return false;
        }
    }

    /// <summary>
    /// Index multiple chunks in batch
    /// </summary>
    /// <param name="chunks">Text chunks to index</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of successfully indexed chunks</returns>
    public async Task<int> IndexChunksBatchAsync(IList<TextChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (!chunks.Any())
            return 0;

        try
        {
            _logger.LogInformation("Starting batch indexing of {ChunkCount} chunks", chunks.Count);

            // Generate embeddings for all chunks
            var embeddingResults = await _embeddingProvider.GenerateBatchEmbeddingsAsync(chunks, cancellationToken);
            
            // Create documents for successful embeddings
            var documents = new List<ChunkDocument>();
            var successfulEmbeddings = 0;

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var embeddingResult = embeddingResults[i];

                if (embeddingResult.Success)
                {
                    var document = ChunkDocument.FromTextChunk(chunk, embeddingResult.Vector, embeddingResult.ModelName!);
                    documents.Add(document);
                    successfulEmbeddings++;
                }
                else
                {
                    _logger.LogWarning("Skipping chunk {ChunkId} due to embedding failure: {Error}", 
                        chunk.Id, embeddingResult.ErrorMessage);
                }
            }

            if (!documents.Any())
            {
                _logger.LogWarning("No chunks could be processed - all embedding generation failed");
                return 0;
            }

            // Index documents in Elasticsearch
            var indexedCount = await _elasticsearchService.IndexDocumentsBatchAsync(documents, cancellationToken);
            
            _logger.LogInformation("Batch indexing completed: {IndexedCount}/{EmbeddingCount}/{TotalCount} chunks indexed", 
                indexedCount, successfulEmbeddings, chunks.Count);

            return indexedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch indexing of {ChunkCount} chunks", chunks.Count);
            return 0;
        }
    }

    /// <summary>
    /// Index all chunks from a file
    /// </summary>
    /// <param name="fileChunks">Chunks from a single file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of successfully indexed chunks</returns>
    public async Task<int> IndexFileChunksAsync(IList<TextChunk> fileChunks, CancellationToken cancellationToken = default)
    {
        if (!fileChunks.Any())
            return 0;

        var sourceFile = fileChunks.First().SourceFile?.Path ?? "unknown";
        
        try
        {
            _logger.LogInformation("Indexing {ChunkCount} chunks from file: {SourceFile}", 
                fileChunks.Count, sourceFile);

            // Delete existing documents for this file first
            await _elasticsearchService.DeleteDocumentsBySourceFileAsync(sourceFile, cancellationToken);

            // Index chunks using batch processing if we have many chunks
            var indexedCount = 0;
            
            if (fileChunks.Count <= _options.BulkBatchSize)
            {
                // Process all chunks in one batch
                indexedCount = await IndexChunksBatchAsync(fileChunks, cancellationToken);
            }
            else
            {
                // Process in smaller batches
                var batches = fileChunks.Chunk(_options.BulkBatchSize);
                
                foreach (var batch in batches)
                {
                    var batchResult = await IndexChunksBatchAsync(batch.ToList(), cancellationToken);
                    indexedCount += batchResult;
                }
            }

            _logger.LogInformation("Completed indexing file {SourceFile}: {IndexedCount}/{TotalCount} chunks indexed", 
                sourceFile, indexedCount, fileChunks.Count);

            return indexedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing chunks from file {SourceFile}", sourceFile);
            return 0;
        }
    }

    /// <summary>
    /// Ensure the system is ready for indexing
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if ready</returns>
    public async Task<bool> EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking indexing system readiness...");

            // Check embedding provider
            var embeddingAvailable = await _embeddingProvider.IsAvailableAsync();
            if (!embeddingAvailable)
            {
                _logger.LogError("Embedding provider is not available");
                return false;
            }

            // Check Elasticsearch
            var elasticsearchAvailable = await _elasticsearchService.IsAvailableAsync();
            if (!elasticsearchAvailable)
            {
                _logger.LogError("Elasticsearch is not available");
                return false;
            }

            // Ensure index exists
            var indexReady = await _elasticsearchService.EnsureIndexExistsAsync(cancellationToken);
            if (!indexReady)
            {
                _logger.LogError("Failed to ensure Elasticsearch index exists");
                return false;
            }

            _logger.LogInformation("Indexing system is ready - embedding provider: {EmbeddingModel}, Elasticsearch: available", 
                _embeddingProvider.ModelName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking indexing system readiness");
            return false;
        }
    }

    /// <summary>
    /// Get indexing statistics
    /// </summary>
    /// <returns>Indexing statistics</returns>
    public async Task<IndexingStats?> GetStatsAsync()
    {
        try
        {
            var indexStats = await _elasticsearchService.GetIndexStatsAsync();
            
            if (indexStats != null)
            {
                return new IndexingStats
                {
                    TotalDocuments = indexStats.DocumentCount,
                    IndexSizeBytes = indexStats.IndexSizeBytes,
                    EmbeddingModel = _embeddingProvider.ModelName,
                    VectorDimensions = _embeddingProvider.VectorDimensions,
                    LastUpdated = indexStats.LastUpdated
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indexing stats");
            return null;
        }
    }
}

/// <summary>
/// Indexing statistics
/// </summary>
public class IndexingStats
{
    public long TotalDocuments { get; set; }
    public long IndexSizeBytes { get; set; }
    public string EmbeddingModel { get; set; } = string.Empty;
    public int VectorDimensions { get; set; }
    public DateTime LastUpdated { get; set; }
}
