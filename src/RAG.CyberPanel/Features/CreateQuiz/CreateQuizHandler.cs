using RAG.CyberPanel.Data;
using RAG.CyberPanel.Domain;
using RAG.Security.Services;

namespace RAG.CyberPanel.Features.CreateQuiz;

public class CreateQuizHandler
{
    private readonly CyberPanelDbContext _db;
    private readonly IUserContextService _userContext;

    public CreateQuizHandler(CyberPanelDbContext db, IUserContextService userContext)
    {
        _db = db;
        _userContext = userContext;
    }

    public async Task<Quiz> Handle(CreateQuizRequest request, CancellationToken cancellationToken)
    {
        var userId = _userContext.GetCurrentUserId() ?? "system";

        var quiz = new Quiz
        {
            Title = request.Title,
            Description = request.Description,
            IsPublished = request.IsPublished,
            Language = request.Language,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var q in request.Questions)
        {
            var question = new Question
            {
                Text = q.Text,
                ImageUrl = q.ImageUrl,
                Points = q.Points
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

            quiz.Questions.Add(question);
        }

        _db.Quizzes.Add(quiz);
        await _db.SaveChangesAsync(cancellationToken);

        return quiz;
    }
}
