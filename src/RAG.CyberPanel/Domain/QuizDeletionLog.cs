using System.ComponentModel.DataAnnotations;

namespace RAG.CyberPanel.Domain;

public class QuizDeletionLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Quiz information
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public string? QuizDescription { get; set; }
    public int QuestionCount { get; set; }
    public int AttemptCount { get; set; }

    // Owner information
    public string QuizOwnerUserId { get; set; } = string.Empty;
    public string QuizOwnerUserName { get; set; } = string.Empty;
    public string? QuizOwnerEmail { get; set; }

    // Deletion information
    public string DeletedByUserId { get; set; } = string.Empty;
    public string DeletedByUserName { get; set; } = string.Empty;
    public string? DeletedByUserEmail { get; set; }
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}
