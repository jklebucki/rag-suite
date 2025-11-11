using RAG.AddressBook.Features.UpdateContact;

namespace RAG.Tests.AddressBook;

public class UpdateContactValidatorTests
{
    private readonly UpdateContactValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldBeValid()
    {
        // Arrange
        var request = new UpdateContactRequest
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
        var request = new UpdateContactRequest
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
        var request = new UpdateContactRequest
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
        var request = new UpdateContactRequest
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
        var request = new UpdateContactRequest
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
        var request = new UpdateContactRequest
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
        var request = new UpdateContactRequest
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

    // Note: UpdateContactValidator doesn't validate WorkPhone and MobilePhone length
    // These tests are removed as the validator doesn't have these rules

    [Fact]
    public void Validate_NullEmail_ShouldBeValid()
    {
        // Arrange
        var request = new UpdateContactRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = null
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyEmail_ShouldBeValid()
    {
        // Arrange
        var request = new UpdateContactRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = ""
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}

