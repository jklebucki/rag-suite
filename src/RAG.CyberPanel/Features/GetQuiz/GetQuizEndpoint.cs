using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.CyberPanel.Features.GetQuiz;

public static class GetQuizEndpoint
{
    public static RouteGroupBuilder MapGetQuiz(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] GetQuizService service,
            CancellationToken ct
        ) =>
        {
            var result = await service.GetQuizAsync(id, ct);

            if (result == null)
                return Results.NotFound(new { Message = "Quiz not found" });

            return Results.Ok(result);
        })
        .WithName("GetQuiz")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
