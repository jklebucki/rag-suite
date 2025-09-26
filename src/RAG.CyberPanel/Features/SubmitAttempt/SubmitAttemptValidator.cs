using FluentValidation;

namespace RAG.CyberPanel.Features.SubmitAttempt;

public class SubmitAttemptValidator : AbstractValidator<SubmitAttemptRequest>
{
    public SubmitAttemptValidator()
    {
        RuleFor(x => x.QuizId).NotEmpty();
        RuleFor(x => x.Answers).NotNull().Must(a => a.Length > 0).WithMessage("At least one answer is required");
        RuleForEach(x => x.Answers).ChildRules(a =>
        {
            a.RuleFor(x => x.QuestionId).NotEmpty();
            a.RuleFor(x => x.SelectedOptionIds).NotNull();
        });
    }
}
