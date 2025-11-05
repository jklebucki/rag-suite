using Microsoft.Extensions.Options;
using RAG.Abstractions.Search;
using RAG.Orchestrator.Api.Common.Constants;
using RAG.Orchestrator.Api.Features.Reconstruction;
using System.Text;
using System.Text.Json;

namespace RAG.Orchestrator.Api.Features.Search.DocumentReconstruction;

/// <summary>
/// Handles document reconstruction from chunks
/// </summary>
public class DocumentReconstructor : IDocumentReconstructor
{
    private readonly HttpClient _httpClient;
    private readonly IDocumentReconstructionService _reconstructionService;
    private readonly ElasticsearchOptions _options;
    private readonly string _indexName;
    private readonly ILogger<DocumentReconstructor> _logger;

    public DocumentReconstructor(
        HttpClient httpClient,
        IDocumentReconstructionService reconstructionService,
        IOptions<ElasticsearchOptions> options,
        ILogger<DocumentReconstructor> logger)
    {
        _httpClient = httpClient;
        _reconstructionService = reconstructionService;
        _options = options.Value;
        _indexName = _options.DefaultIndexName;
        _logger = logger;

        // Configure HTTP client for Elasticsearch
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.Username}:{_options.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            AuthenticationSchemes.Basic, credentials);
        _httpClient.Timeout = TimeSpan.FromMinutes(_options.TimeoutMinutes);
    }

    /// <inheritdoc />
    public async Task<List<ChunkInfo>> FetchAllChunksForDocumentAsync(string sourceFile, CancellationToken cancellationToken = default)
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

    /// <inheritdoc />
    public async Task<SearchResult?> ReconstructDocumentFromChunksAsync(string sourceFile, List<ChunkInfo> chunks, CancellationToken cancellationToken = default)
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
                var allChunks = await FetchAllChunksForDocumentAsync(sourceFile, cancellationToken);
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

                    if (actualChunks >= expectedChunks * _options.MinimumChunkCompleteness)
                    {
                        // Filter out chunks with empty content before reconstruction
                        var validChunks = allChunks.Where(c => !string.IsNullOrWhiteSpace(c.Content)).ToList();

                        if (validChunks.Count == 0)
                        {
                            _logger.LogWarning("All fetched chunks for {SourceFile} have empty content! Falling back to matching chunks.", sourceFile);
                            reconstructedContent = _reconstructionService.ReconstructDocument(sortedChunks);
                        }
                        else
                        {
                            reconstructedContent = _reconstructionService.ReconstructDocument(validChunks);

                            if (validChunks.Count < allChunks.Count)
                            {
                                _logger.LogWarning("Filtered out {EmptyCount} empty chunks from {TotalCount} for {SourceFile}",
                                    allChunks.Count - validChunks.Count, allChunks.Count, sourceFile);
                            }
                        }

                        allHighlights.AddRange(chunks.SelectMany(c => c.Highlights));

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
                        reconstructedContent = _reconstructionService.ReconstructDocument(sortedChunks);
                        allHighlights.AddRange(chunks.SelectMany(c => c.Highlights));
                        _logger.LogWarning("Document {SourceFile} has too many missing chunks ({ActualChunks}/{ExpectedChunks}), using only matching chunks",
                            sourceFile, actualChunks, expectedChunks);
                    }
                }
                else
                {
                    // Fallback to available chunks
                    reconstructedContent = _reconstructionService.ReconstructDocument(sortedChunks);
                    allHighlights.AddRange(chunks.SelectMany(c => c.Highlights));
                    _logger.LogWarning("Could not fetch any chunks for document {SourceFile}, using only matching chunks", sourceFile);
                }
            }
            else
            {
                // Use only the found chunks
                reconstructedContent = _reconstructionService.ReconstructDocument(sortedChunks);
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
                metadata["highlights"] = string.Join(" ... ", allHighlights.Take(5));
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

    /// <inheritdoc />
    public bool ShouldReconstructFullDocument(List<ChunkInfo> chunks)
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
}

