using Xunit;
using RAG.CyberPanel.Features.GetQuiz;
using RAG.CyberPanel.Data;
using RAG.CyberPanel.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RAG.Tests;

public class GetQuizServiceTests : IDisposable
{
    private readonly CyberPanelDbContext _context;

    public GetQuizServiceTests()
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
    public async Task GetQuizAsync_ExistingQuiz_ReturnsResponse()
    {
        // Arrange
        var quizId = Guid.NewGuid();
        var quiz = new Quiz
        {
            Id = quizId,
            Title = "Test Quiz",
            Description = "Test Description",
            IsPublished = true,
            Questions = new System.Collections.Generic.List<Question>
            {
                new Question
                {
                    Id = Guid.NewGuid(),
                    Text = "Question 1",
                    Points = 2,
                    Order = 1,
                    Options = new System.Collections.Generic.List<Option>
                    {
                        new Option { Id = Guid.NewGuid(), Text = "Option A", IsCorrect = true },
                        new Option { Id = Guid.NewGuid(), Text = "Option B", IsCorrect = false }
                    }
                }
            }
        };

        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();

        var service = new GetQuizService(_context);

        // Act
        var result = await service.GetQuizAsync(quizId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(quizId, result!.Id);
        Assert.Equal("Test Quiz", result.Title);
        Assert.Single(result.Questions);
        Assert.Equal(2, result.Questions[0].Points);
        Assert.Equal(2, result.Questions[0].Options.Length);
        // Should not expose IsCorrect
        Assert.DoesNotContain(result.Questions[0].Options, o => o.GetType().GetProperty("IsCorrect") != null);
    }

    [Fact]
    public async Task GetQuizAsync_NonExistingQuiz_ReturnsNull()
    {
        // Arrange
        var service = new GetQuizService(_context);

        // Act
        var result = await service.GetQuizAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}