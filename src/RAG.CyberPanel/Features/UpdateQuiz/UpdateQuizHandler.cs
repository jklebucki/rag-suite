using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;
using RAG.CyberPanel.Domain;
using RAG.Security.Services;

namespace RAG.CyberPanel.Features.UpdateQuiz;

public class UpdateQuizHandler
{
    private readonly CyberPanelDbContext _db;
    private readonly IUserContextService _userContext;

    public UpdateQuizHandler(CyberPanelDbContext db, IUserContextService userContext)
    {
        _db = db;
        _userContext = userContext;
    }

    public async Task<UpdateQuizResponse?> Handle(Guid quizId, UpdateQuizRequest request, CancellationToken cancellationToken)
    {
        var userId = _userContext.GetCurrentUserId();
        var userRoles = _userContext.GetCurrentUserRoles();
        var isAdmin = userRoles.Contains("Admin");

        // Load existing quiz (without tracking to check authorization)
        var quiz = await _db.Quizzes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz == null)
        {
            return null;
        }

        // Authorization: Only creator or Admin can update
        if (quiz.CreatedByUserId != userId && !isAdmin)
        {
            return null;
        }

        // Delete all existing questions and options (cascade delete)
        await _db.Questions
            .Where(q => q.QuizId == quizId)
            .ExecuteDeleteAsync(cancellationToken);

        // Now load the quiz again for tracking and update
        quiz = await _db.Quizzes.FindAsync(new object[] { quizId }, cancellationToken);
        if (quiz == null)
        {
            return null;
        }

        // Update quiz properties
        quiz.Title = request.Title;
        quiz.Description = request.Description;
        quiz.IsPublished = request.IsPublished;

        // Add new questions from request
        var questionOrder = 0;
        foreach (var q in request.Questions)
        {
            var question = new Question
            {
                QuizId = quiz.Id,
                Text = q.Text,
                ImageUrl = q.ImageUrl,
                Points = q.Points,
                Order = questionOrder++
            };

            foreach (var o in q.Options)
            {
                question.Options.Add(new Option
                {
                    Text = o.Text,
                    ImageUrl = o.ImageUrl,
                    IsCorrect = o.IsCorrect
                });
            }

            _db.Questions.Add(question);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateQuizResponse(quiz.Id, quiz.Title, quiz.IsPublished);
    }
}
