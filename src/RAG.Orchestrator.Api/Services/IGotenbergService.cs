namespace RAG.Orchestrator.Api.Services;

public interface IGotenbergService
{
    Task<Stream?> ConvertToPdfAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task<bool> CanConvertAsync(string fileExtension);
}