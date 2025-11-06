using RAG.Collector.Elasticsearch;

namespace RAG.Collector.Indexing;

/// <summary>
/// Service for detecting and cleaning up orphaned documents in Elasticsearch
/// </summary>
public class OrphanedDocumentCleanupService : IOrphanedDocumentCleanupService
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IFileChangeDetectionService _fileChangeDetectionService;
    private readonly ILogger<OrphanedDocumentCleanupService> _logger;

    public OrphanedDocumentCleanupService(
        IElasticsearchService elasticsearchService,
        IFileChangeDetectionService fileChangeDetectionService,
        ILogger<OrphanedDocumentCleanupService> logger)
    {
        _elasticsearchService = elasticsearchService;
        _fileChangeDetectionService = fileChangeDetectionService;
        _logger = logger;
    }

    public async Task<OrphanedDocumentCleanupResult> FindOrphanedDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var result = new OrphanedDocumentCleanupResult { IsDryRun = false };

        try
        {
            _logger.LogInformation("Starting orphaned document detection...");

            // Get all unique file paths from Elasticsearch
            var indexedFilePaths = await GetAllIndexedFilePathsAsync(cancellationToken);
            _logger.LogInformation("Found {Count} unique files in Elasticsearch index", indexedFilePaths.Count);

            if (!indexedFilePaths.Any())
            {
                _logger.LogInformation("No files found in index, nothing to cleanup");
                return result;
            }

            // Check which files no longer exist on disk
            var orphanedFiles = new List<string>();
            var chunksPerFile = new Dictionary<string, int>();

            foreach (var filePath in indexedFilePaths.Keys)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    if (!File.Exists(filePath))
                    {
                        orphanedFiles.Add(filePath);
                        chunksPerFile[filePath] = indexedFilePaths[filePath];
                        _logger.LogDebug("File no longer exists: {FilePath} (had {ChunkCount} chunks)",
                            filePath, indexedFilePaths[filePath]);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking file existence: {FilePath}", filePath);
                    result.Errors.Add($"Error checking file {filePath}: {ex.Message}");
                }
            }

            result.OrphanedFilePaths = orphanedFiles;
            result.ChunksPerFile = chunksPerFile;
            result.TotalOrphanedChunks = chunksPerFile.Values.Sum();

            _logger.LogInformation("Found {OrphanedFileCount} orphaned files with {TotalChunks} chunks total",
                result.OrphanedFileCount, result.TotalOrphanedChunks);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during orphaned document detection");
            result.Errors.Add($"Detection error: {ex.Message}");
            return result;
        }
    }

    public async Task<int> DeleteOrphanedDocumentsAsync(IEnumerable<string> orphanedFilePaths, CancellationToken cancellationToken = default)
    {
        var deletedCount = 0;
        var filePaths = orphanedFilePaths.ToList();

        if (!filePaths.Any())
        {
            _logger.LogInformation("No orphaned files to delete");
            return 0;
        }

        _logger.LogInformation("Starting deletion of documents for {FileCount} orphaned files", filePaths.Count);

        try
        {
            foreach (var filePath in filePaths)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var deletedForFile = await _elasticsearchService.DeleteDocumentsBySourceFileAsync(filePath, cancellationToken);
                    deletedCount += deletedForFile;

                    _logger.LogInformation("Deleted {Count} documents for orphaned file: {FilePath}",
                        deletedForFile, filePath);

                    // Also delete the file metadata
                    var metadataDeleted = await _fileChangeDetectionService.DeleteFileMetadataAsync(filePath, cancellationToken);
                    if (metadataDeleted)
                    {
                        _logger.LogDebug("Deleted metadata for orphaned file: {FilePath}", filePath);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to delete metadata for orphaned file: {FilePath}", filePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting documents for file: {FilePath}", filePath);
                }
            }

            _logger.LogInformation("Orphaned document cleanup completed: {DeletedCount} documents deleted for {FileCount} files",
                deletedCount, filePaths.Count);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during orphaned document deletion");
            return deletedCount;
        }
    }

    public async Task<OrphanedDocumentCleanupResult> DryRunCleanupAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting dry run orphaned document cleanup...");

        var result = await FindOrphanedDocumentsAsync(cancellationToken);
        result.IsDryRun = true;
        result.DocumentsDeleted = 0; // No actual deletions in dry run

        if (result.OrphanedFileCount > 0)
        {
            _logger.LogInformation("DRY RUN: Would delete {TotalChunks} chunks from {FileCount} orphaned files:",
                result.TotalOrphanedChunks, result.OrphanedFileCount);

            foreach (var orphanedFile in result.ChunksPerFile)
            {
                _logger.LogInformation("  - {FilePath}: {ChunkCount} chunks",
                    orphanedFile.Key, orphanedFile.Value);
            }
        }
        else
        {
            _logger.LogInformation("DRY RUN: No orphaned documents found");
        }

        return result;
    }


    /// <summary>
    /// Get all unique file paths currently indexed in Elasticsearch with their chunk counts
    /// </summary>
    private async Task<Dictionary<string, int>> GetAllIndexedFilePathsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Use the ElasticsearchService method to get all indexed file paths
            var filePaths = await _elasticsearchService.GetAllSourceFilePathsAsync(cancellationToken);

            _logger.LogDebug("Retrieved {FileCount} unique files from Elasticsearch", filePaths.Count);
            return filePaths;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indexed file paths from Elasticsearch");
            return new Dictionary<string, int>();
        }
    }
}
