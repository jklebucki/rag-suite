using System.ComponentModel.DataAnnotations;

namespace RAG.CyberPanel.Domain;

public class QuizAnswer
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuizAttemptId { get; set; }
    public QuizAttempt? QuizAttempt { get; set; }
    public Guid QuestionId { get; set; }

    public ICollection<QuizAnswerOption> SelectedOptions { get; set; } = new List<QuizAnswerOption>();
}
