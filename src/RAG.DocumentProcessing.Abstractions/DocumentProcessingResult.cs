namespace RAG.DocumentProcessing.Abstractions;

public sealed record DocumentProcessingResult(
    string Markdown,
    string PlainText,
    int PageCount,
    bool OcrUsed,
    string Provider,
    double QualityScore,
    IReadOnlyList<string> Warnings);
