using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using RAG.Forum.Data;

namespace RAG.Forum.Features.Threads;

public static class DownloadAttachmentEndpoint
{
    public static RouteGroupBuilder MapDownloadThreadAttachment(this RouteGroupBuilder group)
    {
        group.MapGet("/threads/{threadId:guid}/attachments/{attachmentId:guid}", HandleAsync)
            .WithName("Forum_DownloadThreadAttachment");

        return group;
    }

    public static async Task<IResult> HandleAsync(
        Guid threadId,
        Guid attachmentId,
        ForumDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var attachment = await dbContext.Attachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.ThreadId == threadId, cancellationToken);

        if (attachment is null)
        {
            return Results.NotFound();
        }

        return Results.File(attachment.Data, attachment.ContentType, attachment.FileName);
    }
}

