using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;
using RAG.CyberPanel.Domain;

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

    public async Task<ListQuizzesResponse> GetQuizzesAsync(string? language, CancellationToken cancellationToken)
    {
        IQueryable<Quiz> query = _db.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions);

        // Filter by language if provided (only for published quizzes)
        if (!string.IsNullOrWhiteSpace(language))
        {
            query = query.Where(q => !q.IsPublished || q.Language == language || q.Language == null);
        }

        var quizzes = await query
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = quizzes.Select(q => new QuizListItemDto(
            q.Id,
            q.Title,
            q.Description,
            q.IsPublished,
            q.CreatedAt,
            q.Questions.Count,
            q.Language
        )).ToArray();

        return new ListQuizzesResponse(items);
    }
}
