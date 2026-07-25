namespace RAG.DocumentProcessing.Abstractions;

public sealed record DocumentJobAccepted(
    string JobId,
    DocumentJobState Status,
    DateTimeOffset AcceptedAt);
