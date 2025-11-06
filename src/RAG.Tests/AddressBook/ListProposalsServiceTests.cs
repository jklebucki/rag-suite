using Xunit;
using FluentAssertions;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.ListProposals;
using RAG.AddressBook.Features.ProposeChange;
using RAG.AddressBook.Services;
using Moq;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RAG.Tests.AddressBook;

public class ListProposalsServiceTests : IDisposable
{
    private readonly AddressBookDbContext _context;
    private readonly Mock<IAddressBookAuthorizationService> _mockAuthService;
    private readonly ListProposalsService _service;

    public ListProposalsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AddressBookDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AddressBookDbContext(options);
        _mockAuthService = new Mock<IAddressBookAuthorizationService>();
        _service = new ListProposalsService(_context, _mockAuthService.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task ListAsync_AdminUser_ReturnsAllProposals()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(true);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("admin123");

        var proposal1 = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = JsonSerializer.Serialize(new ContactDataDto { FirstName = "User1", LastName = "Test" }),
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user1",
            ProposedAt = DateTime.UtcNow
        };
        var proposal2 = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Update,
            ProposedData = JsonSerializer.Serialize(new ContactDataDto { FirstName = "User2", LastName = "Test" }),
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user2",
            ProposedAt = DateTime.UtcNow
        };
        _context.ContactChangeProposals.AddRange(proposal1, proposal2);
        await _context.SaveChangesAsync();

        var request = new ListProposalsRequest();

        // Act
        var result = await _service.ListAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Proposals.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListAsync_RegularUser_ReturnsOnlyOwnProposals()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(false);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("user1");

        var ownProposal = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = JsonSerializer.Serialize(new ContactDataDto { FirstName = "Own", LastName = "Proposal" }),
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user1",
            ProposedAt = DateTime.UtcNow
        };
        var otherProposal = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Update,
            ProposedData = JsonSerializer.Serialize(new ContactDataDto { FirstName = "Other", LastName = "Proposal" }),
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user2",
            ProposedAt = DateTime.UtcNow
        };
        _context.ContactChangeProposals.AddRange(ownProposal, otherProposal);
        await _context.SaveChangesAsync();

        var request = new ListProposalsRequest();

        // Act
        var result = await _service.ListAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Proposals.Should().HaveCount(1);
        result.Proposals.First().ProposedByUserId.Should().Be("user1");
    }

    [Fact]
    public async Task ListAsync_WithStatusFilter_ReturnsFilteredProposals()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(true);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("admin123");

        var pending = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = JsonSerializer.Serialize(new ContactDataDto { FirstName = "Pending", LastName = "Test" }),
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user1",
            ProposedAt = DateTime.UtcNow
        };
        var approved = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Update,
            ProposedData = JsonSerializer.Serialize(new ContactDataDto { FirstName = "Approved", LastName = "Test" }),
            Status = ProposalStatus.Approved,
            ProposedByUserId = "user2",
            ProposedAt = DateTime.UtcNow
        };
        _context.ContactChangeProposals.AddRange(pending, approved);
        await _context.SaveChangesAsync();

        var request = new ListProposalsRequest { Status = ProposalStatus.Pending };

        // Act
        var result = await _service.ListAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Proposals.Should().HaveCount(1);
        result.Proposals.First().Status.Should().Be(ProposalStatus.Pending);
    }

    [Fact]
    public async Task ListAsync_WithProposalTypeFilter_ReturnsFilteredProposals()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(true);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("admin123");

        var create = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = JsonSerializer.Serialize(new ContactDataDto { FirstName = "Create", LastName = "Test" }),
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user1",
            ProposedAt = DateTime.UtcNow
        };
        var update = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Update,
            ProposedData = JsonSerializer.Serialize(new ContactDataDto { FirstName = "Update", LastName = "Test" }),
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user2",
            ProposedAt = DateTime.UtcNow
        };
        _context.ContactChangeProposals.AddRange(create, update);
        await _context.SaveChangesAsync();

        var request = new ListProposalsRequest { ProposalType = ChangeProposalType.Create };

        // Act
        var result = await _service.ListAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Proposals.Should().HaveCount(1);
        result.Proposals.First().ProposalType.Should().Be(ChangeProposalType.Create);
    }

    [Fact]
    public async Task ListAsync_OrdersByProposedAtDescending()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(true);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("admin123");

        var older = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Create,
            ProposedData = JsonSerializer.Serialize(new ContactDataDto { FirstName = "Older", LastName = "Test" }),
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user1",
            ProposedAt = DateTime.UtcNow.AddHours(-2)
        };
        var newer = new ContactChangeProposal
        {
            ProposalType = ChangeProposalType.Update,
            ProposedData = JsonSerializer.Serialize(new ContactDataDto { FirstName = "Newer", LastName = "Test" }),
            Status = ProposalStatus.Pending,
            ProposedByUserId = "user2",
            ProposedAt = DateTime.UtcNow
        };
        _context.ContactChangeProposals.AddRange(older, newer);
        await _context.SaveChangesAsync();

        var request = new ListProposalsRequest();

        // Act
        var result = await _service.ListAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Proposals.Should().HaveCount(2);
        result.Proposals.First().ProposedAt.Should().BeAfter(result.Proposals.Last().ProposedAt);
    }

    [Fact]
    public async Task ListAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        _mockAuthService.Setup(a => a.IsAdminOrPowerUser()).Returns(true);
        _mockAuthService.Setup(a => a.GetCurrentUserId()).Returns("admin123");

        var request = new ListProposalsRequest();

        // Act
        var result = await _service.ListAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Proposals.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}

