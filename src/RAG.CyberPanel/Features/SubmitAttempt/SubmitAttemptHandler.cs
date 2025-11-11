using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;
using RAG.CyberPanel.Domain;
using RAG.CyberPanel.Services;
using RAG.Security.Services;

namespace RAG.CyberPanel.Features.SubmitAttempt;

public record PerQuestionResult(Guid QuestionId, bool Correct, int PointsAwarded, int MaxPoints);

public record SubmitAttemptResult(Guid AttemptId, int Score, int MaxScore, PerQuestionResult[] PerQuestionResults);

public class SubmitAttemptHandler
{
    private readonly CyberPanelDbContext _db;
    private readonly IUserContextService _userContext;
    private readonly ICyberPanelService _service;

    public SubmitAttemptHandler(CyberPanelDbContext db, IUserContextService userContext, ICyberPanelService service)
    {
        _db = db;
        _userContext = userContext;
        _service = service;
    }

    public async Task<SubmitAttemptResult> Handle(SubmitAttemptRequest request, CancellationToken cancellationToken)
    {
        var quiz = await _db.Quizzes.Include(q => q.Questions).ThenInclude(qn => qn.Options).FirstOrDefaultAsync(q => q.Id == request.QuizId, cancellationToken);
        if (quiz == null) throw new KeyNotFoundException("Quiz not found");

        var attempt = new QuizAttempt
        {
            QuizId = quiz.Id,
            UserId = _userContext.GetCurrentUserId() ?? "anonymous",
            StartedAt = DateTime.UtcNow,
            FinishedAt = DateTime.UtcNow
        };

        foreach (var ans in request.Answers)
        {
            var qa = new QuizAnswer
            {
                QuestionId = ans.QuestionId,
                QuizAttempt = attempt
            };

            foreach (var optId in ans.SelectedOptionIds.Distinct())
            {
                qa.SelectedOptions.Add(new QuizAnswerOption
                {
                    OptionId = optId,
                    QuizAnswer = qa
                });
            }

            attempt.Answers.Add(qa);
        }

        // compute score
        var score = _service.CalculateScore(quiz, attempt.Answers);
        attempt.Score = score;

        _db.QuizAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);

        // build per-question results
        var perQuestion = new List<PerQuestionResult>();
        foreach (var a in attempt.Answers)
        {
            var question = quiz.Questions.FirstOrDefault(q => q.Id == a.QuestionId)!;
            var correctSet = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).OrderBy(x => x).ToArray();
            var selectedSet = a.SelectedOptions.Select(s => s.OptionId).Distinct().OrderBy(x => x).ToArray();
            var correct = correctSet.SequenceEqual(selectedSet);
            perQuestion.Add(new PerQuestionResult(a.QuestionId, correct, correct ? question.Points : 0, question.Points));
        }

        return new SubmitAttemptResult(attempt.Id, score, quiz.Questions.Sum(q => q.Points), perQuestion.ToArray());
    }
}
