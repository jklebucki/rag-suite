using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;

namespace RAG.CyberPanel.Features.ListQuizzes;

/// <summary>
/// Service for listing available quizzes.
/// </summary>
public class ListQuizzesService
{
    private readonly CyberPanelDbContext _db;

    public ListQuizzesService(CyberPanelDbContext db)
    {
        _db = db;
    }

    public async Task<ListQuizzesResponse> GetQuizzesAsync(CancellationToken cancellationToken)
    {
        var quizzes = await _db.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = quizzes.Select(q => new QuizListItemDto(
            q.Id,
            q.Title,
            q.Description,
            q.IsPublished,
            q.CreatedAt,
            q.Questions.Count
        )).ToArray();

        return new ListQuizzesResponse(items);
    }
}
