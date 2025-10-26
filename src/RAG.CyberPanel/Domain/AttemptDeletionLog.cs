using System.ComponentModel.DataAnnotations;

namespace RAG.CyberPanel.Domain;

public class AttemptDeletionLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Attempt information
    public Guid AttemptId { get; set; }
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int Score { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    
    // User who took the quiz
    public string AttemptUserId { get; set; } = string.Empty;
    public string AttemptUserName { get; set; } = string.Empty;
    public string? AttemptUserEmail { get; set; }
    
    // Deletion information
    public string DeletedByUserId { get; set; } = string.Empty;
    public string DeletedByUserName { get; set; } = string.Empty;
    public string? DeletedByUserEmail { get; set; }
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}
