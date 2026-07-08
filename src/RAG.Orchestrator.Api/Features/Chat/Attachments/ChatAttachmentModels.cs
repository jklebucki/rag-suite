namespace RAG.Orchestrator.Api.Features.Chat.Attachments;

public record ChatAttachmentDraft(
    string Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    int TokenCount,
    DateTimeOffset UploadedAt
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
    string Content
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
