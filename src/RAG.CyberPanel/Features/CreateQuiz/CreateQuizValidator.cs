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
            q.RuleFor(x => x.ImageUrl)
                .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
                .When(x => !string.IsNullOrEmpty(x.ImageUrl))
                .WithMessage("ImageUrl must be a valid absolute URL");
            q.RuleFor(x => x.Options).NotNull().Must(o => o.Length >= 2).WithMessage("Each question must have at least two options");
            q.RuleFor(x => x.Options).Must(opts => opts.Any(o => o.IsCorrect)).WithMessage("Each question must have at least one correct option");
            
            q.RuleForEach(x => x.Options).ChildRules(opt =>
            {
                opt.RuleFor(o => o.Text).NotEmpty();
                opt.RuleFor(o => o.ImageUrl)
                    .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
                    .When(o => !string.IsNullOrEmpty(o.ImageUrl))
                    .WithMessage("ImageUrl must be a valid absolute URL");
            });
        });
    }
}
