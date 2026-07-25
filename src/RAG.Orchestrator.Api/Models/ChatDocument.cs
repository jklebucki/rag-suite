using System.ComponentModel.DataAnnotations;

namespace RAG.Orchestrator.Api.Models;

/// <summary>
/// Stores the canonical Markdown result produced for a document attached to a chat message.
/// </summary>
public sealed class ChatDocument
{
    [Key]
    public string Id { get; set; } = null!;

    [Required]
    public string UserMessageId { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string ContentType { get; set; } = null!;

    [Required]
    public string Markdown { get; set; } = null!;

    public long SizeBytes { get; set; }

    public int TokenCount { get; set; }

    public int? PageCount { get; set; }

    [MaxLength(100)]
    public string? Provider { get; set; }

    public DateTime CreatedAt { get; set; }

    public ChatMessage UserMessage { get; set; } = null!;
}
