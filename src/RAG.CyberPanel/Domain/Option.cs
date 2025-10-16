using System.ComponentModel.DataAnnotations;

namespace RAG.CyberPanel.Domain;

/// <summary>
/// Represents an answer option for a question. Supports optional image attachment.
/// </summary>
public class Option
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional URL to an image associated with this answer option.
    /// </summary>
    public string? ImageUrl { get; set; }
    
    public bool IsCorrect { get; set; }
}
