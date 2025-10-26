using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.CyberPanel.Features.ListQuizzes;

public static class ListQuizzesEndpoint
{
    public static RouteGroupBuilder MapListQuizzes(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            [FromServices] ListQuizzesService service,
            [FromQuery] string? language,
            CancellationToken ct
        ) =>
        {
            var result = await service.GetQuizzesAsync(language, ct);
            return Results.Ok(result);
        })
        .WithName("ListQuizzes")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
