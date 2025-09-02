using RAG.Collector.Models;

namespace RAG.Collector.Chunking;

/// <summary>
/// Interface for text chunking strategies
/// </summary>
public interface ITextChunker
{
    /// <summary>
    /// Supported content types for this chunker
    /// </summary>
    IEnumerable<string> SupportedContentTypes { get; }

    /// <summary>
    /// Chunks text into smaller segments
    /// </summary>
    /// <param name="content">Text content to chunk</param>
    /// <param name="metadata">Content metadata</param>
    /// <param name="chunkSize">Maximum chunk size in characters</param>
    /// <param name="overlap">Overlap between chunks in characters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of text chunks</returns>
    Task<IList<TextChunk>> ChunkAsync(
        string content, 
        Dictionary<string, object> metadata,
        int chunkSize = 1200,
        int overlap = 200,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if this chunker can handle the given content type
    /// </summary>
    /// <param name="contentType">Content type to check</param>
    /// <returns>True if supported</returns>
    bool CanChunk(string contentType);
}
