using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;
using RAG.Security.Data;
using RAG.Security.Services;

namespace RAG.CyberPanel.Features.GetAttemptById;

public class GetAttemptByIdHandler
{
    private readonly CyberPanelDbContext _db;
    private readonly SecurityDbContext _securityDb;
    private readonly IUserContextService _userContext;

    public GetAttemptByIdHandler(
        CyberPanelDbContext db,
        SecurityDbContext securityDb,
        IUserContextService userContext)
    {
        _db = db;
        _securityDb = securityDb;
        _userContext = userContext;
    }

    public async Task<GetAttemptByIdResponse?> Handle(Guid attemptId, CancellationToken cancellationToken)
    {
        var currentUserId = _userContext.GetCurrentUserId();
        var userRoles = _userContext.GetCurrentUserRoles();
        var isAdminOrPowerUser = userRoles.Contains("Admin") || userRoles.Contains("PowerUser");

        var attempt = await _db.QuizAttempts
            .Include(a => a.Answers)
            .ThenInclude(ans => ans.SelectedOptions)
            .Where(a => a.Id == attemptId)
            .FirstOrDefaultAsync(cancellationToken);

        if (attempt == null)
        {
            return null;
        }

        // Check authorization: user can only view their own attempts unless Admin/PowerUser
        if (!isAdminOrPowerUser && attempt.UserId != currentUserId)
        {
            return null;
        }

        // Get quiz with questions and options
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(qn => qn.Options)
            .FirstOrDefaultAsync(q => q.Id == attempt.QuizId, cancellationToken);

        if (quiz == null)
        {
            return null;
        }

        // Get user details
        var user = await _securityDb.Users
            .Where(u => u.Id == attempt.UserId)
            .Select(u => new { u.Id, u.UserName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        // Calculate scores
        var maxScore = quiz.Questions.Sum(q => q.Points);

        // Build question results
        var questionResults = new List<QuestionResultDto>();
        var correctCount = 0;
        var actualScore = 0; // Calculate actual score based on current answers

        foreach (var question in quiz.Questions.OrderBy(q => q.Order))
        {
            var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            var selectedOptionIds = answer?.SelectedOptions.Select(so => so.OptionId).ToArray() ?? Array.Empty<Guid>();
            var correctOptionIds = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToArray();

            // Check if answer is correct (all correct options selected, no incorrect ones)
            var isCorrect = selectedOptionIds.OrderBy(x => x).SequenceEqual(correctOptionIds.OrderBy(x => x));
            if (isCorrect)
            {
                correctCount++;
            }

            var pointsAwarded = isCorrect ? question.Points : 0;
            actualScore += pointsAwarded;

            var options = question.Options.Select(o => new OptionDto(
                o.Id,
                o.Text,
                o.ImageUrl,
                o.IsCorrect
            )).ToArray();

            questionResults.Add(new QuestionResultDto(
                question.Id,
                question.Text,
                question.ImageUrl,
                question.Points,
                isCorrect,
                pointsAwarded,
                options,
                selectedOptionIds,
                correctOptionIds
            ));
        }

        // Use the recalculated score instead of the stored one
        var percentageScore = maxScore > 0 ? (double)actualScore / maxScore * 100 : 0;

        var attemptDetail = new AttemptDetailDto(
            attempt.Id,
            quiz.Id,
            quiz.Title,
            attempt.UserId,
            user?.UserName ?? "Unknown User",
            user?.Email,
            actualScore,
            maxScore,
            percentageScore,
            attempt.FinishedAt ?? attempt.StartedAt,
            quiz.Questions.Count,
            correctCount,
            questionResults.ToArray()
        );

        return new GetAttemptByIdResponse(attemptDetail);
    }
}
