using RAG.Forum.Features.Shared;

namespace RAG.Forum.Features.Threads;

public sealed record ForumPostDto(
    Guid Id,
    Guid ThreadId,
    string AuthorId,
    string AuthorEmail,
    string Content,
    bool IsAnswer,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyCollection<ForumAttachmentDto> Attachments);

public sealed record ForumThreadDetailDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Title,
    string AuthorId,
    string AuthorEmail,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime LastPostAt,
    bool IsLocked,
    int ViewCount,
    IReadOnlyCollection<ForumAttachmentDto> Attachments,
    IReadOnlyCollection<ForumPostDto> Posts);

public sealed record GetThreadResponse(ForumThreadDetailDto Thread);

public sealed record CreateThreadRequest(
    Guid CategoryId,
    string Title,
    string Content,
    IReadOnlyCollection<ForumAttachmentUpload>? Attachments);

public sealed record CreateThreadResponse(ForumThreadDetailDto Thread);

