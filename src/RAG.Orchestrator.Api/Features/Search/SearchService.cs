using RAG.Orchestrator.Api.Features.Search;
using RAG.Orchestrator.Api.Features.Embeddings;
using System.Text.Json;
using System.Text;
using Elasticsearch.Net;
using Microsoft.Extensions.Options;
using System.IO;

namespace RAG.Orchestrator.Api.Features.Search;

internal class ChunkInfo
{
    public string Id { get; set; } = "";
    public string Content { get; set; } = "";
    public double Score { get; set; }
    public string SourceFile { get; set; } = "";
    public int ChunkIndex { get; set; }
    public int TotalChunks { get; set; }
    public string FileExtension { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<string> Highlights { get; set; } = new();
}

public class ElasticsearchUnavailableException : Exception
{
    public ElasticsearchUnavailableException(string message) : base(message) { }
    public ElasticsearchUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task<SearchResponse> SearchHybridAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task<DocumentDetail> GetDocumentByIdAsync(string documentId, CancellationToken cancellationToken = default);
}

public class SearchService : ISearchService
{
    private readonly IElasticLowLevelClient _client;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SearchService> _logger;
    private readonly IIndexManagementService _indexManagement;
    private readonly IEmbeddingService _embeddingService;
    private readonly IQueryProcessor _queryProcessor;
    private readonly ElasticsearchOptions _options;
    private readonly string _indexName;

    public SearchService(
        IElasticLowLevelClient client,
        HttpClient httpClient, 
        ILogger<SearchService> logger, 
        IIndexManagementService indexManagement,
        IEmbeddingService embeddingService,
        IQueryProcessor queryProcessor,
        IOptions<ElasticsearchOptions> options)
    {
        _client = client;
        _httpClient = httpClient;
        _logger = logger;
        _indexManagement = indexManagement;
        _embeddingService = embeddingService;
        _queryProcessor = queryProcessor;
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

            // Try hybrid search if embedding service is available
            var embeddingAvailable = await _embeddingService.IsAvailableAsync();
            if (embeddingAvailable)
            {
                _logger.LogInformation("Using hybrid BM25 + kNN search");
                return await SearchHybridAsync(request, cancellationToken);
            }
            
            _logger.LogInformation("Embedding service not available, using traditional search");

            // Create hybrid search query for better results
            var searchQuery = new Dictionary<string, object>
            {
                ["query"] = new Dictionary<string, object>
                {
                    ["bool"] = new Dictionary<string, object>
                    {
                        ["should"] = new object[]
                        {
                            // Exact phrase match gets highest score
                            new Dictionary<string, object>
                            {
                                ["match_phrase"] = new Dictionary<string, object>
                                {
                                    ["content"] = new Dictionary<string, object>
                                    {
                                        ["query"] = request.Query,
                                        ["boost"] = 3.0
                                    }
                                }
                            },
                            // Important terms match with OR operator
                            new Dictionary<string, object>
                            {
                                ["match"] = new Dictionary<string, object>
                                {
                                    ["content"] = new Dictionary<string, object>
                                    {
                                        ["query"] = request.Query,
                                        ["operator"] = "OR",
                                        ["boost"] = 2.0,
                                        ["minimum_should_match"] = "30%" // At least 30% of terms should match
                                    }
                                }
                            },
                            // Fuzzy match for typos
                            new Dictionary<string, object>
                            {
                                ["match"] = new Dictionary<string, object>
                                {
                                    ["content"] = new Dictionary<string, object>
                                    {
                                        ["query"] = request.Query,
                                        ["operator"] = "OR",
                                        ["fuzziness"] = "AUTO",
                                        ["boost"] = 1.0
                                    }
                                }
                            }
                        },
                        ["minimum_should_match"] = 1 // At least one of the should clauses must match
                    }
                },
                ["size"] = request.Limit,
                ["from"] = request.Offset,
                ["highlight"] = new Dictionary<string, object>
                {
                    ["fields"] = new Dictionary<string, object>
                    {
                        ["content"] = new { }
                    },
                    ["pre_tags"] = new[] { "<em>" },
                    ["post_tags"] = new[] { "</em>" },
                    ["fragment_size"] = 200,
                    ["number_of_fragments"] = 3
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
            
            // Group chunks by source file to reconstruct complete documents
            var chunksByFile = new Dictionary<string, List<ChunkInfo>>();
            
            foreach (var hit in hits.GetProperty("hits").EnumerateArray())
            {
                var source = hit.GetProperty("_source");
                var score = hit.GetProperty("_score").GetSingle();
                var id = hit.GetProperty("_id").GetString() ?? "";
                
                // Map fields from ChunkDocument structure
                var sourceFile = source.TryGetProperty("sourceFile", out var sourceFileProp) ? sourceFileProp.GetString() ?? "" : "";
                var docContent = source.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? "" : "";
                var fileExtension = source.TryGetProperty("fileExtension", out var extProp) ? extProp.GetString() ?? "" : "";
                
                // Get chunk information
                var chunkIndex = 0;
                var totalChunks = 1;
                if (source.TryGetProperty("position", out var positionProp))
                {
                    if (positionProp.TryGetProperty("chunkIndex", out var chunkIndexProp))
                        chunkIndex = chunkIndexProp.GetInt32();
                    if (positionProp.TryGetProperty("totalChunks", out var totalChunksProp))
                        totalChunks = totalChunksProp.GetInt32();
                }
                
                var createdAt = source.TryGetProperty("indexedAt", out var indexedProp) 
                    ? DateTime.TryParse(indexedProp.GetString(), out var indexed) ? indexed : DateTime.Now 
                    : DateTime.Now;

                // Group chunks by source file
                if (!string.IsNullOrEmpty(sourceFile))
                {
                    if (!chunksByFile.ContainsKey(sourceFile))
                        chunksByFile[sourceFile] = new List<ChunkInfo>();
                    
                    var highlights = new List<string>();
                    if (hit.TryGetProperty("highlight", out var highlight) && 
                        highlight.TryGetProperty("content", out var contentHighlight))
                    {
                        foreach (var fragment in contentHighlight.EnumerateArray())
                        {
                            highlights.Add(fragment.GetString() ?? "");
                        }
                    }

                    chunksByFile[sourceFile].Add(new ChunkInfo
                    {
                        Id = id,
                        Content = docContent,
                        Score = score,
                        SourceFile = sourceFile,
                        ChunkIndex = chunkIndex,
                        TotalChunks = totalChunks,
                        FileExtension = fileExtension,
                        CreatedAt = createdAt,
                        Highlights = highlights
                    });
                }
            }

            // Process each file and reconstruct documents, respecting the limit
            var filesByScore = chunksByFile
                .Select(kvp => new { File = kvp.Key, Chunks = kvp.Value, MaxScore = kvp.Value.Max(c => c.Score) })
                .OrderByDescending(x => x.MaxScore)
                .Take(request.Limit);
                
            foreach (var fileGroup in filesByScore)
            {
                var reconstructedDoc = await ReconstructDocumentFromChunks(fileGroup.File, fileGroup.Chunks, cancellationToken);
                if (reconstructedDoc != null)
                {
                    results.Add(reconstructedDoc);
                }
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
            
            // First, get the chunk to find the source file
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
            var sourceFile = source.TryGetProperty("sourceFile", out var sourceFileProp) ? sourceFileProp.GetString() ?? "" : "";
            
            if (string.IsNullOrEmpty(sourceFile))
            {
                // Fallback to single chunk if sourceFile is not available
                return await GetSingleChunkAsDocumentDetail(doc, documentId, cancellationToken);
            }
            
            // Get all chunks for this document and reconstruct it
            var allChunks = await FetchAllChunksForDocument(sourceFile, cancellationToken);
            
            if (allChunks.Count == 0)
            {
                // Fallback to single chunk if no chunks found
                return await GetSingleChunkAsDocumentDetail(doc, documentId, cancellationToken);
            }
            
            // Reconstruct the document using the existing logic
            var firstChunk = allChunks.OrderBy(c => c.ChunkIndex).First();
            var reconstructedContent = string.Join("\n\n", allChunks.OrderBy(c => c.ChunkIndex).Select(c => c.Content));
            
            // Get metadata from first chunk
            var fileName = source.TryGetProperty("fileName", out var fileNameProp) ? fileNameProp.GetString() ?? "" : "";
            var fileType = firstChunk.FileExtension ?? "";
            var filePath = sourceFile;
            
            // Extract additional metadata
            var metadata = new Dictionary<string, object>
            {
                { "category", fileType },
                { "index", _indexName },
                { "document_id", documentId },
                { "source_file", sourceFile },
                { "file_path", filePath },
                { "file_extension", fileType },
                { "chunk_count", allChunks.Count },
                { "total_chunks", firstChunk.TotalChunks }
            };
            
            // Add file size and timestamps if available
            if (source.TryGetProperty("fileSize", out var fileSizeProp))
            {
                metadata["file_size"] = fileSizeProp.ToString();
            }
            
            if (source.TryGetProperty("lastModified", out var lastModifiedProp))
            {
                metadata["last_modified"] = lastModifiedProp.ToString();
            }
            
            if (source.TryGetProperty("indexedAt", out var indexedAtProp))
            {
                metadata["indexed_at"] = indexedAtProp.ToString();
            }
            
            var createdAt = source.TryGetProperty("createdAt", out var createdProp) 
                ? DateTime.TryParse(createdProp.GetString(), out var created) ? created : DateTime.Now 
                : DateTime.Now;
                
            var updatedAt = source.TryGetProperty("updatedAt", out var updatedProp) 
                ? DateTime.TryParse(updatedProp.GetString(), out var updated) ? updated : DateTime.Now 
                : DateTime.Now;

            return new DocumentDetail(
                documentId, 
                fileName, 
                reconstructedContent.Length > 500 ? reconstructedContent.Substring(0, 500) + "..." : reconstructedContent, // Summary
                reconstructedContent, // Full content
                1.0, 
                fileType, 
                "Document", 
                filePath, 
                fileName, 
                metadata, 
                createdAt, 
                updatedAt
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

    private async Task<SearchResult?> ReconstructDocumentFromChunks(string sourceFile, List<ChunkInfo> chunks, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Reconstructing document from {ChunkCount} chunks for file: {SourceFile}", chunks.Count, sourceFile);
            
            // Sort chunks by index to maintain proper order
            var sortedChunks = chunks.OrderBy(c => c.ChunkIndex).ToList();
            var firstChunk = sortedChunks.First();
            
            // Determine if we should fetch all chunks for this document
            var shouldReconstructFullDocument = ShouldReconstructFullDocument(chunks);
            
            string reconstructedContent;
            var allHighlights = new List<string>();
            var maxScore = chunks.Max(c => c.Score);
            
            if (shouldReconstructFullDocument && firstChunk.TotalChunks > 1)
            {
                // Fetch all chunks for this document to get complete context
                var allChunks = await FetchAllChunksForDocument(sourceFile, cancellationToken);
                if (allChunks.Count > 0)
                {
                    // Use the actual TotalChunks from fetched chunks (they should all be the same for one document)
                    var expectedChunks = allChunks.FirstOrDefault()?.TotalChunks ?? firstChunk.TotalChunks;
                    var actualChunks = allChunks.Count;
                    
                    // Log discrepancy if TotalChunks values don't match
                    if (firstChunk.TotalChunks != expectedChunks)
                    {
                        _logger.LogWarning("TotalChunks mismatch for {SourceFile}: original chunk says {OriginalTotal}, fetched chunks say {FetchedTotal}, actual count: {ActualCount}", 
                            sourceFile, firstChunk.TotalChunks, expectedChunks, actualChunks);
                    }
                    
                    if (actualChunks >= expectedChunks * _options.MinimumChunkCompleteness) // Use configurable threshold
                    {
                        // Filter out chunks with empty content before reconstruction
                        var validChunks = allChunks.Where(c => !string.IsNullOrWhiteSpace(c.Content)).ToList();
                        
                        if (validChunks.Count == 0)
                        {
                            _logger.LogWarning("All fetched chunks for {SourceFile} have empty content! Falling back to matching chunks.", sourceFile);
                            reconstructedContent = string.Join("\n\n", sortedChunks.Select(c => c.Content));
                        }
                        else
                        {
                            reconstructedContent = string.Join("\n\n", validChunks.OrderBy(c => c.ChunkIndex).Select(c => c.Content));
                            
                            if (validChunks.Count < allChunks.Count)
                            {
                                _logger.LogWarning("Filtered out {EmptyCount} empty chunks from {TotalCount} for {SourceFile}", 
                                    allChunks.Count - validChunks.Count, allChunks.Count, sourceFile);
                            }
                        }
                        
                        allHighlights.AddRange(chunks.SelectMany(c => c.Highlights)); // Keep only highlights from matching chunks
                        
                        _logger.LogInformation("Successfully reconstructed full document with {ActualChunks}/{ExpectedChunks} chunks for {SourceFile}. Content length: {ContentLength} characters", 
                            actualChunks, expectedChunks, sourceFile, reconstructedContent.Length);
                            
                        // Additional debug info for troubleshooting
                        if (reconstructedContent.Length == 0)
                        {
                            _logger.LogWarning("WARNING: Reconstructed content is empty! Chunks found: {ChunkCount}, First chunk content length: {FirstChunkLength}", 
                                allChunks.Count, allChunks.FirstOrDefault()?.Content?.Length ?? 0);
                        }
                        else if (reconstructedContent.Length < 100)
                        {
                            _logger.LogWarning("WARNING: Reconstructed content is very short ({Length} chars): {Content}", 
                                reconstructedContent.Length, reconstructedContent.Substring(0, Math.Min(100, reconstructedContent.Length)));
                        }
                    }
                    else
                    {
                        // Too many missing chunks, fall back to found chunks only
                        reconstructedContent = string.Join("\n\n", sortedChunks.Select(c => c.Content));
                        allHighlights.AddRange(chunks.SelectMany(c => c.Highlights));
                        _logger.LogWarning("Document {SourceFile} has too many missing chunks ({ActualChunks}/{ExpectedChunks}), using only matching chunks", 
                            sourceFile, actualChunks, expectedChunks);
                    }
                }
                else
                {
                    // Fallback to available chunks
                    reconstructedContent = string.Join("\n\n", sortedChunks.Select(c => c.Content));
                    allHighlights.AddRange(chunks.SelectMany(c => c.Highlights));
                    _logger.LogWarning("Could not fetch any chunks for document {SourceFile}, using only matching chunks", sourceFile);
                }
            }
            else
            {
                // Use only the found chunks
                reconstructedContent = string.Join("\n\n", sortedChunks.Select(c => c.Content));
                allHighlights.AddRange(chunks.SelectMany(c => c.Highlights));
            }

            var title = !string.IsNullOrEmpty(sourceFile) ? Path.GetFileName(sourceFile) : "";
            var fileExtension = firstChunk.FileExtension;
            var documentType = !string.IsNullOrEmpty(fileExtension) ? fileExtension.TrimStart('.').ToUpperInvariant() : "";
            var fileName = Path.GetFileName(sourceFile);
            
            // Create source description
            var sourceDescription = chunks.Count == 1 
                ? $"{fileName}"
                : $"{fileName} ({chunks.Count} chunks)";
                
            if (shouldReconstructFullDocument && firstChunk.TotalChunks > chunks.Count)
            {
                sourceDescription += $" [reconstructed from {firstChunk.TotalChunks} total chunks]";
            }

            var metadata = new Dictionary<string, object>
            {
                { "category", documentType },
                { "score", maxScore },
                { "index", _indexName },
                { "chunksFound", chunks.Count },
                { "totalChunks", firstChunk.TotalChunks },
                { "reconstructed", shouldReconstructFullDocument }
            };

            if (allHighlights.Any())
            {
                metadata["highlights"] = string.Join(" ... ", allHighlights.Take(5)); // Limit highlights
            }

            return new SearchResult(
                firstChunk.Id,
                title,
                reconstructedContent,
                maxScore,
                sourceDescription,
                documentType,
                sourceFile,
                fileName,
                metadata,
                firstChunk.CreatedAt,
                DateTime.Now
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconstructing document from chunks for file: {SourceFile}", sourceFile);
            return null;
        }
    }

    private bool ShouldReconstructFullDocument(List<ChunkInfo> chunks)
    {
        // Check configuration setting
        if (!_options.AlwaysReconstructFullDocuments)
        {
            _logger.LogDebug("Document reconstruction disabled in configuration");
            return false;
        }
        
        // Always reconstruct if we have multiple chunks from the same document
        if (chunks.Count > 1)
        {
            _logger.LogDebug("Will reconstruct document: multiple matching chunks ({ChunkCount})", chunks.Count);
            return true;
        }
        
        // Reconstruct if any chunk indicates it's part of a larger document
        var firstChunk = chunks.First();
        if (firstChunk.TotalChunks > 1)
        {
            _logger.LogDebug("Will reconstruct document: chunk {ChunkIndex}/{TotalChunks} indicates multi-chunk document", 
                firstChunk.ChunkIndex, firstChunk.TotalChunks);
            return true;
        }
        
        // Don't reconstruct for single-chunk documents
        _logger.LogDebug("Will not reconstruct: single chunk document");
        return false;
    }

    private async Task<List<ChunkInfo>> FetchAllChunksForDocument(string sourceFile, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fetching all chunks for document: {SourceFile}", sourceFile);
            
            // Use exact match query for sourceFile and sort by chunkIndex
            var searchQuery = new Dictionary<string, object>
            {
                ["query"] = new Dictionary<string, object>
                {
                    ["term"] = new Dictionary<string, object>
                    {
                        ["sourceFile.keyword"] = sourceFile // Use .keyword for exact match
                    }
                },
                ["size"] = _options.MaxChunksPerDocument, // Use configurable size for large documents
                ["sort"] = new[] 
                { 
                    new Dictionary<string, object> 
                    { 
                        ["position.chunkIndex"] = new { order = "asc" } 
                    } 
                },
                ["_source"] = new[] { "content", "position", "sourceFile", "fileExtension", "indexedAt" }
            };

            var json = JsonSerializer.Serialize(searchQuery);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Fetching all chunks query for {SourceFile}: {Query}", sourceFile, json);

            var response = await _httpClient.PostAsync($"{_options.Url}/{_indexName}/_search", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to fetch all chunks for document {SourceFile}. Status: {StatusCode}. Error: {Error}", 
                    sourceFile, response.StatusCode, errorContent);
                
                // Try alternative query without .keyword suffix if first attempt failed
                if (errorContent.Contains("field [sourceFile.keyword]"))
                {
                    _logger.LogInformation("Retrying fetch with alternative query (without .keyword) for {SourceFile}", sourceFile);
                    
                    var alternativeQuery = new Dictionary<string, object>
                    {
                        ["query"] = new Dictionary<string, object>
                        {
                            ["term"] = new Dictionary<string, object>
                            {
                                ["sourceFile"] = sourceFile // Without .keyword
                            }
                        },
                        ["size"] = _options.MaxChunksPerDocument,
                        ["sort"] = new[] 
                        { 
                            new Dictionary<string, object> 
                            { 
                                ["position.chunkIndex"] = new { order = "asc" } 
                            } 
                        },
                        ["_source"] = new[] { "content", "position", "sourceFile", "fileExtension", "indexedAt" }
                    };
                    
                    var altJson = JsonSerializer.Serialize(alternativeQuery);
                    var altContent = new StringContent(altJson, Encoding.UTF8, "application/json");
                    _logger.LogDebug("Alternative query for {SourceFile}: {Query}", sourceFile, altJson);
                    
                    response = await _httpClient.PostAsync($"{_options.Url}/{_indexName}/_search", altContent, cancellationToken);
                }
                
                if (!response.IsSuccessStatusCode)
                {
                    var finalErrorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Final attempt failed for document {SourceFile}. Status: {StatusCode}. Error: {Error}", 
                        sourceFile, response.StatusCode, finalErrorContent);
                    return new List<ChunkInfo>();
                }
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            
            var hits = doc.RootElement.GetProperty("hits").GetProperty("hits");
            var totalFound = doc.RootElement.GetProperty("hits").GetProperty("total").GetProperty("value").GetInt32();
            var allChunks = new List<ChunkInfo>();
            
            foreach (var hit in hits.EnumerateArray())
            {
                var source = hit.GetProperty("_source");
                var chunkContent = source.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? "" : "";
                
                // Debug empty content
                if (string.IsNullOrEmpty(chunkContent))
                {
                    _logger.LogDebug("Empty content found in chunk {ChunkId} from document {SourceFile}. Available fields: {Fields}", 
                        hit.GetProperty("_id").GetString() ?? "unknown", sourceFile, 
                        string.Join(", ", source.EnumerateObject().Select(p => p.Name)));
                }
                
                var chunkIndex = 0;
                var totalChunks = 1;
                if (source.TryGetProperty("position", out var positionProp))
                {
                    if (positionProp.TryGetProperty("chunkIndex", out var chunkIndexProp))
                        chunkIndex = chunkIndexProp.GetInt32();
                    if (positionProp.TryGetProperty("totalChunks", out var totalChunksProp))
                        totalChunks = totalChunksProp.GetInt32();
                }

                var createdAt = source.TryGetProperty("indexedAt", out var indexedProp) 
                    ? DateTime.TryParse(indexedProp.GetString(), out var indexed) ? indexed : DateTime.Now 
                    : DateTime.Now;

                allChunks.Add(new ChunkInfo
                {
                    Content = chunkContent,
                    ChunkIndex = chunkIndex,
                    TotalChunks = totalChunks,
                    SourceFile = sourceFile,
                    CreatedAt = createdAt
                });
            }

            _logger.LogInformation("Successfully fetched {ChunkCount}/{TotalFound} chunks for document {SourceFile}", 
                allChunks.Count, totalFound, sourceFile);
            
            // Debug chunk content lengths
            var chunkContentLengths = allChunks.Select(c => c.Content?.Length ?? 0).ToList();
            var totalContentLength = chunkContentLengths.Sum();
            var emptyChunks = chunkContentLengths.Count(l => l == 0);
            
            _logger.LogDebug("Chunk content analysis - Total content length: {TotalLength}, Empty chunks: {EmptyCount}/{TotalCount}, Avg chunk size: {AvgSize}", 
                totalContentLength, emptyChunks, allChunks.Count, chunkContentLengths.Count > 0 ? chunkContentLengths.Average() : 0);
                
            if (emptyChunks > allChunks.Count * 0.1) // More than 10% empty chunks
            {
                _logger.LogWarning("High number of empty chunks detected: {EmptyCount}/{TotalCount} for document {SourceFile}", 
                    emptyChunks, allChunks.Count, sourceFile);
            }
            
            // Validate chunk sequence completeness
            if (allChunks.Count > 1)
            {
                var sortedChunks = allChunks.OrderBy(c => c.ChunkIndex).ToList();
                var expectedTotalChunks = sortedChunks.First().TotalChunks;
                var missingChunks = new List<int>();
                
                for (int i = 0; i < expectedTotalChunks; i++)
                {
                    if (!sortedChunks.Any(c => c.ChunkIndex == i))
                    {
                        missingChunks.Add(i);
                    }
                }
                
                if (missingChunks.Any())
                {
                    _logger.LogWarning("Document {SourceFile} is missing chunks: {MissingChunks}. Expected {ExpectedTotal}, found {ActualCount}", 
                        sourceFile, string.Join(", ", missingChunks), expectedTotalChunks, allChunks.Count);
                }
                else
                {
                    _logger.LogDebug("Document {SourceFile} has complete chunk sequence: {ChunkCount} chunks", 
                        sourceFile, allChunks.Count);
                }
            }
            
            return allChunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all chunks for document {SourceFile}", sourceFile);
            return new List<ChunkInfo>();
        }
    }

    /// <summary>
    /// Performs hybrid search combining BM25 (keyword-based) and kNN (semantic) scoring
    /// </summary>
    public async Task<SearchResponse> SearchHybridAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting hybrid BM25 + kNN search for query: '{Query}'", request.Query);

            // Process query to optimize for better results
            var queryProcessing = _queryProcessor.ProcessQuery(request.Query);
            _logger.LogDebug("Query processed: Type={Type}, KeyTerms=[{KeyTerms}], Processed='{ProcessedQuery}'", 
                queryProcessing.Type, string.Join(", ", queryProcessing.KeyTerms), queryProcessing.ProcessedQuery);

            // Use processed query for better keyword matching
            var searchQuery = request.Query;
            var embeddingQuery = request.Query;
            
            // For conversational queries, prioritize processed version for BM25
            if (queryProcessing.Type == QueryType.Conversational && queryProcessing.KeyTerms.Any())
            {
                searchQuery = queryProcessing.ProcessedQuery;
                embeddingQuery = queryProcessing.ProcessedQuery; // Use processed query for embeddings too
            }

            // Generate embedding for the optimal query
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(embeddingQuery);
            _logger.LogDebug("Generated query embedding with {Dimensions} dimensions", queryEmbedding.Length);

            // Create hybrid search query with script_score
            var elasticQuery = new Dictionary<string, object>
            {
                ["query"] = new Dictionary<string, object>
                {
                    ["script_score"] = new Dictionary<string, object>
                    {
                        // Base BM25 query
                        ["query"] = new Dictionary<string, object>
                        {
                            ["bool"] = new Dictionary<string, object>
                            {
                                ["should"] = new object[]
                                {
                                    // Phrase match for exact terms - use optimized query
                                    new Dictionary<string, object>
                                    {
                                        ["match_phrase"] = new Dictionary<string, object>
                                        {
                                            ["content"] = new Dictionary<string, object>
                                            {
                                                ["query"] = searchQuery,
                                                ["boost"] = queryProcessing.Type == QueryType.Keywords ? 3.0 : 2.0
                                            }
                                        }
                                    },
                                    // Match with OR for broader coverage
                                    new Dictionary<string, object>
                                    {
                                        ["match"] = new Dictionary<string, object>
                                        {
                                            ["content"] = new Dictionary<string, object>
                                            {
                                                ["query"] = searchQuery,
                                                ["operator"] = "OR",
                                                ["minimum_should_match"] = queryProcessing.Type == QueryType.Keywords ? "50%" : "20%"
                                            }
                                        }
                                    }
                                },
                                ["minimum_should_match"] = 1
                            }
                        },
                        // Hybrid scoring script combining BM25 and cosine similarity with adaptive weights
                        ["script"] = new Dictionary<string, object>
                        {
                            ["source"] = @"
                                double bm25Score = _score;
                                double cosineSim = cosineSimilarity(params.query_vector, 'embedding');
                                
                                // Use query-specific weights from processing
                                double bm25Weight = params.bm25_weight;
                                double semanticWeight = params.semantic_weight;
                                
                                // Combine scores with adaptive weighting
                                return bm25Weight * bm25Score + semanticWeight * (cosineSim + 1.0) * 10.0;
                            ",
                            ["params"] = new Dictionary<string, object>
                            {
                                ["query_vector"] = queryEmbedding,
                                ["bm25_weight"] = queryProcessing.KeywordWeight,
                                ["semantic_weight"] = queryProcessing.SemanticWeight
                            }
                        }
                    }
                },
                ["size"] = request.Limit * 3, // Get more results for better reconstruction
                ["_source"] = new[] { "content", "sourceFile", "position", "fileExtension", "indexedAt" },
                ["highlight"] = new Dictionary<string, object>
                {
                    ["fields"] = new Dictionary<string, object>
                    {
                        ["content"] = new Dictionary<string, object>
                        {
                            ["fragment_size"] = 150,
                            ["number_of_fragments"] = 3
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(elasticQuery);
            _logger.LogDebug("Hybrid search query: {Query}", json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_options.Url}/{_indexName}/_search", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Hybrid search failed with status {StatusCode}. Error: {Error}", 
                    response.StatusCode, errorContent);
                throw new ElasticsearchUnavailableException($"Hybrid search failed: {errorContent}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return await ProcessSearchResults(responseBody, request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in hybrid search for query: '{Query}'", request.Query);
            
            // Fallback to traditional search if hybrid fails
            _logger.LogInformation("Falling back to traditional search");
            return await SearchAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Processes search results and handles document reconstruction
    /// </summary>
    private async Task<SearchResponse> ProcessSearchResults(string responseBody, SearchRequest request, CancellationToken cancellationToken)
    {
        var searchResult = JsonSerializer.Deserialize<JsonElement>(responseBody);
        var hits = searchResult.GetProperty("hits").GetProperty("hits");
        var totalHits = searchResult.GetProperty("hits").GetProperty("total").GetProperty("value").GetInt32();
        var took = searchResult.GetProperty("took").GetInt32();

        var chunks = new List<ChunkInfo>();

        foreach (var hit in hits.EnumerateArray())
        {
            var source = hit.GetProperty("_source");
            var score = hit.GetProperty("_score").GetDouble();
            
            var highlights = new List<string>();
            if (hit.TryGetProperty("highlight", out var highlightProperty) && 
                highlightProperty.TryGetProperty("content", out var contentHighlights))
            {
                highlights.AddRange(contentHighlights.EnumerateArray().Select(h => h.GetString() ?? ""));
            }

            chunks.Add(new ChunkInfo
            {
                Id = hit.GetProperty("_id").GetString() ?? "",
                Content = source.GetProperty("content").GetString() ?? "",
                Score = score,
                SourceFile = source.TryGetProperty("sourceFile", out var sourceFile) ? sourceFile.GetString() ?? "" : "",
                ChunkIndex = source.TryGetProperty("position", out var position) && position.TryGetProperty("chunkIndex", out var chunkIdx) ? chunkIdx.GetInt32() : 0,
                TotalChunks = source.TryGetProperty("position", out var positionTotal) && positionTotal.TryGetProperty("totalChunks", out var totalChunks) ? totalChunks.GetInt32() : 1,
                FileExtension = source.TryGetProperty("fileExtension", out var ext) ? ext.GetString() ?? "" : "",
                CreatedAt = source.TryGetProperty("indexedAt", out var created) ? DateTime.Parse(created.GetString() ?? "") : DateTime.MinValue,
                Highlights = highlights
            });
        }

        _logger.LogInformation("Found {ChunkCount} chunks from {TotalHits} total hits", chunks.Count, totalHits);

        // Group chunks by source file and reconstruct documents
        var results = new List<SearchResult>();
        var chunksByFile = chunks
            .Where(c => !string.IsNullOrEmpty(c.Content) && !string.IsNullOrEmpty(c.SourceFile))
            .GroupBy(c => c.SourceFile)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var kvp in chunksByFile)
        {
            if (ShouldReconstructFullDocument(kvp.Value))
            {
                var reconstructed = await ReconstructDocumentFromChunks(kvp.Key, kvp.Value, cancellationToken);
                if (reconstructed != null)
                {
                    results.Add(reconstructed);
                }
            }
            else
            {
                // Add individual chunks as separate results
                foreach (var chunk in kvp.Value.OrderByDescending(c => c.Score).Take(3))
                {
                    var metadata = new Dictionary<string, object>();
                    if (chunk.Highlights.Any())
                    {
                        metadata["highlights"] = string.Join(" ... ", chunk.Highlights);
                    }
                    metadata["file_extension"] = chunk.FileExtension;

                    results.Add(new SearchResult(
                        chunk.Id,
                        Path.GetFileName(kvp.Key),
                        chunk.Content,
                        chunk.Score,
                        kvp.Key,
                        "chunk",
                        kvp.Key,
                        Path.GetFileName(kvp.Key),
                        metadata,
                        chunk.CreatedAt,
                        DateTime.Now
                    ));
                }
            }

            // Respect the limit
            if (results.Count >= request.Limit)
            {
                break;
            }
        }

        return new SearchResponse(
            results.Take(request.Limit).ToArray(), 
            totalHits, 
            took, 
            request.Query
        );
    }
    
    private Task<DocumentDetail> GetSingleChunkAsDocumentDetail(JsonDocument doc, string documentId, CancellationToken cancellationToken)
    {
        var source = doc.RootElement.GetProperty("_source");
        var id = doc.RootElement.GetProperty("_id").GetString() ?? documentId;
        
        var title = source.TryGetProperty("fileName", out var fileNameProp) ? fileNameProp.GetString() ?? "" : "";
        var content = source.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? "" : "";
        var category = source.TryGetProperty("fileType", out var fileTypeProp) ? fileTypeProp.GetString() ?? "" : "";
        var type = source.TryGetProperty("chunkIndex", out var chunkProp) ? $"Chunk {chunkProp.GetInt32()}" : "";
        var sourceFile = source.TryGetProperty("sourceFile", out var sourceFileProp) ? sourceFileProp.GetString() ?? "" : "";
        
        var createdAt = source.TryGetProperty("createdAt", out var createdProp) 
            ? DateTime.TryParse(createdProp.GetString(), out var created) ? created : DateTime.Now 
            : DateTime.Now;

        var metadata = new Dictionary<string, object>
        {
            { "category", category },
            { "index", _indexName },
            { "document_id", id },
            { "source_file", sourceFile },
            { "file_path", sourceFile },
            { "file_extension", category }
        };

        // Add all source fields to metadata
        foreach (var property in source.EnumerateObject())
        {
            if (property.Name != "fileName" && property.Name != "content" && property.Name != "createdAt")
            {
                metadata[property.Name] = property.Value.ToString();
            }
        }

        return Task.FromResult(new DocumentDetail(
            id, title, content, content, // Using content as both summary and full content
            1.0, category, type, sourceFile, title, metadata, createdAt, DateTime.Now
        ));
    }
}
