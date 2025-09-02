using RAG.Collector.Models;
using System.Runtime.CompilerServices;

namespace RAG.Collector.Enumerators;

/// <summary>
/// File enumerator using standard .NET System.IO for cross-platform compatibility
/// Supports UNC paths on Windows and handles long paths when available
/// </summary>
public class FileEnumerator : IFileEnumerator
{
    private readonly ILogger<FileEnumerator> _logger;

    public FileEnumerator(ILogger<FileEnumerator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<FileItem> EnumerateFilesAsync(
        IEnumerable<string> sourceFolders,
        IEnumerable<string> fileExtensions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var extensions = fileExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        foreach (var sourceFolder in sourceFolders)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            _logger.LogInformation("Enumerating files in folder: {Folder}", sourceFolder);

            await foreach (var fileItem in EnumerateFolderAsync(sourceFolder, extensions, cancellationToken))
            {
                yield return fileItem;
            }
        }
    }

    /// <inheritdoc />
    public async Task<int> GetFileCountAsync(
        IEnumerable<string> sourceFolders,
        IEnumerable<string> fileExtensions,
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        var extensions = fileExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var sourceFolder in sourceFolders)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            count += await CountFilesInFolderAsync(sourceFolder, extensions, cancellationToken);
        }

        return count;
    }

    /// <summary>
    /// Enumerates files in a single folder recursively
    /// </summary>
    private async IAsyncEnumerable<FileItem> EnumerateFolderAsync(
        string sourceFolder,
        HashSet<string> extensions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(sourceFolder))
        {
            _logger.LogWarning("Source folder does not exist: {Folder}", sourceFolder);
            yield break;
        }

        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.System | FileAttributes.Hidden,
            ReturnSpecialDirectories = false
        };

        IEnumerable<string> files;
        
        try
        {
            // Use Directory.EnumerateFiles for better memory efficiency with large directories
            files = Directory.EnumerateFiles(sourceFolder, "*", enumerationOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate files in folder: {Folder}", sourceFolder);
            yield break;
        }

        var processedCount = 0;
        Uri? sourceUri = null;
        
        try
        {
            sourceUri = new Uri(Path.GetFullPath(sourceFolder));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create URI for source folder: {Folder}", sourceFolder);
        }

        foreach (var filePath in files)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            FileItem? fileItem = null;
            
            try
            {
                fileItem = CreateFileItem(filePath, extensions, sourceUri);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogDebug("Access denied to file: {FilePath} - {Message}", filePath, ex.Message);
                continue;
            }
            catch (FileNotFoundException)
            {
                _logger.LogDebug("File not found (may have been deleted): {FilePath}", filePath);
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                _logger.LogDebug("Directory not found: {FilePath}", filePath);
                continue;
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "I/O error accessing file: {FilePath}", filePath);
                continue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing file: {FilePath}", filePath);
                continue;
            }

            if (fileItem != null)
            {
                processedCount++;
                yield return fileItem;

                // Yield control periodically to avoid blocking
                if (processedCount % 100 == 0)
                {
                    await Task.Yield();
                }
            }
        }

        _logger.LogInformation("Completed enumeration of folder: {Folder}, found {Count} matching files", 
            sourceFolder, processedCount);
    }

    /// <summary>
    /// Creates a FileItem from a file path, or returns null if the file should be skipped
    /// </summary>
    private FileItem? CreateFileItem(string filePath, HashSet<string> extensions, Uri? sourceUri)
    {
        var extension = Path.GetExtension(filePath);
        
        // Check if the file extension matches our filter
        if (!extensions.Contains(extension))
            return null;

        var fileInfo = new FileInfo(filePath);
        
        // Skip files that don't exist or are inaccessible
        if (!fileInfo.Exists)
            return null;

        // Calculate relative path for display purposes
        string? relativePath = null;
        if (sourceUri != null)
        {
            try
            {
                var fileUri = new Uri(Path.GetFullPath(filePath));
                relativePath = Uri.UnescapeDataString(sourceUri.MakeRelativeUri(fileUri).ToString());
            }
            catch (Exception)
            {
                // If we can't calculate relative path, just use the file name
                relativePath = Path.GetFileName(filePath);
            }
        }

        return new FileItem
        {
            Path = filePath,
            Extension = extension,
            Size = fileInfo.Length,
            LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
            RelativePath = relativePath
        };
    }

    /// <summary>
    /// Counts files in a folder without creating FileItem objects
    /// </summary>
    private async Task<int> CountFilesInFolderAsync(
        string sourceFolder,
        HashSet<string> extensions,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(sourceFolder))
        {
            _logger.LogWarning("Source folder does not exist for counting: {Folder}", sourceFolder);
            return 0;
        }

        var count = 0;
        
        try
        {
            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.System | FileAttributes.Hidden,
                ReturnSpecialDirectories = false
            };

            await Task.Run(() =>
            {
                foreach (var filePath in Directory.EnumerateFiles(sourceFolder, "*", enumerationOptions))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var extension = Path.GetExtension(filePath);
                    if (extensions.Contains(extension))
                    {
                        count++;
                    }
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to count files in folder: {Folder}", sourceFolder);
        }

        return count;
    }
}
