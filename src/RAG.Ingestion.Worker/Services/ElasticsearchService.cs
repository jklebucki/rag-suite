using Elastic.Clients.Elasticsearch;
using RAG.Ingestion.Worker.Models;

namespace RAG.Ingestion.Worker.Services;

public interface IElasticsearchService
{
    Task<bool> IndexExistsAsync(string indexName);
    Task<bool> CreateIndexAsync(string indexName);
    Task<bool> IndexDocumentAsync(DocumentChunk document, string indexName);
    Task<bool> IndexDocumentsAsync(IEnumerable<DocumentChunk> documents, string indexName);
    Task<bool> DeleteIndexAsync(string indexName);
    Task<long> GetDocumentCountAsync(string indexName);
}

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;

    public ElasticsearchService(ElasticsearchClient client, ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<bool> IndexExistsAsync(string indexName)
    {
        try
        {
            var response = await _client.Indices.ExistsAsync(indexName);
            return response.Exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if index {IndexName} exists", indexName);
            return false;
        }
    }

    public async Task<bool> CreateIndexAsync(string indexName)
    {
        try
        {
            var response = await _client.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(0)
                )
                .Mappings(m => m
                    .Properties<DocumentChunk>(p => p
                        .Keyword(k => k.Id)
                        .Keyword(k => k.DocumentId)
                        .Text(t => t.FileName, t => t.Analyzer("standard"))
                        .Text(t => t.Content, t => t.Analyzer("standard"))
                        .Keyword(k => k.FileType)
                        .IntegerNumber(n => n.ChunkIndex)
                        .DenseVector(d => d.Embedding, d => d.Dims(768)) // Match embedding dimension
                        .Date(d => d.CreatedAt)
                        .Object(o => o.Metadata, o => o.Enabled(false))
                    )
                )
            );

            if (response.IsValidResponse)
            {
                _logger.LogInformation("Successfully created index: {IndexName}", indexName);
                return true;
            }
            else
            {
                _logger.LogError("Failed to create index {IndexName}: {Error}", indexName, response.DebugInformation);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create index: {IndexName}", indexName);
            return false;
        }
    }

    public async Task<bool> IndexDocumentAsync(DocumentChunk document, string indexName)
    {
        try
        {
            var response = await _client.IndexAsync(document, i => i
                .Index(indexName)
                .Id(document.Id)
            );

            if (response.IsValidResponse)
            {
                _logger.LogDebug("Successfully indexed document: {DocumentId}", document.Id);
                return true;
            }
            else
            {
                _logger.LogError("Failed to index document {DocumentId}: {Error}", document.Id, response.DebugInformation);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index document: {DocumentId}", document.Id);
            return false;
        }
    }

    public async Task<bool> IndexDocumentsAsync(IEnumerable<DocumentChunk> documents, string indexName)
    {
        try
        {
            var response = await _client.BulkAsync(b => b
                .Index(indexName)
                .IndexMany(documents, (desc, doc) => desc.Id(doc.Id))
            );

            if (response.IsValidResponse)
            {
                _logger.LogInformation("Successfully bulk indexed {Count} documents", documents.Count());
                return true;
            }
            else
            {
                _logger.LogError("Failed to bulk index documents: {Error}", response.DebugInformation);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk index documents");
            return false;
        }
    }

    public async Task<bool> DeleteIndexAsync(string indexName)
    {
        try
        {
            var response = await _client.Indices.DeleteAsync(indexName);

            if (response.IsValidResponse)
            {
                _logger.LogInformation("Successfully deleted index: {IndexName}", indexName);
                return true;
            }
            else
            {
                _logger.LogError("Failed to delete index {IndexName}: {Error}", indexName, response.DebugInformation);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete index: {IndexName}", indexName);
            return false;
        }
    }

    public async Task<long> GetDocumentCountAsync(string indexName)
    {
        try
        {
            var response = await _client.CountAsync<DocumentChunk>(c => c.Indices(indexName));
            return response.IsValidResponse ? response.Count : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document count for index: {IndexName}", indexName);
            return 0;
        }
    }
}
