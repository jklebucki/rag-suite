using Xunit;
using RAG.CyberPanel.Features.SubmitAttempt;
using RAG.CyberPanel.Data;
using RAG.CyberPanel.Domain;
using RAG.CyberPanel.Services;
using RAG.Security.Services;
using Moq;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RAG.Tests.CyberPanel;

public class SubmitAttemptHandlerTests : IDisposable
{
    private readonly CyberPanelDbContext _context;
    private readonly Mock<IUserContextService> _mockUserContext;
    private readonly Mock<ICyberPanelService> _mockService;

    public SubmitAttemptHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CyberPanelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CyberPanelDbContext(options);
        _mockUserContext = new Mock<IUserContextService>();
        _mockService = new Mock<ICyberPanelService>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesAttemptWithCorrectScore()
    {
        // Arrange
        var quiz = CreateSampleQuiz();
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        var questions = quiz.Questions.OrderBy(q => q.Order).ToList();
        var q1 = questions[0];
        var q1Options = q1.Options.ToList();

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");
        _mockService.Setup(s => s.CalculateScore(It.IsAny<Quiz>(), It.IsAny<IEnumerable<QuizAnswer>>()))
            .Returns(5);

        var handler = new SubmitAttemptHandler(_context, _mockUserContext.Object, _mockService.Object);

        var request = new SubmitAttemptRequest(
            quiz.Id,
            new[]
            {
                new AnswerDto(q1.Id, new[] { q1Options[0].Id })
            }
        );

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.AttemptId);
        Assert.Equal(5, result.Score);
        Assert.Equal(10, result.MaxScore); // 5 + 5 points
        Assert.Single(result.PerQuestionResults);

        var savedAttempt = await _context.QuizAttempts.FirstOrDefaultAsync();
        Assert.NotNull(savedAttempt);
        Assert.Equal("user123", savedAttempt.UserId);
        Assert.Equal(5, savedAttempt.Score);
    }

    [Fact]
    public async Task Handle_AnonymousUser_CreatesAttemptWithAnonymousUserId()
    {
        // Arrange
        var quiz = CreateSampleQuiz();
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        var questions = quiz.Questions.OrderBy(q => q.Order).ToList();
        var q1 = questions[0];
        var q1Options = q1.Options.ToList();

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns((string?)null);
        _mockService.Setup(s => s.CalculateScore(It.IsAny<Quiz>(), It.IsAny<IEnumerable<QuizAnswer>>()))
            .Returns(0);

        var handler = new SubmitAttemptHandler(_context, _mockUserContext.Object, _mockService.Object);

        var request = new SubmitAttemptRequest(
            quiz.Id,
            new[]
            {
                new AnswerDto(q1.Id, new[] { q1Options[1].Id })
            }
        );

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var savedAttempt = await _context.QuizAttempts.FirstOrDefaultAsync();
        Assert.NotNull(savedAttempt);
        Assert.Equal("anonymous", savedAttempt.UserId);
    }

    [Fact]
    public async Task Handle_NonExistentQuiz_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");
        var handler = new SubmitAttemptHandler(_context, _mockUserContext.Object, _mockService.Object);

        var request = new SubmitAttemptRequest(
            Guid.NewGuid(),
            new[] { new AnswerDto(Guid.NewGuid(), new[] { Guid.NewGuid() }) }
        );

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            handler.Handle(request, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AllCorrectAnswers_ReturnsFullScore()
    {
        // Arrange
        var quiz = CreateSampleQuiz();
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        var questions = quiz.Questions.OrderBy(q => q.Order).ToList();
        var q1 = questions[0];
        var q2 = questions[1];
        var q1Options = q1.Options.ToList();
        var q2Options = q2.Options.ToList();

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");
        _mockService.Setup(s => s.CalculateScore(It.IsAny<Quiz>(), It.IsAny<IEnumerable<QuizAnswer>>()))
            .Returns(10); // Full score

        var handler = new SubmitAttemptHandler(_context, _mockUserContext.Object, _mockService.Object);

        var request = new SubmitAttemptRequest(
            quiz.Id,
            new[]
            {
                new AnswerDto(q1.Id, new[] { q1Options[0].Id }),
                new AnswerDto(q2.Id, new[] { q2Options[0].Id })
            }
        );

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(10, result.Score);
        Assert.Equal(10, result.MaxScore);
        Assert.Equal(2, result.PerQuestionResults.Length);
        Assert.All(result.PerQuestionResults, pqr => Assert.True(pqr.Correct));
    }

    [Fact]
    public async Task Handle_PartialAnswers_CreatesAttemptWithPartialScore()
    {
        // Arrange
        var quiz = CreateSampleQuiz();
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        var questions = quiz.Questions.OrderBy(q => q.Order).ToList();
        var q1 = questions[0];
        var q1Options = q1.Options.ToList();

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");
        _mockService.Setup(s => s.CalculateScore(It.IsAny<Quiz>(), It.IsAny<IEnumerable<QuizAnswer>>()))
            .Returns(5); // Partial score

        var handler = new SubmitAttemptHandler(_context, _mockUserContext.Object, _mockService.Object);

        // Only answer first question
        var request = new SubmitAttemptRequest(
            quiz.Id,
            new[]
            {
                new AnswerDto(q1.Id, new[] { q1Options[0].Id })
            }
        );

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(5, result.Score);
        Assert.Equal(10, result.MaxScore);
        Assert.Single(result.PerQuestionResults);
    }

    [Fact]
    public async Task Handle_DuplicateOptionIds_RemovesDuplicates()
    {
        // Arrange
        var quiz = CreateSampleQuiz();
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        var questions = quiz.Questions.OrderBy(q => q.Order).ToList();
        var q1 = questions[0];
        var q1Options = q1.Options.ToList();

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");
        _mockService.Setup(s => s.CalculateScore(It.IsAny<Quiz>(), It.IsAny<IEnumerable<QuizAnswer>>()))
            .Returns(5);

        var handler = new SubmitAttemptHandler(_context, _mockUserContext.Object, _mockService.Object);

        var optionId = q1Options[0].Id;
        var request = new SubmitAttemptRequest(
            quiz.Id,
            new[]
            {
                // Duplicate option IDs
                new AnswerDto(q1.Id, new Guid[] { optionId, optionId, optionId })
            }
        );

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var savedAttempt = await _context.QuizAttempts
            .Include(a => a.Answers)
            .ThenInclude(a => a.SelectedOptions)
            .FirstOrDefaultAsync();

        Assert.NotNull(savedAttempt);
        Assert.Single(savedAttempt.Answers.First().SelectedOptions); // Duplicates removed
    }

    [Fact]
    public async Task Handle_PerQuestionResults_CalculatesCorrectness()
    {
        // Arrange
        var quiz = CreateSampleQuiz();
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        var questions = quiz.Questions.OrderBy(q => q.Order).ToList();
        var q1 = questions[0];
        var q2 = questions[1];
        var q1Options = q1.Options.ToList();
        var q2Options = q2.Options.ToList();

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");
        _mockService.Setup(s => s.CalculateScore(It.IsAny<Quiz>(), It.IsAny<IEnumerable<QuizAnswer>>()))
            .Returns(5);

        var handler = new SubmitAttemptHandler(_context, _mockUserContext.Object, _mockService.Object);

        var request = new SubmitAttemptRequest(
            quiz.Id,
            new[]
            {
                // Correct answer
                new AnswerDto(q1.Id, new[] { q1Options[0].Id }),
                // Wrong answer
                new AnswerDto(q2.Id, new[] { q2Options[1].Id })
            }
        );

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.PerQuestionResults.Length);
        
        var q1Result = result.PerQuestionResults.First(r => r.QuestionId == q1.Id);
        Assert.True(q1Result.Correct);
        Assert.Equal(5, q1Result.PointsAwarded);
        Assert.Equal(5, q1Result.MaxPoints);

        var q2Result = result.PerQuestionResults.First(r => r.QuestionId == q2.Id);
        Assert.False(q2Result.Correct);
        Assert.Equal(0, q2Result.PointsAwarded);
        Assert.Equal(5, q2Result.MaxPoints);
    }

    private Quiz CreateSampleQuiz()
    {
        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            Title = "Sample Quiz",
            IsPublished = true,
            CreatedByUserId = "admin"
        };

        var q1 = new Question
        {
            Id = Guid.NewGuid(),
            Text = "Question 1",
            Points = 5,
            Order = 1,
            Quiz = quiz
        };
        var opt1_1 = new Option { Id = Guid.NewGuid(), Text = "Correct", IsCorrect = true, Question = q1 };
        var opt1_2 = new Option { Id = Guid.NewGuid(), Text = "Wrong", IsCorrect = false, Question = q1 };
        q1.Options.Add(opt1_1);
        q1.Options.Add(opt1_2);

        var q2 = new Question
        {
            Id = Guid.NewGuid(),
            Text = "Question 2",
            Points = 5,
            Order = 2,
            Quiz = quiz
        };
        var opt2_1 = new Option { Id = Guid.NewGuid(), Text = "Correct", IsCorrect = true, Question = q2 };
        var opt2_2 = new Option { Id = Guid.NewGuid(), Text = "Wrong", IsCorrect = false, Question = q2 };
        q2.Options.Add(opt2_1);
        q2.Options.Add(opt2_2);

        quiz.Questions.Add(q1);
        quiz.Questions.Add(q2);

        return quiz;
    }
}
