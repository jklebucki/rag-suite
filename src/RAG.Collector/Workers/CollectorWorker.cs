using Microsoft.Extensions.Options;
using RAG.Collector.Config;

namespace RAG.Collector.Workers;

/// <summary>
/// Main background service for the RAG Collector
/// Periodically scans source folders and indexes documents to Elasticsearch
/// </summary>
public class CollectorWorker : BackgroundService
{
    private readonly ILogger<CollectorWorker> _logger;
    private readonly CollectorOptions _options;

    public CollectorWorker(
        ILogger<CollectorWorker> logger,
        IOptions<CollectorOptions> options)
    {
        _logger = logger;
        _options = options.Value;
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
        // Placeholder for actual implementation
        _logger.LogInformation("Processing documents (placeholder implementation)");
        await Task.Delay(1000, cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RAG Collector is stopping");
        await base.StopAsync(cancellationToken);
    }
}
