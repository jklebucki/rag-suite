using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RAG.Forum.Data;
using RAG.Forum.Domain;
using RAG.Forum.Features.Shared;
using RAG.Forum.Services;
using RAG.Security.Services;

namespace RAG.Forum.Features.Threads;

public static class CreatePostEndpoint
{
    private const int MaxContentLength = 4000;

    public static RouteGroupBuilder MapCreatePost(this RouteGroupBuilder group)
    {
        group.MapPost("/threads/{threadId:guid}/posts", HandleAsync)
            .WithName("Forum_CreatePost")
            .RequireAuthorization();

        return group;
    }

    public static async Task<IResult> HandleAsync(
        Guid threadId,
        [FromBody] CreatePostRequest request,
        ForumDbContext dbContext,
        IUserContextService userContext,
        IEmailService emailService,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        IForumSettingsProvider settingsProvider,
        CancellationToken cancellationToken)
    {
        var userId = userContext.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var userEmail = userContext.GetCurrentUserEmail() ?? string.Empty;
        var userName = userContext.GetCurrentUserName() ?? userId;

        var validationErrors = ValidateRequest(request);

        var thread = await dbContext.Threads
            .Include(t => t.Subscriptions)
            .Include(t => t.Badges)
            .FirstOrDefaultAsync(t => t.Id == threadId, cancellationToken);

        if (thread is null)
        {
            return Results.NotFound();
        }

        if (thread.IsLocked)
        {
            return Results.Conflict(new { message = "Thread is locked and cannot receive new replies." });
        }

        var utcNow = DateTime.UtcNow;
        var postId = Guid.NewGuid();

        var forumSettings = await settingsProvider.GetSettingsAsync(cancellationToken);
        List<ForumAttachment> attachments = new();

        if (forumSettings.EnableAttachments)
        {
            if (!AttachmentMapper.TryCreateAttachments(
                    request.Attachments,
                    thread.Id,
                    postId: postId,
                    createdAt: utcNow,
                    maxAttachmentCount: forumSettings.MaxAttachmentCount,
                    maxAttachmentSizeBytes: forumSettings.MaxAttachmentSizeMb * 1024 * 1024,
                    out attachments,
                    out var attachmentErrors))
            {
                MergeErrors(validationErrors, attachmentErrors);
            }
        }
        else if (request.Attachments is { })
        {
            validationErrors["attachments"] = new[] { "Attachments are currently disabled." };
        }

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var trimmedContent = request.Content.Trim();

        var post = new ForumPost
        {
            Id = postId,
            ThreadId = thread.Id,
            AuthorId = userId,
            AuthorEmail = userEmail,
            Content = trimmedContent,
            IsAnswer = false,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        if (forumSettings.EnableAttachments)
        {
            foreach (var attachment in attachments)
            {
                post.Attachments.Add(attachment);
            }
        }

        dbContext.Posts.Add(post);

        thread.UpdatedAt = utcNow;
        thread.LastPostAt = utcNow;

        UpdateSubscription(thread, request, userId, userEmail, utcNow);
        UpdateBadges(thread, post, userId, utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        var subscriberEmails = thread.Subscriptions
            .Where(s => s.NotifyOnReply && s.UserId != userId && !string.IsNullOrWhiteSpace(s.Email))
            .Select(s => s.Email)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (forumSettings.EnableEmailNotifications && subscriberEmails.Count > 0)
        {
            var threadLink = BuildThreadLink(configuration, thread.Id);
            var preview = BuildPreview(trimmedContent);
            var logger = loggerFactory.CreateLogger("ForumNotifications");

            foreach (var email in subscriberEmails)
            {
                try
                {
                    await emailService.SendForumReplyNotificationAsync(email, thread.Title, userName, threadLink, preview);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send forum reply notification to {Email} for thread {ThreadId}", email, thread.Id);
                }
            }
        }

        var response = new CreatePostResponse(ThreadDtoMapper.ToPostDto(post));
        return Results.Created($"/api/forum/threads/{thread.Id}/posts/{post.Id}", response);
    }

    private static Dictionary<string, string[]> ValidateRequest(CreatePostRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            errors["content"] = new[] { "Content is required." };
        }
        else if (request.Content.Trim().Length > MaxContentLength)
        {
            errors["content"] = new[] { $"Content cannot exceed {MaxContentLength} characters." };
        }

        return errors;
    }

    private static void MergeErrors(Dictionary<string, string[]> target, Dictionary<string, string[]> source)
    {
        foreach (var (key, value) in source)
        {
            if (target.TryGetValue(key, out var existing))
            {
                target[key] = existing.Concat(value).ToArray();
            }
            else
            {
                target[key] = value;
            }
        }
    }

    private static void UpdateSubscription(ForumThread thread, CreatePostRequest request, string userId, string userEmail, DateTime utcNow)
    {
        var subscription = thread.Subscriptions.FirstOrDefault(s => s.UserId == userId);

        if (subscription is null && request.SubscribeToThread)
        {
            thread.Subscriptions.Add(new ThreadSubscription
            {
                Id = Guid.NewGuid(),
                ThreadId = thread.Id,
                UserId = userId,
                Email = userEmail,
                NotifyOnReply = true,
                SubscribedAt = utcNow
            });
        }
        else if (subscription is not null)
        {
            subscription.Email = userEmail;

            if (request.SubscribeToThread)
            {
                subscription.NotifyOnReply = true;
            }
            else
            {
                subscription.NotifyOnReply = false;
            }
        }
    }

    private static void UpdateBadges(ForumThread thread, ForumPost post, string authorId, DateTime utcNow)
    {
        var authorBadge = thread.Badges.FirstOrDefault(b => b.UserId == authorId);
        if (authorBadge is null)
        {
            authorBadge = new ThreadBadge
            {
                Id = Guid.NewGuid(),
                ThreadId = thread.Id,
                UserId = authorId
            };
            thread.Badges.Add(authorBadge);
        }

        authorBadge.LastSeenPostId = post.Id;
        authorBadge.HasUnreadReplies = false;
        authorBadge.UpdatedAt = utcNow;

        foreach (var subscription in thread.Subscriptions.Where(s => s.UserId != authorId))
        {
            var badge = thread.Badges.FirstOrDefault(b => b.UserId == subscription.UserId);
            if (badge is null)
            {
                badge = new ThreadBadge
                {
                    Id = Guid.NewGuid(),
                    ThreadId = thread.Id,
                    UserId = subscription.UserId,
                    HasUnreadReplies = true,
                    UpdatedAt = utcNow
                };
                thread.Badges.Add(badge);
            }
            else
            {
                badge.HasUnreadReplies = true;
                badge.UpdatedAt = utcNow;
            }
        }
    }

    private static string BuildThreadLink(IConfiguration configuration, Guid threadId)
    {
        var baseUrl = configuration["Frontend:BaseUrl"] ?? "https://localhost:5173";
        baseUrl = baseUrl.TrimEnd('/');
        return $"{baseUrl}/forum/{threadId}";
    }

    private static string BuildPreview(string content)
    {
        var normalized = content.ReplaceLineEndings(" ");
        return normalized.Length <= 200 ? normalized : $"{normalized[..200]}…";
    }
}

