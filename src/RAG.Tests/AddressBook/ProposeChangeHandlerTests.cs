using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.ProposeChange;
using RAG.AddressBook.Services;

namespace RAG.Tests.AddressBook;

public class ProposeChangeHandlerTests : IDisposable
{
    private readonly AddressBookDbContext _context;
    private readonly Mock<IAddressBookAuthorizationService> _mockAuthService;
    private readonly ProposeChangeHandler _handler;

    public ProposeChangeHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AddressBookDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AddressBookDbContext(options);
        _mockAuthService = new Mock<IAddressBookAuthorizationService>();
        _handler = new ProposeChangeHandler(_context, _mockAuthService.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task HandleAsync_CreateProposal_RegularUser_CreatesProposal()
    {
        // Arrange
        _mockAuthService.Setup(a => a.CanModifyContacts()).Returns(false);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("user123");
        _mockAuthService.Setup(a => a.GetCurrentUserName()).Returns("Regular User");

        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = new ContactDataDto
            {
                FirstName = "New",
                LastName = "Contact",
                Email = "new@example.com"
            },
            Reason = "New employee"
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ProposalId.Should().NotBeEmpty();
        result.ProposalType.Should().Be(ChangeProposalType.Create);
        result.Status.Should().Be(ProposalStatus.Pending);
        result.Message.Should().Contain("submitted for review");

        var proposal = await _context.ContactChangeProposals.FindAsync(result.ProposalId);
        proposal.Should().NotBeNull();
        proposal!.ProposedByUserId.Should().Be("user123");
        proposal.ProposedByUserName.Should().Be("Regular User");
    }

    [Fact]
    public async Task HandleAsync_UpdateProposal_RegularUser_CreatesProposal()
    {
        // Arrange
        _mockAuthService.Setup(a => a.CanModifyContacts()).Returns(false);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("user123");

        var contact = new Contact { FirstName = "Existing", LastName = "Contact", Email = "existing@example.com" };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var request = new ProposeContactChangeRequest
        {
            ContactId = contact.Id,
            ProposalType = ChangeProposalType.Update,
            ProposedData = new ContactDataDto
            {
                FirstName = "Updated",
                LastName = "Contact",
                Email = "updated@example.com"
            },
            Reason = "Email changed"
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ProposalType.Should().Be(ChangeProposalType.Update);
        result.Status.Should().Be(ProposalStatus.Pending);
    }

    [Fact]
    public async Task HandleAsync_DeleteProposal_RegularUser_CreatesProposal()
    {
        // Arrange
        _mockAuthService.Setup(a => a.CanModifyContacts()).Returns(false);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("user123");

        var contact = new Contact { FirstName = "ToDelete", LastName = "Contact" };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var request = new ProposeContactChangeRequest
        {
            ContactId = contact.Id,
            ProposalType = ChangeProposalType.Delete,
            ProposedData = new ContactDataDto(),
            Reason = "No longer works here"
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ProposalType.Should().Be(ChangeProposalType.Delete);
        result.Status.Should().Be(ProposalStatus.Pending);
    }

    [Fact]
    public async Task HandleAsync_AdminUser_ThrowsException()
    {
        // Arrange
        _mockAuthService.Setup(a => a.CanModifyContacts()).Returns(true);

        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = new ContactDataDto { FirstName = "Test", LastName = "User" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(request));
    }

    [Fact]
    public async Task HandleAsync_UpdateProposal_NonExistentContact_ThrowsException()
    {
        // Arrange
        _mockAuthService.Setup(a => a.CanModifyContacts()).Returns(false);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("user123");

        var nonExistentId = Guid.NewGuid();
        var request = new ProposeContactChangeRequest
        {
            ContactId = nonExistentId,
            ProposalType = ChangeProposalType.Update,
            ProposedData = new ContactDataDto { FirstName = "Test", LastName = "User" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(request));
    }

    [Fact]
    public async Task HandleAsync_NoUserId_UsesSystem()
    {
        // Arrange
        _mockAuthService.Setup(a => a.CanModifyContacts()).Returns(false);
        // Note: AddressBookAuthorizationService.GetCurrentUserId() returns "system" when null
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("system");
        _mockAuthService.Setup(a => a.GetCurrentUserName()).Returns((string?)null);

        var request = new ProposeContactChangeRequest
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = new ContactDataDto { FirstName = "Test", LastName = "User" }
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        var proposal = await _context.ContactChangeProposals.FindAsync(result.ProposalId);
        proposal!.ProposedByUserId.Should().Be("system");
    }
}

