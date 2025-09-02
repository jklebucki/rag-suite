using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using RAG.Orchestrator.Api.Features.Search;

namespace RAG.Orchestrator.Api.Data.Models;

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

public class ChatMessage
{
    [Key]
    public string Id { get; set; } = null!;
    
    [Required]
    public string SessionId { get; set; } = null!;
    
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = null!; // "user", "assistant", "system"
    
    [Required]
    public string Content { get; set; } = null!;
    
    public DateTime Timestamp { get; set; }
    
    // JSON columns for complex data
    [Column("sources")]
    public string? SourcesJson { get; set; }
    
    [Column("metadata")]
    public string? MetadataJson { get; set; }
    
    // Navigation property
    public virtual ChatSession Session { get; set; } = null!;
    
    // Helper properties to work with JSON data
    [NotMapped]
    public SearchResult[]? Sources
    {
        get => string.IsNullOrEmpty(SourcesJson) ? null : JsonSerializer.Deserialize<SearchResult[]>(SourcesJson);
        set => SourcesJson = value == null ? null : JsonSerializer.Serialize(value);
    }
    
    [NotMapped]
    public Dictionary<string, object>? Metadata
    {
        get => string.IsNullOrEmpty(MetadataJson) ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(MetadataJson);
        set => MetadataJson = value == null ? null : JsonSerializer.Serialize(value);
    }
}
