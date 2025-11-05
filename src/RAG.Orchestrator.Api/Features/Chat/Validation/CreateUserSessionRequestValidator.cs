using FluentValidation;
using RAG.Orchestrator.Api.Common.Constants;

namespace RAG.Orchestrator.Api.Features.Chat.Validation;

/// <summary>
/// Validator for CreateUserSessionRequest
/// </summary>
public class CreateUserSessionRequestValidator : AbstractValidator<CreateUserSessionRequest>
{
    public CreateUserSessionRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Title))
            .WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Language)
            .Must(BeValidLanguageCode).When(x => !string.IsNullOrEmpty(x.Language))
            .WithMessage($"Invalid language code. Supported languages: {string.Join(", ", SupportedLanguages.All)}");
    }

    private static bool BeValidLanguageCode(string? language)
    {
        if (string.IsNullOrEmpty(language))
            return true;

        return SupportedLanguages.All.Contains(language);
    }
}

