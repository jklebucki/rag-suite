using System.ComponentModel.DataAnnotations;

namespace RAG.DocumentProcessing.Core;

public sealed class DocumentProcessingOptions
{
    public const string SectionName = "DocumentProcessing";

    [Range(1, 104857600)]
    public long MaxFileSizeBytes { get; init; } = 25 * 1024 * 1024;

    [Range(1, 10000)]
    public int MaxPageCount { get; init; } = 100;

    [Range(1, 32)]
    public int MaxConcurrentJobs { get; init; } = 2;

    [Range(1, 1000)]
    public int MaxQueuedJobs { get; init; } = 20;

    [Range(1, 3600)]
    public int JobTimeoutSeconds { get; init; } = 300;

    [Range(1, 10000)]
    public int MinimumCharactersPerPage { get; init; } = 40;

    [Required]
    public string WorkDirectory { get; init; } = "App_Data/document-processing";
}
