namespace RAG.Orchestrator.Api.Features.Search.QueryBuilding;

/// <summary>
/// Builds Elasticsearch queries for search operations
/// </summary>
public interface ISearchQueryBuilder
{
    /// <summary>
    /// Builds a BM25 (traditional) search query
    /// </summary>
    Dictionary<string, object> BuildBm25Query(string query, int limit, int offset);

    /// <summary>
    /// Builds a hybrid search query combining BM25 and kNN (semantic) scoring
    /// </summary>
    Dictionary<string, object> BuildHybridQuery(
        string query,
        float[] queryEmbedding,
        QueryProcessingResult queryProcessing,
        int limit,
        int offset);
}

