using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace RAG.Orchestrator.Api.Features.Chat.Attachments;

public class MemoryChatAttachmentStore : IChatAttachmentStore
{
    private static readonly TimeSpan DraftTtl = TimeSpan.FromMinutes(60);

    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryChatAttachmentStore> _logger;
    private readonly string _rootPath;
    private readonly object _lock = new();

    public MemoryChatAttachmentStore(IMemoryCache cache, IWebHostEnvironment environment, ILogger<MemoryChatAttachmentStore> logger)
    {
        _cache = cache;
        _logger = logger;
        _rootPath = Path.Combine(environment.ContentRootPath, "App_Data", "chat-attachments");
        Directory.CreateDirectory(_rootPath);
    }

    public Task<ChatAttachmentDraft[]> GetDraftsAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var entries = GetEntries(CacheKey(userId, sessionId), createIfMissing: false);
            var drafts = entries
                .Select(entry => entry.Draft)
                .OrderBy(draft => draft.UploadedAt)
                .ToArray();
            return Task.FromResult(drafts);
        }
    }

    public async Task<ChatAttachmentFile[]> GetFilesAsync(string userId, string sessionId, IEnumerable<string> attachmentIds, CancellationToken cancellationToken = default)
    {
        var requestedIds = attachmentIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requestedIds.Length == 0)
        {
            return Array.Empty<ChatAttachmentFile>();
        }

        StoredAttachment[] entries;
        lock (_lock)
        {
            entries = GetEntries(CacheKey(userId, sessionId), createIfMissing: false)
                .Where(entry => requestedIds.Contains(entry.Draft.Id, StringComparer.OrdinalIgnoreCase))
                .ToArray();
        }

        var files = new List<ChatAttachmentFile>();
        foreach (var entry in entries)
        {
            var content = await File.ReadAllTextAsync(entry.TempPath, Encoding.UTF8, cancellationToken);
            files.Add(new ChatAttachmentFile(
                entry.Draft.Id,
                entry.Draft.FileName,
                entry.Draft.ContentType,
                entry.Draft.SizeBytes,
                entry.Draft.TokenCount,
                content));
        }

        return files.ToArray();
    }

    public async Task SaveBatchAsync(string userId, string sessionId, IEnumerable<ChatAttachmentFile> files, CancellationToken cancellationToken = default)
    {
        var filesToSave = files.ToArray();
        if (filesToSave.Length == 0)
        {
            return;
        }

        var sessionPath = GetSessionPath(userId, sessionId);
        Directory.CreateDirectory(sessionPath);

        var storedFiles = new List<StoredAttachment>();
        try
        {
            foreach (var file in filesToSave)
            {
                var tempPath = Path.Combine(sessionPath, $"{file.Id}.txt");
                await File.WriteAllTextAsync(tempPath, file.Content, Encoding.UTF8, cancellationToken);
                storedFiles.Add(new StoredAttachment(
                    new ChatAttachmentDraft(
                        file.Id,
                        file.FileName,
                        file.ContentType,
                        file.SizeBytes,
                        file.TokenCount,
                        DateTimeOffset.UtcNow),
                    tempPath));
            }

            lock (_lock)
            {
                var entries = GetEntries(CacheKey(userId, sessionId), createIfMissing: true);
                entries.AddRange(storedFiles);
            }
        }
        catch
        {
            foreach (var storedFile in storedFiles)
            {
                DeleteFileQuietly(storedFile.TempPath);
            }

            throw;
        }
    }

    public Task<bool> RemoveAsync(string userId, string sessionId, string attachmentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(attachmentId))
        {
            return Task.FromResult(false);
        }

        StoredAttachment? removed = null;
        lock (_lock)
        {
            var entries = GetEntries(CacheKey(userId, sessionId), createIfMissing: false);
            removed = entries.FirstOrDefault(entry => string.Equals(entry.Draft.Id, attachmentId, StringComparison.OrdinalIgnoreCase));
            if (removed != null)
            {
                entries.Remove(removed);
            }
        }

        if (removed == null)
        {
            return Task.FromResult(false);
        }

        DeleteFileQuietly(removed.TempPath);
        return Task.FromResult(true);
    }

    public async Task RemoveBatchAsync(string userId, string sessionId, IEnumerable<string> attachmentIds, CancellationToken cancellationToken = default)
    {
        foreach (var attachmentId in attachmentIds.ToArray())
        {
            await RemoveAsync(userId, sessionId, attachmentId, cancellationToken);
        }
    }

    public Task ClearSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        _cache.Remove(CacheKey(userId, sessionId));
        return Task.CompletedTask;
    }

    private List<StoredAttachment> GetEntries(string key, bool createIfMissing)
    {
        if (_cache.TryGetValue<List<StoredAttachment>>(key, out var entries))
        {
            return entries ?? new List<StoredAttachment>();
        }

        if (!createIfMissing)
        {
            return new List<StoredAttachment>();
        }

        entries = new List<StoredAttachment>();
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = DraftTtl
        };
        options.RegisterPostEvictionCallback((_, value, _, _) =>
        {
            if (value is not List<StoredAttachment> evictedEntries)
            {
                return;
            }

            foreach (var entry in evictedEntries)
            {
                DeleteFileQuietly(entry.TempPath);
            }
        });

        _cache.Set(key, entries, options);
        return entries;
    }

    private string GetSessionPath(string userId, string sessionId)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}:{sessionId}")))[..32];
        return Path.Combine(_rootPath, hash);
    }

    private static string CacheKey(string userId, string sessionId)
    {
        return $"chat-attachments:{userId}:{sessionId}";
    }

    private static void DeleteFileQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private record StoredAttachment(ChatAttachmentDraft Draft, string TempPath);
}
