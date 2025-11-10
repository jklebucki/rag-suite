using System.ComponentModel.DataAnnotations;

namespace RAG.Orchestrator.Api.Models;

public class FeedbackAttachment
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid FeedbackId { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; }

    public Feedback Feedback { get; set; } = null!;
}

