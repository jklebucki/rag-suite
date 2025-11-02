using FluentValidation;

namespace RAG.AddressBook.Features.CreateContact;

public class CreateContactValidator : AbstractValidator<CreateContactRequest>
{
    public CreateContactValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format");

        RuleFor(x => x.WorkPhone)
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.WorkPhone))
            .WithMessage("Work phone must not exceed 50 characters");

        RuleFor(x => x.MobilePhone)
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.MobilePhone))
            .WithMessage("Mobile phone must not exceed 50 characters");
    }
}
