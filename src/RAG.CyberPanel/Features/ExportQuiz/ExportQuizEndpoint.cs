using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.CyberPanel.Features.ExportQuiz;

/// <summary>
/// Endpoint for exporting quiz data to JSON format.
/// Returns complete quiz with all questions, options, and images.
/// </summary>
public static class ExportQuizEndpoint
{
    public static RouteGroupBuilder MapExportQuiz(this RouteGroupBuilder group)
    {
        group.MapGet("/{quizId:guid}/export", ExportQuizAsync)
            .WithName("ExportQuiz")
            .WithSummary("Export quiz to JSON format")
            .WithDescription("Exports a complete quiz including all questions, options, images (base64 or URLs), and metadata.")
            .Produces<ExportQuizResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return group;
    }

    private static async Task<IResult> ExportQuizAsync(
        [FromRoute] Guid quizId,
        [FromServices] ExportQuizService exportService,
        CancellationToken cancellationToken)
    {
        try
        {
            var exportedQuiz = await exportService.ExportQuizAsync(quizId, cancellationToken);

            // Return JSON with proper content disposition for download
            return Results.Ok(exportedQuiz);
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
    }
}
