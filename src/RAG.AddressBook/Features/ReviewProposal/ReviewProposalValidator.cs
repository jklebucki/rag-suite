using FluentValidation;
using RAG.AddressBook.Domain;

namespace RAG.AddressBook.Features.ReviewProposal;

public class ReviewProposalValidator : AbstractValidator<ReviewProposalRequest>
{
    public ReviewProposalValidator()
    {
        RuleFor(x => x.Decision)
            .Must(d => d == ProposalStatus.Approved || d == ProposalStatus.Rejected)
            .WithMessage("Decision must be either Approved or Rejected");

        RuleFor(x => x.ReviewComment)
            .MaximumLength(1000).WithMessage("Review comment must not exceed 1000 characters");
    }
}
