using RAG.Forum.Services;

namespace RAG.Orchestrator.Api.Services;

public class ForumSettingsProvider : IForumSettingsProvider
{
    private readonly IGlobalSettingsCache _cache;

    public ForumSettingsProvider(IGlobalSettingsCache cache)
    {
        _cache = cache;
    }

    public async Task<ForumRuntimeSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _cache.GetForumSettingsAsync();

        return new ForumRuntimeSettings
        {
            EnableAttachments = settings?.EnableAttachments ?? true,
            MaxAttachmentCount = settings?.MaxAttachmentCount ?? 5,
            MaxAttachmentSizeMb = settings?.MaxAttachmentSizeMb ?? 5,
            EnableEmailNotifications = settings?.EnableEmailNotifications ?? true
        };
    }
}

