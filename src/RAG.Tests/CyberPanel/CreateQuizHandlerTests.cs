using Xunit;
using RAG.CyberPanel.Features.CreateQuiz;
using RAG.CyberPanel.Data;
using RAG.Security.Services;
using Moq;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RAG.Tests.CyberPanel;

public class CreateQuizHandlerTests : IDisposable
{
    private readonly CyberPanelDbContext _context;
    private readonly Mock<IUserContextService> _mockUserContext;

    public CreateQuizHandlerTests()
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
    public async Task Handle_ValidRequest_CreatesQuiz()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var handler = new CreateQuizHandler(_context, _mockUserContext.Object);

        var request = new CreateQuizRequest(
            "Test Quiz",
            "Test Description",
            true,
            new[]
            {
                new QuestionDto(
                    null,
                    "Question 1",
                    null,
                    2,
                    new[]
                    {
                        new OptionDto(null, "Option A", null, true),
                        new OptionDto(null, "Option B", null, false)
                    }
                )
            },
            "en"
        );

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Quiz", result.Title);
        Assert.Equal("user123", result.CreatedByUserId);
        Assert.Single(result.Questions);
        Assert.Equal(2, result.Questions.First().Points);
        Assert.Equal(2, result.Questions.First().Options.Count);
    }

    [Fact]
    public async Task Handle_NoUserId_UsesSystem()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns((string?)null);

        var handler = new CreateQuizHandler(_context, _mockUserContext.Object);

        var request = new CreateQuizRequest(
            "Test Quiz",
            null,
            false,
            new[]
            {
                new QuestionDto(
                    null,
                    "Question 1",
                    null,
                    1,
                    new[]
                    {
                        new OptionDto(null, "Option A", null, true)
                    }
                )
            },
            null
        );

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal("system", result.CreatedByUserId);
    }
}