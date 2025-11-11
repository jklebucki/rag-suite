using Microsoft.AspNetCore.Http;
using FluentAssertions;
//using RAG.Orchestrator.Api.Common.Results;
namespace RAG.Tests.Orchestrator;


public class ResultExtensionsTests
{
    [Fact]
    public void ToHttpResult_WithSuccess_ReturnsIResult()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().NotBeNull();
        httpResult.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public void ToHttpResult_WithFailure_ReturnsIResult()
    {
        // Arrange
        var result = Result.Failure("Error occurred");

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().NotBeNull();
        httpResult.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public void ToHttpResult_WithFailureAndErrors_ReturnsIResult()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var result = Result.Failure("Validation failed", errors);

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().NotBeNull();
        httpResult.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public void ToHttpResult_WithSuccessValue_ReturnsIResult()
    {
        // Arrange
        var data = new { Name = "Test", Value = 123 };
        var result = Result<object>.Success(data);

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().NotBeNull();
        httpResult.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public void ToHttpResult_WithSuccessValueAndMessage_ReturnsIResult()
    {
        // Arrange
        var data = new { Name = "Test" };
        var result = Result<object>.Success(data);
        var successMessage = "Operation completed successfully";

        // Act
        var httpResult = result.ToHttpResult(successMessage);

        // Assert
        httpResult.Should().NotBeNull();
        httpResult.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public void ToHttpResult_WithFailureValue_ReturnsIResult()
    {
        // Arrange
        var result = Result<object>.Failure("Error occurred");

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().NotBeNull();
        httpResult.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public void ToHttpResult_WithNullValue_ReturnsIResult()
    {
        // Arrange
        var result = Result<object>.Success(null!);

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().NotBeNull();
        httpResult.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public void ToHttpResultOrNotFound_WithSuccessValue_ReturnsIResult()
    {
        // Arrange
        var data = new { Name = "Test" };
        var result = Result<object>.Success(data);

        // Act
        var httpResult = result.ToHttpResultOrNotFound();

        // Assert
        httpResult.Should().NotBeNull();
        httpResult.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public void ToHttpResultOrNotFound_WithNullValue_ReturnsIResult()
    {
        // Arrange
        var result = Result<object>.Success(null!);

        // Act
        var httpResult = result.ToHttpResultOrNotFound();

        // Assert
        httpResult.Should().NotBeNull();
        httpResult.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public void ToHttpResultOrNotFound_WithNullValueAndCustomMessage_ReturnsIResult()
    {
        // Arrange
        var result = Result<object>.Success(null!);
        var notFoundMessage = "Item not found";

        // Act
        var httpResult = result.ToHttpResultOrNotFound(notFoundMessage);

        // Assert
        httpResult.Should().NotBeNull();
        httpResult.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public void ToHttpResultOrNotFound_WithFailure_ReturnsIResult()
    {
        // Arrange
        var result = Result<object>.Failure("Error occurred");

        // Act
        var httpResult = result.ToHttpResultOrNotFound();

        // Assert
        httpResult.Should().NotBeNull();
        httpResult.Should().BeAssignableTo<IResult>();
    }
}

