using RAG.AddressBook.Domain;

namespace RAG.AddressBook.Features.ProposeChange;

public record ProposeContactChangeRequest
{
    public Guid? ContactId { get; init; } // null for create proposals
    public ChangeProposalType ProposalType { get; init; }
    public ContactDataDto ProposedData { get; init; } = new();
    public string? Reason { get; init; }
}

public record ContactDataDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Department { get; init; }
    public string? Position { get; init; }
    public string? Location { get; init; }
    public string? Company { get; init; }
    public string? WorkPhone { get; init; }
    public string? MobilePhone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }
    public string? PhotoUrl { get; init; }
}

public record ProposeContactChangeResponse
{
    public Guid ProposalId { get; init; }
    public ChangeProposalType ProposalType { get; init; }
    public ProposalStatus Status { get; init; }
    public DateTime ProposedAt { get; init; }
    public string Message { get; init; } = string.Empty;
}
