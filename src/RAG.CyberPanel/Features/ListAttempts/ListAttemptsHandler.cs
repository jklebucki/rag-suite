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

        // Build query and load attempts with all related data
        var attemptsQuery = _db.QuizAttempts
            .Include(a => a.Answers)
                .ThenInclude(ans => ans.SelectedOptions)
            .AsQueryable();

        // Filter by user if not Admin/PowerUser
        if (!isAdminOrPowerUser)
        {
            attemptsQuery = attemptsQuery.Where(a => a.UserId == currentUserId);
        }

        // Load attempts first with all navigation properties
        var attemptsList = await attemptsQuery
            .OrderByDescending(a => a.FinishedAt ?? a.StartedAt)
            .ToListAsync(cancellationToken);

        // Load quizzes separately to avoid complex projections
        var quizIds = attemptsList.Select(a => a.QuizId).Distinct().ToList();
        var quizzes = await _db.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(qn => qn.Options)
            .Where(q => quizIds.Contains(q.Id))
            .ToListAsync(cancellationToken);

        var quizDict = quizzes.ToDictionary(q => q.Id, q => q);

        // Get user details for all attempts
        var userIds = attemptsList.Select(a => a.UserId).Distinct().ToList();
        var users = await _securityDb.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.Email })
            .ToListAsync(cancellationToken);

        var userDict = users.ToDictionary(u => u.Id, u => u);

        // Build DTOs with score recalculation
        var dtos = attemptsList.Select(a =>
        {
            var quiz = quizDict.GetValueOrDefault(a.QuizId);
            if (quiz == null)
            {
                // Quiz not found, return basic info
                var user = userDict.GetValueOrDefault(a.UserId);
                return new AttemptDto(
                    a.Id,
                    a.QuizId,
                    "Unknown Quiz",
                    a.UserId,
                    user?.UserName ?? "Unknown User",
                    user?.Email,
                    0,
                    0,
                    0,
                    a.FinishedAt ?? a.StartedAt,
                    0,
                    0
                );
            }

            var maxScore = quiz.Questions.Sum(q => q.Points);
            var correctCount = 0;
            var actualScore = 0;

            // Calculate score based on answers
            foreach (var answer in a.Answers)
            {
                var question = quiz.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                if (question == null) continue;

                var correctIds = question.Options
                    .Where(o => o.IsCorrect)
                    .Select(o => o.Id)
                    .OrderBy(x => x)
                    .ToArray();

                var selectedIds = answer.SelectedOptions
                    .Select(so => so.OptionId)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToArray();

                var isCorrect = correctIds.SequenceEqual(selectedIds);
                if (isCorrect)
                {
                    correctCount++;
                    actualScore += question.Points;
                }
            }

            var percentageScore = maxScore > 0 ? (double)actualScore / maxScore * 100 : 0;
            var userInfo = userDict.GetValueOrDefault(a.UserId);

            return new AttemptDto(
                a.Id,
                a.QuizId,
                quiz.Title,
                a.UserId,
                userInfo?.UserName ?? "Unknown User",
                userInfo?.Email,
                actualScore,
                maxScore,
                percentageScore,
                a.FinishedAt ?? a.StartedAt,
                quiz.Questions.Count,
                correctCount
            );
        }).ToArray();

        return new ListAttemptsResponse(dtos);
    }
}
