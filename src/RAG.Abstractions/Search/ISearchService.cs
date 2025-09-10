namespace RAG.Abstractions.Search;

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task<SearchResponse> SearchHybridAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task<DocumentDetail> GetDocumentByIdAsync(string documentId, CancellationToken cancellationToken = default);
}
