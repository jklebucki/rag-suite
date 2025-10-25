using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;

namespace RAG.CyberPanel.Features.ExportQuiz;

/// <summary>
/// Service responsible for exporting quiz data to JSON-compatible format.
/// Includes all quiz content with images (base64 or URLs) and metadata.
/// </summary>
public class ExportQuizService
{
    private readonly CyberPanelDbContext _db;

    public ExportQuizService(CyberPanelDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Exports a complete quiz with all questions, options, and images.
    /// </summary>
    /// <param name="quizId">The ID of the quiz to export</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete quiz data ready for JSON serialization</returns>
    /// <exception cref="InvalidOperationException">When quiz is not found</exception>
    public async Task<ExportQuizResponse> ExportQuizAsync(Guid quizId, CancellationToken cancellationToken)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions.OrderBy(qn => qn.Order))
            .ThenInclude(qn => qn.Options)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz == null)
        {
            throw new InvalidOperationException($"Quiz with ID {quizId} not found");
        }

        var exportedQuestions = quiz.Questions
            .OrderBy(q => q.Order)
            .Select(q => new ExportedQuestionDto(
                Id: q.Id,
                Text: q.Text,
                ImageUrl: q.ImageUrl,
                Order: q.Order,
                Points: q.Points,
                Options: q.Options.Select(o => new ExportedOptionDto(
                    Id: o.Id,
                    Text: o.Text,
                    ImageUrl: o.ImageUrl,
                    IsCorrect: o.IsCorrect
                )).ToArray()
            ))
            .ToArray();

        return new ExportQuizResponse(
            Id: quiz.Id,
            Title: quiz.Title,
            Description: quiz.Description,
            CreatedByUserId: quiz.CreatedByUserId,
            CreatedAt: quiz.CreatedAt,
            IsPublished: quiz.IsPublished,
            Questions: exportedQuestions,
            ExportVersion: "1.0"
        );
    }
}
