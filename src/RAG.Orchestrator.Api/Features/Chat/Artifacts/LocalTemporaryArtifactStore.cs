using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RAG.DocumentProcessing.Abstractions;
using RAG.Orchestrator.Api.Data;

namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public sealed class LocalTemporaryArtifactStore : ITemporaryArtifactStore, IArtifactDownloadService, IArtifactSessionCleanupService
{
    private readonly ChatDbContext _chatDbContext;
    private readonly GeneratedArtifactOptions _options;
    private readonly string _rootPath;
    private readonly ILogger<LocalTemporaryArtifactStore> _logger;

    public LocalTemporaryArtifactStore(
        ChatDbContext chatDbContext,
        IWebHostEnvironment environment,
        IOptions<GeneratedArtifactOptions> options,
        ILogger<LocalTemporaryArtifactStore> logger)
    {
        _chatDbContext = chatDbContext;
        _options = options.Value;
        _logger = logger;
        _rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, _options.RootDirectory));
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredArtifact> SaveAsync(GeneratedArtifact artifact, CancellationToken cancellationToken)
    {
        var fileName = ArtifactFileNameSanitizer.Sanitize(artifact.FileName, artifact.Format);
        var id = Guid.NewGuid().ToString("N");
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddHours(_options.ExpirationHours);
        var storageKey = Path.Combine(createdAt.ToString("yyyy-MM-dd"), id, fileName);
        var fullPath = GetFullPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await File.WriteAllBytesAsync(fullPath, artifact.Content, cancellationToken);
        try
        {
            _chatDbContext.GeneratedArtifacts.Add(new GeneratedArtifactEntity
            {
                Id = id,
                UserId = artifact.UserId,
                SessionId = artifact.SessionId,
                AssistantMessageId = artifact.AssistantMessageId,
                FileName = fileName,
                ContentType = artifact.ContentType,
                StorageKey = storageKey,
                SizeBytes = artifact.Content.LongLength,
                CreatedAt = createdAt.UtcDateTime,
                ExpiresAt = expiresAt.UtcDateTime
            });
            await _chatDbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            DeleteFileQuietly(fullPath);
            throw;
        }

        return new StoredArtifact(id, fileName, artifact.ContentType, artifact.Content.LongLength, createdAt, expiresAt);
    }

    public async Task<StoredArtifact?> GetAsync(string artifactId, string userId, CancellationToken cancellationToken)
    {
        var entity = await _chatDbContext.GeneratedArtifacts
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == artifactId && item.UserId == userId, cancellationToken);
        return entity == null ? null : ToStoredArtifact(entity);
    }

    public async Task<byte[]?> GetContentAsync(string artifactId, string userId, CancellationToken cancellationToken)
    {
        var entity = await _chatDbContext.GeneratedArtifacts
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == artifactId && item.UserId == userId, cancellationToken);
        if (entity == null)
        {
            return null;
        }

        var path = GetFullPath(entity.StorageKey);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, cancellationToken) : null;
    }

    public async Task DeleteExpiredAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expired = await _chatDbContext.GeneratedArtifacts
            .Where(item => item.ExpiresAt <= now)
            .ToListAsync(cancellationToken);
        await DeleteEntitiesAsync(expired, cancellationToken);
    }

    public async Task DeleteForSessionAsync(string userId, string sessionId, CancellationToken cancellationToken)
    {
        var artifacts = await _chatDbContext.GeneratedArtifacts
            .Where(item => item.UserId == userId && item.SessionId == sessionId)
            .ToListAsync(cancellationToken);
        await DeleteEntitiesAsync(artifacts, cancellationToken);
    }

    private async Task DeleteEntitiesAsync(IReadOnlyCollection<GeneratedArtifactEntity> artifacts, CancellationToken cancellationToken)
    {
        if (artifacts.Count == 0)
        {
            return;
        }

        foreach (var artifact in artifacts)
        {
            DeleteFileQuietly(GetFullPath(artifact.StorageKey));
        }

        _chatDbContext.GeneratedArtifacts.RemoveRange(artifacts);
        await _chatDbContext.SaveChangesAsync(cancellationToken);
    }

    private StoredArtifact ToStoredArtifact(GeneratedArtifactEntity entity)
    {
        return new StoredArtifact(
            entity.Id,
            entity.FileName,
            entity.ContentType,
            entity.SizeBytes,
            new DateTimeOffset(DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(entity.ExpiresAt, DateTimeKind.Utc)));
    }

    private string GetFullPath(string storageKey)
    {
        var path = Path.GetFullPath(Path.Combine(_rootPath, storageKey));
        var rootPrefix = _rootPath.EndsWith(Path.DirectorySeparatorChar) ? _rootPath : _rootPath + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Artifact storage key is outside the configured root.");
        }

        return path;
    }

    private void DeleteFileQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete generated artifact file {Path}", path);
        }
    }
}
