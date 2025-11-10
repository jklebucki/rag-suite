using RAG.Forum.Domain;
using RAG.Forum.Features.Shared;

namespace RAG.Forum.Features.Threads;

internal static class ThreadDtoMapper
{
    public static ForumThreadDetailDto ToDetailDto(ForumThread thread)
    {
        return new ForumThreadDetailDto(
            thread.Id,
            thread.CategoryId,
            thread.Category.Name,
            thread.Title,
            thread.AuthorId,
            thread.AuthorEmail,
            thread.Content,
            thread.CreatedAt,
            thread.UpdatedAt,
            thread.LastPostAt,
            thread.IsLocked,
            thread.ViewCount,
            thread.Attachments
                .OrderBy(a => a.CreatedAt)
                .ThenBy(a => a.FileName)
                .Select(ToAttachmentDto)
                .ToList(),
            thread.Posts
                .OrderBy(p => p.CreatedAt)
                .ThenBy(p => p.Id)
                .Select(ToPostDto)
                .ToList());
    }

    public static ForumPostDto ToPostDto(ForumPost post)
    {
        return new ForumPostDto(
            post.Id,
            post.ThreadId,
            post.AuthorId,
            post.AuthorEmail,
            post.Content,
            post.IsAnswer,
            post.CreatedAt,
            post.UpdatedAt,
            post.Attachments
                .OrderBy(a => a.CreatedAt)
                .ThenBy(a => a.FileName)
                .Select(ToAttachmentDto)
                .ToList());
    }

    public static ForumAttachmentDto ToAttachmentDto(ForumAttachment attachment)
    {
        return new ForumAttachmentDto(
            attachment.Id,
            attachment.FileName,
            attachment.ContentType,
            attachment.Size,
            attachment.CreatedAt,
            attachment.PostId);
    }
}

