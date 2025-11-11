using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using RAG.Forum.Data;
using RAG.Forum.Domain;
using System.ComponentModel.DataAnnotations;

namespace RAG.Forum.Features.Categories;

public static class ManageCategoriesEndpoint
{
    public static RouteGroupBuilder MapManageCategories(this RouteGroupBuilder group)
    {
        var adminGroup = group.MapGroup("/categories")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        adminGroup.MapPost("/", async (CreateCategoryRequest request, ForumDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var validationResult = await ValidateCreateRequestAsync(request, dbContext, cancellationToken);
            if (validationResult is IResult errorResult)
            {
                return errorResult;
            }

            var maxOrder = await dbContext.Categories.MaxAsync(c => (int?)c.Order, cancellationToken);
            var order = request.Order ?? (maxOrder ?? 0) + 1;

            var category = new ForumCategory
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Slug = request.Slug.Trim().ToLowerInvariant(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Order = order,
                IsArchived = request.IsArchived,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/forum/categories/{category.Id}", MapToDto(category));
        });

        adminGroup.MapPut("/{categoryId:guid}", async (Guid categoryId, UpdateCategoryRequest request, ForumDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
            if (category == null)
            {
                return Results.NotFound();
            }

            var validationResult = await ValidateUpdateRequestAsync(categoryId, request, dbContext, cancellationToken);
            if (validationResult is IResult errorResult)
            {
                return errorResult;
            }

            category.Name = request.Name.Trim();
            category.Slug = request.Slug.Trim().ToLowerInvariant();
            category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            category.Order = request.Order ?? category.Order;
            category.IsArchived = request.IsArchived;
            category.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(MapToDto(category));
        });

        adminGroup.MapDelete("/{categoryId:guid}", async (Guid categoryId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var category = await dbContext.Categories.Include(c => c.Threads).FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
            if (category == null)
            {
                return Results.NotFound();
            }

            if (category.Threads.Any())
            {
                return Results.BadRequest(new { Message = "Cannot delete category with existing threads." });
            }

            dbContext.Categories.Remove(category);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });

        return group;
    }

    private static async Task<IResult?> ValidateCreateRequestAsync(CreateCategoryRequest request, ForumDbContext dbContext, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCommon(request.Name, request.Slug, request.Description);

        if (await dbContext.Categories.AnyAsync(c => c.Slug == request.Slug.Trim().ToLowerInvariant(), cancellationToken))
        {
            validationErrors.Add("slug", new[] { "Slug must be unique." });
        }

        if (request.Order.HasValue && request.Order <= 0)
        {
            validationErrors.Add("order", new[] { "Order must be greater than 0." });
        }

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        return null;
    }

    private static async Task<IResult?> ValidateUpdateRequestAsync(Guid categoryId, UpdateCategoryRequest request, ForumDbContext dbContext, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCommon(request.Name, request.Slug, request.Description);

        if (request.Order.HasValue && request.Order <= 0)
        {
            validationErrors.Add("order", new[] { "Order must be greater than 0." });
        }

        var slug = request.Slug.Trim().ToLowerInvariant();
        var slugConflict = await dbContext.Categories.AnyAsync(c => c.Slug == slug && c.Id != categoryId, cancellationToken);
        if (slugConflict)
        {
            validationErrors.Add("slug", new[] { "Slug must be unique." });
        }

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        return null;
    }

    private static Dictionary<string, string[]> ValidateCommon(string name, string slug, string? description)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = new[] { "Name is required." };
        }
        else if (name.Length > 100)
        {
            errors["name"] = new[] { "Name cannot exceed 100 characters." };
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            errors["slug"] = new[] { "Slug is required." };
        }
        else if (!SlugRegex.IsMatch(slug.Trim()))
        {
            errors["slug"] = new[] { "Slug must contain only lowercase letters, numbers, and hyphens." };
        }

        if (!string.IsNullOrWhiteSpace(description) && description.Length > 500)
        {
            errors["description"] = new[] { "Description cannot exceed 500 characters." };
        }

        return errors;
    }

    private static ForumCategoryDto MapToDto(ForumCategory category) =>
        new(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.IsArchived,
            category.Order,
            category.CreatedAt,
            category.UpdatedAt);

    private static readonly System.Text.RegularExpressions.Regex SlugRegex =
        new("^[a-z0-9]+(?:-[a-z0-9]+)*$", System.Text.RegularExpressions.RegexOptions.Compiled);
}

public class CreateCategoryRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? Order { get; set; }

    public bool IsArchived { get; set; }
}

public class UpdateCategoryRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? Order { get; set; }

    public bool IsArchived { get; set; }
}

