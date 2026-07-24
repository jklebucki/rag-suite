namespace RAG.DocumentProcessing.Abstractions;

public interface IDocumentProcessingClient
{
    Task<DocumentJobAccepted> SubmitAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);

    Task<DocumentJobStatus> GetStatusAsync(string jobId, CancellationToken cancellationToken);

    Task<DocumentProcessingResult> GetResultAsync(string jobId, CancellationToken cancellationToken);
}
