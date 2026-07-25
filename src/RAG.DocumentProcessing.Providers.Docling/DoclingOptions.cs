using System.ComponentModel.DataAnnotations;

namespace RAG.DocumentProcessing.Providers.Docling;

public sealed class DoclingOptions
{
    public const string SectionName = "Docling";

    [Required]
    public string Endpoint { get; init; } = "http://localhost:5001";

    public string? ApiKey { get; init; }

    [Range(100, 60000)]
    public int PollIntervalMilliseconds { get; init; } = 1000;

    [Range(1, 10000)]
    public int MinimumCharactersPerPage { get; init; } = 40;
}
