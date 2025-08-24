using Microsoft.Extensions.Logging;
using RAG.Connectors.Files.Services;
using RAG.Ingestion.Worker.Models;
using RAG.Ingestion.Worker.Services;

namespace RAG.Ingestion.Worker.Services;

public interface IDocumentIngestionService
{
    Task<bool> ProcessFileAsync(string filePath);
    Task<int> ProcessDirectoryAsync(string directoryPath, bool recursive = true);
    Task<bool> InitializeIndexAsync();
    Task<long> GetIndexedDocumentCountAsync();
}

public class DocumentIngestionService : IDocumentIngestionService
{
    private readonly IDocumentService _documentService;
    private readonly ITextChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IngestionSettings _settings;
    private readonly ILogger<DocumentIngestionService> _logger;

    public DocumentIngestionService(
        IDocumentService documentService,
        ITextChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IElasticsearchService elasticsearchService,
        IngestionSettings settings,
        ILogger<DocumentIngestionService> logger)
    {
        _documentService = documentService;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _elasticsearchService = elasticsearchService;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> InitializeIndexAsync()
    {
        try
        {
            var indexExists = await _elasticsearchService.IndexExistsAsync(_settings.IndexName);
            
            if (!indexExists)
            {
                _logger.LogInformation("Creating index: {IndexName}", _settings.IndexName);
                var created = await _elasticsearchService.CreateIndexAsync(_settings.IndexName);
                
                if (!created)
                {
                    _logger.LogError("Failed to create index: {IndexName}", _settings.IndexName);
                    return false;
                }
            }
            else
            {
                _logger.LogInformation("Index already exists: {IndexName}", _settings.IndexName);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize index: {IndexName}", _settings.IndexName);
            return false;
        }
    }

    public async Task<bool> ProcessFileAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Processing file: {FilePath}", filePath);

            // Parse document
            var document = await _documentService.ProcessDocumentAsync(filePath);
            if (document == null)
            {
                _logger.LogWarning("Failed to parse document: {FilePath}", filePath);
                return false;
            }

            // Chunk document
            var chunks = _chunkingService.ChunkDocument(
                document.Id,
                document.FileName,
                document.Content,
                document.FileType,
                document.Metadata
            ).ToList();

            if (!chunks.Any())
            {
                _logger.LogWarning("No chunks generated for document: {FilePath}", filePath);
                return false;
            }

            _logger.LogInformation("Generated {ChunkCount} chunks for document: {FileName}", chunks.Count, document.FileName);

            // Generate embeddings for each chunk
            foreach (var chunk in chunks)
            {
                try
                {
                    chunk.Embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate embedding for chunk: {ChunkId}", chunk.Id);
                    // Continue with empty embedding
                    chunk.Embedding = new float[_embeddingService.EmbeddingDimension];
                }
            }

            // Index chunks to Elasticsearch
            var indexed = await _elasticsearchService.IndexDocumentsAsync(chunks, _settings.IndexName);
            
            if (indexed)
            {
                _logger.LogInformation("Successfully indexed {ChunkCount} chunks for document: {FileName}", chunks.Count, document.FileName);
                return true;
            }
            else
            {
                _logger.LogError("Failed to index chunks for document: {FileName}", document.FileName);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process file: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<int> ProcessDirectoryAsync(string directoryPath, bool recursive = true)
    {
        try
        {
            _logger.LogInformation("Processing directory: {DirectoryPath} (recursive: {Recursive})", directoryPath, recursive);

            var documents = await _documentService.ProcessDirectoryAsync(directoryPath, recursive);
            var processedCount = 0;

            foreach (var document in documents)
            {
                try
                {
                    // Chunk document
                    var chunks = _chunkingService.ChunkDocument(
                        document.Id,
                        document.FileName,
                        document.Content,
                        document.FileType,
                        document.Metadata
                    ).ToList();

                    if (!chunks.Any())
                    {
                        _logger.LogWarning("No chunks generated for document: {FileName}", document.FileName);
                        continue;
                    }

                    // Generate embeddings for each chunk
                    foreach (var chunk in chunks)
                    {
                        try
                        {
                            chunk.Embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to generate embedding for chunk: {ChunkId}", chunk.Id);
                            chunk.Embedding = new float[_embeddingService.EmbeddingDimension];
                        }
                    }

                    // Index chunks to Elasticsearch
                    var indexed = await _elasticsearchService.IndexDocumentsAsync(chunks, _settings.IndexName);
                    
                    if (indexed)
                    {
                        processedCount++;
                        _logger.LogInformation("Successfully processed document {ProcessedCount}: {FileName} ({ChunkCount} chunks)", 
                            processedCount, document.FileName, chunks.Count);
                    }
                    else
                    {
                        _logger.LogError("Failed to index document: {FileName}", document.FileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process document: {FileName}", document.FileName);
                }
            }

            _logger.LogInformation("Successfully processed {ProcessedCount} documents from directory: {DirectoryPath}", 
                processedCount, directoryPath);
            
            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process directory: {DirectoryPath}", directoryPath);
            return 0;
        }
    }

    public async Task<long> GetIndexedDocumentCountAsync()
    {
        return await _elasticsearchService.GetDocumentCountAsync(_settings.IndexName);
    }
}
