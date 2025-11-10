namespace RAG.Forum.Domain;

public class ForumThread
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public ForumCategory Category { get; set; } = null!;

    public string AuthorId { get; set; } = string.Empty;

    public string AuthorEmail { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime LastPostAt { get; set; }

    public bool IsLocked { get; set; }

    public int ViewCount { get; set; }

    public ICollection<ForumPost> Posts { get; set; } = new HashSet<ForumPost>();

    public ICollection<ForumAttachment> Attachments { get; set; } = new HashSet<ForumAttachment>();

    public ICollection<ThreadSubscription> Subscriptions { get; set; } = new HashSet<ThreadSubscription>();

    public ICollection<ThreadBadge> Badges { get; set; } = new HashSet<ThreadBadge>();
}

