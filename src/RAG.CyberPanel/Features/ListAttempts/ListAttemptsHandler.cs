using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;
using RAG.Security.Services;

namespace RAG.CyberPanel.Features.ListAttempts;

public class ListAttemptsHandler
{
    private readonly CyberPanelDbContext _db;
    private readonly IUserContextService _userContext;

    public ListAttemptsHandler(CyberPanelDbContext db, IUserContextService userContext)
    {
        _db = db;
        _userContext = userContext;
    }

    public async Task<ListAttemptsResponse> Handle(CancellationToken cancellationToken)
    {
        var userId = _userContext.GetCurrentUserId() ?? "anonymous";

        var attempts = await _db.QuizAttempts
            .Include(a => a.Answers)
            .ThenInclude(ans => ans.SelectedOptions)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.FinishedAt ?? a.StartedAt)
            .Select(a => new
            {
                a.Id,
                a.QuizId,
                a.Score,
                a.FinishedAt,
                a.StartedAt,
                Quiz = _db.Quizzes
                    .Where(q => q.Id == a.QuizId)
                    .Select(q => new
                    {
                        q.Title,
                        MaxScore = q.Questions.Sum(qn => qn.Points),
                        QuestionCount = q.Questions.Count
                    })
                    .FirstOrDefault(),
                Answers = a.Answers.Select(ans => new
                {
                    ans.QuestionId,
                    Question = _db.Questions
                        .Where(q => q.Id == ans.QuestionId)
                        .Select(q => new
                        {
                            CorrectOptionIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList()
                        })
                        .FirstOrDefault(),
                    SelectedOptionIds = ans.SelectedOptions.Select(so => so.OptionId).ToList()
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var dtos = attempts.Select(a =>
        {
            var maxScore = a.Quiz?.MaxScore ?? 0;
            var percentageScore = maxScore > 0 ? (double)a.Score / maxScore * 100 : 0;
            
            // Count correct answers
            var correctCount = a.Answers.Count(ans =>
            {
                var correctIds = ans.Question?.CorrectOptionIds ?? new List<Guid>();
                var selectedIds = ans.SelectedOptionIds;
                return correctIds.OrderBy(x => x).SequenceEqual(selectedIds.OrderBy(x => x));
            });

            return new AttemptDto(
                a.Id,
                a.QuizId,
                a.Quiz?.Title ?? "Unknown Quiz",
                a.Score,
                maxScore,
                percentageScore,
                a.FinishedAt ?? a.StartedAt,
                a.Quiz?.QuestionCount ?? 0,
                correctCount
            );
        }).ToArray();

        return new ListAttemptsResponse(dtos);
    }
}
