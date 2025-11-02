using RAG.AddressBook.Domain;

namespace RAG.AddressBook.Features.ReviewProposal;

public record ReviewProposalRequest
{
    public ProposalStatus Decision { get; init; } // Approved or Rejected
    public string? ReviewComment { get; init; }
}

public record ReviewProposalResponse
{
    public Guid ProposalId { get; init; }
    public ProposalStatus Status { get; init; }
    public DateTime ReviewedAt { get; init; }
    public string Message { get; init; } = string.Empty;
}
