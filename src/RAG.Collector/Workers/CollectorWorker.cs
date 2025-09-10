using Microsoft.Extensions.Options;
using RAG.Collector.Chunking;
using RAG.Collector.Config;
using RAG.Collector.ContentExtractors;
using RAG.Collector.Enumerators;
using RAG.Collector.Indexing;
using RAG.Collector.Models;

namespace RAG.Collector.Workers;

/// <summary>
/// Main background service for the RAG Collector
/// Periodically scans source folders and indexes documents to Elasticsearch
/// </summary>
public class CollectorWorker : BackgroundService
{
    private readonly ILogger<CollectorWorker> _logger;
    private readonly CollectorOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private DateTime _lastCleanupTime = DateTime.MinValue;

    public CollectorWorker(
        ILogger<CollectorWorker> logger,
        IOptions<CollectorOptions> options,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _options = options.Value;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RAG Collector started");
        _logger.LogInformation("Configured source folders: {Folders}", string.Join(", ", _options.SourceFolders));
        _logger.LogInformation("File extensions: {Extensions}", string.Join(", ", _options.FileExtensions));
        _logger.LogInformation("Elasticsearch URL: {Url}", _options.ElasticsearchUrl);
        _logger.LogInformation("Index name: {Index}", _options.IndexName);
        _logger.LogInformation("Processing interval: {Interval} minutes", _options.ProcessingIntervalMinutes);
        _logger.LogInformation("Orphaned cleanup enabled: {Enabled}, Interval: {Hours} hours, Dry run: {DryRun}",
            _options.EnableOrphanedCleanup, _options.CleanupIntervalHours, _options.DryRunMode);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting document collection cycle at {Time}", DateTimeOffset.Now);

                // Process documents
                await ProcessDocumentsAsync(stoppingToken);

                // Run orphaned document cleanup if enabled and due
                if (_options.EnableOrphanedCleanup && ShouldRunCleanup())
                {
                    await RunOrphanedCleanupAsync(stoppingToken);
                    _lastCleanupTime = DateTime.UtcNow;
                }

                _logger.LogInformation("Document collection cycle completed at {Time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during document collection cycle");
            }

            // Wait for the next processing interval
            await Task.Delay(TimeSpan.FromMinutes(_options.ProcessingIntervalMinutes), stoppingToken);
        }
    }

    /// <summary>
    /// Process documents from configured source folders
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ProcessDocumentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var fileEnumerator = scope.ServiceProvider.GetRequiredService<IFileEnumerator>();

        try
        {
            _logger.LogInformation("Starting file enumeration...");

            // First, get total count for progress reporting
            var totalFiles = await fileEnumerator.GetFileCountAsync(
                _options.SourceFolders,
                _options.FileExtensions,
                cancellationToken);

            _logger.LogInformation("Found {TotalFiles} files to process", totalFiles);

            if (totalFiles == 0)
            {
                _logger.LogInformation("No files found matching the configured criteria");
                return;
            }

            var processedFiles = 0;
            var startTime = DateTime.UtcNow;

            // Enumerate and process files
            await foreach (var fileItem in fileEnumerator.EnumerateFilesAsync(
                _options.SourceFolders,
                _options.FileExtensions,
                cancellationToken))
            {
                processedFiles++;

                // Log progress every 50 files
                if (processedFiles % 50 == 0 || processedFiles == totalFiles)
                {
                    var elapsed = DateTime.UtcNow - startTime;
                    var rate = processedFiles / elapsed.TotalSeconds;
                    _logger.LogInformation("Progress: {Processed}/{Total} files ({Percentage:F1}%) - {Rate:F1} files/sec",
                        processedFiles, totalFiles, (double)processedFiles / totalFiles * 100, rate);
                }

                // Extract content from file
                await ExtractContentAsync(fileItem, cancellationToken);

                // Chunk content if extraction was successful
                var chunks = await ChunkContentAsync(fileItem, cancellationToken);

                // Index chunks if chunking was successful
                var indexedCount = await IndexChunksAsync(chunks, cancellationToken);

                var aclGroupsText = fileItem.AclGroups.Count > 0 ? $"[{string.Join(", ", fileItem.AclGroups)}]" : "[]";
                var contentText = fileItem.IsContentExtracted ? $", Content: {fileItem.ExtractedContent?.Length ?? 0} chars" : ", Content: extraction failed";
                var chunkText = chunks.Count > 0 ? $", Chunks: {chunks.Count}" : "";
                var indexText = indexedCount > 0 ? $", Indexed: {indexedCount}" : "";
                _logger.LogInformation("Processed file: {FileName} ({Size:N0} bytes, {Extension}, Modified: {Modified:yyyy-MM-dd HH:mm:ss}, ACL: {AclGroups}{ContentInfo}{ChunkInfo}{IndexInfo})",
                    fileItem.FileName, fileItem.Size, fileItem.Extension, fileItem.LastWriteTimeUtc, aclGroupsText, contentText, chunkText, indexText);

                // Simulate processing time
                await Task.Delay(10, cancellationToken);
            }

            var totalElapsed = DateTime.UtcNow - startTime;
            _logger.LogInformation("File enumeration completed: {ProcessedFiles} files in {Elapsed:mm\\:ss}",
                processedFiles, totalElapsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("File enumeration was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file enumeration");
        }
    }

    /// <summary>
    /// Extracts content from a file item
    /// </summary>
    private async Task ExtractContentAsync(FileItem fileItem, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Extracting content from file: {FilePath}", fileItem.Path);

            using var scope = _serviceProvider.CreateScope();
            var contentExtractionService = scope.ServiceProvider.GetRequiredService<ContentExtractionService>();

            // Generate file hash before extraction
            if (string.IsNullOrEmpty(fileItem.FileHash))
            {
                fileItem.FileHash = await GenerateFileHashAsync(fileItem.Path, cancellationToken);
            }

            var result = await contentExtractionService.ExtractContentAsync(fileItem.Path, cancellationToken);

            if (result.IsSuccess)
            {
                fileItem.ExtractedContent = result.Content;
                fileItem.ContentMetadata = result.Metadata;
                fileItem.IsContentExtracted = true;

                _logger.LogDebug("Successfully extracted {CharCount} characters from {FilePath}",
                    result.Content.Length, fileItem.Path);
            }
            else
            {
                fileItem.ContentExtractionError = result.ErrorMessage;
                fileItem.IsContentExtracted = false;

                _logger.LogWarning("Content extraction failed for {FilePath}: {ErrorMessage}",
                    fileItem.Path, result.ErrorMessage);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Content extraction cancelled for {FilePath}", fileItem.Path);
            fileItem.ContentExtractionError = "Operation cancelled";
            fileItem.IsContentExtracted = false;
        }
        catch (Exception ex)
        {
            fileItem.ContentExtractionError = ex.Message;
            fileItem.IsContentExtracted = false;

            _logger.LogError(ex, "Unexpected error during content extraction from {FilePath}", fileItem.Path);
        }
    }

    /// <summary>
    /// Chunks extracted content into smaller segments for processing
    /// </summary>
    /// <param name="fileItem">File item with extracted content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of text chunks</returns>
    private async Task<IList<TextChunk>> ChunkContentAsync(FileItem fileItem, CancellationToken cancellationToken)
    {
        if (!fileItem.IsContentExtracted || string.IsNullOrWhiteSpace(fileItem.ExtractedContent))
        {
            _logger.LogDebug("Skipping chunking for {FilePath} - no content extracted", fileItem.Path);
            return new List<TextChunk>();
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var chunkingService = scope.ServiceProvider.GetRequiredService<ChunkingService>();

            var chunks = await chunkingService.ChunkAsync(
                fileItem,
                _options.ChunkSize,
                _options.ChunkOverlap,
                cancellationToken);

            _logger.LogDebug("Successfully chunked {FilePath} into {ChunkCount} chunks",
                fileItem.Path, chunks.Count);

            return chunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error chunking content from {FilePath}", fileItem.Path);
            return new List<TextChunk>();
        }
    }

    /// <summary>
    /// Index chunks in Elasticsearch with embeddings
    /// </summary>
    /// <param name="chunks">Text chunks to index</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of successfully indexed chunks</returns>
    private async Task<int> IndexChunksAsync(IList<TextChunk> chunks, CancellationToken cancellationToken)
    {
        if (!chunks.Any())
        {
            _logger.LogDebug("No chunks to index");
            return 0;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var indexingService = scope.ServiceProvider.GetRequiredService<IndexingService>();

            var indexedCount = await indexingService.IndexFileChunksAsync(chunks, cancellationToken);

            _logger.LogDebug("Successfully indexed {IndexedCount}/{TotalCount} chunks",
                indexedCount, chunks.Count);

            return indexedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing {ChunkCount} chunks", chunks.Count);
            return 0;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RAG Collector is stopping");
        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Generate SHA-256 hash of file content for change detection
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Base64 encoded hash of the file</returns>
    private async Task<string> GenerateFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = System.Security.Cryptography.SHA256.Create();

            var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate hash for file: {FilePath}", filePath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Check if orphaned document cleanup should be run
    /// </summary>
    private bool ShouldRunCleanup()
    {
        if (_lastCleanupTime == DateTime.MinValue)
        {
            // Never run cleanup before, run it now
            return true;
        }

        var timeSinceLastCleanup = DateTime.UtcNow - _lastCleanupTime;
        var cleanupInterval = TimeSpan.FromHours(_options.CleanupIntervalHours);

        return timeSinceLastCleanup >= cleanupInterval;
    }

    /// <summary>
    /// Run orphaned document cleanup
    /// </summary>
    private async Task RunOrphanedCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting orphaned document cleanup...");

            using var scope = _serviceProvider.CreateScope();
            var cleanupService = scope.ServiceProvider.GetRequiredService<IOrphanedDocumentCleanupService>();

            if (_options.DryRunMode)
            {
                var dryRunResult = await cleanupService.DryRunCleanupAsync(cancellationToken);

                _logger.LogInformation("Dry run cleanup completed: {OrphanedFiles} files would be cleaned, {TotalChunks} chunks would be deleted",
                    dryRunResult.OrphanedFileCount, dryRunResult.TotalOrphanedChunks);

                if (dryRunResult.Errors.Any())
                {
                    _logger.LogWarning("Dry run cleanup encountered {ErrorCount} errors: {Errors}",
                        dryRunResult.Errors.Count, string.Join(", ", dryRunResult.Errors));
                }
            }
            else
            {
                var findResult = await cleanupService.FindOrphanedDocumentsAsync(cancellationToken);

                if (findResult.OrphanedFileCount > 0)
                {
                    _logger.LogInformation("Found {OrphanedFiles} orphaned files with {TotalChunks} chunks",
                        findResult.OrphanedFileCount, findResult.TotalOrphanedChunks);

                    var deletedCount = await cleanupService.DeleteOrphanedDocumentsAsync(findResult.OrphanedFilePaths, cancellationToken);

                    _logger.LogInformation("Orphaned cleanup completed: {DeletedCount} documents deleted",
                        deletedCount);
                }
                else
                {
                    _logger.LogInformation("No orphaned documents found");
                }

                if (findResult.Errors.Any())
                {
                    _logger.LogWarning("Cleanup encountered {ErrorCount} errors: {Errors}",
                        findResult.Errors.Count, string.Join(", ", findResult.Errors));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during orphaned document cleanup");
        }
    }
}
