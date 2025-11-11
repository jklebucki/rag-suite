using RAG.CyberPanel.Features.ImportQuiz;

namespace RAG.Tests.CyberPanel;

public class ImportQuizValidatorTests
{
    private readonly ImportQuizValidator _validator = new();

    [Fact]
    public void Validate_ValidCreateNewRequest_IsValid()
    {
        var request = new ImportQuizRequest(
            "Valid Title",
            "Valid Description",
            true,
            new[]
            {
                new ImportedQuestionDto("Question 1", null, 5, new[]
                {
                    new ImportedOptionDto("Option A", null, true),
                    new ImportedOptionDto("Option B", null, false)
                })
            },
            CreateNew: true
        );

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidOverwriteRequest_IsValid()
    {
        var request = new ImportQuizRequest(
            "Valid Title",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("Question 1", null, 5, new[]
                {
                    new ImportedOptionDto("Option A", null, true),
                    new ImportedOptionDto("Option B", null, false)
                })
            },
            CreateNew: false,
            OverwriteQuizId: Guid.NewGuid()
        );

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyTitle_ShouldHaveError()
    {
        var request = new ImportQuizRequest(
            "",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("Q1", null, 5, new[]
                {
                    new ImportedOptionDto("A", null, true),
                    new ImportedOptionDto("B", null, false)
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
        var request = new ImportQuizRequest(
            new string('a', 201),
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("Q1", null, 5, new[]
                {
                    new ImportedOptionDto("A", null, true),
                    new ImportedOptionDto("B", null, false)
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
        var request = new ImportQuizRequest(
            "Title",
            new string('a', 1001),
            false,
            new[]
            {
                new ImportedQuestionDto("Q1", null, 5, new[]
                {
                    new ImportedOptionDto("A", null, true),
                    new ImportedOptionDto("B", null, false)
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
        var request = new ImportQuizRequest(
            "Title",
            null,
            false,
            Array.Empty<ImportedQuestionDto>()
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Questions");
    }

    [Fact]
    public void Validate_TooManyQuestions_ShouldHaveError()
    {
        var questions = Enumerable.Range(1, 101).Select(i =>
            new ImportedQuestionDto($"Q{i}", null, 1, new[]
            {
                new ImportedOptionDto("A", null, true),
                new ImportedOptionDto("B", null, false)
            })
        ).ToArray();

        var request = new ImportQuizRequest("Title", null, false, questions);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Questions");
    }

    [Fact]
    public void Validate_QuestionTextEmpty_ShouldHaveError()
    {
        var request = new ImportQuizRequest(
            "Title",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("", null, 5, new[]
                {
                    new ImportedOptionDto("A", null, true),
                    new ImportedOptionDto("B", null, false)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Text"));
    }

    [Fact]
    public void Validate_QuestionPointsZero_ShouldHaveError()
    {
        var request = new ImportQuizRequest(
            "Title",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("Question", null, 0, new[]
                {
                    new ImportedOptionDto("A", null, true),
                    new ImportedOptionDto("B", null, false)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Points"));
    }

    [Fact]
    public void Validate_QuestionPointsTooHigh_ShouldHaveError()
    {
        var request = new ImportQuizRequest(
            "Title",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("Question", null, 101, new[]
                {
                    new ImportedOptionDto("A", null, true),
                    new ImportedOptionDto("B", null, false)
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
        var request = new ImportQuizRequest(
            "Title",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("Question", null, 5, new[]
                {
                    new ImportedOptionDto("A", null, true)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Options"));
    }

    [Fact]
    public void Validate_QuestionMoreThan10Options_ShouldHaveError()
    {
        var options = Enumerable.Range(1, 11).Select(i =>
            new ImportedOptionDto($"Option {i}", null, i == 1)
        ).ToArray();

        var request = new ImportQuizRequest(
            "Title",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("Question", null, 5, options)
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Options"));
    }

    [Fact]
    public void Validate_NoCorrectOption_ShouldHaveError()
    {
        var request = new ImportQuizRequest(
            "Title",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("Question", null, 5, new[]
                {
                    new ImportedOptionDto("A", null, false),
                    new ImportedOptionDto("B", null, false)
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
        var request = new ImportQuizRequest(
            "Title",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("Question", null, 5, new[]
                {
                    new ImportedOptionDto("", null, true),
                    new ImportedOptionDto("B", null, false)
                })
            }
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Text"));
    }

    [Fact]
    public void Validate_OverwriteWithoutQuizId_ShouldHaveError()
    {
        var request = new ImportQuizRequest(
            "Title",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("Q1", null, 5, new[]
                {
                    new ImportedOptionDto("A", null, true),
                    new ImportedOptionDto("B", null, false)
                })
            },
            CreateNew: false,
            OverwriteQuizId: null
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "OverwriteQuizId");
    }

    [Fact]
    public void Validate_CreateNewWithQuizId_ShouldHaveError()
    {
        var request = new ImportQuizRequest(
            "Title",
            null,
            false,
            new[]
            {
                new ImportedQuestionDto("Q1", null, 5, new[]
                {
                    new ImportedOptionDto("A", null, true),
                    new ImportedOptionDto("B", null, false)
                })
            },
            CreateNew: true,
            OverwriteQuizId: Guid.NewGuid()
        );

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "OverwriteQuizId");
    }
}
