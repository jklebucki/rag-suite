using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.SearchContacts;

namespace RAG.Tests.AddressBook;

public class SearchContactsServiceTests : IDisposable
{
    private readonly AddressBookDbContext _context;
    private readonly SearchContactsService _service;

    public SearchContactsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AddressBookDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AddressBookDbContext(options);
        _service = new SearchContactsService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SearchAsync_WithMatchingFirstName_ReturnsMatchingContacts()
    {
        // Arrange
        var contact1 = new Contact { FirstName = "John", LastName = "Doe", Email = "john@example.com", IsActive = true };
        var contact2 = new Contact { FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", IsActive = true };
        var contact3 = new Contact { FirstName = "Johnny", LastName = "Johnson", Email = "johnny@example.com", IsActive = true };

        _context.Contacts.AddRange(contact1, contact2, contact3);
        await _context.SaveChangesAsync();

        var request = new SearchContactsRequest { SearchTerm = "John" };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Results.Should().Contain(r => r.FirstName == "John");
        result.Results.Should().Contain(r => r.FirstName == "Johnny");
    }

    [Fact]
    public async Task SearchAsync_WithMatchingLastName_ReturnsMatchingContacts()
    {
        // Arrange
        var contact1 = new Contact { FirstName = "John", LastName = "Doe", Email = "john@example.com", IsActive = true };
        var contact2 = new Contact { FirstName = "Jane", LastName = "Doe", Email = "jane@example.com", IsActive = true };
        var contact3 = new Contact { FirstName = "Bob", LastName = "Smith", Email = "bob@example.com", IsActive = true };

        _context.Contacts.AddRange(contact1, contact2, contact3);
        await _context.SaveChangesAsync();

        var request = new SearchContactsRequest { SearchTerm = "Doe" };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(2);
        result.Results.Should().OnlyContain(r => r.LastName == "Doe");
    }

    [Fact]
    public async Task SearchAsync_WithMatchingEmail_ReturnsMatchingContacts()
    {
        // Arrange
        var contact1 = new Contact { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", IsActive = true };
        var contact2 = new Contact { FirstName = "Jane", LastName = "Smith", Email = "jane.smith@test.com", IsActive = true };

        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        var request = new SearchContactsRequest { SearchTerm = "example.com" };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(1);
        result.Results.First().Email.Should().Be("john.doe@example.com");
    }

    [Fact]
    public async Task SearchAsync_WithMatchingDepartment_ReturnsMatchingContacts()
    {
        // Arrange
        var contact1 = new Contact { FirstName = "John", LastName = "Doe", Department = "IT Department", IsActive = true };
        var contact2 = new Contact { FirstName = "Jane", LastName = "Brown", Department = "Sales", IsActive = true };

        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        var request = new SearchContactsRequest { SearchTerm = "IT" };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().Contain(r => r.Department == "IT Department");
        result.Results.Should().NotContain(r => r.Department == "Sales");
    }

    [Fact]
    public async Task SearchAsync_WithMatchingPosition_ReturnsMatchingContacts()
    {
        // Arrange
        var contact1 = new Contact { FirstName = "John", LastName = "Doe", Position = "Senior Developer", IsActive = true };
        var contact2 = new Contact { FirstName = "Jane", LastName = "Smith", Position = "Manager", IsActive = true };

        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        var request = new SearchContactsRequest { SearchTerm = "Developer" };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(1);
        result.Results.First().Position.Should().Be("Senior Developer");
    }

    [Fact]
    public async Task SearchAsync_WithMatchingLocation_ReturnsMatchingContacts()
    {
        // Arrange
        var contact1 = new Contact { FirstName = "John", LastName = "Doe", Location = "Warsaw, Poland", IsActive = true };
        var contact2 = new Contact { FirstName = "Jane", LastName = "Smith", Location = "Krakow", IsActive = true };

        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        var request = new SearchContactsRequest { SearchTerm = "Warsaw" };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(1);
        result.Results.First().FirstName.Should().Be("John");
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive_ReturnsMatchingContacts()
    {
        // Arrange
        var contact = new Contact { FirstName = "John", LastName = "Doe", Email = "john@example.com", IsActive = true };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var request = new SearchContactsRequest { SearchTerm = "JOHN" };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_OnlyReturnsActiveContacts()
    {
        // Arrange
        var active = new Contact { FirstName = "Active", LastName = "User", Email = "active@example.com", IsActive = true };
        var inactive = new Contact { FirstName = "Inactive", LastName = "User", Email = "inactive@example.com", IsActive = false };

        _context.Contacts.AddRange(active, inactive);
        await _context.SaveChangesAsync();

        var request = new SearchContactsRequest { SearchTerm = "User" };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(1);
        result.Results.First().FirstName.Should().Be("Active");
    }

    [Fact]
    public async Task SearchAsync_EmptySearchTerm_ReturnsEmptyResults()
    {
        // Arrange
        var contact = new Contact { FirstName = "John", LastName = "Doe", IsActive = true };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var request = new SearchContactsRequest { SearchTerm = "" };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_WhitespaceSearchTerm_ReturnsEmptyResults()
    {
        // Arrange
        var contact = new Contact { FirstName = "John", LastName = "Doe", IsActive = true };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var request = new SearchContactsRequest { SearchTerm = "   " };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_NoMatches_ReturnsEmptyResults()
    {
        // Arrange
        var contact = new Contact { FirstName = "John", LastName = "Doe", IsActive = true };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var request = new SearchContactsRequest { SearchTerm = "NonExistent" };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_OrdersByLastNameThenFirstName()
    {
        // Arrange
        var contact1 = new Contact { FirstName = "Alice", LastName = "Zebra", IsActive = true };
        var contact2 = new Contact { FirstName = "Bob", LastName = "Apple", IsActive = true };
        var contact3 = new Contact { FirstName = "Charlie", LastName = "Apple", IsActive = true };

        _context.Contacts.AddRange(contact1, contact2, contact3);
        await _context.SaveChangesAsync();

        var request = new SearchContactsRequest { SearchTerm = "A" };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(3);
        result.Results[0].LastName.Should().Be("Apple");
        result.Results[0].FirstName.Should().Be("Bob");
        result.Results[1].LastName.Should().Be("Apple");
        result.Results[1].FirstName.Should().Be("Charlie");
        result.Results[2].LastName.Should().Be("Zebra");
    }

    [Fact]
    public async Task SearchAsync_ReturnsCorrectDtoProperties()
    {
        // Arrange
        var contact = new Contact
        {
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            Department = "IT",
            Position = "Developer",
            Email = "test@example.com",
            MobilePhone = "+48123456789",
            IsActive = true
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var request = new SearchContactsRequest { SearchTerm = "Test" };

        // Act
        var result = await _service.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        var searchResult = result.Results.First();
        searchResult.Id.Should().Be(contact.Id);
        searchResult.FirstName.Should().Be("Test");
        searchResult.LastName.Should().Be("User");
        searchResult.DisplayName.Should().Be("Test User");
        searchResult.Department.Should().Be("IT");
        searchResult.Position.Should().Be("Developer");
        searchResult.Email.Should().Be("test@example.com");
        searchResult.MobilePhone.Should().Be("+48123456789");
    }
}

