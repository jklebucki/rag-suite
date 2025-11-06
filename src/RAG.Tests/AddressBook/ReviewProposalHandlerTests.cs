using Xunit;
using FluentAssertions;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.ProposeChange;
using RAG.AddressBook.Features.ReviewProposal;
using RAG.AddressBook.Services;
using Moq;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RAG.Tests.AddressBook;

public class ReviewProposalHandlerTests : IDisposable
{
    private readonly AddressBookDbContext _context;
    private readonly Mock<IAddressBookAuthorizationService> _mockAuthService;
    private readonly ReviewProposalHandler _handler;

    public ReviewProposalHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AddressBookDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AddressBookDbContext(options);
        _mockAuthService = new Mock<IAddressBookAuthorizationService>();
        _handler = new ReviewProposalHandler(_context, _mockAuthService.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task HandleAsync_ApproveCreateProposal_AppliesChanges()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(true);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("admin123");
        _mockAuthService.Setup(a => a.GetCurrentUserName()).Returns("Admin User");

        var proposedData = new ContactDataDto
        {
            FirstName = "New",
            LastName = "Contact",
            Email = "new@example.com",
            Department = "IT"
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

        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Approved,
            ReviewComment = "Approved"
        };

        // Act
        var result = await _handler.HandleAsync(proposal.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ProposalStatus.Applied);
        result.Message.Should().Contain("approved and applied");

        var updatedProposal = await _context.ContactChangeProposals.FindAsync(proposal.Id);
        updatedProposal!.Status.Should().Be(ProposalStatus.Applied);
        updatedProposal.ReviewedByUserId.Should().Be("admin123");
        updatedProposal.ReviewComment.Should().Be("Approved");

        var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Email == "new@example.com");
        contact.Should().NotBeNull();
        contact!.FirstName.Should().Be("New");
        contact.LastName.Should().Be("Contact");
    }

    [Fact]
    public async Task HandleAsync_ApproveUpdateProposal_AppliesChanges()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(true);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("admin123");
        _mockAuthService.Setup(a => a.GetCurrentUserName()).Returns("Admin User");

        var contact = new Contact
        {
            FirstName = "Original",
            LastName = "Name",
            Email = "original@example.com"
        };
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
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user123",
            ProposedAt = DateTime.UtcNow
        };
        _context.ContactChangeProposals.Add(proposal);
        await _context.SaveChangesAsync();

        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Approved
        };

        // Act
        var result = await _handler.HandleAsync(proposal.Id, request);

        // Assert
        result.Status.Should().Be(ProposalStatus.Applied);

        var updatedContact = await _context.Contacts.FindAsync(contact.Id);
        updatedContact!.FirstName.Should().Be("Updated");
        updatedContact.Email.Should().Be("updated@example.com");
        // Note: ApplyUpdateAsync uses ProposedByUserId, not ReviewedByUserId
        updatedContact.UpdatedByUserId.Should().Be("user123");
    }

    [Fact]
    public async Task HandleAsync_ApproveDeleteProposal_DeletesContact()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(true);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("admin123");
        _mockAuthService.Setup(a => a.GetCurrentUserName()).Returns("Admin User");

        var contact = new Contact { FirstName = "ToDelete", LastName = "Contact" };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var proposal = new ContactChangeProposal
        {
            ContactId = contact.Id,
            ProposalType = ChangeProposalType.Delete,
            ProposedData = JsonSerializer.Serialize(new ContactDataDto()),
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user123",
            ProposedAt = DateTime.UtcNow
        };
        _context.ContactChangeProposals.Add(proposal);
        await _context.SaveChangesAsync();

        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Approved
        };

        // Act
        var result = await _handler.HandleAsync(proposal.Id, request);

        // Assert
        result.Status.Should().Be(ProposalStatus.Applied);

        var deletedContact = await _context.Contacts.FindAsync(contact.Id);
        deletedContact.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_RejectProposal_DoesNotApplyChanges()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(true);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("admin123");
        _mockAuthService.Setup(a => a.GetCurrentUserName()).Returns("Admin User");

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

        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Rejected,
            ReviewComment = "Not needed"
        };

        // Act
        var result = await _handler.HandleAsync(proposal.Id, request);

        // Assert
        result.Status.Should().Be(ProposalStatus.Rejected);
        result.Message.Should().Contain("rejected");

        var updatedProposal = await _context.ContactChangeProposals.FindAsync(proposal.Id);
        updatedProposal!.Status.Should().Be(ProposalStatus.Rejected);
        updatedProposal.ReviewComment.Should().Be("Not needed");

        var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.FirstName == "New");
        contact.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_RegularUser_ThrowsUnauthorizedException()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(false);

        var proposal = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = JsonSerializer.Serialize(new ContactDataDto { FirstName = "Test", LastName = "User" }),
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user123",
            ProposedAt = DateTime.UtcNow
        };
        _context.ContactChangeProposals.Add(proposal);
        await _context.SaveChangesAsync();

        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Approved
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.HandleAsync(proposal.Id, request));
    }

    [Fact]
    public async Task HandleAsync_NonExistentProposal_ThrowsException()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(true);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("admin123");

        var nonExistentId = Guid.NewGuid();
        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Approved
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(nonExistentId, request));
    }

    [Fact]
    public async Task HandleAsync_AlreadyReviewedProposal_ThrowsException()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(true);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("admin123");

        var proposal = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = JsonSerializer.Serialize(new ContactDataDto { FirstName = "Test", LastName = "User" }),
            Status = ProposalStatus.Approved,
            ProposedByUserId = "user123",
            ProposedAt = DateTime.UtcNow,
            ReviewedByUserId = "admin123",
            ReviewedAt = DateTime.UtcNow
        };
        _context.ContactChangeProposals.Add(proposal);
        await _context.SaveChangesAsync();

        var request = new ReviewProposalRequest
        {
            Decision = ProposalStatus.Approved
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(proposal.Id, request));
    }
}

