using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.ProposeChange;
using RAG.AddressBook.Services;
using System.Text.Json;

namespace RAG.AddressBook.Features.ReviewProposal;

/// <summary>
/// Handler for reviewing (approving/rejecting) change proposals by Admin/PowerUser
/// </summary>
public class ReviewProposalHandler
{
    private readonly AddressBookDbContext _context;
    private readonly IAddressBookAuthorizationService _authService;

    public ReviewProposalHandler(
        AddressBookDbContext context,
        IAddressBookAuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    public async Task<ReviewProposalResponse> HandleAsync(
        Guid proposalId,
        ReviewProposalRequest request,
        CancellationToken cancellationToken = default)
    {
        // Only Admin/PowerUser can review proposals
        if (!_authService.IsAdminOrPowerUser())
        {
            throw new UnauthorizedAccessException(
                "Only users with Admin or PowerUser role can review proposals");
        }

        // Validate decision
        if (request.Decision != ProposalStatus.Approved && request.Decision != ProposalStatus.Rejected)
        {
            throw new InvalidOperationException("Decision must be either Approved or Rejected");
        }

        var proposal = await _context.ContactChangeProposals
            .Include(p => p.Contact)
            .FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken);

        if (proposal == null)
        {
            throw new InvalidOperationException($"Proposal with ID {proposalId} not found");
        }

        if (proposal.Status != ProposalStatus.Pending)
        {
            throw new InvalidOperationException($"Proposal has already been reviewed (Status: {proposal.Status})");
        }

        // Update proposal with review details
        proposal.Status = request.Decision;
        proposal.ReviewedByUserId = _authService.GetCurrentUserId();
        proposal.ReviewedByUserName = _authService.GetCurrentUserName();
        proposal.ReviewedAt = DateTime.UtcNow;
        proposal.ReviewComment = request.ReviewComment;

        // If approved, apply the changes
        if (request.Decision == ProposalStatus.Approved)
        {
            await ApplyProposalAsync(proposal, cancellationToken);
            proposal.Status = ProposalStatus.Applied;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ReviewProposalResponse
        {
            ProposalId = proposal.Id,
            Status = proposal.Status,
            ReviewedAt = proposal.ReviewedAt.Value,
            Message = request.Decision == ProposalStatus.Approved
                ? "Proposal has been approved and applied"
                : "Proposal has been rejected"
        };
    }

    private async Task ApplyProposalAsync(
        ContactChangeProposal proposal,
        CancellationToken cancellationToken)
    {
        var proposedData = JsonSerializer.Deserialize<ContactDataDto>(proposal.ProposedData);

        if (proposedData == null)
        {
            throw new InvalidOperationException("Failed to deserialize proposed data");
        }

        switch (proposal.ProposalType)
        {
            case ChangeProposalType.Create:
                await ApplyCreateAsync(proposedData, proposal.ProposedByUserId, cancellationToken);
                break;

            case ChangeProposalType.Update:
                if (!proposal.ContactId.HasValue)
                {
                    throw new InvalidOperationException("ContactId is required for Update proposals");
                }
                await ApplyUpdateAsync(proposal.ContactId.Value, proposedData, proposal.ProposedByUserId, cancellationToken);
                break;

            case ChangeProposalType.Delete:
                if (!proposal.ContactId.HasValue)
                {
                    throw new InvalidOperationException("ContactId is required for Delete proposals");
                }
                await ApplyDeleteAsync(proposal.ContactId.Value, cancellationToken);
                break;
        }
    }

    private Task ApplyCreateAsync(
        ContactDataDto data,
        string userId,
        CancellationToken cancellationToken)
    {
        var contact = new Contact
        {
            FirstName = data.FirstName,
            LastName = data.LastName,
            DisplayName = data.DisplayName,
            Department = data.Department,
            Position = data.Position,
            Location = data.Location,
            Company = data.Company,
            WorkPhone = data.WorkPhone,
            MobilePhone = data.MobilePhone,
            Email = data.Email,
            Notes = data.Notes,
            PhotoUrl = data.PhotoUrl,
            IsActive = true,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Contacts.Add(contact);
        return Task.CompletedTask;
    }

    private async Task ApplyUpdateAsync(
        Guid contactId,
        ContactDataDto data,
        string userId,
        CancellationToken cancellationToken)
    {
        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Id == contactId, cancellationToken);

        if (contact == null)
        {
            throw new InvalidOperationException($"Contact with ID {contactId} not found");
        }

        contact.FirstName = data.FirstName;
        contact.LastName = data.LastName;
        contact.DisplayName = data.DisplayName;
        contact.Department = data.Department;
        contact.Position = data.Position;
        contact.Location = data.Location;
        contact.Company = data.Company;
        contact.WorkPhone = data.WorkPhone;
        contact.MobilePhone = data.MobilePhone;
        contact.Email = data.Email;
        contact.Notes = data.Notes;
        contact.PhotoUrl = data.PhotoUrl;
        contact.UpdatedAt = DateTime.UtcNow;
        contact.UpdatedByUserId = userId;
    }

    private async Task ApplyDeleteAsync(
        Guid contactId,
        CancellationToken cancellationToken)
    {
        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Id == contactId, cancellationToken);

        if (contact != null)
        {
            _context.Contacts.Remove(contact);
        }
    }
}
