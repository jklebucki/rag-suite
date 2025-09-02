using System.ComponentModel.DataAnnotations;

namespace RAG.Collector.Config;

/// <summary>
/// Configuration options for the RAG Collector service
/// </summary>
public class CollectorOptions
{
    public const string SectionName = "Collector";

    /// <summary>
    /// Source folders to scan for documents (UNC paths supported)
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one source folder must be specified")]
    public List<string> SourceFolders { get; set; } = new();

    /// <summary>
    /// File extensions to process (e.g., .pdf, .docx, .xlsx, .pptx, .txt, .csv, .md)
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one file extension must be specified")]
    public List<string> FileExtensions { get; set; } = new();

    /// <summary>
    /// Size of text chunks in characters
    /// </summary>
    [Range(100, 10000, ErrorMessage = "Chunk size must be between 100 and 10000 characters")]
    public int ChunkSize { get; set; } = 1200;

    /// <summary>
    /// Overlap between chunks in characters
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Chunk overlap must be between 0 and 1000 characters")]
    public int ChunkOverlap { get; set; } = 200;

    /// <summary>
    /// Elasticsearch URL
    /// </summary>
    [Required]
    [Url(ErrorMessage = "Elasticsearch URL must be a valid URL")]
    public string ElasticsearchUrl { get; set; } = "http://localhost:9200";

    /// <summary>
    /// Elasticsearch username for authentication
    /// </summary>
    public string? ElasticsearchUsername { get; set; }

    /// <summary>
    /// Elasticsearch password for authentication
    /// </summary>
    public string? ElasticsearchPassword { get; set; }

    /// <summary>
    /// Elasticsearch API key for authentication (alternative to username/password)
    /// </summary>
    public string? ElasticsearchApiKey { get; set; }

    /// <summary>
    /// Allow self-signed TLS certificates for Elasticsearch
    /// </summary>
    public bool AllowSelfSignedCert { get; set; } = false;

    /// <summary>
    /// Index name for storing document chunks
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-z][a-z0-9\-_]*$", ErrorMessage = "Index name must start with lowercase letter and contain only lowercase letters, numbers, hyphens and underscores")]
    public string IndexName { get; set; } = "rag-chunks";

    /// <summary>
    /// Embedding service URL
    /// </summary>
    [Required]
    [Url(ErrorMessage = "Embedding service URL must be a valid URL")]
    public string EmbeddingServiceUrl { get; set; } = "http://localhost:8580";

    /// <summary>
    /// Embedding model name
    /// </summary>
    [Required]
    public string EmbeddingModelName { get; set; } = "intfloat/multilingual-e5-small";

    /// <summary>
    /// Processing interval in minutes
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Processing interval must be between 1 and 1440 minutes (24 hours)")]
    public int ProcessingIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Batch size for bulk operations to Elasticsearch
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Bulk batch size must be between 1 and 1000")]
    public int BulkBatchSize { get; set; } = 200;

    /// <summary>
    /// Validates the configuration and returns validation results
    /// </summary>
    public IEnumerable<ValidationResult> Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        
        // Basic validation using data annotations
        Validator.TryValidateObject(this, context, results, true);

        // Custom validation logic
        if (ChunkOverlap >= ChunkSize)
        {
            results.Add(new ValidationResult("Chunk overlap must be less than chunk size", new[] { nameof(ChunkOverlap) }));
        }

        // Validate authentication configuration
        var hasBasicAuth = !string.IsNullOrEmpty(ElasticsearchUsername) && !string.IsNullOrEmpty(ElasticsearchPassword);
        var hasApiKey = !string.IsNullOrEmpty(ElasticsearchApiKey);
        
        if (!hasBasicAuth && !hasApiKey)
        {
            results.Add(new ValidationResult("Either username/password or API key must be provided for Elasticsearch authentication", 
                new[] { nameof(ElasticsearchUsername), nameof(ElasticsearchApiKey) }));
        }

        if (hasBasicAuth && hasApiKey)
        {
            results.Add(new ValidationResult("Cannot use both username/password and API key authentication simultaneously", 
                new[] { nameof(ElasticsearchUsername), nameof(ElasticsearchApiKey) }));
        }

        // Validate file extensions format
        foreach (var ext in FileExtensions)
        {
            if (!ext.StartsWith('.'))
            {
                results.Add(new ValidationResult($"File extension '{ext}' must start with a dot", new[] { nameof(FileExtensions) }));
            }
        }

        // Validate source folders
        foreach (var folder in SourceFolders)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                results.Add(new ValidationResult("Source folder cannot be empty or whitespace", new[] { nameof(SourceFolders) }));
            }
        }

        return results;
    }

    /// <summary>
    /// Returns a safe string representation for logging (masks sensitive data)
    /// </summary>
    public string ToLogString()
    {
        var uniqueExtensions = FileExtensions.Distinct().ToList();
        
        return $"CollectorOptions {{ " +
               $"SourceFolders: [{string.Join(", ", SourceFolders)}], " +
               $"FileExtensions: [{string.Join(", ", uniqueExtensions)}], " +
               $"ChunkSize: {ChunkSize}, " +
               $"ChunkOverlap: {ChunkOverlap}, " +
               $"ElasticsearchUrl: {ElasticsearchUrl}, " +
               $"HasUsername: {!string.IsNullOrEmpty(ElasticsearchUsername)}, " +
               $"HasPassword: {!string.IsNullOrEmpty(ElasticsearchPassword)}, " +
               $"HasApiKey: {!string.IsNullOrEmpty(ElasticsearchApiKey)}, " +
               $"AllowSelfSignedCert: {AllowSelfSignedCert}, " +
               $"IndexName: {IndexName}, " +
               $"EmbeddingServiceUrl: {EmbeddingServiceUrl}, " +
               $"EmbeddingModelName: {EmbeddingModelName}, " +
               $"ProcessingIntervalMinutes: {ProcessingIntervalMinutes}, " +
               $"BulkBatchSize: {BulkBatchSize} }}";
    }
}
