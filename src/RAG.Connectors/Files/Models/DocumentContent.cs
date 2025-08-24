namespace RAG.Connectors.Files.Models;

public class DocumentContent
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public long FileSize { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string[] Tags { get; set; } = Array.Empty<string>();
}
