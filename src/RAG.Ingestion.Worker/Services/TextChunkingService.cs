using RAG.Ingestion.Worker.Models;

namespace RAG.Ingestion.Worker.Services;

public interface ITextChunkingService
{
    IEnumerable<DocumentChunk> ChunkDocument(string documentId, string fileName, string content, string fileType, Dictionary<string, object> metadata);
}

public class TextChunkingService : ITextChunkingService
{
    private readonly IngestionSettings _settings;

    public TextChunkingService(IngestionSettings settings)
    {
        _settings = settings;
    }

    public IEnumerable<DocumentChunk> ChunkDocument(string documentId, string fileName, string content, string fileType, Dictionary<string, object> metadata)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            yield break;
        }

        var chunks = new List<DocumentChunk>();
        var chunkSize = _settings.ChunkSize;
        var chunkOverlap = _settings.ChunkOverlap;

        // Split by paragraphs first
        var paragraphs = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = "";
        var chunkIndex = 0;

        foreach (var paragraph in paragraphs)
        {
            // If adding this paragraph would exceed chunk size, save current chunk
            if (currentChunk.Length + paragraph.Length > chunkSize && !string.IsNullOrWhiteSpace(currentChunk))
            {
                yield return CreateChunk(documentId, fileName, currentChunk.Trim(), fileType, chunkIndex, metadata);
                
                // Start new chunk with overlap
                currentChunk = GetOverlapText(currentChunk, chunkOverlap) + " " + paragraph;
                chunkIndex++;
            }
            else
            {
                currentChunk += (string.IsNullOrWhiteSpace(currentChunk) ? "" : "\n\n") + paragraph;
            }
        }

        // Add the last chunk if it has content
        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            yield return CreateChunk(documentId, fileName, currentChunk.Trim(), fileType, chunkIndex, metadata);
        }
    }

    private DocumentChunk CreateChunk(string documentId, string fileName, string content, string fileType, int chunkIndex, Dictionary<string, object> metadata)
    {
        return new DocumentChunk
        {
            Id = $"{documentId}_chunk_{chunkIndex}",
            DocumentId = documentId,
            FileName = fileName,
            Content = content,
            FileType = fileType,
            ChunkIndex = chunkIndex,
            CreatedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>(metadata)
            {
                ["chunkIndex"] = chunkIndex,
                ["chunkLength"] = content.Length
            }
        };
    }

    private string GetOverlapText(string text, int overlapSize)
    {
        if (text.Length <= overlapSize)
            return text;

        // Try to find a word boundary near the overlap size
        var startIndex = text.Length - overlapSize;
        var spaceIndex = text.IndexOf(' ', startIndex);
        
        if (spaceIndex > 0 && spaceIndex < text.Length - 50) // Don't use if space is too close to end
        {
            return text.Substring(spaceIndex + 1);
        }

        return text.Substring(startIndex);
    }
}
