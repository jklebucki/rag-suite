using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.DeleteContact;

namespace RAG.Tests.AddressBook;

public class DeleteContactHandlerTests : IDisposable
{
    private readonly AddressBookDbContext _context;
    private readonly DeleteContactHandler _handler;

    public DeleteContactHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AddressBookDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AddressBookDbContext(options);
        _handler = new DeleteContactHandler(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task HandleAsync_ExistingContact_DeletesAndReturnsTrue()
    {
        // Arrange
        var contact = new Contact
        {
            FirstName = "ToDelete",
            LastName = "Contact",
            Email = "delete@example.com"
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var contactId = contact.Id;

        // Act
        var result = await _handler.HandleAsync(contactId);

        // Assert
        result.Should().BeTrue();

        var deletedContact = await _context.Contacts.FindAsync(contactId);
        deletedContact.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_NonExistentContact_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _handler.HandleAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_DeletesContactWithTags()
    {
        // Arrange
        var contact = new Contact
        {
            FirstName = "Tagged",
            LastName = "Contact",
            Email = "tagged@example.com",
            Tags = new List<ContactTag>
            {
                new ContactTag { TagName = "Tag1" },
                new ContactTag { TagName = "Tag2" }
            }
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var contactId = contact.Id;
        var tagCount = await _context.ContactTags.CountAsync(t => t.ContactId == contactId);
        tagCount.Should().Be(2);

        // Act
        var result = await _handler.HandleAsync(contactId);

        // Assert
        result.Should().BeTrue();

        var deletedContact = await _context.Contacts.FindAsync(contactId);
        deletedContact.Should().BeNull();

        // Tags should be cascade deleted
        var remainingTags = await _context.ContactTags.CountAsync(t => t.ContactId == contactId);
        remainingTags.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_MultipleContacts_DeletesOnlySpecified()
    {
        // Arrange
        var contact1 = new Contact
        {
            FirstName = "Contact",
            LastName = "One",
            Email = "one@example.com"
        };
        var contact2 = new Contact
        {
            FirstName = "Contact",
            LastName = "Two",
            Email = "two@example.com"
        };
        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(contact1.Id);

        // Assert
        result.Should().BeTrue();

        var deletedContact = await _context.Contacts.FindAsync(contact1.Id);
        deletedContact.Should().BeNull();

        var remainingContact = await _context.Contacts.FindAsync(contact2.Id);
        remainingContact.Should().NotBeNull();
        remainingContact!.Email.Should().Be("two@example.com");
    }
}

