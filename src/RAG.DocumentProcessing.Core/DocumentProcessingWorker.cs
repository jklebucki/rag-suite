using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RAG.DocumentProcessing.Core;

public sealed class DocumentProcessingWorker : BackgroundService
{
    private readonly DocumentProcessingJobService _jobs;
    private readonly DocumentProcessingOptions _options;
    private readonly ILogger<DocumentProcessingWorker> _logger;

    public DocumentProcessingWorker(
        DocumentProcessingJobService jobs,
        IOptions<DocumentProcessingOptions> options,
        ILogger<DocumentProcessingWorker> logger)
    {
        _jobs = jobs;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workers = Enumerable.Range(0, _options.MaxConcurrentJobs)
            .Select(_ => ProcessQueueAsync(stoppingToken));
        await Task.WhenAll(workers);
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var jobId = await _jobs.DequeueAsync(stoppingToken);
                await _jobs.ProcessAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Document processing worker loop failed");
            }
        }
    }
}
