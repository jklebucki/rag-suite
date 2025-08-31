using RAG.Domain.Entities;
using RAG.Domain.ValueObjects;

namespace RAG.Infrastructure.Persistence.Mock;

public interface IChatSessionRepository
{
    Task<ChatSession?> GetByIdAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<string> SaveAsync(ChatSession session, CancellationToken cancellationToken = default);
    Task<List<ChatSession>> GetByUserIdAsync(string userId, int limit = 20, int offset = 0, CancellationToken cancellationToken = default);
}

public class MockChatSessionRepository : IChatSessionRepository
{
    private readonly Dictionary<string, ChatSession> _sessions = new();
    private readonly ILogger<MockChatSessionRepository> _logger;

    public MockChatSessionRepository(ILogger<MockChatSessionRepository> logger)
    {
        _logger = logger;
    }

    public Task<ChatSession?> GetByIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId.Value, out var session);
        return Task.FromResult(session);
    }

    public Task<string> SaveAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id.Value] = session;
        _logger.LogInformation("Saved session {SessionId} for user {UserId}", session.Id.Value, session.UserId);
        return Task.FromResult(session.Id.Value);
    }

    public Task<List<ChatSession>> GetByUserIdAsync(string userId, int limit = 20, int offset = 0, CancellationToken cancellationToken = default)
    {
        var userSessions = _sessions.Values
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToList();

        return Task.FromResult(userSessions);
    }
}
