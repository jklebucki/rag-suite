using RAG.Abstractions.Search;
using RAG.Orchestrator.Api.Features.Search.DocumentReconstruction;
using System.Text.Json;

namespace RAG.Orchestrator.Api.Features.Search.ResultMapping;

/// <summary>
/// Maps Elasticsearch results to domain models
/// </summary>
public class ResultMapper : IResultMapper
{
    private readonly IDocumentReconstructor _documentReconstructor;
    private readonly ILogger<ResultMapper> _logger;

    public ResultMapper(
        IDocumentReconstructor documentReconstructor,
        ILogger<ResultMapper> logger)
    {
        _documentReconstructor = documentReconstructor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SearchResponse> MapSearchResponseAsync(
        string elasticsearchResponseBody,
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var searchResult = JsonSerializer.Deserialize<JsonElement>(elasticsearchResponseBody);
        var hits = searchResult.GetProperty("hits").GetProperty("hits");
        var totalHits = searchResult.GetProperty("hits").GetProperty("total").GetProperty("value").GetInt32();
        var took = searchResult.GetProperty("took").GetInt32();

        var chunks = new List<ChunkInfo>();

        foreach (var hit in hits.EnumerateArray())
        {
            chunks.Add(MapChunkInfo(hit));
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
            if (_documentReconstructor.ShouldReconstructFullDocument(kvp.Value))
            {
                var reconstructed = await _documentReconstructor.ReconstructDocumentFromChunksAsync(kvp.Key, kvp.Value, cancellationToken);
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
                    results.Add(MapChunkToSearchResult(chunk, kvp.Key));
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

    /// <inheritdoc />
    public ChunkInfo MapChunkInfo(JsonElement hit)
    {
        var source = hit.GetProperty("_source");
        var score = hit.GetProperty("_score").GetDouble();

        var highlights = new List<string>();
        if (hit.TryGetProperty("highlight", out var highlightProperty) &&
            highlightProperty.TryGetProperty("content", out var contentHighlights))
        {
            highlights.AddRange(contentHighlights.EnumerateArray().Select(h => h.GetString() ?? ""));
        }

        return new ChunkInfo
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
        };
    }

    /// <inheritdoc />
    public SearchResult MapChunkToSearchResult(ChunkInfo chunk, string sourceFile)
    {
        var metadata = new Dictionary<string, object>();
        if (chunk.Highlights.Any())
        {
            metadata["highlights"] = string.Join(" ... ", chunk.Highlights);
        }
        metadata["file_extension"] = chunk.FileExtension;

        return new SearchResult(
            chunk.Id,
            Path.GetFileName(sourceFile),
            chunk.Content,
            chunk.Score,
            sourceFile,
            "chunk",
            sourceFile,
            Path.GetFileName(sourceFile),
            metadata,
            chunk.CreatedAt,
            DateTime.Now
        );
    }
}

