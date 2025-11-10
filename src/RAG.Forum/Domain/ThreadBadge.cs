namespace RAG.Forum.Domain;

public class ThreadBadge
{
    public Guid Id { get; set; }

    public Guid ThreadId { get; set; }

    public ForumThread Thread { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public Guid? LastSeenPostId { get; set; }

    public ForumPost? LastSeenPost { get; set; }

    public bool HasUnreadReplies { get; set; }

    public DateTime UpdatedAt { get; set; }
}

