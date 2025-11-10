using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using RAG.Forum.Data;
using RAG.Forum.Domain;
using RAG.Forum.Features.Shared;
using RAG.Forum.Services;
using RAG.Security.Services;

namespace RAG.Forum.Features.Threads;

public static class CreateThreadEndpoint
{
    private const int MaxTitleLength = 200;
    private const int MaxContentLength = 4000;

    public static RouteGroupBuilder MapCreateThread(this RouteGroupBuilder group)
    {
        group.MapPost("/threads", HandleAsync)
            .WithName("Forum_CreateThread")
            .RequireAuthorization();

        return group;
    }

    public static async Task<IResult> HandleAsync(
        [FromBody] CreateThreadRequest request,
        ForumDbContext dbContext,
        IUserContextService userContext,
        IForumSettingsProvider settingsProvider,
        CancellationToken cancellationToken)
    {
        var userId = userContext.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var userEmail = userContext.GetCurrentUserEmail() ?? string.Empty;
        var validationErrors = ValidateRequest(request);

        var category = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
        {
            validationErrors["categoryId"] = new[] { "Selected category does not exist." };
        }
        else if (category.IsArchived)
        {
            validationErrors["categoryId"] = new[] { "Selected category is archived and cannot accept new threads." };
        }

        var utcNow = DateTime.UtcNow;
        var threadId = Guid.NewGuid();

        var forumSettings = await settingsProvider.GetSettingsAsync(cancellationToken);
        List<ForumAttachment> attachments = new();

        if (forumSettings.EnableAttachments)
        {
            if (!AttachmentMapper.TryCreateAttachments(
                    request.Attachments,
                    threadId,
                    postId: null,
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

        var trimmedTitle = request.Title.Trim();
        var trimmedContent = request.Content.Trim();

        var thread = new ForumThread
        {
            Id = threadId,
            CategoryId = request.CategoryId,
            AuthorId = userId,
            AuthorEmail = userEmail,
            Title = trimmedTitle,
            Content = trimmedContent,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            LastPostAt = utcNow,
            IsLocked = false,
            ViewCount = 0
        };

        if (forumSettings.EnableAttachments)
        {
            foreach (var attachment in attachments)
            {
                thread.Attachments.Add(attachment);
            }
        }

        thread.Subscriptions.Add(new ThreadSubscription
        {
            Id = Guid.NewGuid(),
            ThreadId = thread.Id,
            UserId = userId,
            Email = userEmail,
            NotifyOnReply = forumSettings.EnableEmailNotifications,
            SubscribedAt = utcNow
        });

        thread.Badges.Add(new ThreadBadge
        {
            Id = Guid.NewGuid(),
            ThreadId = thread.Id,
            UserId = userId,
            HasUnreadReplies = false,
            UpdatedAt = utcNow
        });

        dbContext.Threads.Add(thread);
        await dbContext.SaveChangesAsync(cancellationToken);

        var createdThread = await dbContext.Threads
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Attachments)
            .Include(t => t.Posts)
                .ThenInclude(p => p.Attachments)
            .FirstAsync(t => t.Id == threadId, cancellationToken);

        var response = new CreateThreadResponse(ThreadDtoMapper.ToDetailDto(createdThread));

        return Results.Created($"/api/forum/threads/{threadId}", response);
    }

    private static Dictionary<string, string[]> ValidateRequest(CreateThreadRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.CategoryId == Guid.Empty)
        {
            errors["categoryId"] = new[] { "Category is required." };
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = new[] { "Title is required." };
        }
        else if (request.Title.Trim().Length > MaxTitleLength)
        {
            errors["title"] = new[] { $"Title cannot exceed {MaxTitleLength} characters." };
        }

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
}

