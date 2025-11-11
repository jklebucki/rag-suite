using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;
using RAG.CyberPanel.Domain;
using RAG.Security.Services;

namespace RAG.CyberPanel.Features.ImportQuiz;

/// <summary>
/// Handler for importing quiz data from JSON export.
/// Supports creating new quiz or overwriting existing one.
/// </summary>
public class ImportQuizHandler
{
    private readonly CyberPanelDbContext _db;
    private readonly IUserContextService _userContext;

    public ImportQuizHandler(CyberPanelDbContext db, IUserContextService userContext)
    {
        _db = db;
        _userContext = userContext;
    }

    /// <summary>
    /// Imports quiz data. Can create new quiz or overwrite existing one.
    /// </summary>
    /// <param name="request">Import request with quiz data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result with created/updated quiz ID</returns>
    /// <exception cref="InvalidOperationException">When overwrite quiz is not found</exception>
    public async Task<ImportQuizResponse> Handle(ImportQuizRequest request, CancellationToken cancellationToken)
    {
        var userId = _userContext.GetCurrentUserId() ?? "system";
        Quiz quiz;
        bool wasOverwritten = false;

        if (!request.CreateNew && request.OverwriteQuizId.HasValue)
        {
            // Overwrite existing quiz
            var existingQuiz = await _db.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(qn => qn.Options)
                .FirstOrDefaultAsync(q => q.Id == request.OverwriteQuizId.Value, cancellationToken);

            if (existingQuiz == null)
            {
                throw new InvalidOperationException($"Quiz with ID {request.OverwriteQuizId.Value} not found");
            }

            quiz = existingQuiz;

            // Update quiz properties first
            quiz.Title = request.Title;
            quiz.Description = request.Description;
            quiz.IsPublished = request.IsPublished;
            quiz.Language = request.Language;

            // Remove existing questions and options (cascade delete will handle options)
            var questionsToRemove = quiz.Questions.ToList();
            quiz.Questions.Clear();
            _db.Questions.RemoveRange(questionsToRemove);

            // Save changes to persist question deletion before adding new ones
            await _db.SaveChangesAsync(cancellationToken);

            // After SaveChanges, quiz.Questions should be empty and ready for new questions
            // No need to reload quiz as it's still tracked and properties are already updated

            wasOverwritten = true;
        }
        else
        {
            // Create new quiz
            quiz = new Quiz
            {
                Title = request.Title,
                Description = request.Description,
                IsPublished = request.IsPublished,
                Language = request.Language,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _db.Quizzes.Add(quiz);
        }

        // Add questions and options
        int questionOrder = 0;
        int totalOptions = 0;

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
                totalOptions++;
            }

            _db.Questions.Add(question);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new ImportQuizResponse(
            QuizId: quiz.Id,
            Title: quiz.Title,
            QuestionsImported: quiz.Questions.Count,
            OptionsImported: totalOptions,
            WasOverwritten: wasOverwritten,
            ImportedAt: DateTime.UtcNow
        );
    }
}
