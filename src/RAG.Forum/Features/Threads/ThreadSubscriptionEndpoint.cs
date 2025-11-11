using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using RAG.Forum.Data;
using RAG.Forum.Domain;
using RAG.Security.Services;

namespace RAG.Forum.Features.Threads;

public static class ThreadSubscriptionEndpoint
{
    public static RouteGroupBuilder MapThreadSubscriptions(this RouteGroupBuilder group)
    {
        group.MapPost("/threads/{threadId:guid}/subscribe", SubscribeAsync)
            .WithName("Forum_SubscribeThread")
            .RequireAuthorization();

        group.MapDelete("/threads/{threadId:guid}/subscribe", UnsubscribeAsync)
            .WithName("Forum_UnsubscribeThread")
            .RequireAuthorization();

        return group;
    }

    public static async Task<IResult> SubscribeAsync(
        Guid threadId,
        [FromBody] SubscribeThreadRequest request,
        ForumDbContext dbContext,
        IUserContextService userContext,
        CancellationToken cancellationToken)
    {
        var userId = userContext.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var userEmail = userContext.GetCurrentUserEmail() ?? string.Empty;

        var thread = await dbContext.Threads
            .Include(t => t.Subscriptions)
            .Include(t => t.Badges)
            .FirstOrDefaultAsync(t => t.Id == threadId, cancellationToken);

        if (thread is null)
        {
            return Results.NotFound();
        }

        var subscription = thread.Subscriptions.FirstOrDefault(s => s.UserId == userId);
        var utcNow = DateTime.UtcNow;

        if (subscription is null)
        {
            subscription = new ThreadSubscription
            {
                Id = Guid.NewGuid(),
                ThreadId = thread.Id,
                UserId = userId,
                Email = userEmail,
                NotifyOnReply = request.NotifyOnReply,
                SubscribedAt = utcNow
            };
            thread.Subscriptions.Add(subscription);
        }
        else
        {
            subscription.Email = userEmail;
            subscription.NotifyOnReply = request.NotifyOnReply;
        }

        var badge = thread.Badges.FirstOrDefault(b => b.UserId == userId);
        if (badge is null)
        {
            thread.Badges.Add(new ThreadBadge
            {
                Id = Guid.NewGuid(),
                ThreadId = thread.Id,
                UserId = userId,
                HasUnreadReplies = false,
                UpdatedAt = utcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }

    public static async Task<IResult> UnsubscribeAsync(
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

        var subscription = await dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.ThreadId == threadId && s.UserId == userId, cancellationToken);

        if (subscription is null)
        {
            return Results.NotFound();
        }

        dbContext.Subscriptions.Remove(subscription);

        var badge = await dbContext.Badges
            .FirstOrDefaultAsync(b => b.ThreadId == threadId && b.UserId == userId, cancellationToken);

        if (badge is not null)
        {
            dbContext.Badges.Remove(badge);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}

public sealed record SubscribeThreadRequest(bool NotifyOnReply);

