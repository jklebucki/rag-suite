using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using RAG.Collector.Config;
using RAG.Collector.Enumerators;
using RAG.Collector.ContentExtractors;
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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting document collection cycle at {Time}", DateTimeOffset.Now);

                // TODO: Implement the actual collection logic in next steps
                await ProcessDocumentsAsync(stoppingToken);

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
                
                var aclGroupsText = fileItem.AclGroups.Count > 0 ? $"[{string.Join(", ", fileItem.AclGroups)}]" : "[]";
                var contentText = fileItem.IsContentExtracted ? $", Content: {fileItem.ExtractedContent?.Length ?? 0} chars" : ", Content: extraction failed";
                _logger.LogInformation("Processed file: {FileName} ({Size:N0} bytes, {Extension}, Modified: {Modified:yyyy-MM-dd HH:mm:ss}, ACL: {AclGroups}{ContentInfo})", 
                    fileItem.FileName, fileItem.Size, fileItem.Extension, fileItem.LastWriteTimeUtc, aclGroupsText, contentText);
                
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
        catch (Exception ex)
        {
            fileItem.ContentExtractionError = ex.Message;
            fileItem.IsContentExtracted = false;
            
            _logger.LogError(ex, "Unexpected error during content extraction from {FilePath}", fileItem.Path);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RAG Collector is stopping");
        await base.StopAsync(cancellationToken);
    }
}
