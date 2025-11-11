using RAG.CyberPanel.Domain;
using RAG.CyberPanel.Services;

namespace RAG.Tests.CyberPanel;

public class CyberPanelServiceTests
{
    [Fact]
    public void CalculateScore_ExactMatch_AwardsPoints()
    {
        var quiz = new Quiz { Questions = new List<Question>() };
        var q1 = new Question { Id = Guid.NewGuid(), Points = 2 };
        var o1 = new Option { Id = Guid.NewGuid(), IsCorrect = true };
        var o2 = new Option { Id = Guid.NewGuid(), IsCorrect = false };
        q1.Options.Add(o1);
        q1.Options.Add(o2);
        quiz.Questions.Add(q1);

        var answer = new QuizAnswer { QuestionId = q1.Id };
        answer.SelectedOptions.Add(new QuizAnswerOption { OptionId = o1.Id });

        var svc = new CyberPanelService();
        var score = svc.CalculateScore(quiz, new[] { answer });

        score.Should().Be(2);
    }

    [Fact]
    public void CalculateScore_PartialOrWrong_NoPoints()
    {
        var quiz = new Quiz { Questions = new List<Question>() };
        var q1 = new Question { Id = Guid.NewGuid(), Points = 3 };
        var o1 = new Option { Id = Guid.NewGuid(), IsCorrect = true };
        var o2 = new Option { Id = Guid.NewGuid(), IsCorrect = true };
        var o3 = new Option { Id = Guid.NewGuid(), IsCorrect = false };
        q1.Options.Add(o1); q1.Options.Add(o2); q1.Options.Add(o3);
        quiz.Questions.Add(q1);

        var answer = new QuizAnswer { QuestionId = q1.Id };
        // select only one of the two correct options -> not exact -> no points
        answer.SelectedOptions.Add(new QuizAnswerOption { OptionId = o1.Id });

        var svc = new CyberPanelService();
        var score = svc.CalculateScore(quiz, new[] { answer });

        score.Should().Be(0);
    }

    [Fact]
    public void CalculateScore_MultipleQuestions_SumsCorrectly()
    {
        var quiz = new Quiz { Questions = new List<Question>() };

        // Question 1: 2 points, correct
        var q1 = new Question { Id = Guid.NewGuid(), Points = 2 };
        var o1 = new Option { Id = Guid.NewGuid(), IsCorrect = true };
        q1.Options.Add(o1);
        quiz.Questions.Add(q1);

        // Question 2: 3 points, wrong
        var q2 = new Question { Id = Guid.NewGuid(), Points = 3 };
        var o2 = new Option { Id = Guid.NewGuid(), IsCorrect = true };
        var o3 = new Option { Id = Guid.NewGuid(), IsCorrect = false };
        q2.Options.Add(o2); q2.Options.Add(o3);
        quiz.Questions.Add(q2);

        var answers = new[]
        {
            new QuizAnswer { QuestionId = q1.Id, SelectedOptions = new List<QuizAnswerOption> { new QuizAnswerOption { OptionId = o1.Id } } },
            new QuizAnswer { QuestionId = q2.Id, SelectedOptions = new List<QuizAnswerOption> { new QuizAnswerOption { OptionId = o3.Id } } }
        };

        var svc = new CyberPanelService();
        var score = svc.CalculateScore(quiz, answers);

        score.Should().Be(2); // Only q1 correct
    }

    [Fact]
    public void CalculateScore_InvalidQuestionId_Skips()
    {
        var quiz = new Quiz { Questions = new List<Question>() };
        var q1 = new Question { Id = Guid.NewGuid(), Points = 2 };
        var o1 = new Option { Id = Guid.NewGuid(), IsCorrect = true };
        q1.Options.Add(o1);
        quiz.Questions.Add(q1);

        var answer = new QuizAnswer { QuestionId = Guid.NewGuid() }; // Invalid ID
        answer.SelectedOptions.Add(new QuizAnswerOption { OptionId = o1.Id });

        var svc = new CyberPanelService();
        var score = svc.CalculateScore(quiz, new[] { answer });

        score.Should().Be(0);
    }
}