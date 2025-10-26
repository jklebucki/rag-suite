using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;
using RAG.CyberPanel.Domain;
using RAG.Security.Data;
using RAG.Security.Services;

namespace RAG.CyberPanel.Features.DeleteQuiz;

public class DeleteQuizHandler
{
    private readonly CyberPanelDbContext _db;
    private readonly SecurityDbContext _securityDb;
    private readonly IUserContextService _userContext;

    public DeleteQuizHandler(
        CyberPanelDbContext db, 
        SecurityDbContext securityDb,
        IUserContextService userContext)
    {
        _db = db;
        _securityDb = securityDb;
        _userContext = userContext;
    }

    public async Task<DeleteQuizResponse?> Handle(Guid quizId, CancellationToken cancellationToken)
    {
        var userId = _userContext.GetCurrentUserId();
        var userRoles = _userContext.GetCurrentUserRoles();
        var isAdmin = userRoles.Contains("Admin");

        // Load quiz with counts
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz == null)
        {
            return null;
        }

        // Authorization: Only creator or Admin can delete
        if (quiz.CreatedByUserId != userId && !isAdmin)
        {
            return null;
        }

        // Get attempt count
        var attemptCount = await _db.QuizAttempts
            .Where(a => a.QuizId == quizId)
            .CountAsync(cancellationToken);

        // Get owner user info
        var ownerUser = await _securityDb.Users
            .Where(u => u.Id == quiz.CreatedByUserId)
            .Select(u => new { u.UserName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        // Get deleting user info
        var deletingUser = await _securityDb.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.UserName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        var deletedAt = DateTime.UtcNow;

        // Create deletion log entry
        var deletionLog = new QuizDeletionLog
        {
            QuizId = quiz.Id,
            QuizTitle = quiz.Title,
            QuizDescription = quiz.Description,
            QuestionCount = quiz.Questions.Count,
            AttemptCount = attemptCount,
            QuizOwnerUserId = quiz.CreatedByUserId,
            QuizOwnerUserName = ownerUser?.UserName ?? "Unknown",
            QuizOwnerEmail = ownerUser?.Email,
            DeletedByUserId = userId ?? string.Empty,
            DeletedByUserName = deletingUser?.UserName ?? "Unknown",
            DeletedByUserEmail = deletingUser?.Email,
            DeletedAt = deletedAt
        };

        _db.QuizDeletionLogs.Add(deletionLog);
        await _db.SaveChangesAsync(cancellationToken);

        // Delete quiz (cascade will handle QuizAttempt -> QuizAnswer -> QuizAnswerOption)
        await _db.Quizzes
            .Where(q => q.Id == quizId)
            .ExecuteDeleteAsync(cancellationToken);

        return new DeleteQuizResponse(
            quiz.Id,
            quiz.Title,
            quiz.Questions.Count,
            attemptCount,
            ownerUser?.UserName ?? "Unknown",
            deletingUser?.UserName ?? "Unknown",
            deletedAt
        );
    }
}
