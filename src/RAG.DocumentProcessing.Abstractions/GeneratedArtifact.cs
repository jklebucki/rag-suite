namespace RAG.DocumentProcessing.Abstractions;

public sealed record GeneratedArtifact(
    GeneratedArtifactFormat Format,
    string FileName,
    string ContentType,
    byte[] Content,
    string UserId,
    string SessionId,
    string AssistantMessageId);
