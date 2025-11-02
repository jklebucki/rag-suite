using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.AddressBook.Features.ListContacts;

public static class ListContactsEndpoint
{
    public static RouteHandlerBuilder MapListContactsEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/", ListContactsAsync)
            .WithName("ListContacts")
            .WithTags("AddressBook")
            .WithOpenApi()
            .RequireAuthorization();
    }

    private static async Task<IResult> ListContactsAsync(
        [AsParameters] ListContactsRequest request,
        [FromServices] ListContactsService service,
        CancellationToken cancellationToken)
    {
        var response = await service.ListAsync(request, cancellationToken);
        return Results.Ok(response);
    }
}
