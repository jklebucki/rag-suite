using RAG.DocumentProcessing.Abstractions;

namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public sealed class ExpiredArtifactCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredArtifactCleanupService> _logger;

    public ExpiredArtifactCleanupService(IServiceScopeFactory scopeFactory, ILogger<ExpiredArtifactCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var store = scope.ServiceProvider.GetRequiredService<ITemporaryArtifactStore>();
                await store.DeleteExpiredAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generated artifact cleanup failed");
            }
        }
    }
}
