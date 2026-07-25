using RAG.DocumentProcessing.Abstractions;

namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public interface IGeneratedArtifactService
{
    Task<ArtifactGenerationResult> ProcessAsync(
        string response,
        string userId,
        string sessionId,
        string assistantMessageId,
        CancellationToken cancellationToken);

    Task<ArtifactGenerationResult> CreateAsync(
        GeneratedArtifactFormat format,
        string fileName,
        string markdown,
        string userId,
        string sessionId,
        string assistantMessageId,
        CancellationToken cancellationToken);
}
