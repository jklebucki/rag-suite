using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.CyberPanel.Features.SubmitAttempt;

public static class SubmitAttemptEndpoint
{
    public static RouteGroupBuilder MapSubmitAttempt(this RouteGroupBuilder group)
    {
        group.MapPost("/{quizId:guid}/attempts", async (
            Guid quizId,
            [FromBody] SubmitAttemptRequest request,
            [FromServices] SubmitAttemptHandler handler,
            [FromServices] IValidator<SubmitAttemptRequest> validator,
            CancellationToken ct
        ) =>
        {
            // Ensure quizId in route matches request body
            if (request.QuizId != quizId)
            {
                return Results.BadRequest(new { Message = "Quiz ID in URL does not match request body" });
            }

            var validationResult = await validator.ValidateAsync(request, ct);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            try
            {
                var result = await handler.Handle(request, ct);

                return Results.Created(
                    $"/api/cyberpanel/quizzes/{quizId}/attempts/{result.AttemptId}",
                    result
                );
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { Message = ex.Message });
            }
        })
        .WithName("SubmitQuizAttempt")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
