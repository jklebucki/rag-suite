using RAG.CyberPanel.Features.SubmitAttempt;

namespace RAG.Tests.CyberPanel;

public class SubmitAttemptValidatorTests
{
    private readonly SubmitAttemptValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var request = new SubmitAttemptRequest(
            Guid.NewGuid(),
            new[]
            {
                new AnswerDto(Guid.NewGuid(), new[] { Guid.NewGuid() })
            }
        );

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyQuizId_ShouldHaveError()
    {
        var request = new SubmitAttemptRequest(
            Guid.Empty,
            new[]
            {
                new AnswerDto(Guid.NewGuid(), new[] { Guid.NewGuid() })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "QuizId");
    }

    [Fact]
    public void Validate_NullAnswers_ShouldHaveError()
    {
        var request = new SubmitAttemptRequest(Guid.NewGuid(), null!);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Answers");
    }

    [Fact]
    public void Validate_EmptyAnswers_ShouldHaveError()
    {
        var request = new SubmitAttemptRequest(Guid.NewGuid(), Array.Empty<AnswerDto>());

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Answers");
    }

    [Fact]
    public void Validate_EmptyQuestionId_ShouldHaveError()
    {
        var request = new SubmitAttemptRequest(
            Guid.NewGuid(),
            new[]
            {
                new AnswerDto(Guid.Empty, new[] { Guid.NewGuid() })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("QuestionId"));
    }

    [Fact]
    public void Validate_NullSelectedOptionIds_ShouldHaveError()
    {
        var request = new SubmitAttemptRequest(
            Guid.NewGuid(),
            new[]
            {
                new AnswerDto(Guid.NewGuid(), null!)
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("SelectedOptionIds"));
    }

    [Fact]
    public void Validate_MultipleAnswers_ValidatesEach()
    {
        var request = new SubmitAttemptRequest(
            Guid.NewGuid(),
            new[]
            {
                new AnswerDto(Guid.NewGuid(), new[] { Guid.NewGuid() }), // Valid
                new AnswerDto(Guid.Empty, new[] { Guid.NewGuid() }),     // Invalid QuestionId
                new AnswerDto(Guid.NewGuid(), null!)                     // Invalid SelectedOptionIds
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2); // At least 2 errors from invalid answers
    }

    [Fact]
    public void Validate_EmptySelectedOptionIds_IsValid()
    {
        // Empty array is valid - user can skip answering
        var request = new SubmitAttemptRequest(
            Guid.NewGuid(),
            new[]
            {
                new AnswerDto(Guid.NewGuid(), Array.Empty<Guid>())
            }
        );

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
