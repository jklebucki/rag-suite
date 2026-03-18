using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.AddressBook.Features.DeleteContactsWithoutPhoto;

public static class DeleteContactsWithoutPhotoEndpoint
{
    public static RouteHandlerBuilder MapDeleteContactsWithoutPhotoEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapDelete("/without-photo", DeleteContactsWithoutPhotoAsync)
            .WithName("DeleteContactsWithoutPhoto")
            .WithTags("AddressBook")
            .WithOpenApi()
            .RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> DeleteContactsWithoutPhotoAsync(
        [FromServices] DeleteContactsWithoutPhotoHandler handler,
        CancellationToken cancellationToken)
    {
        var deletedCount = await handler.HandleAsync(cancellationToken);

        return Results.Ok(new DeleteContactsWithoutPhotoResponse
        {
            DeletedCount = deletedCount
        });
    }
}
