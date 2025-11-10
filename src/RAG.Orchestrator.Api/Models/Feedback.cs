using System.ComponentModel.DataAnnotations;

namespace RAG.Orchestrator.Api.Models;

public class Feedback
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = null!;

    [MaxLength(320)]
    public string? UserEmail { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = null!;

    [Required]
    [MaxLength(4000)]
    public string Message { get; set; } = null!;

    [MaxLength(4000)]
    public string? Response { get; set; }

    [MaxLength(320)]
    public string? ResponseAuthorEmail { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

