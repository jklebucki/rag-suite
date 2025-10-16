using System.ComponentModel.DataAnnotations;

namespace RAG.CyberPanel.Features.CreateQuiz;

/// <summary>
/// DTO for an answer option. Supports optional image URL.
/// </summary>
public record OptionDto(Guid? Id, string Text, string? ImageUrl, bool IsCorrect);

/// <summary>
/// DTO for a quiz question. Supports optional image URL.
/// </summary>
public record QuestionDto(Guid? Id, string Text, string? ImageUrl, int Points, OptionDto[] Options);

/// <summary>
/// Request to create a new cybersecurity quiz with questions and options.
/// </summary>
public record CreateQuizRequest
(
    string Title,
    string? Description,
    bool IsPublished,
    QuestionDto[] Questions
);
