using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using RAG.Forum.Data;

namespace RAG.Forum.Features.Threads;

public static class ListThreadsEndpoint
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;

    public static RouteGroupBuilder MapListThreads(this RouteGroupBuilder group)
    {
        group.MapGet("/threads", HandleAsync)
            .WithName("Forum_ListThreads");

        return group;
    }

    public static async Task<IResult> HandleAsync(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        ForumDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var effectivePage = page == 0 ? 1 : page;
        var effectivePageSize = pageSize == 0 ? DefaultPageSize : pageSize;

        var request = new ListThreadsRequest(effectivePage, effectivePageSize, categoryId, search);

        var errors = new Dictionary<string, string[]>();

        if (request.Page < 0)
        {
            errors["page"] = new[] { "Page must be greater than or equal to 1." };
        }

        if (request.PageSize < 0)
        {
            errors["pageSize"] = new[] { "Page size must be greater than 0." };
        }
        else if (request.PageSize == 0)
        {
            errors["pageSize"] = new[] { "Page size must be greater than 0." };
        }
        else if (request.PageSize > MaxPageSize)
        {
            errors["pageSize"] = new[] { $"Page size cannot exceed {MaxPageSize}." };
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var pageValue = request.Page <= 0 ? 1 : request.Page;
        var pageSizeValue = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);

        var query = dbContext.Threads
            .AsNoTracking()
            .AsQueryable();

        if (request.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search.Trim()}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.Title, term) ||
                EF.Functions.ILike(t.Content, term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var threads = await query
            .OrderByDescending(t => t.LastPostAt)
            .Skip((pageValue - 1) * pageSizeValue)
            .Take(pageSizeValue)
            .Select(t => new ForumThreadSummaryDto(
                t.Id,
                t.CategoryId,
                t.Category.Name,
                t.Title,
                t.AuthorId,
                t.AuthorEmail,
                t.CreatedAt,
                t.UpdatedAt,
                t.LastPostAt,
                t.IsLocked,
                t.ViewCount,
                t.Posts.Count,
                t.Attachments.Count))
            .ToListAsync(cancellationToken);

        var response = new ListThreadsResponse(
            threads,
            pageValue,
            pageSizeValue,
            totalCount);

        return Results.Ok(response);
    }
}

