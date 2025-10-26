using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;
using RAG.Security.Services;

namespace RAG.CyberPanel.Features.DeleteQuiz;

public class DeleteQuizHandler
{
    private readonly CyberPanelDbContext _db;
    private readonly IUserContextService _userContext;

    public DeleteQuizHandler(CyberPanelDbContext db, IUserContextService userContext)
    {
        _db = db;
        _userContext = userContext;
    }

    public async Task<bool> Handle(Guid quizId, CancellationToken cancellationToken)
    {
        var userId = _userContext.GetCurrentUserId();
        var userRoles = _userContext.GetCurrentUserRoles();
        var isAdmin = userRoles.Contains("Admin");

        var quiz = await _db.Quizzes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz == null)
        {
            return false;
        }

        // Authorization: Only creator or Admin can delete
        if (quiz.CreatedByUserId != userId && !isAdmin)
        {
            return false;
        }

        // Delete quiz (cascade delete will handle questions, options, attempts)
        await _db.Quizzes
            .Where(q => q.Id == quizId)
            .ExecuteDeleteAsync(cancellationToken);

        return true;
    }
}
