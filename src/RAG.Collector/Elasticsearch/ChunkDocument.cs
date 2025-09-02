using RAG.Collector.Models;

namespace RAG.Collector.Elasticsearch;

/// <summary>
/// Document model for Elasticsearch indexing
/// </summary>
public class ChunkDocument
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
    /// Embedding vector for semantic search
    /// </summary>
    public float[] Embedding { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Source file path
    /// </summary>
    public string SourceFile { get; set; } = string.Empty;

    /// <summary>
    /// File extension
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Last modified date of source file
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Chunk position within the document
    /// </summary>
    public ChunkPositionInfo Position { get; set; } = new();

    /// <summary>
    /// Content metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// ACL groups with access to this chunk
    /// </summary>
    public List<string> AclGroups { get; set; } = new();

    /// <summary>
    /// Hash of the content for deduplication
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// When this document was indexed
    /// </summary>
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Estimated token count
    /// </summary>
    public int EstimatedTokens { get; set; }

    /// <summary>
    /// Embedding model information
    /// </summary>
    public EmbeddingInfo EmbeddingDetails { get; set; } = new();

    /// <summary>
    /// Create ChunkDocument from TextChunk and embedding
    /// </summary>
    public static ChunkDocument FromTextChunk(TextChunk chunk, float[] embedding, string modelName)
    {
        var sourceFile = chunk.SourceFile;
        
        return new ChunkDocument
        {
            Id = chunk.Id,
            Content = chunk.Content,
            Embedding = embedding,
            SourceFile = sourceFile?.Path ?? "unknown",
            FileExtension = sourceFile?.Extension ?? "",
            FileSize = sourceFile?.Size ?? 0,
            LastModified = sourceFile?.LastWriteTimeUtc ?? DateTime.MinValue,
            Position = new ChunkPositionInfo
            {
                StartIndex = chunk.Position.StartIndex,
                EndIndex = chunk.Position.EndIndex,
                ChunkIndex = chunk.Position.ChunkIndex,
                TotalChunks = chunk.Position.TotalChunks,
                Page = chunk.Position.Page,
                Section = chunk.Position.Section
            },
            Metadata = new Dictionary<string, object>(chunk.Metadata),
            AclGroups = sourceFile?.AclGroups?.ToList() ?? new List<string>(),
            ContentHash = chunk.ContentHash,
            EstimatedTokens = chunk.EstimatedTokens,
            EmbeddingDetails = new EmbeddingInfo
            {
                ModelName = modelName,
                Dimensions = embedding.Length,
                GeneratedAt = DateTime.UtcNow
            }
        };
    }
}

/// <summary>
/// Chunk position information for Elasticsearch
/// </summary>
public class ChunkPositionInfo
{
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public int ChunkIndex { get; set; }
    public int TotalChunks { get; set; }
    public int? Page { get; set; }
    public string? Section { get; set; }
}

/// <summary>
/// Embedding model information
/// </summary>
public class EmbeddingInfo
{
    public string ModelName { get; set; } = string.Empty;
    public int Dimensions { get; set; }
    public DateTime GeneratedAt { get; set; }
}
