namespace RAG.CyberPanel.Features.ListQuizzes;

/// <summary>
/// DTO for a quiz in the list view.
/// </summary>
public record QuizListItemDto(
    Guid Id,
    string Title,
    string? Description,
    bool IsPublished,
    DateTime CreatedAt,
    int QuestionCount
);

/// <summary>
/// Response DTO for list of quizzes.
/// </summary>
public record ListQuizzesResponse(QuizListItemDto[] Quizzes);
