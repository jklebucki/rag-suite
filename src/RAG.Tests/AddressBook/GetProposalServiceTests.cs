using Microsoft.EntityFrameworkCore;
using Moq;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.GetProposal;
using RAG.AddressBook.Features.ProposeChange;
using RAG.AddressBook.Services;
using System.Text.Json;

namespace RAG.Tests.AddressBook;

public class GetProposalServiceTests : IDisposable
{
    private readonly AddressBookDbContext _context;
    private readonly Mock<IAddressBookAuthorizationService> _mockAuthService;
    private readonly GetProposalService _service;

    public GetProposalServiceTests()
    {
        var options = new DbContextOptionsBuilder<AddressBookDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AddressBookDbContext(options);
        _mockAuthService = new Mock<IAddressBookAuthorizationService>();
        _service = new GetProposalService(_context, _mockAuthService.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingProposal_ReturnsProposal()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(true);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("admin123");

        var contact = new Contact { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var proposedData = new ContactDataDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@example.com"
        };

        var proposal = new ContactChangeProposal
        {
            ContactId = contact.Id,
            ProposalType = ChangeProposalType.Update,
            ProposedData = JsonSerializer.Serialize(proposedData),
            Reason = "Update email",
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user123",
            ProposedByUserName = "Regular User",
            ProposedAt = DateTime.UtcNow
        };
        _context.ContactChangeProposals.Add(proposal);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(proposal.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(proposal.Id);
        result.ProposalType.Should().Be(ChangeProposalType.Update);
        result.Status.Should().Be(ProposalStatus.Pending);
        result.ProposedData.FirstName.Should().Be("Updated");
        result.ProposedData.LastName.Should().Be("Name");
        result.Reason.Should().Be("Update email");
        result.ProposedByUserId.Should().Be("user123");
        result.CurrentContact.Should().NotBeNull();
        result.CurrentContact!.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentProposal_ReturnsNull()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(true);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_RegularUserOwnProposal_ReturnsProposal()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(false);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("user123");

        var proposedData = new ContactDataDto
        {
            FirstName = "New",
            LastName = "Contact"
        };

        var proposal = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = JsonSerializer.Serialize(proposedData),
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user123",
            ProposedAt = DateTime.UtcNow
        };
        _context.ContactChangeProposals.Add(proposal);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(proposal.Id);

        // Assert
        result.Should().NotBeNull();
        result!.ProposedByUserId.Should().Be("user123");
    }

    [Fact]
    public async Task GetByIdAsync_RegularUserOtherUserProposal_ReturnsNull()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(false);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("user123");

        var proposal = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = JsonSerializer.Serialize(new ContactDataDto { FirstName = "Test", LastName = "User" }),
            Status = ProposalStatus.Pending,
            ProposedByUserId = "otheruser",
            ProposedAt = DateTime.UtcNow
        };
        _context.ContactChangeProposals.Add(proposal);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(proposal.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_CreateProposal_ReturnsProposalWithoutContact()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(true);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("admin123");

        var proposedData = new ContactDataDto
        {
            FirstName = "New",
            LastName = "Contact",
            Email = "new@example.com"
        };

        var proposal = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = JsonSerializer.Serialize(proposedData),
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user123",
            ProposedAt = DateTime.UtcNow
        };
        _context.ContactChangeProposals.Add(proposal);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(proposal.Id);

        // Assert
        result.Should().NotBeNull();
        result!.ContactId.Should().BeNull();
        result.CurrentContact.Should().BeNull();
        result.ProposedData.FirstName.Should().Be("New");
    }
}

