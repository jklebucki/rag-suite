using Xunit;
using RAG.CyberPanel.Features.UpdateQuiz;
using RAG.CyberPanel.Data;
using RAG.CyberPanel.Domain;
using RAG.Security.Services;
using Moq;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RAG.Tests.CyberPanel;

public class UpdateQuizHandlerTests : IDisposable
{
    private readonly CyberPanelDbContext _context;
    private readonly Mock<IUserContextService> _mockUserContext;

    public UpdateQuizHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CyberPanelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CyberPanelDbContext(options);
        _mockUserContext = new Mock<IUserContextService>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Handle_OwnerUpdatesQuiz_Success()
    {
        // Arrange
        var quiz = CreateSampleQuiz("owner123");
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("owner123");
        _mockUserContext.Setup(u => u.GetCurrentUserRoles()).Returns(new[] { "User" });

        var handler = new UpdateQuizHandler(_context, _mockUserContext.Object);

        var request = new UpdateQuizRequest(
            null,
            "Updated Title",
            "Updated Description",
            null,
            true,
            new[]
            {
                new QuestionRequest(null, "New Question", null, 10, 0, new[]
                {
                    new OptionRequest(null, "Option A", null, true),
                    new OptionRequest(null, "Option B", null, false)
                })
            },
            "pl"
        );

        // Act
        var result = await handler.Handle(quiz.Id, request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.True(result.IsPublished);

        var updatedQuiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == quiz.Id);

        Assert.NotNull(updatedQuiz);
        Assert.Equal("Updated Title", updatedQuiz.Title);
        Assert.Equal("Updated Description", updatedQuiz.Description);
        Assert.Equal("pl", updatedQuiz.Language);
        Assert.Single(updatedQuiz.Questions);
        
        var question = updatedQuiz.Questions.First();
        Assert.Equal("New Question", question.Text);
        Assert.Equal(10, question.Points);
        Assert.Equal(2, question.Options.Count);
    }

    [Fact]
    public async Task Handle_AdminUpdatesOthersQuiz_Success()
    {
        // Arrange
        var quiz = CreateSampleQuiz("owner123");
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("admin456");
        _mockUserContext.Setup(u => u.GetCurrentUserRoles()).Returns(new[] { "Admin" });

        var handler = new UpdateQuizHandler(_context, _mockUserContext.Object);

        var request = new UpdateQuizRequest(
            null,
            "Admin Updated",
            null,
            null,
            true,
            new[]
            {
                new QuestionRequest(null, "Question 1", null, 5, 0, new[]
                {
                    new OptionRequest(null, "A", null, true)
                })
            }
        );

        // Act
        var result = await handler.Handle(quiz.Id, request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Admin Updated", result.Title);
    }

    [Fact]
    public async Task Handle_NonOwnerNonAdmin_ReturnsNull()
    {
        // Arrange
        var quiz = CreateSampleQuiz("owner123");
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("other456");
        _mockUserContext.Setup(u => u.GetCurrentUserRoles()).Returns(new[] { "User" });

        var handler = new UpdateQuizHandler(_context, _mockUserContext.Object);

        var request = new UpdateQuizRequest(
            null,
            "Unauthorized Update",
            null,
            null,
            true,
            new[]
            {
                new QuestionRequest(null, "Q1", null, 5, 0, new[]
                {
                    new OptionRequest(null, "A", null, true)
                })
            }
        );

        // Act
        var result = await handler.Handle(quiz.Id, request, CancellationToken.None);

        // Assert
        Assert.Null(result);

        // Verify quiz was not changed
        var unchangedQuiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Id == quiz.Id);
        Assert.NotNull(unchangedQuiz);
        Assert.Equal("Original Title", unchangedQuiz.Title);
    }

    [Fact]
    public async Task Handle_NonExistentQuiz_ReturnsNull()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");
        _mockUserContext.Setup(u => u.GetCurrentUserRoles()).Returns(new[] { "User" });

        var handler = new UpdateQuizHandler(_context, _mockUserContext.Object);

        var request = new UpdateQuizRequest(
            null,
            "Title",
            null,
            null,
            true,
            new[]
            {
                new QuestionRequest(null, "Q1", null, 5, 0, new[]
                {
                    new OptionRequest(null, "A", null, true)
                })
            }
        );

        // Act
        var result = await handler.Handle(Guid.NewGuid(), request, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_DeletesOldQuestions_AddsNewQuestions()
    {
        // Arrange
        var quiz = CreateSampleQuiz("owner123");
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        var originalQuestionCount = quiz.Questions.Count;
        Assert.Equal(2, originalQuestionCount);

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("owner123");
        _mockUserContext.Setup(u => u.GetCurrentUserRoles()).Returns(new[] { "User" });

        var handler = new UpdateQuizHandler(_context, _mockUserContext.Object);

        var request = new UpdateQuizRequest(
            null,
            "Updated Quiz",
            null,
            null,
            true,
            new[]
            {
                new QuestionRequest(null, "Brand New Question 1", null, 3, 0, new[]
                {
                    new OptionRequest(null, "X", null, true),
                    new OptionRequest(null, "Y", null, false)
                }),
                new QuestionRequest(null, "Brand New Question 2", null, 4, 1, new[]
                {
                    new OptionRequest(null, "Z", null, true)
                }),
                new QuestionRequest(null, "Brand New Question 3", null, 5, 2, new[]
                {
                    new OptionRequest(null, "W", null, false),
                    new OptionRequest(null, "V", null, true)
                })
            }
        );

        // Act
        var result = await handler.Handle(quiz.Id, request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var updatedQuiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == quiz.Id);

        Assert.NotNull(updatedQuiz);
        Assert.Equal(3, updatedQuiz.Questions.Count); // 3 new questions
        
        var questions = updatedQuiz.Questions.OrderBy(q => q.Order).ToList();
        Assert.Equal("Brand New Question 1", questions[0].Text);
        Assert.Equal("Brand New Question 2", questions[1].Text);
        Assert.Equal("Brand New Question 3", questions[2].Text);
    }

    [Fact]
    public async Task Handle_UpdatesPublishedStatus()
    {
        // Arrange
        var quiz = CreateSampleQuiz("owner123");
        quiz.IsPublished = true;
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("owner123");
        _mockUserContext.Setup(u => u.GetCurrentUserRoles()).Returns(new[] { "User" });

        var handler = new UpdateQuizHandler(_context, _mockUserContext.Object);

        var request = new UpdateQuizRequest(
            null,
            "Title",
            null,
            null,
            false, // Unpublish
            new[]
            {
                new QuestionRequest(null, "Q1", null, 5, 0, new[]
                {
                    new OptionRequest(null, "A", null, true)
                })
            }
        );

        // Act
        var result = await handler.Handle(quiz.Id, request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsPublished);

        var updatedQuiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Id == quiz.Id);
        Assert.NotNull(updatedQuiz);
        Assert.False(updatedQuiz.IsPublished);
    }

    [Fact]
    public async Task Handle_UpdatesLanguage()
    {
        // Arrange
        var quiz = CreateSampleQuiz("owner123");
        quiz.Language = "en";
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("owner123");
        _mockUserContext.Setup(u => u.GetCurrentUserRoles()).Returns(new[] { "User" });

        var handler = new UpdateQuizHandler(_context, _mockUserContext.Object);

        var request = new UpdateQuizRequest(
            null,
            "Title",
            null,
            null,
            true,
            new[]
            {
                new QuestionRequest(null, "Q1", null, 5, 0, new[]
                {
                    new OptionRequest(null, "A", null, true)
                })
            },
            "ro" // Change to Romanian
        );

        // Act
        var result = await handler.Handle(quiz.Id, request, CancellationToken.None);

        // Assert
        var updatedQuiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Id == quiz.Id);
        Assert.NotNull(updatedQuiz);
        Assert.Equal("ro", updatedQuiz.Language);
    }

    private Quiz CreateSampleQuiz(string createdBy)
    {
        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Original Description",
            IsPublished = false,
            CreatedByUserId = createdBy,
            Language = "en"
        };

        var q1 = new Question
        {
            Id = Guid.NewGuid(),
            Text = "Original Question 1",
            Points = 5,
            Order = 0,
            Quiz = quiz
        };
        q1.Options.Add(new Option { Id = Guid.NewGuid(), Text = "A", IsCorrect = true, Question = q1 });
        q1.Options.Add(new Option { Id = Guid.NewGuid(), Text = "B", IsCorrect = false, Question = q1 });

        var q2 = new Question
        {
            Id = Guid.NewGuid(),
            Text = "Original Question 2",
            Points = 5,
            Order = 1,
            Quiz = quiz
        };
        q2.Options.Add(new Option { Id = Guid.NewGuid(), Text = "C", IsCorrect = true, Question = q2 });

        quiz.Questions.Add(q1);
        quiz.Questions.Add(q2);

        return quiz;
    }
}
