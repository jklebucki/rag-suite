using RAG.DocumentProcessing.Abstractions;

namespace RAG.DocumentProcessing.Core;

public interface IDocumentProcessingJobService
{
    Task<DocumentJobAccepted> SubmitAsync(
        DocumentDescriptor document,
        Stream content,
        CancellationToken cancellationToken);

    DocumentJobStatus? GetStatus(string jobId);

    DocumentProcessingResult? GetResult(string jobId);

    Task<bool> DeleteAsync(string jobId, CancellationToken cancellationToken);

    Task ProcessAsync(string jobId, CancellationToken cancellationToken);
}
