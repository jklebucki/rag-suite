using Elasticsearch.Net;
using System.Text.Json;

namespace RAG.Orchestrator.Api.Features.Search;

public interface IIndexManagementService
{
    Task<bool> IndexExistsAsync(string indexName, CancellationToken cancellationToken = default);
    Task CreateIndexAsync(string indexName, CancellationToken cancellationToken = default);
    Task<bool> EnsureIndexExistsAsync(string indexName, CancellationToken cancellationToken = default);
    Task<string[]> GetAvailableIndicesAsync(CancellationToken cancellationToken = default);
}

public class IndexManagementService : IIndexManagementService
{
    private readonly IElasticLowLevelClient _client;
    private readonly ILogger<IndexManagementService> _logger;

    public IndexManagementService(IElasticLowLevelClient client, ILogger<IndexManagementService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<bool> IndexExistsAsync(string indexName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.Indices.ExistsAsync<StringResponse>(indexName);
            return response.Success && response.HttpStatusCode == 200;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if index {IndexName} exists", indexName);
            return false;
        }
    }

    public async Task CreateIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        try
        {
            // This mapping MUST stay aligned with the document written by RAG.Collector
            // (RAG.Collector/Elasticsearch/ElasticsearchService.CreateIndexMapping + ChunkDocument).
            // The Collector is the primary owner of the index; this is a consistent fallback so that
            // the admin endpoint / auto-create never produces a divergent (and broken) mapping.
            var indexMapping = new
            {
                mappings = new
                {
                    properties = new
                    {
                        id = new { type = "keyword" },
                        content = new
                        {
                            type = "text",
                            analyzer = "standard",
                            fields = new
                            {
                                folded = new { type = "text", analyzer = "rag_folded" }
                            }
                        },
                        fileName = new
                        {
                            type = "text",
                            analyzer = "rag_filename",
                            fields = new
                            {
                                keyword = new { type = "keyword", ignore_above = 1024 }
                            }
                        },
                        title = new { type = "text", analyzer = "rag_filename" },
                        embedding = new
                        {
                            type = "dense_vector",
                            dims = 768,
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
                        char_filter = new
                        {
                            filename_separators = new
                            {
                                type = "pattern_replace",
                                pattern = "[_.\\-]",
                                replacement = " "
                            }
                        },
                        analyzer = new
                        {
                            rag_folded = new
                            {
                                type = "custom",
                                tokenizer = "standard",
                                filter = new[] { "lowercase", "asciifolding" }
                            },
                            rag_filename = new
                            {
                                type = "custom",
                                char_filter = new[] { "filename_separators" },
                                tokenizer = "standard",
                                filter = new[] { "lowercase", "asciifolding" }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(indexMapping);
            var response = await _client.Indices.CreateAsync<StringResponse>(indexName,
                PostData.String(json));

            if (!response.Success)
            {
                var errorMessage = response.DebugInformation ?? "Unknown error";
                throw new InvalidOperationException($"Failed to create index {indexName}: {errorMessage}");
            }

            _logger.LogInformation("Successfully created index: {IndexName}", indexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating index {IndexName}", indexName);
            throw;
        }
    }

    public async Task<bool> EnsureIndexExistsAsync(string indexName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await IndexExistsAsync(indexName, cancellationToken))
            {
                _logger.LogDebug("Index {IndexName} already exists", indexName);
                return true;
            }

            _logger.LogInformation("Creating missing index: {IndexName}", indexName);
            await CreateIndexAsync(indexName, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure index {IndexName} exists", indexName);
            return false;
        }
    }

    public async Task<string[]> GetAvailableIndicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.Indices.GetAsync<StringResponse>("_all");

            if (!response.Success)
            {
                _logger.LogWarning("Failed to get available indices: {Error}", response.DebugInformation);
                return Array.Empty<string>();
            }

            using var doc = JsonDocument.Parse(response.Body);
            var indices = new List<string>();

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                // Filter out system indices (starting with .)
                if (!property.Name.StartsWith('.'))
                {
                    indices.Add(property.Name);
                }
            }

            return indices.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available indices");
            return Array.Empty<string>();
        }
    }
}
