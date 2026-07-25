using RAG.DocumentProcessing.Abstractions;
using RAG.DocumentProcessing.Core;

namespace RAG.DocumentProcessing.Api;

public static class DocumentProcessingEndpointMappings
{
    public static IEndpointRouteBuilder MapDocumentProcessingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var jobs = endpoints.MapGroup("/api/v1/jobs").WithTags("Document processing");

        jobs.MapPost("", async (
            HttpRequest request,
            IDocumentProcessingJobService jobService,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { code = "INVALID_CONTENT_TYPE", message = "Multipart form data is required." });
            }

            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            if (file == null)
            {
                return Results.BadRequest(new { code = "FILE_REQUIRED", message = "A PDF file is required." });
            }

            var fileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(fileName) || !string.Equals(fileName, file.FileName, StringComparison.Ordinal))
            {
                return Results.BadRequest(new { code = "INVALID_FILE_NAME", message = "The file name is invalid." });
            }

            try
            {
                await using var content = file.OpenReadStream();
                var accepted = await jobService.SubmitAsync(
                    new DocumentDescriptor(fileName, file.ContentType, file.Length),
                    content,
                    cancellationToken);
                return Results.Accepted($"/api/v1/jobs/{accepted.JobId}", accepted);
            }
            catch (DocumentProcessingException ex)
            {
                return ex.Code == "QUEUE_FULL"
                    ? Results.Json(new { code = ex.Code, message = ex.Message }, statusCode: StatusCodes.Status429TooManyRequests)
                    : Results.BadRequest(new { code = ex.Code, message = ex.Message });
            }
        });

        jobs.MapGet("/{jobId}", (string jobId, IDocumentProcessingJobService jobService) =>
        {
            var status = jobService.GetStatus(jobId);
            return status == null ? Results.NotFound() : Results.Ok(status);
        });

        jobs.MapGet("/{jobId}/result", (string jobId, IDocumentProcessingJobService jobService) =>
        {
            var status = jobService.GetStatus(jobId);
            if (status == null)
            {
                return Results.NotFound();
            }

            if (status.Status == DocumentJobState.Failed)
            {
                return Results.Problem(status.ErrorCode ?? "Document processing failed.", statusCode: StatusCodes.Status422UnprocessableEntity);
            }

            var result = jobService.GetResult(jobId);
            return result == null
                ? Results.Conflict(new { code = "RESULT_NOT_READY", message = "The result is not ready." })
                : Results.Ok(result);
        });

        jobs.MapDelete("/{jobId}", async (string jobId, IDocumentProcessingJobService jobService, CancellationToken cancellationToken) =>
        {
            return await jobService.DeleteAsync(jobId, cancellationToken) ? Results.NoContent() : Results.NotFound();
        });

        endpoints.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
        return endpoints;
    }
}
