using RAG.Orchestrator.Api.Features.Chat;

namespace RAG.Orchestrator.Api.Features.Chat.SessionManagement;

/// <summary>
/// Manages chat sessions for users
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Gets all sessions for a user
    /// </summary>
    Task<UserChatSession[]> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new chat session for a user
    /// </summary>
    Task<UserChatSession> CreateUserSessionAsync(string userId, CreateUserSessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific session by ID if it belongs to the user
    /// </summary>
    Task<UserChatSession?> GetUserSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session if it belongs to the user
    /// </summary>
    Task<bool> DeleteUserSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
}

