namespace RAG.Forum.Domain;

public class ForumAttachment
{
    public Guid Id { get; set; }

    public Guid ThreadId { get; set; }

    public ForumThread Thread { get; set; } = null!;

    public Guid? PostId { get; set; }

    public ForumPost? Post { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long Size { get; set; }

    public byte[] Data { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; }
}

