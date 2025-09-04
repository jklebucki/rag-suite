namespace RAG.Collector.Indexing;

/// <summary>
/// Service for checking if files need reindexing based on content changes
/// </summary>
public interface IFileChangeDetectionService
{
    /// <summary>
    /// Check if a file needs to be reindexed
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="fileHash">Content hash of the file</param>
    /// <param name="lastModified">Last modified date of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if file needs reindexing, false if already up to date</returns>
    Task<bool> ShouldReindexFileAsync(string filePath, string fileHash, DateTime lastModified, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record that a file has been successfully indexed
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="fileHash">Content hash of the file</param>
    /// <param name="lastModified">Last modified date of the file</param>
    /// <param name="chunkCount">Number of chunks indexed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordIndexedFileAsync(string filePath, string fileHash, DateTime lastModified, int chunkCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get statistics about indexed files
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Statistics about indexed files</returns>
    Task<FileIndexStats> GetFileIndexStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about file indexing
/// </summary>
public class FileIndexStats
{
    public int TotalIndexedFiles { get; set; }
    public int TotalChunks { get; set; }
    public DateTime? LastIndexedAt { get; set; }
    public Dictionary<string, int> FilesByExtension { get; set; } = new();
}
