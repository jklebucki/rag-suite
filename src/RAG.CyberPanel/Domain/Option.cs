using System.ComponentModel.DataAnnotations;

namespace RAG.CyberPanel.Domain;

public class Option
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
