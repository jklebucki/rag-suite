namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public interface IArtifactSessionCleanupService
{
    Task DeleteForSessionAsync(string userId, string sessionId, CancellationToken cancellationToken);
}
