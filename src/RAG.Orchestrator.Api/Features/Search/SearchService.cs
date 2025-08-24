using RAG.Orchestrator.Api.Features.Search;
using System.Text.Json;
using System.Text;

namespace RAG.Orchestrator.Api.Features.Search;

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task<DocumentDetail> GetDocumentByIdAsync(string documentId, CancellationToken cancellationToken = default);
}

public class SearchService : ISearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SearchService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _esUrl;
    private readonly string _esUsername;
    private readonly string _esPassword;
    private readonly string _indexName = "rag_documents";

    public SearchService(HttpClient httpClient, ILogger<SearchService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _esUrl = configuration["Services:Elasticsearch:Url"] ?? "http://localhost:9200";
        _esUsername = configuration["Services:Elasticsearch:Username"] ?? "elastic";
        _esPassword = configuration["Services:Elasticsearch:Password"] ?? "changeme";
        
        // Configure HTTP client for Elasticsearch
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_esUsername}:{_esPassword}"));
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
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
                from = request.Offset,
                _source = new[] { "fileName", "content", "fileType", "chunkIndex", "documentId", "createdAt" },
                highlight = new
                {
                    fields = new
                    {
                        fileName = new { },
                        content = new { fragment_size = 150, number_of_fragments = 3 }
                    }
                }
            };

            var json = JsonSerializer.Serialize(searchQuery);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_esUrl}/{_indexName}/_search", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Elasticsearch search failed with status {StatusCode}", response.StatusCode);
                return new SearchResponse(Array.Empty<SearchResult>(), 0, 0, request.Query);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            
            var hits = doc.RootElement.GetProperty("hits");
            var totalHits = hits.GetProperty("total").GetProperty("value").GetInt32();
            var took = doc.RootElement.GetProperty("took").GetInt32();
            
            var results = new List<SearchResult>();
            
            foreach (var hit in hits.GetProperty("hits").EnumerateArray())
            {
                var source = hit.GetProperty("_source");
                var score = hit.GetProperty("_score").GetSingle();
                var id = hit.GetProperty("_id").GetString() ?? "";
                
                var title = source.TryGetProperty("fileName", out var fileNameProp) ? fileNameProp.GetString() ?? "" : "";
                var docContent = source.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? "" : "";
                var category = source.TryGetProperty("fileType", out var fileTypeProp) ? fileTypeProp.GetString() ?? "" : "";
                var type = source.TryGetProperty("chunkIndex", out var chunkProp) ? $"Chunk {chunkProp.GetInt32()}" : "";
                
                var createdAt = source.TryGetProperty("createdAt", out var createdProp) 
                    ? DateTime.TryParse(createdProp.GetString(), out var created) ? created : DateTime.Now 
                    : DateTime.Now;

                var metadata = new Dictionary<string, object>
                {
                    { "category", category },
                    { "score", score },
                    { "index", _indexName }
                };

                // Add highlights if available
                if (hit.TryGetProperty("highlight", out var highlight))
                {
                    var highlightText = new List<string>();
                    if (highlight.TryGetProperty("content", out var contentHighlight))
                    {
                        foreach (var fragment in contentHighlight.EnumerateArray())
                        {
                            highlightText.Add(fragment.GetString() ?? "");
                        }
                    }
                    if (highlightText.Any())
                    {
                        metadata["highlights"] = string.Join(" ... ", highlightText);
                    }
                }

                results.Add(new SearchResult(
                    id, title, docContent, score, category, type, metadata, createdAt, DateTime.Now
                ));
            }

            return new SearchResponse(results.ToArray(), totalHits, took, request.Query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Elasticsearch for query: {Query}", request.Query);
            return new SearchResponse(Array.Empty<SearchResult>(), 0, 0, request.Query);
        }
    }

    public async Task<DocumentDetail> GetDocumentByIdAsync(string documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_esUrl}/{_indexName}/_doc/{documentId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException($"Document with ID '{documentId}' not found");
                }
                _logger.LogError("Elasticsearch get document failed with status {StatusCode}", response.StatusCode);
                throw new InvalidOperationException($"Failed to retrieve document: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            
            var source = doc.RootElement.GetProperty("_source");
            var id = doc.RootElement.GetProperty("_id").GetString() ?? documentId;
            
            var title = source.TryGetProperty("fileName", out var fileNameProp) ? fileNameProp.GetString() ?? "" : "";
            var content = source.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? "" : "";
            var category = source.TryGetProperty("fileType", out var fileTypeProp) ? fileTypeProp.GetString() ?? "" : "";
            var type = source.TryGetProperty("chunkIndex", out var chunkProp) ? $"Chunk {chunkProp.GetInt32()}" : "";
            
            var createdAt = source.TryGetProperty("createdAt", out var createdProp) 
                ? DateTime.TryParse(createdProp.GetString(), out var created) ? created : DateTime.Now 
                : DateTime.Now;

            var metadata = new Dictionary<string, object>
            {
                { "category", category },
                { "index", _indexName },
                { "document_id", id }
            };

            // Add all source fields to metadata
            foreach (var property in source.EnumerateObject())
            {
                if (property.Name != "fileName" && property.Name != "content" && property.Name != "createdAt")
                {
                    metadata[property.Name] = property.Value.ToString();
                }
            }

            return new DocumentDetail(
                id, title, content, content, // Using content as both summary and full content
                1.0, category, type, metadata, createdAt, DateTime.Now
            );
        }
        catch (Exception ex) when (!(ex is KeyNotFoundException))
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId} from Elasticsearch", documentId);
            throw new InvalidOperationException($"Failed to retrieve document: {ex.Message}", ex);
        }
    }
}
