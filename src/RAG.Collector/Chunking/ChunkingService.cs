using RAG.Collector.Models;
using Microsoft.Extensions.Logging;

namespace RAG.Collector.Chunking;

/// <summary>
/// Service for coordinating text chunking operations
/// </summary>
public class ChunkingService
{
    private readonly ILogger<ChunkingService> _logger;
    private readonly Dictionary<string, ITextChunker> _chunkers;

    public ChunkingService(ILogger<ChunkingService> logger)
    {
        _logger = logger;
        _chunkers = new Dictionary<string, ITextChunker>();
        
        RegisterChunkers();
    }

    /// <summary>
    /// Gets the total number of registered chunkers
    /// </summary>
    public int ChunkerCount => _chunkers.Count;

    /// <summary>
    /// Gets all supported content types across all chunkers
    /// </summary>
    public IEnumerable<string> SupportedContentTypes => 
        _chunkers.Values.SelectMany(c => c.SupportedContentTypes).Distinct();

    /// <summary>
    /// Chunks text content using the appropriate chunker for the content type
    /// </summary>
    /// <param name="fileItem">File item with extracted content</param>
    /// <param name="chunkSize">Maximum chunk size in characters</param>
    /// <param name="overlap">Overlap between chunks in characters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of text chunks</returns>
    public async Task<IList<TextChunk>> ChunkAsync(
        FileItem fileItem,
        int chunkSize = 1200,
        int overlap = 200,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileItem.ExtractedContent))
        {
            _logger.LogWarning("No extracted content found for file: {FilePath}", fileItem.Path);
            return new List<TextChunk>();
        }

        var contentType = DetermineContentType(fileItem);
        var chunker = GetChunkerForContentType(contentType);

        if (chunker == null)
        {
            _logger.LogWarning("No chunker found for content type: {ContentType}, file: {FilePath}", 
                contentType, fileItem.Path);
            return new List<TextChunk>();
        }

        try
        {
            _logger.LogDebug("Chunking content for file: {FilePath} using {ChunkerType}", 
                fileItem.Path, chunker.GetType().Name);

            var metadata = CreateChunkMetadata(fileItem);
            var chunks = await chunker.ChunkAsync(
                fileItem.ExtractedContent,
                metadata,
                chunkSize,
                overlap,
                cancellationToken);

            // Set source file reference and file hash for all chunks
            foreach (var chunk in chunks)
            {
                chunk.SourceFile = fileItem;
                chunk.FileHash = fileItem.FileHash;
            }

            _logger.LogInformation("Successfully chunked file: {FilePath} into {ChunkCount} chunks", 
                fileItem.Path, chunks.Count);

            return chunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error chunking content for file: {FilePath}", fileItem.Path);
            return new List<TextChunk>();
        }
    }

    /// <summary>
    /// Gets chunker for specific content type
    /// </summary>
    /// <param name="contentType">Content type to find chunker for</param>
    /// <returns>Chunker instance or null if not found</returns>
    public ITextChunker? GetChunkerForContentType(string contentType)
    {
        return _chunkers.Values.FirstOrDefault(c => c.CanChunk(contentType));
    }

    private void RegisterChunkers()
    {
        var sentenceChunker = new SentenceAwareChunker();
        var pdfChunker = new PdfAwareChunker();
        var officeChunker = new OfficeDocumentChunker();

        // Register chunkers by their supported content types
        foreach (var contentType in sentenceChunker.SupportedContentTypes)
        {
            _chunkers[contentType] = sentenceChunker;
        }

        foreach (var contentType in pdfChunker.SupportedContentTypes)
        {
            _chunkers[contentType] = pdfChunker;
        }

        foreach (var contentType in officeChunker.SupportedContentTypes)
        {
            _chunkers[contentType] = officeChunker;
        }

        _logger.LogInformation("Chunking service initialized with {ChunkerCount} chunkers supporting {ContentTypeCount} content types",
            ChunkerCount, SupportedContentTypes.Count());
        
        _logger.LogDebug("Supported content types: {ContentTypes}", 
            string.Join(", ", SupportedContentTypes));
    }

    private string DetermineContentType(FileItem fileItem)
    {
        var extension = Path.GetExtension(fileItem.Path).ToLowerInvariant();
        
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".json" => "application/json",
            ".csv" => "text/csv",
            ".xml" => "text/xml",
            ".yaml" or ".yml" => "text/yaml",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "text/plain" // Default fallback
        };
    }

    private Dictionary<string, object> CreateChunkMetadata(FileItem fileItem)
    {
        var metadata = new Dictionary<string, object>
        {
            ["source_file"] = fileItem.RelativePath ?? fileItem.Path,
            ["file_size"] = fileItem.Size,
            ["last_modified"] = fileItem.LastWriteTimeUtc,
            ["file_extension"] = Path.GetExtension(fileItem.Path),
            ["content_extracted_at"] = DateTime.UtcNow
        };

        // Add content metadata if available
        if (fileItem.ContentMetadata != null)
        {
            foreach (var (key, value) in fileItem.ContentMetadata)
            {
                metadata[$"content_{key}"] = value;
            }
        }

        // Add ACL information if available
        if (fileItem.AclGroups?.Any() == true)
        {
            metadata["acl_groups"] = fileItem.AclGroups.ToList();
        }

        return metadata;
    }
}
