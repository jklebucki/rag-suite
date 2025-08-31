using RAG.Application.Commands.SendMessage;
using RAG.Domain.Entities;

namespace RAG.Infrastructure.Persistence;

/// <summary>
/// In-memory implementation for development and testing
/// </summary>
public class InMemoryChatSessionRepository : IChatSessionRepository
{
    private static readonly Dictionary<string, ChatSession> _sessions = new();
    private readonly ILogger<InMemoryChatSessionRepository> _logger;

    public InMemoryChatSessionRepository(ILogger<InMemoryChatSessionRepository> logger)
    {
        _logger = logger;
    }

    public Task<ChatSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting session {SessionId}", id);
        
        _sessions.TryGetValue(id, out var session);
        return Task.FromResult(session);
    }

    public Task<ChatSession> CreateAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating session {SessionId}", session.Id);
        
        _sessions[session.Id] = session;
        return Task.FromResult(session);
    }

    public Task UpdateAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating session {SessionId}", session.Id);
        
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting session {SessionId}", id);
        
        _sessions.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ChatSession>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all sessions");
        
        return Task.FromResult(_sessions.Values.AsEnumerable());
    }
}

/// <summary>
/// Elasticsearch service adapter for existing implementation
/// </summary>
public class ElasticsearchServiceAdapter : RAG.Application.Plugins.IElasticsearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ElasticsearchServiceAdapter> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _esUrl;
    private readonly string _indexName = "rag_documents";

    public ElasticsearchServiceAdapter(
        HttpClient httpClient, 
        ILogger<ElasticsearchServiceAdapter> logger, 
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _esUrl = configuration["Services:Elasticsearch:Url"] ?? "http://localhost:9200";
        
        // Configure authentication
        var username = configuration["Services:Elasticsearch:Username"] ?? "elastic";
        var password = configuration["Services:Elasticsearch:Password"] ?? "changeme";
        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task<IEnumerable<RAG.Application.Services.SearchResultDto>> SearchAsync(
        RAG.Application.Plugins.SearchRequest request)
    {
        try
        {
            var searchQuery = new
            {
                query = new
                {
                    multi_match = new
                    {
                        query = request.Query,
                        fields = new[] { "fileName^2", "content", "fileType" },
                        type = "best_fields",
                        fuzziness = "AUTO"
                    }
                },
                size = request.Limit,
                _source = new[] { "fileName", "content", "fileType", "chunkIndex", "documentId", "createdAt" }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(searchQuery);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_esUrl}/{_indexName}/_search", content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Elasticsearch search failed with status {StatusCode}", response.StatusCode);
                return Array.Empty<RAG.Application.Services.SearchResultDto>();
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(responseJson);
            
            var hits = doc.RootElement.GetProperty("hits");
            var results = new List<RAG.Application.Services.SearchResultDto>();
            
            foreach (var hit in hits.GetProperty("hits").EnumerateArray())
            {
                var source = hit.GetProperty("_source");
                var score = hit.GetProperty("_score").GetSingle();
                var id = hit.GetProperty("_id").GetString() ?? "";
                
                var title = source.TryGetProperty("fileName", out var fileNameProp) ? fileNameProp.GetString() ?? "" : "";
                var docContent = source.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? "" : "";
                var category = source.TryGetProperty("fileType", out var fileTypeProp) ? fileTypeProp.GetString() ?? "" : "";
                
                var createdAt = source.TryGetProperty("createdAt", out var createdProp) 
                    ? DateTime.TryParse(createdProp.GetString(), out var created) ? created : DateTime.Now 
                    : DateTime.Now;

                results.Add(new RAG.Application.Services.SearchResultDto
                {
                    Id = id,
                    Title = title,
                    Content = docContent,
                    Score = score,
                    Category = category,
                    CreatedAt = createdAt
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Elasticsearch for query: {Query}", request.Query);
            return Array.Empty<RAG.Application.Services.SearchResultDto>();
        }
    }

    public async Task<RAG.Application.Plugins.DocumentDetail> GetDocumentByIdAsync(string id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_esUrl}/{_indexName}/_doc/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                throw new KeyNotFoundException($"Document with ID '{id}' not found");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(responseJson);
            
            var source = doc.RootElement.GetProperty("_source");
            
            var title = source.TryGetProperty("fileName", out var fileNameProp) ? fileNameProp.GetString() ?? "" : "";
            var content = source.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? "" : "";
            var category = source.TryGetProperty("fileType", out var fileTypeProp) ? fileTypeProp.GetString() ?? "" : "";
            
            var createdAt = source.TryGetProperty("createdAt", out var createdProp) 
                ? DateTime.TryParse(createdProp.GetString(), out var created) ? created : DateTime.Now 
                : DateTime.Now;

            return new RAG.Application.Plugins.DocumentDetail
            {
                Id = id,
                Title = title,
                Content = content,
                Category = category,
                CreatedAt = createdAt
            };
        }
        catch (Exception ex) when (!(ex is KeyNotFoundException))
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId} from Elasticsearch", id);
            throw new InvalidOperationException($"Failed to retrieve document: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync(string type)
    {
        try
        {
            var aggregationQuery = new
            {
                size = 0,
                aggs = new
                {
                    categories = new
                    {
                        terms = new
                        {
                            field = "fileType.keyword",
                            size = 100
                        }
                    }
                },
                query = new
                {
                    term = new
                    {
                        fileType = type
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(aggregationQuery);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_esUrl}/{_indexName}/_search", content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Elasticsearch aggregation failed with status {StatusCode}", response.StatusCode);
                return Array.Empty<string>();
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(responseJson);
            
            var categories = new List<string>();
            
            if (doc.RootElement.TryGetProperty("aggregations", out var aggs) &&
                aggs.TryGetProperty("categories", out var categoriesAgg) &&
                categoriesAgg.TryGetProperty("buckets", out var buckets))
            {
                foreach (var bucket in buckets.EnumerateArray())
                {
                    if (bucket.TryGetProperty("key", out var key))
                    {
                        var category = key.GetString();
                        if (!string.IsNullOrEmpty(category))
                        {
                            categories.Add(category);
                        }
                    }
                }
            }

            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories for type: {Type}", type);
            return Array.Empty<string>();
        }
    }
}
