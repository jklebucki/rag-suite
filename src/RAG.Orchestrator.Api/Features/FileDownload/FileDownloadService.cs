using Microsoft.Extensions.Options;
using RAG.Orchestrator.Api.Features.FileDownload;

namespace RAG.Orchestrator.Api.Features.FileDownload;

public interface IFileDownloadService
{
    Task<IResult> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default);
}

public class FileDownloadService : IFileDownloadService
{
    private readonly SharedFoldersOptions _options;
    private readonly ILogger<FileDownloadService> _logger;

    public FileDownloadService(
        IOptions<SharedFoldersOptions> options,
        ILogger<FileDownloadService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<IResult> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file path
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("Empty file path provided");
                return Task.FromResult(Results.BadRequest(new FileDownloadError
                {
                    Message = "File path cannot be empty",
                    FilePath = filePath
                }));
            }

            // Find matching shared folder configuration
            var matchingConfig = FindMatchingSharedFolder(filePath);
            if (matchingConfig == null)
            {
                _logger.LogWarning("No matching shared folder configuration found for path: {FilePath}", filePath);
                return Task.FromResult(Results.BadRequest(new FileDownloadError
                {
                    Message = "No matching shared folder configuration found",
                    FilePath = filePath
                }));
            }

            // Replace path prefix
            var relativePath = GetRelativePath(filePath, matchingConfig.PathToReplace);
            var fullPath = Path.Combine(matchingConfig.Path, relativePath);

            // Ensure the resolved path is within the shared folder
            var sharedFolderFullPath = Path.GetFullPath(matchingConfig.Path);
            var requestedFileFullPath = Path.GetFullPath(fullPath);

            if (!requestedFileFullPath.StartsWith(sharedFolderFullPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Attempted directory traversal attack: {RequestedPath}", fullPath);
                return Task.FromResult(Results.Forbid());
            }

            // Check if file exists
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found: {FilePath}", fullPath);
                return Task.FromResult(Results.NotFound(new FileDownloadError
                {
                    Message = "File not found",
                    FilePath = filePath
                }));
            }

            // Get file info for response
            var fileInfo = new FileInfo(fullPath);
            var contentType = GetContentType(filePath);

            _logger.LogInformation("Serving file: {FileName} ({Size} bytes) from {OriginalPath} -> {ResolvedPath}",
                Path.GetFileName(filePath), fileInfo.Length, filePath, fullPath);

            // Return file as stream
            var stream = File.OpenRead(fullPath);
            return Task.FromResult(Results.File(stream, contentType, Path.GetFileName(filePath), enableRangeProcessing: true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FilePath}", filePath);
            return Task.FromResult(Results.Problem("An error occurred while downloading the file"));
        }
    }

    private SharedFolderConfig? FindMatchingSharedFolder(string filePath)
    {
        return _options.SharedFolders.FirstOrDefault(config =>
            filePath.StartsWith(config.PathToReplace, StringComparison.OrdinalIgnoreCase));
    }

    private string GetRelativePath(string fullPath, string prefix)
    {
        if (fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = fullPath.Substring(prefix.Length);
            // Remove leading path separators
            relativePath = relativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relativePath;
        }
        return fullPath;
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".zip" => "application/zip",
            ".json" => "application/json",
            ".xml" => "application/xml",
            _ => "application/octet-stream"
        };
    }
}