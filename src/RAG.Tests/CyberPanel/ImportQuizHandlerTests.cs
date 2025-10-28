using Xunit;
using RAG.CyberPanel.Features.ImportQuiz;
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

public class ImportQuizHandlerTests : IDisposable
{
    private readonly CyberPanelDbContext _context;
    private readonly Mock<IUserContextService> _mockUserContext;

    public ImportQuizHandlerTests()
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
    public async Task Handle_CreateNew_CreatesQuizSuccessfully()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var handler = new ImportQuizHandler(_context, _mockUserContext.Object);

        var request = new ImportQuizRequest(
            "Imported Quiz",
            "Imported Description",
            true,
            new[]
            {
                new ImportedQuestionDto(
                    "Question 1",
                    null,
                    5,
                    new[]
                    {
                        new ImportedOptionDto("Option A", null, true),
                        new ImportedOptionDto("Option B", null, false)
                    }
                ),
                new ImportedQuestionDto(
                    "Question 2",
                    null,
                    10,
                    new[]
                    {
                        new ImportedOptionDto("Option C", null, false),
                        new ImportedOptionDto("Option D", null, true),
                        new ImportedOptionDto("Option E", null, false)
                    }
                )
            },
            CreateNew: true,
            Language: "en"
        );

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.QuizId);
        Assert.Equal("Imported Quiz", result.Title);
        Assert.Equal(2, result.QuestionsImported);
        Assert.Equal(5, result.OptionsImported); // 2 + 3
        Assert.False(result.WasOverwritten);

        var savedQuiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == result.QuizId);

        Assert.NotNull(savedQuiz);
        Assert.Equal("user123", savedQuiz.CreatedByUserId);
        Assert.Equal(2, savedQuiz.Questions.Count);
        Assert.True(savedQuiz.IsPublished);
        Assert.Equal("en", savedQuiz.Language);
    }

    [Fact]
    public async Task Handle_CreateNewWithAnonymousUser_UsesSystemUserId()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns((string?)null);

        var handler = new ImportQuizHandler(_context, _mockUserContext.Object);

        var request = new ImportQuizRequest(
            "Test Quiz",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("Q1", null, 5, new[]
                {
                    new ImportedOptionDto("A", null, true),
                    new ImportedOptionDto("B", null, false)
                })
            }
        );

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var savedQuiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Id == result.QuizId);
        Assert.NotNull(savedQuiz);
        Assert.Equal("system", savedQuiz.CreatedByUserId);
    }

    [Fact]
    public async Task Handle_Overwrite_OverwritesExistingQuiz()
    {
        // Arrange
        var existingQuiz = CreateSampleQuiz("owner123");
        await _context.Quizzes.AddAsync(existingQuiz);
        await _context.SaveChangesAsync();

        var originalQuestionCount = existingQuiz.Questions.Count;
        Assert.Equal(2, originalQuestionCount);

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user456");

        var handler = new ImportQuizHandler(_context, _mockUserContext.Object);

        var request = new ImportQuizRequest(
            "Overwritten Title",
            "Overwritten Description",
            true,
            new[]
            {
                new ImportedQuestionDto("New Q1", null, 7, new[]
                {
                    new ImportedOptionDto("X", null, true),
                    new ImportedOptionDto("Y", null, false),
                    new ImportedOptionDto("Z", null, false)
                })
            },
            CreateNew: false,
            OverwriteQuizId: existingQuiz.Id,
            Language: "pl"
        );

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(existingQuiz.Id, result.QuizId);
        Assert.Equal("Overwritten Title", result.Title);
        Assert.Equal(1, result.QuestionsImported);
        Assert.Equal(3, result.OptionsImported);
        Assert.True(result.WasOverwritten);

        var overwrittenQuiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == existingQuiz.Id);

        Assert.NotNull(overwrittenQuiz);
        Assert.Equal("Overwritten Title", overwrittenQuiz.Title);
        Assert.Equal("Overwritten Description", overwrittenQuiz.Description);
        Assert.Equal("pl", overwrittenQuiz.Language);
        Assert.Single(overwrittenQuiz.Questions);
        Assert.Equal("New Q1", overwrittenQuiz.Questions.First().Text);
        Assert.Equal(3, overwrittenQuiz.Questions.First().Options.Count);
    }

    [Fact]
    public async Task Handle_OverwriteNonExistentQuiz_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var handler = new ImportQuizHandler(_context, _mockUserContext.Object);

        var request = new ImportQuizRequest(
            "Title",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("Q1", null, 5, new[]
                {
                    new ImportedOptionDto("A", null, true),
                    new ImportedOptionDto("B", null, false)
                })
            },
            CreateNew: false,
            OverwriteQuizId: Guid.NewGuid()
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(request, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ImportWithImages_PreservesImageData()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var handler = new ImportQuizHandler(_context, _mockUserContext.Object);

        var request = new ImportQuizRequest(
            "Quiz with Images",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto(
                    "Image Question",
                    "data:image/png;base64,iVBORw0KGgoAAAANSUhEUg",
                    5,
                    new[]
                    {
                        new ImportedOptionDto("Option A", "data:image/png;base64,ABC123", true),
                        new ImportedOptionDto("Option B", null, false)
                    }
                )
            }
        );

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var savedQuiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == result.QuizId);

        Assert.NotNull(savedQuiz);
        var question = savedQuiz.Questions.First();
        Assert.Equal("data:image/png;base64,iVBORw0KGgoAAAANSUhEUg", question.ImageUrl);
        Assert.Equal("data:image/png;base64,ABC123", question.Options.First().ImageUrl);
    }

    [Fact]
    public async Task Handle_ImportMultipleQuestions_MaintainsOrder()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var handler = new ImportQuizHandler(_context, _mockUserContext.Object);

        var request = new ImportQuizRequest(
            "Ordered Quiz",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("First", null, 1, new[]
                {
                    new ImportedOptionDto("A", null, true),
                    new ImportedOptionDto("B", null, false)
                }),
                new ImportedQuestionDto("Second", null, 2, new[]
                {
                    new ImportedOptionDto("C", null, true),
                    new ImportedOptionDto("D", null, false)
                }),
                new ImportedQuestionDto("Third", null, 3, new[]
                {
                    new ImportedOptionDto("E", null, true),
                    new ImportedOptionDto("F", null, false)
                })
            }
        );

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var savedQuiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == result.QuizId);

        Assert.NotNull(savedQuiz);
        var questions = savedQuiz.Questions.OrderBy(q => q.Order).ToList();
        Assert.Equal(3, questions.Count);
        Assert.Equal("First", questions[0].Text);
        Assert.Equal(0, questions[0].Order);
        Assert.Equal("Second", questions[1].Text);
        Assert.Equal(1, questions[1].Order);
        Assert.Equal("Third", questions[2].Text);
        Assert.Equal(2, questions[2].Order);
    }

    [Fact]
    public async Task Handle_CountsOptionsCorrectly()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var handler = new ImportQuizHandler(_context, _mockUserContext.Object);

        var request = new ImportQuizRequest(
            "Quiz",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("Q1", null, 5, new[]
                {
                    new ImportedOptionDto("A", null, true),
                    new ImportedOptionDto("B", null, false)
                }),
                new ImportedQuestionDto("Q2", null, 5, new[]
                {
                    new ImportedOptionDto("C", null, true),
                    new ImportedOptionDto("D", null, false),
                    new ImportedOptionDto("E", null, false),
                    new ImportedOptionDto("F", null, false)
                })
            }
        );

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.QuestionsImported);
        Assert.Equal(6, result.OptionsImported); // 2 + 4
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
