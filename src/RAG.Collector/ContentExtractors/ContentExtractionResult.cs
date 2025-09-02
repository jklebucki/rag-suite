namespace RAG.Collector.ContentExtractors;

/// <summary>
/// Result of content extraction from a file
/// </summary>
public class ContentExtractionResult
{
    /// <summary>
    /// Extracted text content
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// File metadata (title, author, etc.)
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Number of pages (for documents that support pagination)
    /// </summary>
    public int? PageCount { get; init; }

    /// <summary>
    /// Whether extraction was successful
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if extraction failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful extraction result
    /// </summary>
    public static ContentExtractionResult Success(string content, Dictionary<string, string>? metadata = null, int? pageCount = null)
    {
        return new ContentExtractionResult
        {
            Content = content,
            Metadata = metadata ?? new Dictionary<string, string>(),
            PageCount = pageCount,
            IsSuccess = true
        };
    }

    /// <summary>
    /// Creates a failed extraction result
    /// </summary>
    public static ContentExtractionResult Failure(string errorMessage)
    {
        return new ContentExtractionResult
        {
            Content = string.Empty,
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
