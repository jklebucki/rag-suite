using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.ImportContacts;
using RAG.Security.Services;

namespace RAG.Tests.AddressBook;

public class ImportContactsHandlerTests : IDisposable
{
    private readonly AddressBookDbContext _context;
    private readonly Mock<IUserContextService> _mockUserContext;
    private readonly ImportContactsHandler _handler;

    public ImportContactsHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AddressBookDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AddressBookDbContext(options);
        _mockUserContext = new Mock<IUserContextService>();
        _handler = new ImportContactsHandler(_context, _mockUserContext.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task HandleAsync_ValidCsv_ImportsContacts()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var csvContent = @"Imię;Nazwisko;Dział;Telefon służbowy;Telefon komórkowy;Adres e-mail;Nazwa wyświetlana;Stanowisko;Lokalizacja
""John"";""Doe"";""IT"";""+48123456789"";""+48987654321"";""john.doe@example.com"";""John Doe - Tech Corp"";""Developer"";""Warsaw""";

        var request = new ImportContactsRequest
        {
            CsvContent = csvContent,
            SkipDuplicates = false
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);
        result.TotalRows.Should().Be(1);
        result.ImportedContacts.Should().HaveCount(1);

        var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Email == "john.doe@example.com");
        contact.Should().NotBeNull();
        contact!.FirstName.Should().Be("John");
        contact.LastName.Should().Be("Doe");
        contact.Department.Should().Be("IT");
        contact.Company.Should().Be("Tech Corp");
        contact.Position.Should().Be("Developer");
        contact.Location.Should().Be("Warsaw");
    }

    [Fact]
    public async Task HandleAsync_EmptyCsv_ReturnsError()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var request = new ImportContactsRequest
        {
            CsvContent = "",
            SkipDuplicates = false
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().Contain("CSV file is empty");
        result.SuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_WithSkipDuplicates_SkipsDuplicateEmails()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var existingContact = new Contact
        {
            FirstName = "Existing",
            LastName = "Contact",
            Email = "existing@example.com"
        };
        _context.Contacts.Add(existingContact);
        await _context.SaveChangesAsync();

        var csvContent = @"Imię;Nazwisko;Dział;Telefon służbowy;Telefon komórkowy;Adres e-mail;Nazwa wyświetlana;Stanowisko;Lokalizacja
""Existing"";""Contact"";""IT"";"""";"""";""existing@example.com"";"""";"""";""""
""New"";""Contact"";""IT"";"""";"""";""new@example.com"";"""";"""";""""";

        var request = new ImportContactsRequest
        {
            CsvContent = csvContent,
            SkipDuplicates = true
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
        result.ImportedContacts.Should().HaveCount(1);
        result.ImportedContacts.First().Email.Should().Be("new@example.com");
    }

    [Fact]
    public async Task HandleAsync_WithoutSkipDuplicates_ImportsAllContacts()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var csvContent = @"Imię;Nazwisko;Dział;Telefon służbowy;Telefon komórkowy;Adres e-mail;Nazwa wyświetlana;Stanowisko;Lokalizacja
""John"";""Doe"";""IT"";"""";"""";""john@example.com"";"""";"""";""""
""Jane"";""Smith"";""Sales"";"""";"""";""jane@example.com"";"""";"""";""""";

        var request = new ImportContactsRequest
        {
            CsvContent = csvContent,
            SkipDuplicates = false
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(2);
        result.SkippedCount.Should().Be(0);
        result.ImportedContacts.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_InvalidCsvLine_SkipsAndReportsError()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var csvContent = @"Imię;Nazwisko;Dział;Telefon służbowy;Telefon komórkowy;Adres e-mail;Nazwa wyświetlana;Stanowisko;Lokalizacja
""John"";""Doe"";""IT"";"""";"""";""john@example.com"";"""";"""";""""
Invalid line without enough fields";

        var request = new ImportContactsRequest
        {
            CsvContent = csvContent,
            SkipDuplicates = false
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_EmptyFirstNameOrLastName_SkipsRow()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var csvContent = @"Imię;Nazwisko;Dział;Telefon służbowy;Telefon komórkowy;Adres e-mail;Nazwa wyświetlana;Stanowisko;Lokalizacja
"""";""Doe"";""IT"";"""";"""";""test@example.com"";"""";"""";""""
""John"";"""";""IT"";"""";"""";""test2@example.com"";"""";"""";""""";

        var request = new ImportContactsRequest
        {
            CsvContent = csvContent,
            SkipDuplicates = false
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(0);
        result.SkippedCount.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_ExtractsCompanyFromDisplayName()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var csvContent = @"Imię;Nazwisko;Dział;Telefon służbowy;Telefon komórkowy;Adres e-mail;Nazwa wyświetlana;Stanowisko;Lokalizacja
""John"";""Doe"";""IT"";"""";"""";""john@example.com"";""John Doe - Tech Corp"";""Developer"";""Warsaw""";

        var request = new ImportContactsRequest
        {
            CsvContent = csvContent,
            SkipDuplicates = false
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Email == "john@example.com");
        contact.Should().NotBeNull();
        contact!.Company.Should().Be("Tech Corp");
        contact.DisplayName.Should().Be("John Doe - Tech Corp");
    }

    [Fact]
    public async Task HandleAsync_NoUserId_UsesSystem()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns((string?)null);

        var csvContent = @"Imię;Nazwisko;Dział;Telefon służbowy;Telefon komórkowy;Adres e-mail;Nazwa wyświetlana;Stanowisko;Lokalizacja
""John"";""Doe"";""IT"";"""";"""";""john@example.com"";"""";"""";""""";

        var request = new ImportContactsRequest
        {
            CsvContent = csvContent,
            SkipDuplicates = false
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Email == "john@example.com");
        contact!.CreatedByUserId.Should().Be("system");
    }
}

