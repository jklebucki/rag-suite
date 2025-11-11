using FluentValidation;

namespace RAG.Orchestrator.Api.Features.Feedback.Validation;

public class CreateFeedbackRequestValidator : AbstractValidator<CreateFeedbackRequest>
{
    public CreateFeedbackRequestValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(x => x.Attachments)
            .Must(attachments => attachments.Count <= 5)
            .WithMessage("A maximum of 5 attachments is allowed per feedback entry.");

        RuleForEach(x => x.Attachments).ChildRules(attachment =>
        {
            attachment.RuleFor(a => a.FileName)
                .NotEmpty()
                .MaximumLength(255);

            attachment.RuleFor(a => a.ContentType)
                .NotEmpty()
                .MaximumLength(255);

            attachment.RuleFor(a => a.DataBase64)
                .NotEmpty()
                .Must(data => IsValidBase64WithLimit(data, 5 * 1024 * 1024))
                .WithMessage("Attachment must be valid base64 and cannot exceed 5 MB.");
        });

        static bool IsValidBase64WithLimit(string data, int maxBytes)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return false;
            }

            try
            {
                var buffer = new Span<byte>(new byte[(int)(data.Length * 0.75) + 1]);
                if (!Convert.TryFromBase64String(data, buffer, out var bytesWritten))
                {
                    return false;
                }

                return bytesWritten <= maxBytes;
            }
            catch
            {
                return false;
            }
        }
    }
}

