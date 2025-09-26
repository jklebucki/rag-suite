using System;
using System.Collections.Generic;
using RAG.CyberPanel.Domain;
using RAG.CyberPanel.Services;
using Xunit;

namespace RAG.CyberPanel.Tests;

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

        Assert.Equal(2, score);
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

        Assert.Equal(0, score);
    }
}
