namespace RAG.Forum.Features.Threads;

public sealed record ListThreadsRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? CategoryId = null,
    string? Search = null);

public sealed record ForumThreadSummaryDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Title,
    string AuthorId,
    string AuthorEmail,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime LastPostAt,
    bool IsLocked,
    int ViewCount,
    int ReplyCount,
    int AttachmentCount);

public sealed record ListThreadsResponse(
    IReadOnlyCollection<ForumThreadSummaryDto> Threads,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

