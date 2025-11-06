using Xunit;
using FluentAssertions;
using RAG.AddressBook.Features.ImportContacts;
using FluentValidation;

namespace RAG.Tests.AddressBook;

public class ImportContactsValidatorTests
{
    private readonly ImportContactsValidator _validator;

    public ImportContactsValidatorTests()
    {
        _validator = new ImportContactsValidator();
    }

    [Fact]
    public void Validate_ValidCsvContent_ShouldPass()
    {
        // Arrange
        var request = new ImportContactsRequest
        {
            CsvContent = "Imię;Nazwisko;Dział;Telefon służbowy;Telefon komórkowy;Adres e-mail;Nazwa wyświetlana;Stanowisko;Lokalizacja\n\"John\";\"Doe\";\"IT\";\"\";\"\";\"john@example.com\";\"\";\"\";\"\"",
            SkipDuplicates = true
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyCsvContent_ShouldFail()
    {
        // Arrange
        var request = new ImportContactsRequest
        {
            CsvContent = "",
            SkipDuplicates = false
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CsvContent" && e.ErrorMessage == "CSV content is required");
    }

    [Fact]
    public void Validate_WhitespaceCsvContent_ShouldFail()
    {
        // Arrange
        var request = new ImportContactsRequest
        {
            CsvContent = "   ",
            SkipDuplicates = false
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CsvContent" && e.ErrorMessage == "CSV content is required");
    }

    [Fact]
    public void Validate_CsvContentWithoutSemicolon_ShouldFail()
    {
        // Arrange
        var request = new ImportContactsRequest
        {
            CsvContent = "Name,Email,Phone",
            SkipDuplicates = false
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CsvContent" && e.ErrorMessage == "Invalid CSV format - semicolon delimiter expected");
    }

    [Fact]
    public void Validate_CsvContentWithSemicolon_ShouldPass()
    {
        // Arrange
        var request = new ImportContactsRequest
        {
            CsvContent = "Header1;Header2;Header3",
            SkipDuplicates = false
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_SkipDuplicates_ShouldNotAffectValidation()
    {
        // Arrange
        var request = new ImportContactsRequest
        {
            CsvContent = "Name;Email",
            SkipDuplicates = true
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}

