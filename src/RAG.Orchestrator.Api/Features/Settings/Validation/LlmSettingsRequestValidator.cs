using FluentValidation;

namespace RAG.Orchestrator.Api.Features.Settings.Validation;

/// <summary>
/// Validator for LlmSettingsRequest
/// </summary>
public class LlmSettingsRequestValidator : AbstractValidator<LlmSettingsRequest>
{
    public LlmSettingsRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("URL is required")
            .Must(BeValidUrl).WithMessage("Invalid URL format");

        RuleFor(x => x.MaxTokens)
            .GreaterThan(0).WithMessage("MaxTokens must be greater than 0")
            .LessThanOrEqualTo(100000).WithMessage("MaxTokens must not exceed 100000");

        RuleFor(x => x.Temperature)
            .InclusiveBetween(0.0, 2.0).WithMessage("Temperature must be between 0.0 and 2.0");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model name is required")
            .MaximumLength(200).WithMessage("Model name must not exceed 200 characters");

        RuleFor(x => x.TimeoutMinutes)
            .GreaterThan(0).WithMessage("TimeoutMinutes must be greater than 0")
            .LessThanOrEqualTo(60).WithMessage("TimeoutMinutes must not exceed 60");

        RuleFor(x => x.ChatEndpoint)
            .NotEmpty().WithMessage("ChatEndpoint is required")
            .Must(BeValidPath).WithMessage("ChatEndpoint must be a valid path starting with /");

        RuleFor(x => x.GenerateEndpoint)
            .NotEmpty().WithMessage("GenerateEndpoint is required")
            .Must(BeValidPath).WithMessage("GenerateEndpoint must be a valid path starting with /");
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    private static bool BeValidPath(string path)
    {
        return !string.IsNullOrEmpty(path) && path.StartsWith("/");
    }
}

