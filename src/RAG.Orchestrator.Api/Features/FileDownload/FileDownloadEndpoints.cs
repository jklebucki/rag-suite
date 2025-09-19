using RAG.Orchestrator.Api.Features.FileDownload;

namespace RAG.Orchestrator.Api.Features.FileDownload;

public static class FileDownloadEndpoints
{
    public static IEndpointRouteBuilder MapFileDownloadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/filedownload")
            .WithTags("FileDownload")
            .WithOpenApi();

        group.MapGet("/{*filePath}", async (string filePath, IFileDownloadService fileDownloadService) =>
        {
            return await fileDownloadService.DownloadFileAsync(filePath);
        })
        .WithName("DownloadFile")
        .WithSummary("Download a file from shared folder")
        .WithDescription("Download a file from configured shared folders by full file path. The path prefix will be automatically replaced based on SharedFolders configuration.")
        .Produces(200)
        .Produces(400)
        .Produces(403)
        .Produces(404)
        .Produces(500);

        return endpoints;
    }
}