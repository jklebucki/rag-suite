namespace RAG.CyberPanel.Features.UpdateQuiz;

// Reusing CreateQuizRequest since update has the same structure
public record UpdateQuizRequest(
    string? Id, // Ignored, taken from route parameter
    string Title,
    string? Description,
    string? ImageUrl,
    bool IsPublished,
    QuestionRequest[] Questions
);

public record QuestionRequest(
    string? Id,
    string Text,
    string? ImageUrl,
    int Points,
    int Order,
    OptionRequest[] Options
);

public record OptionRequest(
    string? Id,
    string Text,
    string? ImageUrl,
    bool IsCorrect
);

public record UpdateQuizResponse(
    Guid Id,
    string Title,
    bool IsPublished
);
