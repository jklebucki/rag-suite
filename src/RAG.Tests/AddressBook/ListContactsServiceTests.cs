using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.ListContacts;

namespace RAG.Tests.AddressBook;

public class ListContactsServiceTests : IDisposable
{
    private readonly AddressBookDbContext _context;
    private readonly ListContactsService _service;

    public ListContactsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AddressBookDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AddressBookDbContext(options);
        _service = new ListContactsService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task ListAsync_WithNoFilters_ReturnsAllActiveContacts()
    {
        // Arrange
        var active1 = new Contact { FirstName = "Active", LastName = "One", IsActive = true };
        var active2 = new Contact { FirstName = "Active", LastName = "Two", IsActive = true };
        var inactive = new Contact { FirstName = "Inactive", LastName = "Three", IsActive = false };

        _context.Contacts.AddRange(active1, active2, inactive);
        await _context.SaveChangesAsync();

        var request = new ListContactsRequest
        {
            IncludeInactive = false
        };

        // Act
        var result = await _service.ListAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Contacts.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Contacts.Should().OnlyContain(c => c.IsActive);
    }

    [Fact]
    public async Task ListAsync_WithIncludeInactive_ReturnsAllContacts()
    {
        // Arrange
        var active = new Contact { FirstName = "Active", LastName = "One", IsActive = true };
        var inactive = new Contact { FirstName = "Inactive", LastName = "Two", IsActive = false };

        _context.Contacts.AddRange(active, inactive);
        await _context.SaveChangesAsync();

        var request = new ListContactsRequest
        {
            IncludeInactive = true
        };

        // Act
        var result = await _service.ListAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Contacts.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListAsync_WithDepartmentFilter_ReturnsFilteredContacts()
    {
        // Arrange
        var itContact = new Contact { FirstName = "IT", LastName = "User", Department = "IT", IsActive = true };
        var salesContact = new Contact { FirstName = "Sales", LastName = "User", Department = "Sales", IsActive = true };
        var anotherItContact = new Contact { FirstName = "Another", LastName = "IT", Department = "IT", IsActive = true };

        _context.Contacts.AddRange(itContact, salesContact, anotherItContact);
        await _context.SaveChangesAsync();

        var request = new ListContactsRequest
        {
            Department = "IT"
        };

        // Act
        var result = await _service.ListAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Contacts.Should().HaveCount(2);
        result.Contacts.Should().OnlyContain(c => c.Department == "IT");
    }

    [Fact]
    public async Task ListAsync_WithLocationFilter_ReturnsFilteredContacts()
    {
        // Arrange
        var warsawContact = new Contact { FirstName = "Warsaw", LastName = "User", Location = "Warsaw", IsActive = true };
        var krakowContact = new Contact { FirstName = "Krakow", LastName = "User", Location = "Krakow", IsActive = true };
        var anotherWarsawContact = new Contact { FirstName = "Another", LastName = "Warsaw", Location = "Warsaw", IsActive = true };

        _context.Contacts.AddRange(warsawContact, krakowContact, anotherWarsawContact);
        await _context.SaveChangesAsync();

        var request = new ListContactsRequest
        {
            Location = "Warsaw"
        };

        // Act
        var result = await _service.ListAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Contacts.Should().HaveCount(2);
        result.Contacts.Should().OnlyContain(c => c.Location == "Warsaw");
    }

    [Fact]
    public async Task ListAsync_WithMultipleFilters_ReturnsFilteredContacts()
    {
        // Arrange
        var matching = new Contact { FirstName = "Match", LastName = "One", Department = "IT", Location = "Warsaw", IsActive = true };
        var wrongDept = new Contact { FirstName = "Wrong", LastName = "Dept", Department = "Sales", Location = "Warsaw", IsActive = true };
        var wrongLocation = new Contact { FirstName = "Wrong", LastName = "Loc", Department = "IT", Location = "Krakow", IsActive = true };

        _context.Contacts.AddRange(matching, wrongDept, wrongLocation);
        await _context.SaveChangesAsync();

        var request = new ListContactsRequest
        {
            Department = "IT",
            Location = "Warsaw"
        };

        // Act
        var result = await _service.ListAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Contacts.Should().HaveCount(1);
        result.Contacts.First().FirstName.Should().Be("Match");
    }

    [Fact]
    public async Task ListAsync_OrdersByLastNameThenFirstName()
    {
        // Arrange
        var contact1 = new Contact { FirstName = "Alice", LastName = "Zebra", IsActive = true };
        var contact2 = new Contact { FirstName = "Bob", LastName = "Apple", IsActive = true };
        var contact3 = new Contact { FirstName = "Charlie", LastName = "Apple", IsActive = true };

        _context.Contacts.AddRange(contact1, contact2, contact3);
        await _context.SaveChangesAsync();

        var request = new ListContactsRequest();

        // Act
        var result = await _service.ListAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Contacts.Should().HaveCount(3);
        result.Contacts[0].LastName.Should().Be("Apple");
        result.Contacts[0].FirstName.Should().Be("Bob");
        result.Contacts[1].LastName.Should().Be("Apple");
        result.Contacts[1].FirstName.Should().Be("Charlie");
        result.Contacts[2].LastName.Should().Be("Zebra");
    }

    [Fact]
    public async Task ListAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var request = new ListContactsRequest();

        // Act
        var result = await _service.ListAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Contacts.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ListAsync_ReturnsCorrectDtoProperties()
    {
        // Arrange
        var contact = new Contact
        {
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            Department = "IT",
            Position = "Developer",
            Location = "Warsaw",
            Email = "test@example.com",
            MobilePhone = "+48123456789",
            IsActive = true
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var request = new ListContactsRequest();

        // Act
        var result = await _service.ListAsync(request);

        // Assert
        result.Should().NotBeNull();
        var contactDto = result.Contacts.First();
        contactDto.Id.Should().Be(contact.Id);
        contactDto.FirstName.Should().Be("Test");
        contactDto.LastName.Should().Be("User");
        contactDto.DisplayName.Should().Be("Test User");
        contactDto.Department.Should().Be("IT");
        contactDto.Position.Should().Be("Developer");
        contactDto.Location.Should().Be("Warsaw");
        contactDto.Email.Should().Be("test@example.com");
        contactDto.MobilePhone.Should().Be("+48123456789");
        contactDto.IsActive.Should().BeTrue();
    }
}

