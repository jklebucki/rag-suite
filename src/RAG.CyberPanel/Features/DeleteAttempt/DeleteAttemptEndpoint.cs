using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.CyberPanel.Features.DeleteAttempt;

public static class DeleteAttemptEndpoint
{
    public static void MapDeleteAttempt(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/attempts/{attemptId:guid}", DeleteAttempt)
            .WithName("DeleteAttempt")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> DeleteAttempt(
        [FromRoute] Guid attemptId,
        DeleteAttemptHandler handler,
        CancellationToken ct)
    {
        var result = await handler.Handle(attemptId, ct);

        if (!result)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
