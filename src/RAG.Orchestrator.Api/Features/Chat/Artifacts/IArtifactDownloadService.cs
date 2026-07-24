namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public interface IArtifactDownloadService
{
    Task<byte[]?> GetContentAsync(string artifactId, string userId, CancellationToken cancellationToken);
}
