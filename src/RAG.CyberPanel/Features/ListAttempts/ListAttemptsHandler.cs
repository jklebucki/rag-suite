using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;
using RAG.Security.Data;
using RAG.Security.Services;

namespace RAG.CyberPanel.Features.ListAttempts;

public class ListAttemptsHandler
{
    private readonly CyberPanelDbContext _db;
    private readonly SecurityDbContext _securityDb;
    private readonly IUserContextService _userContext;

    public ListAttemptsHandler(
        CyberPanelDbContext db, 
        SecurityDbContext securityDb,
        IUserContextService userContext)
    {
        _db = db;
        _securityDb = securityDb;
        _userContext = userContext;
    }

    public async Task<ListAttemptsResponse> Handle(CancellationToken cancellationToken)
    {
        var currentUserId = _userContext.GetCurrentUserId() ?? "anonymous";
        var userRoles = _userContext.GetCurrentUserRoles();
        var isAdminOrPowerUser = userRoles.Contains("Admin") || userRoles.Contains("PowerUser");

        // Build query based on role
        var attemptsQuery = _db.QuizAttempts
            .Include(a => a.Answers)
            .ThenInclude(ans => ans.SelectedOptions)
            .AsQueryable();

        // Filter by user if not Admin/PowerUser
        if (!isAdminOrPowerUser)
        {
            attemptsQuery = attemptsQuery.Where(a => a.UserId == currentUserId);
        }

        var attempts = await attemptsQuery
            .OrderByDescending(a => a.FinishedAt ?? a.StartedAt)
            .Select(a => new
            {
                a.Id,
                a.QuizId,
                a.UserId,
                a.Score,
                a.FinishedAt,
                a.StartedAt,
                Quiz = _db.Quizzes
                    .Where(q => q.Id == a.QuizId)
                    .Select(q => new
                    {
                        q.Title,
                        MaxScore = q.Questions.Sum(qn => qn.Points),
                        QuestionCount = q.Questions.Count,
                        Questions = q.Questions.Select(qn => new
                        {
                            qn.Id,
                            qn.Points,
                            CorrectOptionIds = qn.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList()
                        }).ToList()
                    })
                    .FirstOrDefault(),
                Answers = a.Answers.Select(ans => new
                {
                    ans.QuestionId,
                    SelectedOptionIds = ans.SelectedOptions.Select(so => so.OptionId).ToList()
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        // Get user details for all attempts
        var userIds = attempts.Select(a => a.UserId).Distinct().ToList();
        var users = await _securityDb.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.Email })
            .ToListAsync(cancellationToken);

        var userDict = users.ToDictionary(u => u.Id, u => u);

        var dtos = attempts.Select(a =>
        {
            var maxScore = a.Quiz?.MaxScore ?? 0;
            var questions = a.Quiz?.Questions;
            
            // Count correct answers and calculate actual score
            var correctCount = 0;
            var actualScore = 0;
            
            if (questions != null)
            {
                foreach (var ans in a.Answers)
                {
                    var question = questions.FirstOrDefault(q => q.Id == ans.QuestionId);
                    if (question != null)
                    {
                        var correctIds = question.CorrectOptionIds;
                        var selectedIds = ans.SelectedOptionIds;
                        var isCorrect = correctIds.OrderBy(x => x).SequenceEqual(selectedIds.OrderBy(x => x));
                        
                        if (isCorrect)
                        {
                            correctCount++;
                            actualScore += question.Points;
                        }
                    }
                }
            }
            
            var percentageScore = maxScore > 0 ? (double)actualScore / maxScore * 100 : 0;

            var user = userDict.GetValueOrDefault(a.UserId);

            return new AttemptDto(
                a.Id,
                a.QuizId,
                a.Quiz?.Title ?? "Unknown Quiz",
                a.UserId,
                user?.UserName ?? "Unknown User",
                user?.Email,
                actualScore,
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
