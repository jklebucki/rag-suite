using System.ComponentModel.DataAnnotations;

namespace RAG.CyberPanel.Domain;

/// <summary>
/// Represents a question in a cybersecurity quiz. Supports optional image attachment.
/// </summary>
public class Question
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuizId { get; set; }
    public Quiz? Quiz { get; set; }
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Optional URL to an image associated with this question (e.g., diagram, screenshot).
    /// </summary>
    public string? ImageUrl { get; set; }

    public int Order { get; set; }
    public int Points { get; set; } = 1;

    public ICollection<Option> Options { get; set; } = new List<Option>();
}
