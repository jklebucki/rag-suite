namespace RAG.CyberPanel.Features.CreateQuiz;

public record OptionDto(Guid? Id, string Text, bool IsCorrect);
public record QuestionDto(Guid? Id, string Text, int Points, OptionDto[] Options);

public record CreateQuizRequest
(
    string Title,
    string? Description,
    bool IsPublished,
    QuestionDto[] Questions
);
