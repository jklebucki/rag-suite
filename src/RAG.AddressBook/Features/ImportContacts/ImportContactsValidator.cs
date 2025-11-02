using FluentValidation;

namespace RAG.AddressBook.Features.ImportContacts;

public class ImportContactsValidator : AbstractValidator<ImportContactsRequest>
{
    public ImportContactsValidator()
    {
        RuleFor(x => x.CsvContent)
            .NotEmpty().WithMessage("CSV content is required")
            .Must(content => content.Contains(";")).WithMessage("Invalid CSV format - semicolon delimiter expected");
    }
}
