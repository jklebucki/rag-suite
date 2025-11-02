using System.ComponentModel.DataAnnotations;

namespace RAG.AddressBook.Domain;

/// <summary>
/// Contact entity representing a person in the address book
/// </summary>
public class Contact
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Personal information
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    
    // Work information
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? Location { get; set; }
    public string? Company { get; set; }
    
    // Contact information
    public string? WorkPhone { get; set; }
    public string? MobilePhone { get; set; }
    public string? Email { get; set; }
    
    // Additional standard fields
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PhotoUrl { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string? UpdatedByUserId { get; set; }
    
    // Tags for categorization
    public ICollection<ContactTag> Tags { get; set; } = new List<ContactTag>();
}
