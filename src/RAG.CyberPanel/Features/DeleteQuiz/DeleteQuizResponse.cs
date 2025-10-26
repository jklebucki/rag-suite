namespace RAG.CyberPanel.Features.DeleteQuiz;

public record DeleteQuizResponse(
    Guid QuizId,
    string QuizTitle,
    int QuestionCount,
    int AttemptCount,
    string OwnerUserName,
    string DeletedByUserName,
    DateTime DeletedAt
);
