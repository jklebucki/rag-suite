using RAG.AddressBook.Domain;

namespace RAG.AddressBook.Features.ListProposals;

public record ListProposalsRequest
{
    public ProposalStatus? Status { get; init; }
    public ChangeProposalType? ProposalType { get; init; }
    public string? ProposedByUserId { get; init; }
}

public record ListProposalsResponse
{
    public List<ProposalListItemDto> Proposals { get; init; } = new();
    public int TotalCount { get; init; }
}

public record ProposalListItemDto
{
    public Guid Id { get; init; }
    public Guid? ContactId { get; init; }
    public string? ContactName { get; init; }
    public ChangeProposalType ProposalType { get; init; }
    public ProposalStatus Status { get; init; }
    public string ProposedByUserId { get; init; } = string.Empty;
    public string? ProposedByUserName { get; init; }
    public DateTime ProposedAt { get; init; }
    public string? ReviewedByUserName { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? Reason { get; init; }
}
