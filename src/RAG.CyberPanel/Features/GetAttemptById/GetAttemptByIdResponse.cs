namespace RAG.CyberPanel.Features.GetAttemptById;

public record AttemptDetailDto(
    Guid Id,
    Guid QuizId,
    string QuizTitle,
    string UserId,
    string UserName,
    string? UserEmail,
    int Score,
    int MaxScore,
    double PercentageScore,
    DateTime SubmittedAt,
    int QuestionCount,
    int CorrectAnswers,
    QuestionResultDto[] Questions
);

public record QuestionResultDto(
    Guid QuestionId,
    string QuestionText,
    string? QuestionImageUrl,
    int Points,
    bool IsCorrect,
    int PointsAwarded,
    OptionDto[] Options,
    Guid[] SelectedOptionIds,
    Guid[] CorrectOptionIds
);

public record OptionDto(
    Guid Id,
    string Text,
    string? ImageUrl,
    bool IsCorrect
);

public record GetAttemptByIdResponse(AttemptDetailDto Attempt);
