using Microsoft.Extensions.Options;
using RAG.Orchestrator.Api.Features.FileDownload;
using RAG.Orchestrator.Api.Services;

namespace RAG.Orchestrator.Api.Features.FileDownload;

public record FileDownloadInfo(
    string FullPath,
    string ContentType,
    long FileSize,
    string FileName
);

public interface IFileDownloadService
{
    Task<IResult> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<FileDownloadInfo?> GetFileInfoAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IResult> DownloadFileWithConversionAsync(string filePath, bool forceConvert, CancellationToken cancellationToken = default);
}

public class FileDownloadService : IFileDownloadService
{
    private readonly SharedFoldersOptions _options;
    private readonly ILogger<FileDownloadService> _logger;
    private readonly IGotenbergService _gotenbergService;

    public FileDownloadService(
        IOptions<SharedFoldersOptions> options,
        ILogger<FileDownloadService> logger,
        IGotenbergService gotenbergService)
    {
        _options = options.Value;
        _logger = logger;
        _gotenbergService = gotenbergService;
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

            // Decode URL-encoded path
            filePath = Uri.UnescapeDataString(filePath);
            _logger.LogDebug("Decoded file path: {DecodedPath}", filePath);

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
            var relativePath = GetRelativePath(filePath, matchingConfig.PathToReplace, matchingConfig.Path);
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

    private string GetRelativePath(string fullPath, string prefix, string targetPath)
    {
        if (fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = fullPath.Substring(prefix.Length);
            // Remove leading path separators
            relativePath = relativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Normalize path separators to match the target system
            // If target path uses '/', convert all '\' to '/'
            // If target path uses '\', convert all '/' to '\'
            var targetSeparator = targetPath.Contains('/') ? '/' : '\\';
            var sourceSeparator = targetSeparator == '/' ? '\\' : '/';

            relativePath = relativePath.Replace(sourceSeparator, targetSeparator);

            return relativePath;
        }
        return fullPath;
    }

    public Task<FileDownloadInfo?> GetFileInfoAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file path
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("Empty file path provided");
                return Task.FromResult<FileDownloadInfo?>(null);
            }

            // Decode URL-encoded path
            filePath = Uri.UnescapeDataString(filePath);

            // Find matching shared folder configuration
            var matchingConfig = FindMatchingSharedFolder(filePath);
            if (matchingConfig == null)
            {
                _logger.LogWarning("No matching shared folder configuration found for path: {FilePath}", filePath);
                return Task.FromResult<FileDownloadInfo?>(null);
            }

            // Replace path prefix
            var relativePath = GetRelativePath(filePath, matchingConfig.PathToReplace, matchingConfig.Path);
            var fullPath = Path.Combine(matchingConfig.Path, relativePath);

            // Ensure the resolved path is within the shared folder
            var sharedFolderFullPath = Path.GetFullPath(matchingConfig.Path);
            var requestedFileFullPath = Path.GetFullPath(fullPath);

            if (!requestedFileFullPath.StartsWith(sharedFolderFullPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Attempted directory traversal attack: {RequestedPath}", fullPath);
                return Task.FromResult<FileDownloadInfo?>(null);
            }

            // Check if file exists
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found: {FilePath}", fullPath);
                return Task.FromResult<FileDownloadInfo?>(null);
            }

            // Get file info
            var fileInfo = new FileInfo(fullPath);
            var contentType = GetContentType(filePath);

            _logger.LogInformation("File info retrieved: {FileName} ({Size} bytes) from {OriginalPath} -> {ResolvedPath}",
                Path.GetFileName(filePath), fileInfo.Length, filePath, fullPath);

            return Task.FromResult<FileDownloadInfo?>(new FileDownloadInfo(
                FullPath: fullPath,
                ContentType: contentType,
                FileSize: fileInfo.Length,
                FileName: Path.GetFileName(filePath)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info: {FilePath}", filePath);
            return Task.FromResult<FileDownloadInfo?>(null);
        }
    }

    private string GetContentType(string filePath)
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

    public async Task<IResult> DownloadFileWithConversionAsync(string filePath, bool forceConvert, CancellationToken cancellationToken = default)
    {
        try
        {
            // Sprawdź czy możemy skonwertować plik na PDF
            var fileExtension = Path.GetExtension(filePath);
            var canConvert = await _gotenbergService.CanConvertAsync(fileExtension);

            if (!canConvert && !forceConvert)
            {
                // Zwróć oryginalny plik jeśli konwersja nie jest możliwa
                return await DownloadFileAsync(filePath, cancellationToken);
            }

            // Pobierz informacje o pliku
            var fileInfo = await GetFileInfoAsync(filePath, cancellationToken);
            if (fileInfo == null)
            {
                // Jeśli nie możemy pobrać informacji o pliku, zwróć oryginalny wynik
                return await DownloadFileAsync(filePath, cancellationToken);
            }

            // Otwórz plik jako stream
            await using var fileStream = File.OpenRead(fileInfo.FullPath);

            // Spróbuj skonwertować plik na PDF
            var pdfStream = await _gotenbergService.ConvertToPdfAsync(fileStream, fileInfo.FileName);

            if (pdfStream == null)
            {
                // Konwersja się nie udała, zwróć oryginalny plik
                return await DownloadFileAsync(filePath, cancellationToken);
            }

            // Zwróć skonwertowany plik PDF
            var pdfFileName = Path.ChangeExtension(fileInfo.FileName, ".pdf");
            return Results.File(pdfStream, "application/pdf", pdfFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file conversion for path: {FilePath}", filePath);
            // W przypadku błędu, spróbuj zwrócić oryginalny plik
            return await DownloadFileAsync(filePath, cancellationToken);
        }
    }
}