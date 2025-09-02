namespace RAG.Collector.ContentExtractors;

/// <summary>
/// Interface for extracting text content from various file types
/// </summary>
public interface IContentExtractor
{
    /// <summary>
    /// Gets the file extensions supported by this extractor (e.g., ".pdf", ".docx")
    /// </summary>
    IEnumerable<string> SupportedExtensions { get; }

    /// <summary>
    /// Extracts content from the specified file
    /// </summary>
    /// <param name="filePath">Path to the file to extract content from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extraction result containing content and metadata</returns>
    Task<ContentExtractionResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if this extractor can handle the specified file extension
    /// </summary>
    /// <param name="extension">File extension (including the dot)</param>
    /// <returns>True if this extractor supports the extension</returns>
    bool CanExtract(string extension);
}
