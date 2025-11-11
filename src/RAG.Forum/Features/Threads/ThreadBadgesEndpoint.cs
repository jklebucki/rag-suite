using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RAG.Forum.Data;
using RAG.Security.Services;

namespace RAG.Forum.Features.Threads;

public static class ThreadBadgesEndpoint
{
    public static RouteGroupBuilder MapThreadBadges(this RouteGroupBuilder group)
    {
        group.MapGet("/badges", GetBadgesAsync)
            .WithName("Forum_GetBadges")
            .RequireAuthorization();

        group.MapPatch("/badges/{threadId:guid}/ack", AcknowledgeBadgeAsync)
            .WithName("Forum_AcknowledgeBadge")
            .RequireAuthorization();

        return group;
    }

    public static async Task<IResult> GetBadgesAsync(
        ForumDbContext dbContext,
        IUserContextService userContext,
        CancellationToken cancellationToken)
    {
        var userId = userContext.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var badges = await dbContext.Badges
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Include(b => b.Thread)
                .ThenInclude(t => t.Category)
            .OrderByDescending(b => b.UpdatedAt)
            .Select(b => new ThreadBadgeDto(
                b.ThreadId,
                b.Thread.Title,
                b.Thread.Category.Name,
                b.HasUnreadReplies,
                b.UpdatedAt,
                b.LastSeenPostId))
            .ToListAsync(cancellationToken);

        return Results.Ok(new ThreadBadgesResponse(badges));
    }

    public static async Task<IResult> AcknowledgeBadgeAsync(
        Guid threadId,
        ForumDbContext dbContext,
        IUserContextService userContext,
        CancellationToken cancellationToken)
    {
        var userId = userContext.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var badge = await dbContext.Badges
            .FirstOrDefaultAsync(b => b.ThreadId == threadId && b.UserId == userId, cancellationToken);

        if (badge is null)
        {
            return Results.NotFound();
        }

        var latestPostId = await dbContext.Posts
            .Where(p => p.ThreadId == threadId)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        badge.LastSeenPostId = latestPostId;
        badge.HasUnreadReplies = false;
        badge.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}

