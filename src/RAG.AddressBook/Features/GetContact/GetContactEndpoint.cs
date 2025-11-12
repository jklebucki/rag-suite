using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RAG.Abstractions.Common.Api;
using RAG.AddressBook.Common;

namespace RAG.AddressBook.Features.GetContact;

public static class GetContactEndpoint
{
    public static RouteHandlerBuilder MapGetContactEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/{id:guid}", GetContactAsync)
            .WithName("GetContact")
            .WithTags("AddressBook")
            .WithOpenApi()
            .RequireAuthorization();
    }

    private static async Task<IResult> GetContactAsync(
        Guid id,
        [FromServices] GetContactService service,
        CancellationToken cancellationToken)
    {
        var response = await service.GetByIdAsync(id, cancellationToken);
        return response != null
            ? response.ToApiResponse()
            : ApiResponseExtensions.ToApiNotFoundResponse<GetContactResponse>("Contact not found");
    }
}
