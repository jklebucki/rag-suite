using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Features.Chat;

public interface IUserChatService
{
    Task<UserChatSession[]> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserChatSession> CreateUserSessionAsync(string userId, CreateUserSessionRequest request, CancellationToken cancellationToken = default);
    Task<UserChatSession?> GetUserSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
    Task<Models.MultilingualChatResponse> SendUserMultilingualMessageAsync(string userId, string sessionId, MultilingualChatRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
}