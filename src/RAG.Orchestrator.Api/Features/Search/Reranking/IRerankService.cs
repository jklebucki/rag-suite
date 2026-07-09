namespace RAG.Orchestrator.Api.Features.Search.Reranking;

/// <summary>
/// A single rerank result: the index of the input document and its relevance score.
/// </summary>
public readonly record struct RerankHit(int Index, double Score);

/// <summary>
/// Second-stage cross-encoder reranking. Given a query and a set of candidate documents,
/// returns them scored by true relevance so a keyword-stuffed or tangential document cannot
/// outrank the one that actually answers the query.
/// </summary>
public interface IRerankService
{
    /// <summary>Whether reranking is configured and enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Number of candidates to retrieve before reranking down to the requested top-k.</summary>
    int RetrieveTopN { get; }

    /// <summary>Number of top reranked documents to keep (e.g. inject into the LLM prompt) when reranking is enabled.</summary>
    int TopK { get; }

    /// <summary>
    /// Scores each candidate document against the query using the reranker model.
    /// Returns hits sorted by descending score. Returns an empty list if reranking is
    /// disabled or the reranker is unavailable, so callers can fall back to the original order.
    /// </summary>
    Task<IReadOnlyList<RerankHit>> RerankAsync(string query, IReadOnlyList<string> documents, CancellationToken cancellationToken = default);
}
