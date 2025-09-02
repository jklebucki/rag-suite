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
            var indexMapping = new
            {
                mappings = new
                {
                    properties = new
                    {
                        fileName = new { type = "text", analyzer = "standard" },
                        content = new { type = "text", analyzer = "standard" },
                        fileType = new { type = "keyword" },
                        chunkIndex = new { type = "integer" },
                        documentId = new { type = "keyword" },
                        createdAt = new { type = "date" },
                        contentVector = new
                        {
                            type = "dense_vector",
                            dims = 384,
                            similarity = "cosine"
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
                                type = "standard"
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
