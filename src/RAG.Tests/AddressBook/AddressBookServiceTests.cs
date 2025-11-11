using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Services;

namespace RAG.Tests.AddressBook;

public class AddressBookServiceTests : IDisposable
{
    private readonly AddressBookDbContext _context;
    private readonly AddressBookService _service;

    public AddressBookServiceTests()
    {
        var options = new DbContextOptionsBuilder<AddressBookDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AddressBookDbContext(options);
        _service = new AddressBookService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateContactAsync_ValidContact_CreatesAndReturnsContact()
    {
        // Arrange
        var contact = new Contact
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            IsActive = true
        };

        // Act
        var result = await _service.CreateContactAsync(contact);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Email.Should().Be("john.doe@example.com");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetContactByIdAsync_ExistingContact_ReturnsContact()
    {
        // Arrange
        var contact = new Contact
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com"
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetContactByIdAsync(contact.Id);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Smith");
        result.Email.Should().Be("jane.smith@example.com");
    }

    [Fact]
    public async Task GetContactByIdAsync_NonExistentContact_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetContactByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllContactsAsync_WithActiveAndInactive_ReturnsOnlyActiveByDefault()
    {
        // Arrange
        var activeContact = new Contact { FirstName = "Active", LastName = "User", IsActive = true };
        var inactiveContact = new Contact { FirstName = "Inactive", LastName = "User", IsActive = false };

        _context.Contacts.AddRange(activeContact, inactiveContact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllContactsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllContactsAsync_IncludeInactive_ReturnsAllContacts()
    {
        // Arrange
        var activeContact = new Contact { FirstName = "Active", LastName = "User", IsActive = true };
        var inactiveContact = new Contact { FirstName = "Inactive", LastName = "User", IsActive = false };

        _context.Contacts.AddRange(activeContact, inactiveContact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllContactsAsync(includeInactive: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateContactAsync_ExistingContact_UpdatesAndReturnsContact()
    {
        // Arrange
        var contact = new Contact
        {
            FirstName = "Original",
            LastName = "Name",
            Email = "original@example.com"
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var updatedContact = new Contact
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@example.com"
        };

        // Act
        var result = await _service.UpdateContactAsync(contact.Id, updatedContact);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Updated");
        result.Email.Should().Be("updated@example.com");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateContactAsync_NonExistentContact_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var contact = new Contact { FirstName = "Test", LastName = "User" };

        // Act
        var result = await _service.UpdateContactAsync(nonExistentId, contact);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteContactAsync_ExistingContact_ReturnsTrue()
    {
        // Arrange
        var contact = new Contact { FirstName = "ToDelete", LastName = "User" };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteContactAsync(contact.Id);

        // Assert
        result.Should().BeTrue();
        var deletedContact = await _context.Contacts.FindAsync(contact.Id);
        deletedContact.Should().BeNull();
    }

    [Fact]
    public async Task DeleteContactAsync_NonExistentContact_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteContactAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SearchContactsAsync_WithMatchingTerm_ReturnsMatchingContacts()
    {
        // Arrange
        var contact1 = new Contact { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        var contact2 = new Contact { FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" };
        var contact3 = new Contact { FirstName = "Bob", LastName = "Johnson", Email = "bob@example.com" };

        _context.Contacts.AddRange(contact1, contact2, contact3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SearchContactsAsync("John");

        // Assert
        result.Should().HaveCount(2); // John Doe and Bob Johnson
        result.Should().Contain(c => c.FirstName == "John" || c.LastName == "Johnson");
    }

    [Fact]
    public async Task SearchContactsAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var contact = new Contact { FirstName = "John", LastName = "Doe" };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SearchContactsAsync("NonExistent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateContactAsync_WithTags_CreatesContactWithTags()
    {
        // Arrange
        var contact = new Contact
        {
            FirstName = "Tagged",
            LastName = "Contact",
            Tags = new List<ContactTag>
            {
                new ContactTag { TagName = "Developer" },
                new ContactTag { TagName = "Team Lead" }
            }
        };

        // Act
        var result = await _service.CreateContactAsync(contact);

        // Assert
        result.Should().NotBeNull();
        result.Tags.Should().HaveCount(2);
        result.Tags.Should().Contain(t => t.TagName == "Developer");
        result.Tags.Should().Contain(t => t.TagName == "Team Lead");
    }
}

