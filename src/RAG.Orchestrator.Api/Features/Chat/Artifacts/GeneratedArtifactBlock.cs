using RAG.DocumentProcessing.Abstractions;

namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public sealed record GeneratedArtifactBlock(
    GeneratedArtifactFormat Format,
    string FileName,
    string Markdown);
