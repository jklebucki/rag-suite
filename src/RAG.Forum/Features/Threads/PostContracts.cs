using RAG.Forum.Features.Shared;

namespace RAG.Forum.Features.Threads;

public sealed record CreatePostRequest(
    string Content,
    bool SubscribeToThread = true,
    IReadOnlyCollection<ForumAttachmentUpload>? Attachments = null);

public sealed record CreatePostResponse(ForumPostDto Post);

public sealed record ThreadBadgeDto(
    Guid ThreadId,
    string ThreadTitle,
    string CategoryName,
    bool HasUnreadReplies,
    DateTime UpdatedAt,
    Guid? LastSeenPostId);

public sealed record ThreadBadgesResponse(IReadOnlyCollection<ThreadBadgeDto> Badges);

