using Microsoft.EntityFrameworkCore;
using Moq;
using RAG.AddressBook.Data;
using RAG.AddressBook.Features.CreateContact;
using RAG.Security.Services;

namespace RAG.Tests.AddressBook;

public class CreateContactHandlerTests : IDisposable
{
    private readonly AddressBookDbContext _context;
    private readonly Mock<IUserContextService> _mockUserContext;
    private readonly CreateContactHandler _handler;

    public CreateContactHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AddressBookDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AddressBookDbContext(options);
        _mockUserContext = new Mock<IUserContextService>();
        _handler = new CreateContactHandler(_context, _mockUserContext.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesContact()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var request = new CreateContactRequest
        {
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "Johnny",
            Department = "IT",
            Position = "Developer",
            Location = "Warsaw",
            Company = "Tech Corp",
            WorkPhone = "+48123456789",
            MobilePhone = "+48987654321",
            Email = "john.doe@example.com",
            Notes = "Test contact",
            PhotoUrl = "https://example.com/photo.jpg",
            Tags = new List<string> { "Developer", "Team Lead" }
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");

        var contact = await _context.Contacts
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.Id == result.Id);

        contact.Should().NotBeNull();
        contact!.FirstName.Should().Be("John");
        contact.LastName.Should().Be("Doe");
        contact.Email.Should().Be("john.doe@example.com");
        contact.CreatedByUserId.Should().Be("user123");
        contact.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        contact.Tags.Should().HaveCount(2);
        contact.Tags.Should().Contain(t => t.TagName == "Developer");
        contact.Tags.Should().Contain(t => t.TagName == "Team Lead");
    }

    [Fact]
    public async Task HandleAsync_NoUserId_UsesSystem()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns((string?)null);

        var request = new CreateContactRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com"
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        var contact = await _context.Contacts.FindAsync(result.Id);
        contact.Should().NotBeNull();
        contact!.CreatedByUserId.Should().Be("system");
    }

    [Fact]
    public async Task HandleAsync_WithTags_CreatesContactWithTags()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var request = new CreateContactRequest
        {
            FirstName = "Tagged",
            LastName = "Contact",
            Email = "tagged@example.com",
            Tags = new List<string> { "Tag1", "Tag2", "Tag3" }
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        var contact = await _context.Contacts
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.Id == result.Id);

        contact!.Tags.Should().HaveCount(3);
        contact.Tags.Select(t => t.TagName).Should().Contain(new[] { "Tag1", "Tag2", "Tag3" });
    }

    [Fact]
    public async Task HandleAsync_WithoutTags_CreatesContactWithoutTags()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        var request = new CreateContactRequest
        {
            FirstName = "NoTags",
            LastName = "Contact",
            Email = "notags@example.com"
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        var contact = await _context.Contacts
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.Id == result.Id);

        contact!.Tags.Should().BeEmpty();
    }
}

