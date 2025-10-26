namespace RAG.CyberPanel.Features.ImportQuiz;

/// <summary>
/// DTO for an imported answer option.
/// </summary>
public record ImportedOptionDto(
    string Text,
    string? ImageUrl,
    bool IsCorrect
);

/// <summary>
/// DTO for an imported quiz question.
/// </summary>
public record ImportedQuestionDto(
    string Text,
    string? ImageUrl,
    int Points,
    ImportedOptionDto[] Options
);

/// <summary>
/// Request to import a quiz from JSON export.
/// Supports both creating new quiz or overwriting existing one.
/// </summary>
public record ImportQuizRequest(
    string Title,
    string? Description,
    bool IsPublished,
    ImportedQuestionDto[] Questions,
    bool CreateNew = true,
    Guid? OverwriteQuizId = null,
    string? Language = null
);

/// <summary>
/// Response after successful quiz import.
/// </summary>
public record ImportQuizResponse(
    Guid QuizId,
    string Title,
    int QuestionsImported,
    int OptionsImported,
    bool WasOverwritten,
    DateTime ImportedAt
);
