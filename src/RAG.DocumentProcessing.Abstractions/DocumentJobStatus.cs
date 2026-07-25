namespace RAG.DocumentProcessing.Abstractions;

public sealed record DocumentJobStatus(
    string JobId,
    DocumentJobState Status,
    int Progress,
    int? PageCount,
    string? Provider,
    string? ErrorCode);
