using FluentValidation;
using Microsoft.Extensions.Configuration;
using RAG.Orchestrator.Api.Common.Constants;

namespace RAG.Orchestrator.Api.Features.Chat.Validation;

/// <summary>
/// Validator for UserChatRequest
/// </summary>
public class UserChatRequestValidator : AbstractValidator<UserChatRequest>
{
    public UserChatRequestValidator(IConfiguration configuration)
    {
        var maxMessageLength = configuration.GetValue<int>(ConfigurationKeys.Chat.MaxMessageLength, 10000);

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(maxMessageLength).WithMessage($"Message must not exceed {maxMessageLength} characters");

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

