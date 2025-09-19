namespace RAG.Abstractions.Conversion;

public record ConversionRequest(
    Stream FileStream,
    string FileName,
    string ContentType
);

public record ConversionResult(
    Stream? PdfStream,
    bool IsConverted,
    string? ErrorMessage = null
);