using System.ComponentModel.DataAnnotations;

namespace RAG.CyberPanel.Domain;

public class Question
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuizId { get; set; }
    public Quiz? Quiz { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
    public int Points { get; set; } = 1;

    public ICollection<Option> Options { get; set; } = new List<Option>();
}
