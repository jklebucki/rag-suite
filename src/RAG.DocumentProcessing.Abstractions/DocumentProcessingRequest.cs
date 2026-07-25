namespace RAG.DocumentProcessing.Abstractions;

public sealed record DocumentProcessingRequest(
    DocumentDescriptor Document,
    Stream Content,
    bool ForceOcr = false);
