using FluentValidation;

namespace RAG.Orchestrator.Api.Features.Settings.Validation;

public class ForumSettingsRequestValidator : AbstractValidator<ForumSettingsRequest>
{
    public ForumSettingsRequestValidator()
    {
        RuleFor(x => x.MaxAttachmentCount)
            .GreaterThan(0)
            .LessThanOrEqualTo(50);

        RuleFor(x => x.MaxAttachmentSizeMb)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);

        RuleFor(x => x.BadgeRefreshSeconds)
            .GreaterThanOrEqualTo(15)
            .LessThanOrEqualTo(600);
    }
}

