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
                .Select(entry => ToDraft(entry.File))
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
                .Where(entry => requestedIds.Contains(entry.File.Id, StringComparer.OrdinalIgnoreCase))
                .ToArray();
        }

        var files = new List<ChatAttachmentFile>();
        foreach (var entry in entries)
        {
            var content = entry.TempPath == null
                ? entry.File.Content
                : await File.ReadAllTextAsync(entry.TempPath, Encoding.UTF8, cancellationToken);
            files.Add(entry.File with { Content = content });
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
                string? tempPath = null;
                if (!string.IsNullOrEmpty(file.Content))
                {
                    tempPath = Path.Combine(sessionPath, $"{file.Id}.txt");
                    await File.WriteAllTextAsync(tempPath, file.Content, Encoding.UTF8, cancellationToken);
                }

                storedFiles.Add(new StoredAttachment(file with { UploadedAt = file.UploadedAt ?? DateTimeOffset.UtcNow }, tempPath));
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
                if (storedFile.TempPath != null)
                {
                    DeleteFileQuietly(storedFile.TempPath);
                }
            }

            throw;
        }
    }

    public async Task UpdateAsync(string userId, string sessionId, ChatAttachmentFile file, CancellationToken cancellationToken = default)
    {
        StoredAttachment? entry;
        lock (_lock)
        {
            entry = GetEntries(CacheKey(userId, sessionId), createIfMissing: false)
                .FirstOrDefault(candidate => string.Equals(candidate.File.Id, file.Id, StringComparison.OrdinalIgnoreCase));
        }

        if (entry == null)
        {
            return;
        }

        var tempPath = entry.TempPath;
        if (!string.IsNullOrEmpty(file.Content))
        {
            var sessionPath = GetSessionPath(userId, sessionId);
            Directory.CreateDirectory(sessionPath);
            tempPath ??= Path.Combine(sessionPath, $"{file.Id}.txt");
            await File.WriteAllTextAsync(tempPath, file.Content, Encoding.UTF8, cancellationToken);
        }

        lock (_lock)
        {
            entry.File = file with { UploadedAt = file.UploadedAt ?? entry.File.UploadedAt };
            entry.TempPath = tempPath;
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
            removed = entries.FirstOrDefault(entry => string.Equals(entry.File.Id, attachmentId, StringComparison.OrdinalIgnoreCase));
            if (removed != null)
            {
                entries.Remove(removed);
            }
        }

        if (removed == null)
        {
            return Task.FromResult(false);
        }

        if (removed.TempPath != null)
        {
            DeleteFileQuietly(removed.TempPath);
        }
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
                if (entry.TempPath != null)
                {
                    DeleteFileQuietly(entry.TempPath);
                }
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

    private static ChatAttachmentDraft ToDraft(ChatAttachmentFile file)
    {
        return new ChatAttachmentDraft(
            file.Id,
            file.FileName,
            file.ContentType,
            file.SizeBytes,
            file.TokenCount,
            file.UploadedAt ?? DateTimeOffset.UtcNow,
            file.Status,
            file.Progress,
            file.PageCount,
            file.Provider,
            file.ErrorCode,
            file.DocumentJobId);
    }

    private sealed class StoredAttachment
    {
        public StoredAttachment(ChatAttachmentFile file, string? tempPath)
        {
            File = file;
            TempPath = tempPath;
        }

        public ChatAttachmentFile File { get; set; }

        public string? TempPath { get; set; }
    }
}
