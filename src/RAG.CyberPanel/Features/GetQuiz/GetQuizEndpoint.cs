using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RAG.CyberPanel.Common;

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

            return result != null
                ? result.ToApiResponse()
                : ApiResponseExtensions.ToApiNotFoundResponse<GetQuizResponse>("Quiz not found");
        })
        .WithName("GetQuiz")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
