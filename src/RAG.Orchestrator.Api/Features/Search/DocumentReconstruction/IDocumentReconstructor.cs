using RAG.Abstractions.Search;
using RAG.Orchestrator.Api.Features.Search;

namespace RAG.Orchestrator.Api.Features.Search.DocumentReconstruction;

/// <summary>
/// Handles document reconstruction from chunks
/// </summary>
public interface IDocumentReconstructor
{
    /// <summary>
    /// Fetches all chunks for a document by source file
    /// </summary>
    Task<List<ChunkInfo>> FetchAllChunksForDocumentAsync(string sourceFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reconstructs a document from chunks
    /// </summary>
    Task<SearchResult?> ReconstructDocumentFromChunksAsync(string sourceFile, List<ChunkInfo> chunks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if full document reconstruction should be performed
    /// </summary>
    bool ShouldReconstructFullDocument(List<ChunkInfo> chunks);
}

