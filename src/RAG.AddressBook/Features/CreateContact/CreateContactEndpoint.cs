using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.AddressBook.Features.CreateContact;

public static class CreateContactEndpoint
{
    public static RouteHandlerBuilder MapCreateContactEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapPost("/", CreateContactAsync)
            .WithName("CreateContact")
            .WithTags("AddressBook")
            .WithOpenApi()
            .RequireAuthorization("AdminOrPowerUser");
    }

    private static async Task<IResult> CreateContactAsync(
        [FromBody] CreateContactRequest request,
        [FromServices] CreateContactHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(request, cancellationToken);
        return Results.Created($"/api/addressbook/{response.Id}", response);
    }
}
