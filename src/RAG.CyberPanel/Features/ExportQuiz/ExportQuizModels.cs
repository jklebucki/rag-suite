namespace RAG.CyberPanel.Features.ExportQuiz;

/// <summary>
/// DTO for an exported answer option with all data including correctness flag.
/// </summary>
public record ExportedOptionDto(
    Guid Id,
    string Text,
    string? ImageUrl,
    bool IsCorrect
);

/// <summary>
/// DTO for an exported quiz question with all data.
/// </summary>
public record ExportedQuestionDto(
    Guid Id,
    string Text,
    string? ImageUrl,
    int Order,
    int Points,
    ExportedOptionDto[] Options
);

/// <summary>
/// Complete quiz export including all metadata and content.
/// </summary>
public record ExportQuizResponse(
    Guid Id,
    string Title,
    string? Description,
    string CreatedByUserId,
    DateTime CreatedAt,
    bool IsPublished,
    ExportedQuestionDto[] Questions,
    string ExportVersion = "1.0"
)
{
    public DateTime ExportedAt { get; init; } = DateTime.UtcNow;
}
