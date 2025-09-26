namespace RAG.CyberPanel.Features.SubmitAttempt;

public record AnswerDto(Guid QuestionId, Guid[] SelectedOptionIds);

public record SubmitAttemptRequest(Guid QuizId, AnswerDto[] Answers);
