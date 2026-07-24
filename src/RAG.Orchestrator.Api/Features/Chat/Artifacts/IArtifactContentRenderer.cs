using RAG.DocumentProcessing.Abstractions;

namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public interface IArtifactContentRenderer
{
    byte[] Render(GeneratedArtifactFormat format, string markdown);

    string GetContentType(GeneratedArtifactFormat format);
}
