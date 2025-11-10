namespace RAG.Forum.Features.Categories;

public sealed record ForumCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    bool IsArchived,
    int Order,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ListCategoriesResponse(IReadOnlyCollection<ForumCategoryDto> Categories);

