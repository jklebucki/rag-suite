namespace RAG.DocumentProcessing.Abstractions;

public interface IDocumentProcessingProvider
{
    string Name { get; }

    bool CanProcess(DocumentDescriptor document);

    Task<DocumentProcessingResult> ProcessAsync(
        DocumentProcessingRequest request,
        CancellationToken cancellationToken);
}
