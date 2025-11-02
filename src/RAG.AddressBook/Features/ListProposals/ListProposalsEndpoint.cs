using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.AddressBook.Features.ListProposals;

public static class ListProposalsEndpoint
{
    public static RouteHandlerBuilder MapListProposalsEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/proposals", ListProposalsAsync)
            .WithName("ListProposals")
            .WithTags("AddressBook", "Proposals")
            .WithOpenApi()
            .RequireAuthorization();
    }

    private static async Task<IResult> ListProposalsAsync(
        [AsParameters] ListProposalsRequest request,
        [FromServices] ListProposalsService service,
        CancellationToken cancellationToken)
    {
        var response = await service.ListAsync(request, cancellationToken);
        return Results.Ok(response);
    }
}
