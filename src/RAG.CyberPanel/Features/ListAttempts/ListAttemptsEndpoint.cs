using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.CyberPanel.Features.ListAttempts;

public static class ListAttemptsEndpoint
{
    public static RouteGroupBuilder MapListAttempts(this RouteGroupBuilder group)
    {
        group.MapGet("/attempts", async (
            [FromServices] ListAttemptsHandler handler,
            CancellationToken ct
        ) =>
        {
            var result = await handler.Handle(ct);
            return Results.Ok(result);
        })
        .WithName("ListQuizAttempts")
        .WithOpenApi()
        .WithDescription("Get all quiz attempts/results for the current user")
        .RequireAuthorization();

        return group;
    }
}
