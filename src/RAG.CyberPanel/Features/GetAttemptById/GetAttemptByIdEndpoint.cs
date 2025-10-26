using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace RAG.CyberPanel.Features.GetAttemptById;

public static class GetAttemptByIdEndpoint
{
    public static RouteGroupBuilder MapGetAttemptById(this RouteGroupBuilder group)
    {
        group.MapGet("/attempts/{attemptId:guid}", GetAttemptById)
            .WithName("GetAttemptById")
            .RequireAuthorization()
            .Produces<GetAttemptByIdResponse>()
            .Produces(404);

        return group;
    }

    private static async Task<Results<Ok<GetAttemptByIdResponse>, NotFound>> GetAttemptById(
        Guid attemptId,
        GetAttemptByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(attemptId, cancellationToken);
        return response != null ? TypedResults.Ok(response) : TypedResults.NotFound();
    }
}