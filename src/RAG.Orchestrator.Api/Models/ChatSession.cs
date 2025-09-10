using System.ComponentModel.DataAnnotations;
namespace RAG.Orchestrator.Api.Models;
public class ChatSession
{
    [Key]
    public string Id { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}