namespace RAG.Forum.Domain;

public class ForumPost
{
    public Guid Id { get; set; }

    public Guid ThreadId { get; set; }

    public ForumThread Thread { get; set; } = null!;

    public string AuthorId { get; set; } = string.Empty;

    public string AuthorEmail { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsAnswer { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<ForumAttachment> Attachments { get; set; } = new HashSet<ForumAttachment>();
}

