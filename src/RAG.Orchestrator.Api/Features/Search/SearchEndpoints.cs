using RAG.Orchestrator.Api.Features.Search;
using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Features.Search;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/search")
            .WithTags("Search")
            .WithOpenApi();

        group.MapPost("/", async (SearchRequest request, ISearchService searchService) =>
        {
            var response = await searchService.SearchAsync(request);
            return response.ToApiResponse();
        })
        .WithName("SearchDocuments")
        .WithSummary("Search documents")
        .WithDescription("Search for documents in the knowledge base using natural language queries");

        group.MapPost("/hybrid", async (SearchRequest request, ISearchService searchService) =>
        {
            var response = await searchService.SearchHybridAsync(request);
            return response.ToApiResponse();
        })
        .WithName("SearchDocumentsHybrid")
        .WithSummary("Search documents with hybrid BM25 + kNN")
        .WithDescription("Search for documents using hybrid approach combining BM25 keyword search with semantic similarity (kNN)");

        group.MapGet("/", async (string query, int? limit, int? offset, ISearchService searchService) =>
        {
            var request = new SearchRequest(query, null, limit ?? 10, offset ?? 0);
            var response = await searchService.SearchAsync(request);
            return response.ToApiResponse();
        })
        .WithName("SearchDocumentsGet")
        .WithSummary("Search documents (GET)")
        .WithDescription("Search for documents using query parameters");

        group.MapGet("/documents/{id}", async (string id, ISearchService searchService) =>
        {
            var response = await searchService.GetDocumentByIdAsync(id);
            return response.ToApiResponse();
        })
        .WithName("GetDocumentDetails")
        .WithSummary("Get document details")
        .WithDescription("Get detailed information about a specific document by ID");

        return endpoints;
    }
}
