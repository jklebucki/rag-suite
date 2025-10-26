using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.CyberPanel.Features.UpdateQuiz;

public static class UpdateQuizEndpoint
{
    public static RouteGroupBuilder MapUpdateQuiz(this RouteGroupBuilder group)
    {
        group.MapPut("/{quizId:guid}", UpdateQuiz)
            .WithName("UpdateQuiz")
            .WithOpenApi()
            .RequireAuthorization()
            .Produces<UpdateQuizResponse>()
            .Produces(404)
            .Produces(403)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<Results<Ok<UpdateQuizResponse>, NotFound, UnauthorizedHttpResult, ValidationProblem>> UpdateQuiz(
        Guid quizId,
        [FromBody] UpdateQuizRequest request,
        [FromServices] UpdateQuizHandler handler,
        [FromServices] IValidator<UpdateQuizRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var response = await handler.Handle(quizId, request, ct);

        if (response == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(response);
    }
}
