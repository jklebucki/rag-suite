using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Features.Chat.SessionManagement;
using RAG.Orchestrator.Api.Localization;
using RAG.Orchestrator.Api.Models;
using RAG.Orchestrator.Api.Services;

namespace RAG.Tests.Orchestrator;

public class SessionManagerTests : IDisposable
{
    private readonly ChatDbContext _context;
    private readonly Mock<ILanguageService> _mockLanguageService;
    private readonly Mock<ILlmService> _mockLlmService;
    private readonly Mock<IGlobalSettingsService> _mockGlobalSettingsService;
    private readonly Mock<ILogger<SessionManager>> _mockLogger;
    private readonly SessionManager _sessionManager;

    public SessionManagerTests()
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChatDbContext(options);
        _mockLanguageService = new Mock<ILanguageService>();
        _mockLlmService = new Mock<ILlmService>();
        _mockGlobalSettingsService = new Mock<IGlobalSettingsService>();
        _mockLogger = new Mock<ILogger<SessionManager>>();

        _mockLanguageService.Setup(ls => ls.GetDefaultLanguage()).Returns("en");
        _mockLanguageService.Setup(ls => ls.NormalizeLanguage(It.IsAny<string>())).Returns<string>(lang => lang ?? "en");
        _mockLanguageService.Setup(ls => ls.GetLocalizedString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("New Conversation");

        _sessionManager = new SessionManager(
            _context,
            _mockLanguageService.Object,
            _mockLlmService.Object,
            _mockGlobalSettingsService.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetUserSessionsAsync_WhenNoSessions_ReturnsEmptyArray()
    {
        // Arrange
        var userId = "user1";

        // Act
        var result = await _sessionManager.GetUserSessionsAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserSessionsAsync_WhenSessionsExist_ReturnsSessionsOrderedByUpdatedAt()
    {
        // Arrange
        var userId = "user1";
        var session1 = new ChatSession
        {
            Id = "session1",
            UserId = userId,
            Title = "Session 1",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };
        var session2 = new ChatSession
        {
            Id = "session2",
            UserId = userId,
            Title = "Session 2",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.ChatSessions.AddRange(session1, session2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionManager.GetUserSessionsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be("session2"); // Most recent first
        result[1].Id.Should().Be("session1");
    }

    [Fact]
    public async Task GetUserSessionsAsync_WhenOtherUserSessionsExist_ReturnsOnlyUserSessions()
    {
        // Arrange
        var userId = "user1";
        var otherUserId = "user2";

        var userSession = new ChatSession
        {
            Id = "session1",
            UserId = userId,
            Title = "User Session",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var otherSession = new ChatSession
        {
            Id = "session2",
            UserId = otherUserId,
            Title = "Other Session",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ChatSessions.AddRange(userSession, otherSession);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionManager.GetUserSessionsAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("session1");
        result[0].UserId.Should().Be(userId);
    }

    [Fact]
    public async Task CreateUserSessionAsync_WithValidRequest_CreatesSession()
    {
        // Arrange
        var userId = "user1";
        var request = new CreateUserSessionRequest
        {
            Title = "My Session",
            Language = "en"
        };

        _mockGlobalSettingsService.Setup(gs => gs.GetLlmSettingsAsync())
            .ReturnsAsync(new LlmSettings { IsOllama = false });
        _mockLlmService.Setup(ls => ls.GetSystemMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("System message");

        // Act
        var result = await _sessionManager.CreateUserSessionAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Title.Should().Be("My Session");

        var dbSession = await _context.ChatSessions.FindAsync(result.Id);
        dbSession.Should().NotBeNull();
        dbSession!.UserId.Should().Be(userId);
        dbSession.Title.Should().Be("My Session");
    }

    [Fact]
    public async Task CreateUserSessionAsync_WithoutTitle_UsesDefaultTitle()
    {
        // Arrange
        var userId = "user1";
        var request = new CreateUserSessionRequest
        {
            Language = "en"
        };

        _mockGlobalSettingsService.Setup(gs => gs.GetLlmSettingsAsync())
            .ReturnsAsync(new LlmSettings { IsOllama = false });

        // Act
        var result = await _sessionManager.CreateUserSessionAsync(userId, request);

        // Assert
        result.Title.Should().Be("New Conversation");
    }

    [Fact]
    public async Task CreateUserSessionAsync_WithoutLanguage_UsesDefaultLanguage()
    {
        // Arrange
        var userId = "user1";
        var request = new CreateUserSessionRequest
        {
            Title = "Test Session"
        };

        _mockGlobalSettingsService.Setup(gs => gs.GetLlmSettingsAsync())
            .ReturnsAsync(new LlmSettings { IsOllama = false });

        // Act
        var result = await _sessionManager.CreateUserSessionAsync(userId, request);

        // Assert
        _mockLanguageService.Verify(ls => ls.GetDefaultLanguage(), Times.Once);
        _mockLanguageService.Verify(ls => ls.NormalizeLanguage("en"), Times.Once);
    }

    [Fact]
    public async Task GetUserSessionAsync_WhenSessionExists_ReturnsSessionWithMessages()
    {
        // Arrange
        var userId = "user1";
        var sessionId = "session1";
        var session = new ChatSession
        {
            Id = sessionId,
            UserId = userId,
            Title = "Test Session",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Messages = new List<ChatMessage>
            {
                new ChatMessage
                {
                    Id = "msg1",
                    Role = "user",
                    Content = "Hello",
                    Timestamp = DateTime.UtcNow.AddMinutes(-5)
                },
                new ChatMessage
                {
                    Id = "msg2",
                    Role = "assistant",
                    Content = "Hi there",
                    Timestamp = DateTime.UtcNow
                }
            }
        };

        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionManager.GetUserSessionAsync(userId, sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(sessionId);
        result.Messages.Should().HaveCount(2);
        result.Messages[0].Content.Should().Be("Hello");
        result.Messages[1].Content.Should().Be("Hi there");
    }

    [Fact]
    public async Task GetUserSessionAsync_WhenSessionDoesNotExist_ReturnsNull()
    {
        // Arrange
        var userId = "user1";
        var sessionId = "nonexistent";

        // Act
        var result = await _sessionManager.GetUserSessionAsync(userId, sessionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserSessionAsync_WhenSessionBelongsToOtherUser_ReturnsNull()
    {
        // Arrange
        var userId = "user1";
        var otherUserId = "user2";
        var sessionId = "session1";
        var session = new ChatSession
        {
            Id = sessionId,
            UserId = otherUserId,
            Title = "Other User Session",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionManager.GetUserSessionAsync(userId, sessionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteUserSessionAsync_WhenSessionExists_DeletesSession()
    {
        // Arrange
        var userId = "user1";
        var sessionId = "session1";
        var session = new ChatSession
        {
            Id = sessionId,
            UserId = userId,
            Title = "Test Session",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionManager.DeleteUserSessionAsync(userId, sessionId);

        // Assert
        result.Should().BeTrue();
        var dbSession = await _context.ChatSessions.FindAsync(sessionId);
        dbSession.Should().BeNull();
    }

    [Fact]
    public async Task DeleteUserSessionAsync_WhenSessionDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var userId = "user1";
        var sessionId = "nonexistent";

        // Act
        var result = await _sessionManager.DeleteUserSessionAsync(userId, sessionId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUserSessionAsync_WhenSessionBelongsToOtherUser_ReturnsFalse()
    {
        // Arrange
        var userId = "user1";
        var otherUserId = "user2";
        var sessionId = "session1";
        var session = new ChatSession
        {
            Id = sessionId,
            UserId = otherUserId,
            Title = "Other User Session",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionManager.DeleteUserSessionAsync(userId, sessionId);

        // Assert
        result.Should().BeFalse();
        var dbSession = await _context.ChatSessions.FindAsync(sessionId);
        dbSession.Should().NotBeNull(); // Session should still exist
    }

    [Fact]
    public async Task CreateUserSessionAsync_WithOllama_InitializesSystemMessage()
    {
        // Arrange
        var userId = "user1";
        var request = new CreateUserSessionRequest
        {
            Title = "Ollama Session",
            Language = "en"
        };

        _mockGlobalSettingsService.Setup(gs => gs.GetLlmSettingsAsync())
            .ReturnsAsync(new LlmSettings { IsOllama = true });
        _mockLlmService.Setup(ls => ls.GetSystemMessageAsync("en", It.IsAny<CancellationToken>()))
            .ReturnsAsync("System message for Ollama");

        // Act
        var result = await _sessionManager.CreateUserSessionAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        _mockLlmService.Verify(ls => ls.GetSystemMessageAsync("en", It.IsAny<CancellationToken>()), Times.Once);
    }
}

