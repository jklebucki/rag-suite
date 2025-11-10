namespace RAG.Forum.Features.Shared;

public sealed record ForumAttachmentUpload(
    string FileName,
    string ContentType,
    string DataBase64);

public sealed record ForumAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    DateTime CreatedAt,
    Guid? PostId);

