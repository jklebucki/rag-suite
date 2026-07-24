namespace RAG.DocumentProcessing.Abstractions;

public sealed record DocumentDescriptor(
    string FileName,
    string ContentType,
    long SizeBytes);
