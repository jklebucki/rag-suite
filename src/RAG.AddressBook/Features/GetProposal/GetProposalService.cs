using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.ProposeChange;
using RAG.AddressBook.Services;

namespace RAG.AddressBook.Features.GetProposal;

public record GetProposalResponse
{
    public Guid Id { get; init; }
    public Guid? ContactId { get; init; }
    public ChangeProposalType ProposalType { get; init; }
    public ContactDataDto ProposedData { get; init; } = new();
    public ProposalStatus Status { get; init; }
    public string? Reason { get; init; }
    public string ProposedByUserId { get; init; } = string.Empty;
    public string? ProposedByUserName { get; init; }
    public DateTime ProposedAt { get; init; }
    public string? ReviewedByUserId { get; init; }
    public string? ReviewedByUserName { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? ReviewComment { get; init; }
    public ContactSummaryDto? CurrentContact { get; init; }
}

public record ContactSummaryDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Department { get; init; }
}

public class GetProposalService
{
    private readonly AddressBookDbContext _context;
    private readonly IAddressBookAuthorizationService _authService;

    public GetProposalService(
        AddressBookDbContext context,
        IAddressBookAuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    public async Task<GetProposalResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var proposal = await _context.ContactChangeProposals
            .Include(p => p.Contact)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (proposal == null)
            return null;

        // Check authorization
        var canViewAll = _authService.IsAdminOrPowerUser();
        var currentUserId = _authService.GetCurrentUserId();

        if (!canViewAll && proposal.ProposedByUserId != currentUserId)
        {
            return null; // User can only view their own proposals
        }

        var proposedData = JsonSerializer.Deserialize<ContactDataDto>(proposal.ProposedData) 
            ?? new ContactDataDto();

        return new GetProposalResponse
        {
            Id = proposal.Id,
            ContactId = proposal.ContactId,
            ProposalType = proposal.ProposalType,
            ProposedData = proposedData,
            Status = proposal.Status,
            Reason = proposal.Reason,
            ProposedByUserId = proposal.ProposedByUserId,
            ProposedByUserName = proposal.ProposedByUserName,
            ProposedAt = proposal.ProposedAt,
            ReviewedByUserId = proposal.ReviewedByUserId,
            ReviewedByUserName = proposal.ReviewedByUserName,
            ReviewedAt = proposal.ReviewedAt,
            ReviewComment = proposal.ReviewComment,
            CurrentContact = proposal.Contact != null ? new ContactSummaryDto
            {
                Id = proposal.Contact.Id,
                FirstName = proposal.Contact.FirstName,
                LastName = proposal.Contact.LastName,
                Email = proposal.Contact.Email,
                Department = proposal.Contact.Department
            } : null
        };
    }
}
