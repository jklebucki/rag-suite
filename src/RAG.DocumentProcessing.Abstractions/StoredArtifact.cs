namespace RAG.DocumentProcessing.Abstractions;

public sealed record StoredArtifact(
    string Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
