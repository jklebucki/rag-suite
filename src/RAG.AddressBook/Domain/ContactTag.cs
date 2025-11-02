using System.ComponentModel.DataAnnotations;

namespace RAG.AddressBook.Domain;

/// <summary>
/// Tag entity for categorizing contacts
/// </summary>
public class ContactTag
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    
    public string TagName { get; set; } = string.Empty;
    public string? Color { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
