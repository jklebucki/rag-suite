using RAG.DocumentProcessing.Abstractions;
using System.Security.Claims;

namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public static class ArtifactEndpointMappings
{
    public static IEndpointRouteBuilder MapArtifactEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/user-chat/artifacts/{artifactId}/download", async (
            string artifactId,
            ClaimsPrincipal user,
            ITemporaryArtifactStore store,
            IArtifactDownloadService downloadService,
            CancellationToken cancellationToken) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var artifact = await store.GetAsync(artifactId, userId, cancellationToken);
            if (artifact == null)
            {
                return Results.NotFound();
            }

            if (artifact.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return Results.StatusCode(StatusCodes.Status410Gone);
            }

            var content = await downloadService.GetContentAsync(artifactId, userId, cancellationToken);
            return content == null
                ? Results.NotFound()
                : Results.File(content, artifact.ContentType, artifact.FileName);
        }).RequireAuthorization().WithTags("Generated artifacts");

        return endpoints;
    }
}
