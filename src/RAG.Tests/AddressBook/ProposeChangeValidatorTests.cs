using FluentAssertions;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.ProposeChange;

namespace RAG.Tests.AddressBook;

public class ProposeChangeValidatorTests
{
    private readonly ProposeChangeValidator _validator;

    public ProposeChangeValidatorTests()
    {
        _validator = new ProposeChangeValidator();
    }

    [Fact]
    public void Validate_CreateProposal_ValidData_ShouldPass()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = new ContactDataDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            },
            Reason = "New employee"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_CreateProposal_EmptyContactId_ShouldPass()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ContactId = null,
            ProposedData = new ContactDataDto
            {
                FirstName = "John",
                LastName = "Doe"
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UpdateProposal_WithContactId_ShouldPass()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Update,
            ContactId = Guid.NewGuid(),
            ProposedData = new ContactDataDto
            {
                FirstName = "Updated",
                LastName = "Name",
                Email = "updated@example.com"
            },
            Reason = "Email changed"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UpdateProposal_WithoutContactId_ShouldFail()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Update,
            ContactId = null,
            ProposedData = new ContactDataDto
            {
                FirstName = "Updated",
                LastName = "Name"
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContactId" && e.ErrorMessage == "ContactId is required for Update and Delete proposals");
    }

    [Fact]
    public void Validate_DeleteProposal_WithContactId_ShouldPass()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Delete,
            ContactId = Guid.NewGuid(),
            ProposedData = new ContactDataDto(),
            Reason = "No longer works here"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_DeleteProposal_WithoutContactId_ShouldFail()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Delete,
            ContactId = null,
            ProposedData = new ContactDataDto()
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContactId" && e.ErrorMessage == "ContactId is required for Update and Delete proposals");
    }

    [Fact]
    public void Validate_CreateProposal_EmptyFirstName_ShouldFail()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = new ContactDataDto
            {
                FirstName = "",
                LastName = "Doe"
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProposedData.FirstName" && e.ErrorMessage == "First name is required");
    }

    [Fact]
    public void Validate_CreateProposal_EmptyLastName_ShouldFail()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = new ContactDataDto
            {
                FirstName = "John",
                LastName = ""
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProposedData.LastName" && e.ErrorMessage == "Last name is required");
    }

    [Fact]
    public void Validate_DeleteProposal_EmptyFirstName_ShouldPass()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Delete,
            ContactId = Guid.NewGuid(),
            ProposedData = new ContactDataDto
            {
                FirstName = "",
                LastName = ""
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_FirstNameTooLong_ShouldFail()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = new ContactDataDto
            {
                FirstName = new string('a', 101), // 101 characters
                LastName = "Doe"
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProposedData.FirstName");
    }

    [Fact]
    public void Validate_LastNameTooLong_ShouldFail()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = new ContactDataDto
            {
                FirstName = "John",
                LastName = new string('a', 101) // 101 characters
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProposedData.LastName");
    }

    [Fact]
    public void Validate_InvalidEmail_ShouldFail()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = new ContactDataDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "invalid-email"
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProposedData.Email" && e.ErrorMessage == "Invalid email format");
    }

    [Fact]
    public void Validate_ValidEmail_ShouldPass()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = new ContactDataDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyEmail_ShouldPass()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = new ContactDataDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = ""
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullEmail_ShouldPass()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = new ContactDataDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = null
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ReasonTooLong_ShouldFail()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = new ContactDataDto
            {
                FirstName = "John",
                LastName = "Doe"
            },
            Reason = new string('a', 1001) // 1001 characters
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason" && e.ErrorMessage == "Reason must not exceed 1000 characters");
    }

    [Fact]
    public void Validate_ReasonMaxLength_ShouldPass()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = new ContactDataDto
            {
                FirstName = "John",
                LastName = "Doe"
            },
            Reason = new string('a', 1000) // Exactly 1000 characters
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidProposalType_ShouldFail()
    {
        // Arrange
        var request = new ProposeContactChangeRequest
        {
            ProposalType = (ChangeProposalType)999, // Invalid enum value
            ProposedData = new ContactDataDto
            {
                FirstName = "John",
                LastName = "Doe"
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProposalType" && e.ErrorMessage == "Invalid proposal type");
    }
}

