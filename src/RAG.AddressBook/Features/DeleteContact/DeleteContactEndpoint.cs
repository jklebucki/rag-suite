using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.AddressBook.Features.DeleteContact;

public static class DeleteContactEndpoint
{
    public static RouteHandlerBuilder MapDeleteContactEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapDelete("/{id:guid}", DeleteContactAsync)
            .WithName("DeleteContact")
            .WithTags("AddressBook")
            .WithOpenApi()
            .RequireAuthorization("AdminOrPowerUser");
    }

    private static async Task<IResult> DeleteContactAsync(
        Guid id,
        [FromServices] DeleteContactHandler handler,
        CancellationToken cancellationToken)
    {
        var success = await handler.HandleAsync(id, cancellationToken);
        return success ? Results.NoContent() : Results.NotFound();
    }
}
