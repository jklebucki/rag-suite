using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.CyberPanel.Features.ImportQuiz;

/// <summary>
/// Endpoint for importing quiz data from JSON.
/// Supports creating new quiz or overwriting existing one.
/// </summary>
public static class ImportQuizEndpoint
{
    public static RouteGroupBuilder MapImportQuiz(this RouteGroupBuilder group)
    {
        group.MapPost("/import", ImportQuizAsync)
            .WithName("ImportQuiz")
            .WithSummary("Import quiz from JSON")
            .WithDescription("Imports quiz data from JSON export. Can create new quiz or overwrite existing one. Validates all data before import.")
            .Produces<ImportQuizResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithOpenApi();

        return group;
    }

    private static async Task<IResult> ImportQuizAsync(
        [FromBody] ImportQuizRequest request,
        [FromServices] IValidator<ImportQuizRequest> validator,
        [FromServices] ImportQuizHandler handler,
        CancellationToken cancellationToken)
    {
        // Validate request
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors, title: "Quiz import validation failed");
        }

        try
        {
            var result = await handler.Handle(request, cancellationToken);
            
            return Results.Created(
                $"/api/cyberpanel/quizzes/{result.QuizId}",
                result
            );
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Quiz not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Quiz import failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status422UnprocessableEntity
            );
        }
    }
}
