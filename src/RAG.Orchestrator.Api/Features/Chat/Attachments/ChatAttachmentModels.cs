namespace RAG.Orchestrator.Api.Features.Chat.Attachments;

public record ChatAttachmentDraft(
    string Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    int TokenCount,
    DateTimeOffset UploadedAt,
    string Status = "ready",
    int Progress = 100,
    int? PageCount = null,
    string? Provider = null,
    string? ErrorCode = null,
    string? DocumentJobId = null
);

public record ChatContextUsageResponse(
    int UsedTokens,
    int LimitTokens,
    int PercentUsed,
    bool IsLimitExceeded,
    int AttachmentTokens,
    int AttachmentLimitTokens,
    ChatAttachmentDraft[] Attachments
);

public record ChatAttachmentUploadResponse(ChatContextUsageResponse ContextUsage);

public record ChatAttachmentFile(
    string Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    int TokenCount,
    string Content,
    string Status = "ready",
    int Progress = 100,
    int? PageCount = null,
    string? Provider = null,
    string? ErrorCode = null,
    string? DocumentJobId = null,
    DateTimeOffset? UploadedAt = null
);

public record PreparedChatAttachments(
    ChatAttachmentFile[] Files,
    int TokenCount,
    ChatContextUsageResponse ContextUsage
);

public class ChatAttachmentException : Exception
{
    public ChatAttachmentException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
