using FluentValidation;

namespace RAG.CyberPanel.Features.CreateQuiz;

public class CreateQuizValidator : AbstractValidator<CreateQuizRequest>
{
    public CreateQuizValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Questions).NotNull().Must(q => q.Length > 0).WithMessage("Quiz must contain at least one question");
        RuleForEach(x => x.Questions).ChildRules(q =>
        {
            q.RuleFor(x => x.Text).NotEmpty();
            q.RuleFor(x => x.Options).NotNull().Must(o => o.Length >= 2).WithMessage("Each question must have at least two options");
            q.RuleFor(x => x.Options).Must(opts => opts.Any(o => o.IsCorrect)).WithMessage("Each question must have at least one correct option");
        });
    }
}
