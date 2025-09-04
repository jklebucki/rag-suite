namespace RAG.Collector.Models;

/// <summary>
/// Represents a file discovered during enumeration
/// </summary>
public class FileItem
{
    /// <summary>
    /// Full path to the file (including UNC paths)
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// File extension (including the dot, e.g., ".pdf")
    /// </summary>
    public required string Extension { get; init; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Last write time in UTC
    /// </summary>
    public DateTime LastWriteTimeUtc { get; init; }

    /// <summary>
    /// File name without path
    /// </summary>
    public string FileName => System.IO.Path.GetFileName(Path);

    /// <summary>
    /// File name without extension
    /// </summary>
    public string FileNameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(Path);

    /// <summary>
    /// Directory path containing the file
    /// </summary>
    public string DirectoryPath => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;

    /// <summary>
    /// Relative path from the source folder (for display purposes)
    /// </summary>
    public string? RelativePath { get; init; }

    /// <summary>
    /// List of Active Directory group names that have access to this file
    /// </summary>
    public List<string> AclGroups { get; set; } = new();

    /// <summary>
    /// Extracted content from the file (null if not yet extracted)
    /// </summary>
    public string? ExtractedContent { get; set; }

    /// <summary>
    /// Content extraction metadata
    /// </summary>
    public Dictionary<string, string> ContentMetadata { get; set; } = new();

    /// <summary>
    /// Whether content extraction was successful
    /// </summary>
    public bool IsContentExtracted { get; set; }

    /// <summary>
    /// Error message if content extraction failed
    /// </summary>
    public string? ContentExtractionError { get; set; }

    /// <summary>
    /// Hash of the entire file content for change detection
    /// </summary>
    public string FileHash { get; set; } = string.Empty;

    public override string ToString()
    {
        var groupsText = AclGroups.Count > 0 ? $"[{string.Join(", ", AclGroups)}]" : "[]";
        var contentText = IsContentExtracted ? $", Content: {ExtractedContent?.Length ?? 0} chars" : ", Content: not extracted";
        return $"FileItem {{ Path: {Path}, Extension: {Extension}, Size: {Size:N0} bytes, LastWrite: {LastWriteTimeUtc:yyyy-MM-dd HH:mm:ss} UTC, Groups: {groupsText}{contentText} }}";
    }
}
