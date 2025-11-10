namespace RAG.Forum.Domain;

public class ThreadSubscription
{
    public Guid Id { get; set; }

    public Guid ThreadId { get; set; }

    public ForumThread Thread { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool NotifyOnReply { get; set; } = true;

    public DateTime SubscribedAt { get; set; }

    public DateTime? LastNotifiedAt { get; set; }
}

