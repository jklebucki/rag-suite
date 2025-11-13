using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.AddressBook.Features.UpdateContact;

public static class UpdateContactEndpoint
{
    public static RouteHandlerBuilder MapUpdateContactEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapPut("/{id:guid}", UpdateContactAsync)
            .WithName("UpdateContact")
            .WithTags("AddressBook")
            .WithOpenApi()
            .RequireAuthorization("AdminOrPowerUser");
    }

    private static async Task<IResult> UpdateContactAsync(
        Guid id,
        [FromBody] UpdateContactRequest request,
        [FromServices] UpdateContactHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);
        return response != null ? Results.Ok(response) : Results.NotFound();
    }
}
