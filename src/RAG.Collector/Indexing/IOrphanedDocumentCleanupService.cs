namespace RAG.Collector.Indexing;

/// <summary>
/// Service for detecting and cleaning up orphaned documents in Elasticsearch
/// that reference files that no longer exist on the file system
/// </summary>
public interface IOrphanedDocumentCleanupService
{
    /// <summary>
    /// Find all documents in Elasticsearch that reference non-existent files
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cleanup result with found orphaned documents</returns>
    Task<OrphanedDocumentCleanupResult> FindOrphanedDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete documents for files that no longer exist
    /// </summary>
    /// <param name="orphanedFilePaths">List of file paths that no longer exist</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of documents deleted</returns>
    Task<int> DeleteOrphanedDocumentsAsync(IEnumerable<string> orphanedFilePaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get statistics about orphaned documents cleanup
    /// </summary>
    /// <returns>Cleanup statistics</returns>
    Task<OrphanedCleanupStats> GetCleanupStatsAsync();

    /// <summary>
    /// Perform a dry run to see what would be deleted without actually deleting
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cleanup result showing what would be deleted</returns>
    Task<OrphanedDocumentCleanupResult> DryRunCleanupAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of orphaned document cleanup operation
/// </summary>
public class OrphanedDocumentCleanupResult
{
    /// <summary>
    /// List of file paths that no longer exist on disk
    /// </summary>
    public List<string> OrphanedFilePaths { get; set; } = new();

    /// <summary>
    /// Total number of orphaned chunks found
    /// </summary>
    public int TotalOrphanedChunks { get; set; }

    /// <summary>
    /// Number of unique orphaned files
    /// </summary>
    public int OrphanedFileCount => OrphanedFilePaths.Count;

    /// <summary>
    /// Number of documents actually deleted (0 for dry run)
    /// </summary>
    public int DocumentsDeleted { get; set; }

    /// <summary>
    /// Whether this was a dry run (no actual deletions)
    /// </summary>
    public bool IsDryRun { get; set; }

    /// <summary>
    /// Breakdown of orphaned chunks per file
    /// </summary>
    public Dictionary<string, int> ChunksPerFile { get; set; } = new();

    /// <summary>
    /// Any errors encountered during the operation
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Statistics about orphaned document cleanup operations
/// </summary>
public class OrphanedCleanupStats
{
    /// <summary>
    /// Total number of cleanup operations performed
    /// </summary>
    public int TotalCleanupOperations { get; set; }

    /// <summary>
    /// Total number of orphaned documents deleted
    /// </summary>
    public int TotalDocumentsDeleted { get; set; }

    /// <summary>
    /// Total number of orphaned files found and cleaned
    /// </summary>
    public int TotalOrphanedFilesDeleted { get; set; }

    /// <summary>
    /// Last cleanup operation timestamp
    /// </summary>
    public DateTime? LastCleanupAt { get; set; }

    /// <summary>
    /// Average time taken for cleanup operations
    /// </summary>
    public TimeSpan? AverageCleanupTime { get; set; }
}
