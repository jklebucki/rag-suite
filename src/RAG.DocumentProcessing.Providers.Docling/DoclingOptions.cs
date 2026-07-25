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

    [Required]
    public string OcrPreset { get; init; } = "tesseract";

    [Required]
    public string OcrLanguages { get; init; } = "pol,eng";

    [Range(0d, 1d)]
    public double MinimumTextQuality { get; init; } = 0.6d;

    public bool EnableTableFallback { get; init; } = true;

    [Required]
    public string TableFallbackOcrPreset { get; init; } = "auto";

    public string TableFallbackOcrLanguages { get; init; } = string.Empty;

    [Range(0d, 1d)]
    public double MinimumTableCompleteness { get; init; } = 0.85d;

    public IReadOnlyList<string> GetOcrLanguages()
    {
        return ParseLanguages(OcrLanguages);
    }

    public IReadOnlyList<string> GetTableFallbackOcrLanguages()
    {
        return ParseLanguages(TableFallbackOcrLanguages);
    }

    private static IReadOnlyList<string> ParseLanguages(string value)
    {
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
