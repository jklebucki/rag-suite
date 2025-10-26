using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.CyberPanel.Features.DeleteQuiz;

public static class DeleteQuizEndpoint
{
    public static void MapDeleteQuiz(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/{quizId:guid}", DeleteQuiz)
            .WithName("DeleteQuiz")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> DeleteQuiz(
        [FromRoute] Guid quizId,
        DeleteQuizHandler handler,
        CancellationToken ct)
    {
        var result = await handler.Handle(quizId, ct);

        if (!result)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
