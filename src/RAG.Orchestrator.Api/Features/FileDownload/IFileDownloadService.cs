namespace RAG.Orchestrator.Api.Features.FileDownload;

public interface IFileDownloadService
{
    Task<IResult> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<FileDownloadInfo?> GetFileInfoAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IResult> DownloadFileWithConversionAsync(string filePath, bool forceConvert, CancellationToken cancellationToken = default);
}