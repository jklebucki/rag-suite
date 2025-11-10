using System.ComponentModel.DataAnnotations;

namespace RAG.Orchestrator.Api.Features.Settings;

public record ForumSettingsResponse(
    bool EnableAttachments,
    int MaxAttachmentCount,
    int MaxAttachmentSizeMb,
    bool EnableEmailNotifications,
    int BadgeRefreshSeconds);

public class ForumSettingsRequest
{
    [Required]
    public bool EnableAttachments { get; set; }

    [Range(1, 50)]
    public int MaxAttachmentCount { get; set; } = 5;

    [Range(1, 100)]
    public int MaxAttachmentSizeMb { get; set; } = 5;

    [Required]
    public bool EnableEmailNotifications { get; set; }

    [Range(15, 300)]
    public int BadgeRefreshSeconds { get; set; } = 60;
}

