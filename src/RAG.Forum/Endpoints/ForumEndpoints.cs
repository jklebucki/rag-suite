using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RAG.Forum.Features.Categories;
using RAG.Forum.Features.Threads;

namespace RAG.Forum.Endpoints;

public static class ForumEndpoints
{
    public static RouteGroupBuilder MapForumEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/forum")
            .WithTags("Forum");

        group.MapListCategories();
        group.MapListThreads();
        group.MapGetThread();
        group.MapCreateThread();
        group.MapCreatePost();
        group.MapThreadSubscriptions();
        group.MapThreadBadges();
        group.MapDownloadThreadAttachment();

        return group;
    }
}

