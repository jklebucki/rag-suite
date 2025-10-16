namespace RAG.CyberPanel.Features.GetQuiz;

/// <summary>
/// DTO for an answer option (read-only, without IsCorrect flag for quiz takers).
/// </summary>
public record QuizOptionDto(Guid Id, string Text, string? ImageUrl);

/// <summary>
/// DTO for a quiz question (read-only for quiz takers).
/// </summary>
public record QuizQuestionDto(Guid Id, string Text, string? ImageUrl, int Points, QuizOptionDto[] Options);

/// <summary>
/// Response DTO for quiz details.
/// </summary>
public record GetQuizResponse(
    Guid Id,
    string Title,
    string? Description,
    bool IsPublished,
    QuizQuestionDto[] Questions
);
