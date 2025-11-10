namespace RAG.Forum.Domain;

public class ForumCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsArchived { get; set; }

    public int Order { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<ForumThread> Threads { get; set; } = new HashSet<ForumThread>();
}

