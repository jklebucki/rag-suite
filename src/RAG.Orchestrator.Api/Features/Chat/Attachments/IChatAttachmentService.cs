namespace RAG.Orchestrator.Api.Features.Chat.Attachments;

public interface IChatAttachmentService
{
    Task<ChatContextUsageResponse?> GetContextAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
    Task<ChatAttachmentUploadResponse> UploadAsync(string userId, string sessionId, IFormFileCollection files, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string userId, string sessionId, string attachmentId, CancellationToken cancellationToken = default);
    Task<PreparedChatAttachments> PrepareForMessageAsync(string userId, string sessionId, string message, IEnumerable<string>? attachmentIds, CancellationToken cancellationToken = default);
    Task CommitMessageAttachmentsAsync(string userId, string sessionId, IEnumerable<string>? attachmentIds, CancellationToken cancellationToken = default);
    Task ClearSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
}
