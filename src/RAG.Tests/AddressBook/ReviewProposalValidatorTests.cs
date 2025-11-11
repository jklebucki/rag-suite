using FluentAssertions;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.ReviewProposal;

namespace RAG.Tests.AddressBook;

public class ReviewProposalValidatorTests
{
    private readonly ReviewProposalValidator _validator;

    public ReviewProposalValidatorTests()
    {
        _validator = new ReviewProposalValidator();
    }

    [Fact]
    public void Validate_ApprovedDecision_ShouldPass()
    {
        // Arrange
        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Approved,
            ReviewComment = "Looks good"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_RejectedDecision_ShouldPass()
    {
        // Arrange
        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Rejected,
            ReviewComment = "Not appropriate"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_PendingDecision_ShouldFail()
    {
        // Arrange
        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Pending,
            ReviewComment = "Test"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Decision" && e.ErrorMessage == "Decision must be either Approved or Rejected");
    }

    [Fact]
    public void Validate_AppliedDecision_ShouldFail()
    {
        // Arrange
        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Applied,
            ReviewComment = "Test"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Decision" && e.ErrorMessage == "Decision must be either Approved or Rejected");
    }

    [Fact]
    public void Validate_ReviewCommentTooLong_ShouldFail()
    {
        // Arrange
        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Approved,
            ReviewComment = new string('a', 1001) // 1001 characters
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReviewComment" && e.ErrorMessage == "Review comment must not exceed 1000 characters");
    }

    [Fact]
    public void Validate_ReviewCommentMaxLength_ShouldPass()
    {
        // Arrange
        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Approved,
            ReviewComment = new string('a', 1000) // Exactly 1000 characters
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ReviewCommentNull_ShouldPass()
    {
        // Arrange
        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Approved,
            ReviewComment = null
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ReviewCommentEmpty_ShouldPass()
    {
        // Arrange
        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Approved,
            ReviewComment = ""
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}

