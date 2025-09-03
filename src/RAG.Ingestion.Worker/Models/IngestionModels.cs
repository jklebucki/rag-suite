namespace RAG.Ingestion.Worker.Models;

public class DocumentChunk
{
    public string Id { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class IngestionSettings
{
    public string ElasticsearchUrl { get; set; } = "http://localhost:9200";
    public string ElasticsearchUsername { get; set; } = "elastic";
    public string ElasticsearchPassword { get; set; } = "elastic";
    public string IndexName { get; set; } = "rag_documents";
    public string DocumentsPath { get; set; } = "/data/documents";
    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 100;
    public bool ProcessOnStartup { get; set; } = true;
    public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromHours(1);
}
