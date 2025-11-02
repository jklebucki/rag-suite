using FluentValidation;
using RAG.AddressBook.Domain;

namespace RAG.AddressBook.Features.ProposeChange;

public class ProposeChangeValidator : AbstractValidator<ProposeContactChangeRequest>
{
    public ProposeChangeValidator()
    {
        RuleFor(x => x.ProposalType)
            .IsInEnum().WithMessage("Invalid proposal type");

        RuleFor(x => x.ContactId)
            .NotEmpty().When(x => x.ProposalType != ChangeProposalType.Create)
            .WithMessage("ContactId is required for Update and Delete proposals");

        RuleFor(x => x.ProposedData.FirstName)
            .NotEmpty().When(x => x.ProposalType != ChangeProposalType.Delete)
            .WithMessage("First name is required")
            .MaximumLength(100);

        RuleFor(x => x.ProposedData.LastName)
            .NotEmpty().When(x => x.ProposalType != ChangeProposalType.Delete)
            .WithMessage("Last name is required")
            .MaximumLength(100);

        RuleFor(x => x.ProposedData.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.ProposedData.Email))
            .WithMessage("Invalid email format");

        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters");
    }
}
