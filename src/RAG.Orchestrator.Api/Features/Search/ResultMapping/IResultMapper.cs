using RAG.Abstractions.Search;
using RAG.Orchestrator.Api.Features.Search;
using System.Text.Json;

namespace RAG.Orchestrator.Api.Features.Search.ResultMapping;

/// <summary>
/// Maps Elasticsearch results to domain models
/// </summary>
public interface IResultMapper
{
    /// <summary>
    /// Maps Elasticsearch response to SearchResponse
    /// </summary>
    Task<SearchResponse> MapSearchResponseAsync(
        string elasticsearchResponseBody,
        SearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps a single Elasticsearch hit to ChunkInfo
    /// </summary>
    ChunkInfo MapChunkInfo(JsonElement hit);

    /// <summary>
    /// Maps a single chunk to SearchResult
    /// </summary>
    SearchResult MapChunkToSearchResult(ChunkInfo chunk, string sourceFile);
}

