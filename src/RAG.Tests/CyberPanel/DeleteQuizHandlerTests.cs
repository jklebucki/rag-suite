using Xunit;
using RAG.CyberPanel.Features.DeleteQuiz;
using RAG.CyberPanel.Data;
using RAG.CyberPanel.Domain;
using RAG.Security.Data;
using RAG.Security.Services;
using Moq;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RAG.Tests.CyberPanel;

public class DeleteQuizHandlerTests : IDisposable
{
    private readonly CyberPanelDbContext _context;
    private readonly SecurityDbContext _securityContext;
    private readonly Mock<IUserContextService> _mockUserContext;

    public DeleteQuizHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CyberPanelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var securityOptions = new DbContextOptionsBuilder<SecurityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CyberPanelDbContext(options);
        _securityContext = new SecurityDbContext(securityOptions);
        _mockUserContext = new Mock<IUserContextService>();
    }

    public void Dispose()
    {
        _context.Dispose();
        _securityContext.Dispose();
    }

    [Fact]
    public async Task Handle_OwnerDeletesQuiz_Success()
    {
        // Arrange
        var quiz = CreateSampleQuiz("owner123");
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("owner123");
        _mockUserContext.Setup(u => u.GetCurrentUserRoles()).Returns(new[] { "User" });

        var handler = new DeleteQuizHandler(_context, _securityContext, _mockUserContext.Object);

        // Act
        var result = await handler.Handle(quiz.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(quiz.Id, result.QuizId);

        var deletedQuiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Id == quiz.Id);
        Assert.Null(deletedQuiz);
    }

    [Fact]
    public async Task Handle_AdminDeletesOthersQuiz_Success()
    {
        // Arrange
        var quiz = CreateSampleQuiz("owner123");
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("admin456");
        _mockUserContext.Setup(u => u.GetCurrentUserRoles()).Returns(new[] { "Admin" });

        var handler = new DeleteQuizHandler(_context, _securityContext, _mockUserContext.Object);

        // Act
        var result = await handler.Handle(quiz.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(quiz.Id, result.QuizId);
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

        var handler = new DeleteQuizHandler(_context, _securityContext, _mockUserContext.Object);

        // Act
        var result = await handler.Handle(quiz.Id, CancellationToken.None);

        // Assert
        Assert.Null(result);

        // Verify quiz still exists
        var quizStill = await _context.Quizzes.FirstOrDefaultAsync(q => q.Id == quiz.Id);
        Assert.NotNull(quizStill);
    }

    [Fact]
    public async Task Handle_NonExistentQuiz_ReturnsNull()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");
        _mockUserContext.Setup(u => u.GetCurrentUserRoles()).Returns(new[] { "User" });

        var handler = new DeleteQuizHandler(_context, _securityContext, _mockUserContext.Object);

        // Act
        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    private Quiz CreateSampleQuiz(string createdBy)
    {
        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            Title = "Test Quiz",
            IsPublished = false,
            CreatedByUserId = createdBy
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
        quiz.Questions.Add(q1);

        return quiz;
    }
}
