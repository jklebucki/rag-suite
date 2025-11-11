using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RAG.Forum.Data;

namespace RAG.Forum.Features.Categories;

public static class ListCategoriesEndpoint
{
    public static RouteGroupBuilder MapListCategories(this RouteGroupBuilder group)
    {
        group.MapGet("/categories", HandleAsync)
            .WithName("Forum_ListCategories");

        return group;
    }

    public static async Task<IResult> HandleAsync(
        ForumDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.Order)
            .ThenBy(c => c.Name)
            .Select(c => new ForumCategoryDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.IsArchived,
                c.Order,
                c.CreatedAt,
                c.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(new ListCategoriesResponse(categories));
    }
}

