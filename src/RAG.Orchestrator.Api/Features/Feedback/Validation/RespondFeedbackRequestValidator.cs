using FluentValidation;

namespace RAG.Orchestrator.Api.Features.Feedback.Validation;

public class RespondFeedbackRequestValidator : AbstractValidator<RespondFeedbackRequest>
{
    public RespondFeedbackRequestValidator()
    {
        RuleFor(x => x.Response)
            .NotEmpty()
            .MaximumLength(4000);
    }
}

