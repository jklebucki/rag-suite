namespace RAG.Abstractions.Common.Api;

public record ApiResponse<T>(
    T Data,
    bool Success = true,
    string? Message = null,
    string[]? Errors = null
);

