using RAG.Orchestrator.Api.Features.Search;
using System.Text.Json;
using System.Text;
using Elasticsearch.Net;
using Microsoft.Extensions.Options;
using System.IO;

namespace RAG.Orchestrator.Api.Features.Search;

public class ElasticsearchUnavailableException : Exception
{
    public ElasticsearchUnavailableException(string message) : base(message) { }
    public ElasticsearchUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task<DocumentDetail> GetDocumentByIdAsync(string documentId, CancellationToken cancellationToken = default);
}

public class SearchService : ISearchService
{
    private readonly IElasticLowLevelClient _client;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SearchService> _logger;
    private readonly IIndexManagementService _indexManagement;
    private readonly ElasticsearchOptions _options;
    private readonly string _indexName;

    public SearchService(
        IElasticLowLevelClient client,
        HttpClient httpClient, 
        ILogger<SearchService> logger, 
        IIndexManagementService indexManagement,
        IOptions<ElasticsearchOptions> options)
    {
        _client = client;
        _httpClient = httpClient;
        _logger = logger;
        _indexManagement = indexManagement;
        _options = options.Value;
        _indexName = _options.DefaultIndexName;
        
        // Configure HTTP client for fallback operations
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.Username}:{_options.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        _httpClient.Timeout = TimeSpan.FromMinutes(_options.TimeoutMinutes);
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting search for query: '{Query}' with limit: {Limit}", request.Query, request.Limit);
            
            // Check if index exists before searching
            var indexExists = await _indexManagement.IndexExistsAsync(_indexName, cancellationToken);
            if (!indexExists)
            {
                _logger.LogWarning("Index {IndexName} does not exist", _indexName);
                return new SearchResponse([], 0, 0, request.Query);
            }

            // Create search query - simplified for debugging
            var searchQuery = new
            {
                query = new
                {
                    query_string = new
                    {
                        query = request.Query,
                        fields = new[] { "content" },
                        default_operator = "AND"
                    }
                },
                size = request.Limit,
                from = request.Offset,
                highlight = new
                {
                    fields = new
                    {
                        content = new { }
                    },
                    pre_tags = new[] { "<em>" },
                    post_tags = new[] { "</em>" },
                    fragment_size = 200,
                    number_of_fragments = 3
                }
            };

            var json = JsonSerializer.Serialize(searchQuery);
            _logger.LogDebug("Elasticsearch query: {Query}", json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_options.Url}/{_indexName}/_search", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Elasticsearch search failed with status {StatusCode}. Error: {Error}", 
                    response.StatusCode, errorContent);
                return CreateUnavailableResponse(request.Query);
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
                
                // Map fields from ChunkDocument structure
                var sourceFile = source.TryGetProperty("sourceFile", out var sourceFileProp) ? sourceFileProp.GetString() ?? "" : "";
                var title = !string.IsNullOrEmpty(sourceFile) ? Path.GetFileName(sourceFile) : "";
                var docContent = source.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? "" : "";
                var fileExtension = source.TryGetProperty("fileExtension", out var extProp) ? extProp.GetString() ?? "" : "";
                var documentType = !string.IsNullOrEmpty(fileExtension) ? fileExtension.TrimStart('.').ToUpperInvariant() : "";
                
                // Get chunk information for better source identification
                var chunkIndex = 0;
                var totalChunks = 1;
                if (source.TryGetProperty("position", out var positionProp))
                {
                    if (positionProp.TryGetProperty("chunkIndex", out var chunkIndexProp))
                        chunkIndex = chunkIndexProp.GetInt32();
                    if (positionProp.TryGetProperty("totalChunks", out var totalChunksProp))
                        totalChunks = totalChunksProp.GetInt32();
                }
                
                var sourceWithChunk = !string.IsNullOrEmpty(sourceFile) 
                    ? $"{Path.GetFileName(sourceFile)}" + (totalChunks > 1 ? $" (chunk {chunkIndex + 1}/{totalChunks})" : "")
                    : "";
                
                var createdAt = source.TryGetProperty("indexedAt", out var indexedProp) 
                    ? DateTime.TryParse(indexedProp.GetString(), out var indexed) ? indexed : DateTime.Now 
                    : DateTime.Now;

                _logger.LogDebug("Processing search result: title={Title}, source={Source}, documentType={DocumentType}, sourceFile={SourceFile}", 
                    title, sourceWithChunk, documentType, sourceFile);

                var metadata = new Dictionary<string, object>
                {
                    { "category", documentType },
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

                // Extract file information
                var fileName = !string.IsNullOrEmpty(sourceFile) ? Path.GetFileName(sourceFile) : "";
                var filePath = !string.IsNullOrEmpty(sourceFile) ? sourceFile : "";

                results.Add(new SearchResult(
                    id, title, docContent, score, sourceWithChunk, documentType, filePath, fileName, metadata, createdAt, DateTime.Now
                ));
            }

            return new SearchResponse(results.ToArray(), totalHits, took, request.Query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Elasticsearch for query: {Query}", request.Query);
            return CreateUnavailableResponse(request.Query);
        }
    }

    private async Task CheckElasticsearchHealth(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5)); // Quick health check
            
            var response = await _httpClient.GetAsync($"{_options.Url}/", cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Elasticsearch health check failed with status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch is unavailable");
            throw new ElasticsearchUnavailableException("Document database is currently unavailable", ex);
        }
    }

    private static SearchResponse CreateUnavailableResponse(string query)
    {
        return new SearchResponse(Array.Empty<SearchResult>(), 0, 0, query);
    }

    public async Task<DocumentDetail> GetDocumentByIdAsync(string documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure index exists
            if (_options.AutoCreateIndices)
            {
                await _indexManagement.EnsureIndexExistsAsync(_indexName, cancellationToken);
            }
            
            // Check Elasticsearch availability first
            await CheckElasticsearchHealth(cancellationToken);
            
            var response = await _httpClient.GetAsync($"{_options.Url}/{_indexName}/_doc/{documentId}", cancellationToken);
            
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
                1.0, category, type, null, null, metadata, createdAt, DateTime.Now
            );
        }
        catch (ElasticsearchUnavailableException)
        {
            throw; // Re-throw to let caller handle
        }
        catch (Exception ex) when (!(ex is KeyNotFoundException))
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId} from Elasticsearch", documentId);
            throw new InvalidOperationException($"Failed to retrieve document: {ex.Message}", ex);
        }
    }
}
