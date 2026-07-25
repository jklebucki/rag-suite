using System.ComponentModel.DataAnnotations;

namespace RAG.DocumentProcessing.Client;

public sealed class DocumentProcessingClientOptions
{
    public const string SectionName = "Services:DocumentProcessing";

    [Required]
    public string Endpoint { get; init; } = "http://localhost:5080";

    public string? ApiKey { get; init; }

    [Range(1, 3600)]
    public int TimeoutSeconds { get; init; } = 360;
}
