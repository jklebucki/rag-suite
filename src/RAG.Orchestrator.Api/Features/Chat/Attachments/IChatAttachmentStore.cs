namespace RAG.Orchestrator.Api.Features.Chat.Attachments;

public interface IChatAttachmentStore
{
    Task<ChatAttachmentDraft[]> GetDraftsAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
    Task<ChatAttachmentFile[]> GetFilesAsync(string userId, string sessionId, IEnumerable<string> attachmentIds, CancellationToken cancellationToken = default);
    Task SaveBatchAsync(string userId, string sessionId, IEnumerable<ChatAttachmentFile> files, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string userId, string sessionId, string attachmentId, CancellationToken cancellationToken = default);
    Task RemoveBatchAsync(string userId, string sessionId, IEnumerable<string> attachmentIds, CancellationToken cancellationToken = default);
    Task ClearSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
}
