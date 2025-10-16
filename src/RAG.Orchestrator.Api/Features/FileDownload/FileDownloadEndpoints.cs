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

        group.MapGet("/convert/{*filePath}", async (string filePath, bool forceConvert, IFileDownloadService fileDownloadService) =>
        {
            return await fileDownloadService.DownloadFileWithConversionAsync(filePath, forceConvert);
        })
        .WithName("DownloadFileWithConversion")
        .WithSummary("Download a file with optional PDF conversion")
        .WithDescription("Download a file from configured shared folders. If the file can be converted to PDF and conversion is supported, returns the PDF version. Otherwise returns the original file. Use forceConvert=true to attempt conversion even for unsupported formats.")
        .Produces(200)
        .Produces(400)
        .Produces(403)
        .Produces(404)
        .Produces(500);

        return endpoints;
    }
}