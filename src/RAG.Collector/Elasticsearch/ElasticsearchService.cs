using System.Text;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RAG.Collector.Config;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RAG.Collector.Elasticsearch;

/// <summary>
/// Elasticsearch service implementation using Elasticsearch.Net
/// </summary>
public class ElasticsearchService : IElasticsearchService
{
    private readonly IElasticLowLevelClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly CollectorOptions _options;
    private readonly string _indexName;

    public ElasticsearchService(
        IElasticLowLevelClient client,
        ILogger<ElasticsearchService> logger,
        IOptions<CollectorOptions> options)
    {
        _client = client;
        _logger = logger;
        _options = options.Value;
        _indexName = _options.IndexName;
    }

    public async Task<bool> IndexDocumentAsync(ChunkDocument document, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(document, GetJsonOptions());
            var response = await _client.IndexAsync<StringResponse>(
                _indexName,
                document.Id,
                PostData.String(json));

            if (response.Success)
            {
                _logger.LogDebug("Successfully indexed document {DocumentId}", document.Id);
                return true;
            }

            _logger.LogError("Failed to index document {DocumentId}: {Error}", 
                document.Id, response.DebugInformation);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document {DocumentId}", document.Id);
            return false;
        }
    }

    public async Task<int> IndexDocumentsBatchAsync(IList<ChunkDocument> documents, CancellationToken cancellationToken = default)
    {
        if (!documents.Any())
            return 0;

        try
        {
            var bulkBody = new StringBuilder();
            
            foreach (var document in documents)
            {
                // Index action
                var indexAction = new
                {
                    index = new
                    {
                        _index = _indexName,
                        _id = document.Id
                    }
                };
                
                bulkBody.AppendLine(JsonSerializer.Serialize(indexAction, GetJsonOptions()));
                bulkBody.AppendLine(JsonSerializer.Serialize(document, GetJsonOptions()));
            }

            var response = await _client.BulkAsync<StringResponse>(
                PostData.String(bulkBody.ToString()));

            if (response.Success)
            {
                try
                {
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var bulkResponse = JsonSerializer.Deserialize<BulkResponse>(response.Body, jsonOptions);
                    var successCount = CountSuccessfulOperations(bulkResponse);
                    
                    if (successCount > 0)
                    {
                        _logger.LogInformation("Successfully indexed {SuccessCount}/{TotalCount} documents in batch", 
                            successCount, documents.Count);
                    }
                    else
                    {
                        _logger.LogWarning("Bulk indexing completed but no documents were successfully indexed ({TotalCount} attempted)", 
                            documents.Count);
                        
                        // Log detailed response for debugging
                        _logger.LogWarning("Bulk response: {Response}", response.Body);
                    }
                    
                    return successCount;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse bulk response, assuming all {Count} documents were indexed", documents.Count);
                    // If parsing fails but response was successful, assume all documents were indexed
                    return documents.Count;
                }
            }

            _logger.LogError("Bulk indexing failed: {Error}", response.DebugInformation);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk indexing of {DocumentCount} documents", documents.Count);
            return 0;
        }
    }

    public async Task<bool> EnsureIndexExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if index exists
            var existsResponse = await _client.Indices.ExistsAsync<StringResponse>(_indexName);
            
            if (existsResponse.HttpStatusCode == 200)
            {
                _logger.LogDebug("Index {IndexName} already exists", _indexName);
                return true;
            }

            // Create index with mapping
            var indexMapping = CreateIndexMapping();
            var createResponse = await _client.Indices.CreateAsync<StringResponse>(
                _indexName,
                PostData.String(indexMapping));

            if (createResponse.Success)
            {
                _logger.LogInformation("Successfully created index {IndexName}", _indexName);
                return true;
            }

            _logger.LogError("Failed to create index {IndexName}: {Error}", 
                _indexName, createResponse.DebugInformation);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring index {IndexName} exists", _indexName);
            return false;
        }
    }

    public async Task<int> DeleteDocumentsBySourceFileAsync(string sourceFile, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new
            {
                query = new
                {
                    term = new
                    {
                        sourceFile = new { value = sourceFile }
                    }
                }
            };

            var json = JsonSerializer.Serialize(query, GetJsonOptions());
            var response = await _client.DeleteByQueryAsync<StringResponse>(
                _indexName,
                PostData.String(json));

            if (response.Success)
            {
                var deleteResponse = JsonSerializer.Deserialize<DeleteByQueryResponse>(response.Body);
                var deletedCount = deleteResponse?.Deleted ?? 0;
                
                _logger.LogInformation("Deleted {DeletedCount} documents for source file {SourceFile}", 
                    deletedCount, sourceFile);
                
                return deletedCount;
            }

            _logger.LogError("Failed to delete documents for source file {SourceFile}: {Error}", 
                sourceFile, response.DebugInformation);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting documents for source file {SourceFile}", sourceFile);
            return 0;
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _client.PingAsync<StringResponse>();
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch availability check failed");
            return false;
        }
    }

    public async Task<IndexStats?> GetIndexStatsAsync()
    {
        try
        {
            var response = await _client.Indices.StatsAsync<StringResponse>(_indexName);
            
            if (response.Success)
            {
                var stats = JsonSerializer.Deserialize<JsonElement>(response.Body);
                var indexStats = stats.GetProperty("indices").GetProperty(_indexName);
                
                return new IndexStats
                {
                    IndexName = _indexName,
                    DocumentCount = indexStats.GetProperty("total").GetProperty("docs").GetProperty("count").GetInt64(),
                    IndexSizeBytes = indexStats.GetProperty("total").GetProperty("store").GetProperty("size_in_bytes").GetInt64(),
                    LastUpdated = DateTime.UtcNow
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get index stats for {IndexName}", _indexName);
            return null;
        }
    }

    private string CreateIndexMapping()
    {
        var mapping = new
        {
            mappings = new
            {
                properties = new
                {
                    id = new { type = "keyword" },
                    content = new { type = "text", analyzer = "standard" },
                    embedding = new
                    {
                        type = "dense_vector",
                        dims = 384, // For multilingual-e5-small
                        index = true,
                        similarity = "cosine"
                    },
                    sourceFile = new { type = "keyword" },
                    fileExtension = new { type = "keyword" },
                    fileSize = new { type = "long" },
                    lastModified = new { type = "date" },
                    position = new
                    {
                        properties = new
                        {
                            startIndex = new { type = "integer" },
                            endIndex = new { type = "integer" },
                            chunkIndex = new { type = "integer" },
                            totalChunks = new { type = "integer" },
                            page = new { type = "integer" },
                            section = new { type = "keyword" }
                        }
                    },
                    metadata = new { type = "object", enabled = true },
                    aclGroups = new { type = "keyword" },
                    contentHash = new { type = "keyword" },
                    indexedAt = new { type = "date" },
                    estimatedTokens = new { type = "integer" },
                    embeddingDetails = new
                    {
                        properties = new
                        {
                            modelName = new { type = "keyword" },
                            dimensions = new { type = "integer" },
                            generatedAt = new { type = "date" }
                        }
                    }
                }
            },
            settings = new
            {
                number_of_shards = 1,
                number_of_replicas = 0,
                analysis = new
                {
                    analyzer = new
                    {
                        standard = new
                        {
                            type = "standard",
                            stopwords = "_english_"
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(mapping, GetJsonOptions());
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    private static int CountSuccessfulOperations(BulkResponse? bulkResponse)
    {
        if (bulkResponse?.Items == null || !bulkResponse.Items.Any())
            return 0;

        var successCount = 0;
        foreach (var item in bulkResponse.Items)
        {
            if (item.Index?.Status != null)
            {
                // HTTP status codes 200-299 indicate success
                if (item.Index.Status >= 200 && item.Index.Status < 300)
                {
                    successCount++;
                }
            }
            else if (item.Create?.Status != null)
            {
                if (item.Create.Status >= 200 && item.Create.Status < 300)
                {
                    successCount++;
                }
            }
            else if (item.Update?.Status != null)
            {
                if (item.Update.Status >= 200 && item.Update.Status < 300)
                {
                    successCount++;
                }
            }
        }

        return successCount;
    }

    // Response models for JSON deserialization
    private class BulkResponse
    {
        public bool? Errors { get; set; }
        public List<BulkResponseItem> Items { get; set; } = new();
    }

    private class BulkResponseItem
    {
        public BulkOperationResponse? Index { get; set; }
        public BulkOperationResponse? Create { get; set; }
        public BulkOperationResponse? Update { get; set; }
        public BulkOperationResponse? Delete { get; set; }
    }

    private class BulkOperationResponse
    {
        [JsonPropertyName("_id")]
        public string? _Id { get; set; }
        
        [JsonPropertyName("_index")]
        public string? _Index { get; set; }
        
        [JsonPropertyName("status")]
        public int Status { get; set; }
        
        [JsonPropertyName("error")]
        public object? Error { get; set; }
        
        [JsonPropertyName("result")]
        public string? Result { get; set; }
    }

    private class DeleteByQueryResponse
    {
        public int Deleted { get; set; }
    }
}
