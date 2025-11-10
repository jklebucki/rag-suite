namespace RAG.Forum.Services;

public interface IForumSettingsProvider
{
    Task<ForumRuntimeSettings> GetSettingsAsync(CancellationToken cancellationToken = default);
}

public class ForumRuntimeSettings
{
    public bool EnableAttachments { get; init; } = true;
    public int MaxAttachmentCount { get; init; } = 5;
    public int MaxAttachmentSizeMb { get; init; } = 5;
    public bool EnableEmailNotifications { get; init; } = true;
}

