using Xunit;
using FluentAssertions;
using RAG.AddressBook.Features.CreateContact;

namespace RAG.Tests.AddressBook;

public class CreateContactValidatorTests
{
    private readonly CreateContactValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldBeValid()
    {
        // Arrange
        var request = new CreateContactRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyFirstName_ShouldBeInvalid()
    {
        // Arrange
        var request = new CreateContactRequest
        {
            FirstName = "",
            LastName = "Doe"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void Validate_EmptyLastName_ShouldBeInvalid()
    {
        // Arrange
        var request = new CreateContactRequest
        {
            FirstName = "John",
            LastName = ""
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName");
    }

    [Fact]
    public void Validate_FirstNameTooLong_ShouldBeInvalid()
    {
        // Arrange
        var request = new CreateContactRequest
        {
            FirstName = new string('a', 101),
            LastName = "Doe"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void Validate_LastNameTooLong_ShouldBeInvalid()
    {
        // Arrange
        var request = new CreateContactRequest
        {
            FirstName = "John",
            LastName = new string('a', 101)
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName");
    }

    [Fact]
    public void Validate_InvalidEmail_ShouldBeInvalid()
    {
        // Arrange
        var request = new CreateContactRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "invalid-email"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_ValidEmail_ShouldBeValid()
    {
        // Arrange
        var request = new CreateContactRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WorkPhoneTooLong_ShouldBeInvalid()
    {
        // Arrange
        var request = new CreateContactRequest
        {
            FirstName = "John",
            LastName = "Doe",
            WorkPhone = new string('1', 51)
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WorkPhone");
    }

    [Fact]
    public void Validate_MobilePhoneTooLong_ShouldBeInvalid()
    {
        // Arrange
        var request = new CreateContactRequest
        {
            FirstName = "John",
            LastName = "Doe",
            MobilePhone = new string('1', 51)
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MobilePhone");
    }
}

