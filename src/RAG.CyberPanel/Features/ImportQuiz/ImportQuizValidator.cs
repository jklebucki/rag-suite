using FluentValidation;

namespace RAG.CyberPanel.Features.ImportQuiz;

/// <summary>
/// Validates imported quiz data to ensure data integrity and business rules.
/// </summary>
public class ImportQuizValidator : AbstractValidator<ImportQuizRequest>
{
    public ImportQuizValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Quiz title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Questions)
            .NotEmpty().WithMessage("Quiz must have at least one question")
            .Must(questions => questions.Length <= 100)
            .WithMessage("Quiz cannot have more than 100 questions");

        RuleForEach(x => x.Questions).ChildRules(question =>
        {
            question.RuleFor(q => q.Text)
                .NotEmpty().WithMessage("Question text is required")
                .MaximumLength(1000).WithMessage("Question text cannot exceed 1000 characters");

            question.RuleFor(q => q.Points)
                .GreaterThan(0).WithMessage("Question points must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Question points cannot exceed 100");

            question.RuleFor(q => q.ImageUrl)
                .MaximumLength(100000).WithMessage("Image data is too large (max 100KB base64)")
                .When(q => !string.IsNullOrEmpty(q.ImageUrl));

            question.RuleFor(q => q.Options)
                .NotEmpty().WithMessage("Question must have at least one option")
                .Must(options => options.Length >= 2)
                .WithMessage("Question must have at least 2 options")
                .Must(options => options.Length <= 10)
                .WithMessage("Question cannot have more than 10 options")
                .Must(options => options.Count(o => o.IsCorrect) >= 1)
                .WithMessage("Question must have at least one correct answer");

            question.RuleForEach(q => q.Options).ChildRules(option =>
            {
                option.RuleFor(o => o.Text)
                    .NotEmpty().WithMessage("Option text is required")
                    .MaximumLength(500).WithMessage("Option text cannot exceed 500 characters");

                option.RuleFor(o => o.ImageUrl)
                    .MaximumLength(100000).WithMessage("Image data is too large (max 100KB base64)")
                    .When(o => !string.IsNullOrEmpty(o.ImageUrl));
            });
        });

        // Validation for overwrite scenario
        RuleFor(x => x.OverwriteQuizId)
            .NotEmpty().WithMessage("OverwriteQuizId is required when CreateNew is false")
            .When(x => !x.CreateNew);

        RuleFor(x => x.OverwriteQuizId)
            .Empty().WithMessage("OverwriteQuizId must be null when CreateNew is true")
            .When(x => x.CreateNew);
    }
}
