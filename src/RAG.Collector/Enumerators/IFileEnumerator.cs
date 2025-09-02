using RAG.Collector.Models;

namespace RAG.Collector.Enumerators;

/// <summary>
/// Interface for enumerating files from various sources
/// </summary>
public interface IFileEnumerator
{
    /// <summary>
    /// Enumerates files from the specified source folders that match the given file extensions
    /// </summary>
    /// <param name="sourceFolders">List of folder paths to scan (supports UNC paths)</param>
    /// <param name="fileExtensions">List of file extensions to include (e.g., ".pdf", ".docx")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of discovered files</returns>
    IAsyncEnumerable<FileItem> EnumerateFilesAsync(
        IEnumerable<string> sourceFolders,
        IEnumerable<string> fileExtensions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of files that would be enumerated (for progress reporting)
    /// </summary>
    /// <param name="sourceFolders">List of folder paths to scan</param>
    /// <param name="fileExtensions">List of file extensions to include</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total file count</returns>
    Task<int> GetFileCountAsync(
        IEnumerable<string> sourceFolders,
        IEnumerable<string> fileExtensions,
        CancellationToken cancellationToken = default);
}
