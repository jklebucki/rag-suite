using System.ComponentModel.DataAnnotations;

namespace RAG.AddressBook.Domain;

/// <summary>
/// Represents a proposed change to a contact by a non-privileged user
/// </summary>
public class ContactChangeProposal
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Reference to existing contact (null for new contact proposals)
    public Guid? ContactId { get; set; }
    public Contact? Contact { get; set; }

    // Type of change
    public ChangeProposalType ProposalType { get; set; }

    // Proposed data (JSON serialized Contact object)
    public string ProposedData { get; set; } = string.Empty;

    // Reason for change
    public string? Reason { get; set; }

    // Proposal metadata
    public ProposalStatus Status { get; set; } = ProposalStatus.Pending;
    public string ProposedByUserId { get; set; } = string.Empty;
    public string? ProposedByUserName { get; set; }
    public DateTime ProposedAt { get; set; } = DateTime.UtcNow;

    // Review metadata
    public string? ReviewedByUserId { get; set; }
    public string? ReviewedByUserName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewComment { get; set; }
}

public enum ChangeProposalType
{
    Create = 1,
    Update = 2,
    Delete = 3
}

public enum ProposalStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Applied = 4
}
