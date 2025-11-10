using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
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
            .Join(
                dbContext.Threads.AsNoTracking(),
                badge => badge.ThreadId,
                thread => thread.Id,
                (badge, thread) => new { badge, thread })
            .Join(
                dbContext.Categories.AsNoTracking(),
                combined => combined.thread.CategoryId,
                category => category.Id,
                (combined, category) => new ThreadBadgeDto(
                    combined.badge.ThreadId,
                    combined.thread.Title,
                    category.Name,
                    combined.badge.HasUnreadReplies,
                    combined.badge.UpdatedAt,
                    combined.badge.LastSeenPostId))
            .OrderByDescending(b => b.UpdatedAt)
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

