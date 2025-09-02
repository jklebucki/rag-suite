namespace RAG.Collector.Models;

/// <summary>
/// Represents a chunk of text extracted from a document
/// </summary>
public class TextChunk
{
    /// <summary>
    /// Unique identifier for the chunk
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Text content of the chunk
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Source file information
    /// </summary>
    public FileItem SourceFile { get; set; } = null!;

    /// <summary>
    /// Position of the chunk within the original document
    /// </summary>
    public ChunkPosition Position { get; set; } = new();

    /// <summary>
    /// Metadata associated with the chunk
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Size of the chunk in characters
    /// </summary>
    public int Size => Content.Length;

    /// <summary>
    /// Estimated token count (rough approximation)
    /// </summary>
    public int EstimatedTokens => (int)Math.Ceiling(Content.Length / 4.0);

    /// <summary>
    /// Hash of the content for deduplication
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// When this chunk was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Position information for a text chunk
/// </summary>
public class ChunkPosition
{
    /// <summary>
    /// Starting character position in the original document
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Ending character position in the original document
    /// </summary>
    public int EndIndex { get; set; }

    /// <summary>
    /// Page number (if applicable)
    /// </summary>
    public int? Page { get; set; }

    /// <summary>
    /// Section or heading context
    /// </summary>
    public string? Section { get; set; }

    /// <summary>
    /// Sequential chunk number within the document
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Total number of chunks in the document
    /// </summary>
    public int TotalChunks { get; set; }
}
