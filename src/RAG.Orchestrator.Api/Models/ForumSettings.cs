namespace RAG.Orchestrator.Api.Models;

public class ForumSettings
{
    public bool EnableAttachments { get; set; } = true;
    public int MaxAttachmentCount { get; set; } = 5;
    public int MaxAttachmentSizeMb { get; set; } = 5;
    public bool EnableEmailNotifications { get; set; } = true;
    public int BadgeRefreshSeconds { get; set; } = 60;
}

