using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.CyberPanel.Features.CreateQuiz;

public static class CreateQuizEndpoint
{
    public static RouteGroupBuilder MapCreateQuiz(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            [FromBody] CreateQuizRequest request,
            [FromServices] CreateQuizHandler handler,
            [FromServices] IValidator<CreateQuizRequest> validator,
            CancellationToken ct
        ) =>
        {
            var validationResult = await validator.ValidateAsync(request, ct);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var quiz = await handler.Handle(request, ct);

            return Results.Created($"/api/cyberpanel/quizzes/{quiz.Id}", new
            {
                quiz.Id,
                quiz.Title,
                quiz.IsPublished
            });
        })
        .WithName("CreateQuiz")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
