using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using RAG.Forum.Data;

namespace RAG.Forum.Features.Threads;

public static class GetThreadEndpoint
{
    public static RouteGroupBuilder MapGetThread(this RouteGroupBuilder group)
    {
        group.MapGet("/threads/{threadId:guid}", HandleAsync)
            .WithName("Forum_GetThread");

        return group;
    }

    public static async Task<IResult> HandleAsync(
        Guid threadId,
        ForumDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var thread = await dbContext.Threads
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Attachments)
            .Include(t => t.Posts)
                .ThenInclude(p => p.Attachments)
            .FirstOrDefaultAsync(t => t.Id == threadId, cancellationToken);

        if (thread is null)
        {
            return Results.NotFound();
        }

        var response = new GetThreadResponse(ThreadDtoMapper.ToDetailDto(thread));
        return Results.Ok(response);
    }
}

