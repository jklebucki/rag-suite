namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public sealed record ArtifactDownload(string ContentType, string FileName, byte[] Content);
