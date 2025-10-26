using FluentValidation;

namespace RAG.CyberPanel.Features.UpdateQuiz;

public class UpdateQuizValidator : AbstractValidator<UpdateQuizRequest>
{
    public UpdateQuizValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.Questions)
            .NotEmpty().WithMessage("Quiz must have at least one question")
            .Must(questions => questions.Length > 0).WithMessage("Quiz must have at least one question");

        RuleForEach(x => x.Questions).ChildRules(question =>
        {
            question.RuleFor(q => q.Text)
                .NotEmpty().WithMessage("Question text is required")
                .MaximumLength(500).WithMessage("Question text must not exceed 500 characters");

            question.RuleFor(q => q.Points)
                .GreaterThan(0).WithMessage("Question points must be greater than 0");

            question.RuleFor(q => q.Options)
                .NotEmpty().WithMessage("Question must have at least one option")
                .Must(options => options.Length >= 2).WithMessage("Question must have at least 2 options")
                .Must(options => options.Any(o => o.IsCorrect)).WithMessage("At least one option must be marked as correct");

            question.RuleForEach(q => q.Options).ChildRules(option =>
            {
                option.RuleFor(o => o.Text)
                    .NotEmpty().WithMessage("Option text is required")
                    .MaximumLength(300).WithMessage("Option text must not exceed 300 characters");
            });
        });
    }
}
