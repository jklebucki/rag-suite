using RAG.CyberPanel.Features.UpdateQuiz;

namespace RAG.Tests.CyberPanel;

public class UpdateQuizValidatorTests
{
    private readonly UpdateQuizValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var request = new UpdateQuizRequest(
            null,
            "Valid Title",
            "Valid Description",
            null,
            true,
            new[]
            {
                new QuestionRequest(null, "Question 1", null, 5, 0, new[]
                {
                    new OptionRequest(null, "Option A", null, true),
                    new OptionRequest(null, "Option B", null, false)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyTitle_ShouldHaveError()
    {
        var request = new UpdateQuizRequest(
            null,
            "",
            null,
            null,
            false,
            new[]
            {
                new QuestionRequest(null, "Q1", null, 5, 0, new[]
                {
                    new OptionRequest(null, "A", null, true),
                    new OptionRequest(null, "B", null, false)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_TitleTooLong_ShouldHaveError()
    {
        var request = new UpdateQuizRequest(
            null,
            new string('a', 201),
            null,
            null,
            false,
            new[]
            {
                new QuestionRequest(null, "Q1", null, 5, 0, new[]
                {
                    new OptionRequest(null, "A", null, true),
                    new OptionRequest(null, "B", null, false)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_DescriptionTooLong_ShouldHaveError()
    {
        var request = new UpdateQuizRequest(
            null,
            "Title",
            new string('a', 1001),
            null,
            false,
            new[]
            {
                new QuestionRequest(null, "Q1", null, 5, 0, new[]
                {
                    new OptionRequest(null, "A", null, true),
                    new OptionRequest(null, "B", null, false)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }

    [Fact]
    public void Validate_NoQuestions_ShouldHaveError()
    {
        var request = new UpdateQuizRequest(
            null,
            "Title",
            null,
            null,
            false,
            Array.Empty<QuestionRequest>()
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Questions");
    }

    [Fact]
    public void Validate_QuestionTextEmpty_ShouldHaveError()
    {
        var request = new UpdateQuizRequest(
            null,
            "Title",
            null,
            null,
            false,
            new[]
            {
                new QuestionRequest(null, "", null, 5, 0, new[]
                {
                    new OptionRequest(null, "A", null, true),
                    new OptionRequest(null, "B", null, false)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Text"));
    }

    [Fact]
    public void Validate_QuestionTextTooLong_ShouldHaveError()
    {
        var request = new UpdateQuizRequest(
            null,
            "Title",
            null,
            null,
            false,
            new[]
            {
                new QuestionRequest(null, new string('a', 501), null, 5, 0, new[]
                {
                    new OptionRequest(null, "A", null, true),
                    new OptionRequest(null, "B", null, false)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Text"));
    }

    [Fact]
    public void Validate_QuestionPointsZeroOrNegative_ShouldHaveError()
    {
        var request = new UpdateQuizRequest(
            null,
            "Title",
            null,
            null,
            false,
            new[]
            {
                new QuestionRequest(null, "Question", null, 0, 0, new[]
                {
                    new OptionRequest(null, "A", null, true),
                    new OptionRequest(null, "B", null, false)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Points"));
    }

    [Fact]
    public void Validate_QuestionLessThanTwoOptions_ShouldHaveError()
    {
        var request = new UpdateQuizRequest(
            null,
            "Title",
            null,
            null,
            false,
            new[]
            {
                new QuestionRequest(null, "Question", null, 5, 0, new[]
                {
                    new OptionRequest(null, "A", null, true)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Options"));
    }

    [Fact]
    public void Validate_NoCorrectOption_ShouldHaveError()
    {
        var request = new UpdateQuizRequest(
            null,
            "Title",
            null,
            null,
            false,
            new[]
            {
                new QuestionRequest(null, "Question", null, 5, 0, new[]
                {
                    new OptionRequest(null, "A", null, false),
                    new OptionRequest(null, "B", null, false)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Options"));
    }

    [Fact]
    public void Validate_OptionTextEmpty_ShouldHaveError()
    {
        var request = new UpdateQuizRequest(
            null,
            "Title",
            null,
            null,
            false,
            new[]
            {
                new QuestionRequest(null, "Question", null, 5, 0, new[]
                {
                    new OptionRequest(null, "", null, true),
                    new OptionRequest(null, "B", null, false)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Text"));
    }

    [Fact]
    public void Validate_OptionTextTooLong_ShouldHaveError()
    {
        var request = new UpdateQuizRequest(
            null,
            "Title",
            null,
            null,
            false,
            new[]
            {
                new QuestionRequest(null, "Question", null, 5, 0, new[]
                {
                    new OptionRequest(null, new string('a', 301), null, true),
                    new OptionRequest(null, "B", null, false)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Text"));
    }

    [Fact]
    public void Validate_MultipleCorrectOptions_IsValid()
    {
        // Multiple correct answers should be allowed
        var request = new UpdateQuizRequest(
            null,
            "Title",
            null,
            null,
            false,
            new[]
            {
                new QuestionRequest(null, "Multi-select question", null, 10, 0, new[]
                {
                    new OptionRequest(null, "A", null, true),
                    new OptionRequest(null, "B", null, true),
                    new OptionRequest(null, "C", null, false)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
