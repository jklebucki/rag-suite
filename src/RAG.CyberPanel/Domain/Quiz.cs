using System.ComponentModel.DataAnnotations;

namespace RAG.CyberPanel.Domain;

public class Quiz
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; }
    public string? Language { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
