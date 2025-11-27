using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.UpdateContact;
using RAG.Security.Services;

namespace RAG.Tests.AddressBook;

public class UpdateContactHandlerTests : IDisposable
{
    private readonly AddressBookDbContext _context;
    private readonly Mock<IUserContextService> _mockUserContext;
    private readonly UpdateContactHandler _handler;

    public UpdateContactHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AddressBookDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AddressBookDbContext(options);
        _mockUserContext = new Mock<IUserContextService>();
        _handler = new UpdateContactHandler(_context, _mockUserContext.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task HandleAsync_ExistingContact_UpdatesAndReturnsTrue()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var contact = new Contact
        {
            FirstName = "Original",
            LastName = "Name",
            Email = "original@example.com",
            IsActive = true
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var request = new UpdateContactRequest
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@example.com",
            Department = "IT",
            Position = "Senior Developer",
            IsActive = true
        };

        // Act
        var result = await _handler.HandleAsync(contact.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Updated");
        result.Email.Should().Be("updated@example.com");
        result.Department.Should().Be("IT");
        result.Position.Should().Be("Senior Developer");

        var updatedContact = await _context.Contacts.FindAsync(contact.Id);
        updatedContact.Should().NotBeNull();
        updatedContact!.FirstName.Should().Be("Updated");
        updatedContact.Email.Should().Be("updated@example.com");
        updatedContact.Department.Should().Be("IT");
        updatedContact.Position.Should().Be("Senior Developer");
        updatedContact.UpdatedByUserId.Should().Be("user123");
        updatedContact.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandleAsync_NonExistentContact_ReturnsNull()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var nonExistentId = Guid.NewGuid();
        var request = new UpdateContactRequest
        {
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var result = await _handler.HandleAsync(nonExistentId, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_NoUserId_UsesSystem()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns((string?)null);

        var contact = new Contact
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var request = new UpdateContactRequest
        {
            FirstName = "Updated",
            LastName = "User"
        };

        // Act
        var result = await _handler.HandleAsync(contact.Id, request);

        // Assert
        result.Should().NotBeNull();

        var updatedContact = await _context.Contacts.FindAsync(contact.Id);
        updatedContact!.UpdatedByUserId.Should().Be("system");
    }

    [Fact]
    public async Task HandleAsync_UpdatesAllFields()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var contact = new Contact
        {
            FirstName = "Old",
            LastName = "Name",
            Email = "old@example.com"
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var request = new UpdateContactRequest
        {
            FirstName = "New",
            LastName = "Name",
            DisplayName = "New Display",
            Department = "Sales",
            Position = "Manager",
            Location = "Warsaw",
            Company = "New Corp",
            WorkPhone = "+48123456789",
            MobilePhone = "+48987654321",
            Email = "new@example.com",
            Notes = "Updated notes",
            PhotoUrl = "https://example.com/new-photo.jpg",
            IsActive = false
        };

        // Act
        var result = await _handler.HandleAsync(contact.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("New");
        result.DisplayName.Should().Be("New Display");
        result.Department.Should().Be("Sales");
        result.Position.Should().Be("Manager");
        result.Location.Should().Be("Warsaw");
        result.Company.Should().Be("New Corp");
        result.WorkPhone.Should().Be("+48123456789");
        result.MobilePhone.Should().Be("+48987654321");
        result.Email.Should().Be("new@example.com");
        result.Notes.Should().Be("Updated notes");
        result.PhotoUrl.Should().Be("https://example.com/new-photo.jpg");
        result.IsActive.Should().Be(false);

        var updatedContact = await _context.Contacts.FindAsync(contact.Id);
        updatedContact!.FirstName.Should().Be("New");
        updatedContact.DisplayName.Should().Be("New Display");
        updatedContact.Department.Should().Be("Sales");
        updatedContact.Position.Should().Be("Manager");
        updatedContact.Location.Should().Be("Warsaw");
        updatedContact.Company.Should().Be("New Corp");
        updatedContact.WorkPhone.Should().Be("+48123456789");
        updatedContact.MobilePhone.Should().Be("+48987654321");
        updatedContact.Email.Should().Be("new@example.com");
        updatedContact.Notes.Should().Be("Updated notes");
        updatedContact.PhotoUrl.Should().Be("https://example.com/new-photo.jpg");
        updatedContact.IsActive.Should().Be(false);
    }

    [Fact]
    public async Task HandleAsync_UpdatesTimestamp()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var contact = new Contact
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var originalUpdatedAt = contact.UpdatedAt;

        var request = new UpdateContactRequest
        {
            FirstName = "Updated",
            LastName = "User"
        };

        // Act
        await _handler.HandleAsync(contact.Id, request);

        // Assert
        var updatedContact = await _context.Contacts.FindAsync(contact.Id);
        updatedContact!.UpdatedAt.Should().BeAfter(originalUpdatedAt ?? DateTime.MinValue);
    }
}

