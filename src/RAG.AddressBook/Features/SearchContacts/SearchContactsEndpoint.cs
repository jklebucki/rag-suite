using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.AddressBook.Features.SearchContacts;

public static class SearchContactsEndpoint
{
    public static RouteHandlerBuilder MapSearchContactsEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/search", SearchContactsAsync)
            .WithName("SearchContacts")
            .WithTags("AddressBook")
            .WithOpenApi()
            .RequireAuthorization();
    }

    private static async Task<IResult> SearchContactsAsync(
        [FromQuery] string searchTerm,
        [FromServices] SearchContactsService service,
        CancellationToken cancellationToken)
    {
        var request = new SearchContactsRequest { SearchTerm = searchTerm };
        var response = await service.SearchAsync(request, cancellationToken);
        return Results.Ok(response);
    }
}
