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

        return endpoints;
    }
}
