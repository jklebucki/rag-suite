using RAG.Ingestion.Worker.Models;
using RAG.Ingestion.Worker.Services;
using RAG.Shared;

namespace RAG.Ingestion.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDocumentIngestionService _ingestionService;
    private readonly IngestionSettings _settings;

    public Worker(
        ILogger<Worker> logger,
        IDocumentIngestionService ingestionService,
        IngestionSettings settings)
    {
        _logger = logger;
        _ingestionService = ingestionService;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RAG Ingestion Worker starting on {OS}...", PathHelper.GetOSName());

        // Normalize and ensure documents path exists
        var documentsPath = PathHelper.NormalizePath(_settings.DocumentsPath);
        PathHelper.EnsureDirectoryExists(documentsPath);

        _logger.LogInformation("Using documents path: {DocumentsPath}", documentsPath);

        // Initialize index
        var initialized = await _ingestionService.InitializeIndexAsync();
        if (!initialized)
        {
            _logger.LogError("Failed to initialize Elasticsearch index. Stopping worker.");
            return;
        }

        // Process documents on startup if configured
        if (_settings.ProcessOnStartup && Directory.Exists(documentsPath))
        {
            _logger.LogInformation("Processing documents on startup from: {DocumentsPath}", documentsPath);
            var processed = await _ingestionService.ProcessDirectoryAsync(documentsPath, recursive: true);
            _logger.LogInformation("Processed {Count} documents on startup", processed);
        }

        // Main processing loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);

                // Check for new documents periodically
                if (Directory.Exists(documentsPath))
                {
                    var currentCount = await _ingestionService.GetIndexedDocumentCountAsync();
                    _logger.LogInformation("Current indexed document count: {Count}", currentCount);

                    // You could implement file watching here for real-time processing
                    // For now, we just log the status
                }

                await Task.Delay(_settings.ProcessingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in worker execution");
                // Continue running despite errors
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("RAG Ingestion Worker stopping...");
    }
}
