using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;
using RAG.CyberPanel.Domain;
using RAG.CyberPanel.Features.ExportQuiz;

namespace RAG.Tests.CyberPanel;

public class ExportQuizServiceTests : IDisposable
{
    private readonly CyberPanelDbContext _context;

    public ExportQuizServiceTests()
    {
        var options = new DbContextOptionsBuilder<CyberPanelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CyberPanelDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task ExportQuizAsync_ValidQuiz_ReturnsCompleteExport()
    {
        // Arrange
        var quiz = CreateSampleQuiz();
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        var service = new ExportQuizService(_context);

        // Act
        var result = await service.ExportQuizAsync(quiz.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(quiz.Id, result.Id);
        Assert.Equal("Export Quiz", result.Title);
        Assert.Equal("Description", result.Description);
        Assert.Equal("user123", result.CreatedByUserId);
        Assert.True(result.IsPublished);
        Assert.Equal("en", result.Language);
        Assert.Equal("1.0", result.ExportVersion);
        Assert.Equal(2, result.Questions.Length);
    }

    [Fact]
    public async Task ExportQuizAsync_NonExistentQuiz_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new ExportQuizService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExportQuizAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ExportQuizAsync_IncludesAllQuestions()
    {
        // Arrange
        var quiz = CreateSampleQuiz();
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        var service = new ExportQuizService(_context);

        // Act
        var result = await service.ExportQuizAsync(quiz.Id, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Questions.Length);
        Assert.Equal("Question 1", result.Questions[0].Text);
        Assert.Equal("Question 2", result.Questions[1].Text);
    }

    [Fact]
    public async Task ExportQuizAsync_IncludesAllOptions()
    {
        // Arrange
        var quiz = CreateSampleQuiz();
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        var service = new ExportQuizService(_context);

        // Act
        var result = await service.ExportQuizAsync(quiz.Id, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Questions[0].Options.Length);
        Assert.Equal(3, result.Questions[1].Options.Length);

        Assert.True(result.Questions[0].Options[0].IsCorrect);
        Assert.False(result.Questions[0].Options[1].IsCorrect);
    }

    [Fact]
    public async Task ExportQuizAsync_PreservesImageData()
    {
        // Arrange
        var quiz = CreateSampleQuiz();
        quiz.Questions.First().ImageUrl = "data:image/png;base64,ABC123";
        quiz.Questions.First().Options.First().ImageUrl = "data:image/png;base64,XYZ789";

        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        var service = new ExportQuizService(_context);

        // Act
        var result = await service.ExportQuizAsync(quiz.Id, CancellationToken.None);

        // Assert
        Assert.Equal("data:image/png;base64,ABC123", result.Questions[0].ImageUrl);
        Assert.Equal("data:image/png;base64,XYZ789", result.Questions[0].Options[0].ImageUrl);
    }

    [Fact]
    public async Task ExportQuizAsync_MaintainsQuestionOrder()
    {
        // Arrange
        var quiz = CreateSampleQuiz();
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        var service = new ExportQuizService(_context);

        // Act
        var result = await service.ExportQuizAsync(quiz.Id, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.Questions[0].Order);
        Assert.Equal(1, result.Questions[1].Order);
        Assert.Equal("Question 1", result.Questions[0].Text);
        Assert.Equal("Question 2", result.Questions[1].Text);
    }

    private Quiz CreateSampleQuiz()
    {
        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            Title = "Export Quiz",
            Description = "Description",
            IsPublished = true,
            CreatedByUserId = "user123",
            Language = "en",
            CreatedAt = DateTime.UtcNow
        };

        var q1 = new Question
        {
            Id = Guid.NewGuid(),
            Text = "Question 1",
            Points = 5,
            Order = 0,
            Quiz = quiz
        };
        q1.Options.Add(new Option { Id = Guid.NewGuid(), Text = "A", IsCorrect = true, Question = q1 });
        q1.Options.Add(new Option { Id = Guid.NewGuid(), Text = "B", IsCorrect = false, Question = q1 });

        var q2 = new Question
        {
            Id = Guid.NewGuid(),
            Text = "Question 2",
            Points = 10,
            Order = 1,
            Quiz = quiz
        };
        q2.Options.Add(new Option { Id = Guid.NewGuid(), Text = "C", IsCorrect = true, Question = q2 });
        q2.Options.Add(new Option { Id = Guid.NewGuid(), Text = "D", IsCorrect = false, Question = q2 });
        q2.Options.Add(new Option { Id = Guid.NewGuid(), Text = "E", IsCorrect = false, Question = q2 });

        quiz.Questions.Add(q1);
        quiz.Questions.Add(q2);

        return quiz;
    }
}
