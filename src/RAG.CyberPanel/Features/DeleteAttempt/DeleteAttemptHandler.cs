using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;
using RAG.CyberPanel.Domain;
using RAG.Security.Data;
using RAG.Security.Services;

namespace RAG.CyberPanel.Features.DeleteAttempt;

public class DeleteAttemptHandler
{
    private readonly CyberPanelDbContext _db;
    private readonly SecurityDbContext _securityDb;
    private readonly IUserContextService _userContext;

    public DeleteAttemptHandler(
        CyberPanelDbContext db,
        SecurityDbContext securityDb,
        IUserContextService userContext)
    {
        _db = db;
        _securityDb = securityDb;
        _userContext = userContext;
    }

    public async Task<bool> Handle(Guid attemptId, CancellationToken cancellationToken)
    {
        var userId = _userContext.GetCurrentUserId();
        var userRoles = _userContext.GetCurrentUserRoles();
        var isAdminOrPowerUser = userRoles.Contains("Admin") || userRoles.Contains("PowerUser");

        // Authorization: Only Admin or PowerUser can delete attempts
        if (!isAdminOrPowerUser)
        {
            return false;
        }

        // Load attempt with quiz info
        var attempt = await _db.QuizAttempts
            .Include(a => a.Answers)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken);

        if (attempt == null)
        {
            return false;
        }

        // Get quiz title
        var quiz = await _db.Quizzes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == attempt.QuizId, cancellationToken);

        // Get attempt user info
        var attemptUser = await _securityDb.Users
            .Where(u => u.Id == attempt.UserId)
            .Select(u => new { u.UserName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        // Get deleting user info
        var deletingUser = await _securityDb.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.UserName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        // Create deletion log
        var deletionLog = new AttemptDeletionLog
        {
            AttemptId = attempt.Id,
            QuizId = attempt.QuizId,
            QuizTitle = quiz?.Title ?? "Unknown Quiz",
            Score = attempt.Score,
            StartedAt = attempt.StartedAt,
            FinishedAt = attempt.FinishedAt,
            AttemptUserId = attempt.UserId,
            AttemptUserName = attemptUser?.UserName ?? "Unknown",
            AttemptUserEmail = attemptUser?.Email,
            DeletedByUserId = userId ?? string.Empty,
            DeletedByUserName = deletingUser?.UserName ?? "Unknown",
            DeletedByUserEmail = deletingUser?.Email,
            DeletedAt = DateTime.UtcNow
        };

        _db.AttemptDeletionLogs.Add(deletionLog);
        await _db.SaveChangesAsync(cancellationToken);

        // Delete attempt (cascade will handle QuizAnswer -> QuizAnswerOption)
        await _db.QuizAttempts
            .Where(a => a.Id == attemptId)
            .ExecuteDeleteAsync(cancellationToken);

        return true;
    }
}
