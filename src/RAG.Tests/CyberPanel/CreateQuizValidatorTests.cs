using Xunit;
using RAG.CyberPanel.Features.CreateQuiz;

namespace RAG.Tests.CyberPanel;

public class CreateQuizValidatorTests
{
    private readonly CreateQuizValidator _validator = new();

    [Fact]
    public void Validate_TitleEmpty_ShouldHaveError()
    {
        var request = new CreateQuizRequest("", null, false, Array.Empty<QuestionDto>());
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_TitleTooLong_ShouldHaveError()
    {
        var request = new CreateQuizRequest(new string('a', 201), null, false, Array.Empty<QuestionDto>());
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_NoQuestions_ShouldHaveError()
    {
        var request = new CreateQuizRequest("Test", null, false, Array.Empty<QuestionDto>());
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Questions");
    }

    [Fact]
    public void Validate_QuestionTextEmpty_ShouldHaveError()
    {
        var request = new CreateQuizRequest(
            "Test",
            null,
            false,
            new[]
            {
                new QuestionDto(null, "", null, 1, new[] { new OptionDto(null, "A", null, true) })
            }
        );
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Text"));
    }

    [Fact]
    public void Validate_QuestionHasOneOption_ShouldHaveError()
    {
        var request = new CreateQuizRequest(
            "Test",
            null,
            false,
            new[]
            {
                new QuestionDto(null, "Question", null, 1, new[] { new OptionDto(null, "A", null, true) })
            }
        );
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Options"));
    }

    [Fact]
    public void Validate_QuestionNoCorrectOption_ShouldHaveError()
    {
        var request = new CreateQuizRequest(
            "Test",
            null,
            false,
            new[]
            {
                new QuestionDto(
                    null,
                    "Question",
                    null,
                    1,
                    new[]
                    {
                        new OptionDto(null, "A", null, false),
                        new OptionDto(null, "B", null, false)
                    }
                )
            }
        );
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Options"));
    }

    [Fact]
    public void Validate_InvalidImageUrl_ShouldHaveError()
    {
        var request = new CreateQuizRequest(
            "Test",
            null,
            false,
            new[]
            {
                new QuestionDto(
                    null,
                    "Question",
                    "invalid-url",
                    1,
                    new[]
                    {
                        new OptionDto(null, "A", null, true),
                        new OptionDto(null, "B", null, false)
                    }
                )
            }
        );
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("ImageUrl"));
    }

    [Fact]
    public void Validate_ValidRequest_ShouldNotHaveErrors()
    {
        var request = new CreateQuizRequest(
            "Test Quiz",
            "Description",
            false,
            new[]
            {
                new QuestionDto(
                    null,
                    "Question 1",
                    null,
                    2,
                    new[]
                    {
                        new OptionDto(null, "A", null, true),
                        new OptionDto(null, "B", null, false)
                    }
                )
            }
        );
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }
}