namespace RAG.Abstractions.Conversion;

public interface IGotenbergClient
{
    Task<Stream?> ConvertToPdfAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task<bool> CanConvertAsync(string fileExtension);
}