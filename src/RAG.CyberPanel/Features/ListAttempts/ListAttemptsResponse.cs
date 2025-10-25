namespace RAG.CyberPanel.Features.ListAttempts;

public record AttemptDto(
    Guid Id,
    Guid QuizId,
    string QuizTitle,
    int Score,
    int MaxScore,
    double PercentageScore,
    DateTime SubmittedAt,
    int QuestionCount,
    int CorrectAnswers
);

public record ListAttemptsResponse(AttemptDto[] Attempts);
