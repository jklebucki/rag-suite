using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.AddressBook.Features.GetProposal;

public static class GetProposalEndpoint
{
    public static RouteHandlerBuilder MapGetProposalEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/proposals/{id:guid}", GetProposalAsync)
            .WithName("GetProposal")
            .WithTags("AddressBook", "Proposals")
            .WithOpenApi()
            .RequireAuthorization();
    }

    private static async Task<IResult> GetProposalAsync(
        Guid id,
        [FromServices] GetProposalService service,
        CancellationToken cancellationToken)
    {
        var response = await service.GetByIdAsync(id, cancellationToken);
        return response is not null ? Results.Ok(response) : Results.NotFound();
    }
}
