namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public interface IGeneratedArtifactService
{
    Task<ArtifactGenerationResult> ProcessAsync(
        string response,
        string userId,
        string sessionId,
        string assistantMessageId,
        CancellationToken cancellationToken);
}
