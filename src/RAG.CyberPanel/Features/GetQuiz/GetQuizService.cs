using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;

namespace RAG.CyberPanel.Features.GetQuiz;

/// <summary>
/// Service for retrieving quiz details (without revealing correct answers).
/// </summary>
public class GetQuizService
{
    private readonly CyberPanelDbContext _db;

    public GetQuizService(CyberPanelDbContext db)
    {
        _db = db;
    }

    public async Task<GetQuizResponse?> GetQuizAsync(Guid id, CancellationToken cancellationToken)
    {
        var quiz = await _db.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions.OrderBy(qn => qn.Order))
            .ThenInclude(qn => qn.Options)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (quiz == null)
            return null;

        // Map to DTO without exposing IsCorrect flag to quiz takers
        return new GetQuizResponse(
            quiz.Id,
            quiz.Title,
            quiz.Description,
            quiz.IsPublished,
            quiz.Questions.Select(q => new QuizQuestionDto(
                q.Id,
                q.Text,
                q.ImageUrl,
                q.Points,
                q.Options.Select(o => new QuizOptionDto(
                    o.Id,
                    o.Text,
                    o.ImageUrl
                )).ToArray()
            )).ToArray()
        );
    }
}
