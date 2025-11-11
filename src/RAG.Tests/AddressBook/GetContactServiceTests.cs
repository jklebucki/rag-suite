using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.GetContact;

namespace RAG.Tests.AddressBook;

public class GetContactServiceTests : IDisposable
{
    private readonly AddressBookDbContext _context;
    private readonly GetContactService _service;

    public GetContactServiceTests()
    {
        var options = new DbContextOptionsBuilder<AddressBookDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AddressBookDbContext(options);
        _service = new GetContactService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingContact_ReturnsContactWithTags()
    {
        // Arrange
        var contact = new Contact
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Department = "IT",
            Position = "Developer",
            Tags = new List<ContactTag>
            {
                new ContactTag { TagName = "Developer", Color = "#FF0000" },
                new ContactTag { TagName = "Team Lead" }
            }
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(contact.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(contact.Id);
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Email.Should().Be("john.doe@example.com");
        result.Department.Should().Be("IT");
        result.Position.Should().Be("Developer");
        result.Tags.Should().HaveCount(2);
        result.Tags.Should().Contain(t => t.TagName == "Developer" && t.Color == "#FF0000");
        result.Tags.Should().Contain(t => t.TagName == "Team Lead");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentContact_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ContactWithoutTags_ReturnsContactWithEmptyTags()
    {
        // Arrange
        var contact = new Contact
        {
            FirstName = "NoTags",
            LastName = "Contact",
            Email = "notags@example.com"
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(contact.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAllContactFields()
    {
        // Arrange
        var contact = new Contact
        {
            FirstName = "Full",
            LastName = "Contact",
            DisplayName = "Full Contact",
            Department = "Sales",
            Position = "Manager",
            Location = "Warsaw",
            Company = "Tech Corp",
            WorkPhone = "+48123456789",
            MobilePhone = "+48987654321",
            Email = "full@example.com",
            Notes = "Some notes",
            PhotoUrl = "https://example.com/photo.jpg",
            IsActive = true
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(contact.Id);

        // Assert
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Full Contact");
        result.Location.Should().Be("Warsaw");
        result.Company.Should().Be("Tech Corp");
        result.WorkPhone.Should().Be("+48123456789");
        result.MobilePhone.Should().Be("+48987654321");
        result.Notes.Should().Be("Some notes");
        result.PhotoUrl.Should().Be("https://example.com/photo.jpg");
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

