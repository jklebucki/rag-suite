using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Services;

namespace RAG.AddressBook.Features.ProposeChange;

/// <summary>
/// Handler for proposing contact changes by regular users
/// </summary>
public class ProposeChangeHandler
{
    private readonly AddressBookDbContext _context;
    private readonly IAddressBookAuthorizationService _authService;

    public ProposeChangeHandler(
        AddressBookDbContext context,
        IAddressBookAuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    public async Task<ProposeContactChangeResponse> HandleAsync(
        ProposeContactChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        // If user has modify permissions, reject the proposal (they should use direct endpoints)
        if (_authService.CanModifyContacts())
        {
            throw new InvalidOperationException(
                "Users with Admin or PowerUser role should use direct modification endpoints");
        }

        // Validate that contact exists for Update/Delete proposals
        if (request.ProposalType != ChangeProposalType.Create && request.ContactId.HasValue)
        {
            var contactExists = await _context.Contacts
                .AnyAsync(c => c.Id == request.ContactId.Value, cancellationToken);
            
            if (!contactExists)
            {
                throw new InvalidOperationException($"Contact with ID {request.ContactId} not found");
            }
        }

        // Serialize proposed data
        var proposedDataJson = JsonSerializer.Serialize(request.ProposedData);

        var proposal = new ContactChangeProposal
        {
            ContactId = request.ContactId,
            ProposalType = request.ProposalType,
            ProposedData = proposedDataJson,
            Reason = request.Reason,
            Status = ProposalStatus.Pending,
            ProposedByUserId = _authService.GetCurrentUserId(),
            ProposedByUserName = _authService.GetCurrentUserName(),
            ProposedAt = DateTime.UtcNow
        };

        _context.ContactChangeProposals.Add(proposal);
        await _context.SaveChangesAsync(cancellationToken);

        return new ProposeContactChangeResponse
        {
            ProposalId = proposal.Id,
            ProposalType = proposal.ProposalType,
            Status = proposal.Status,
            ProposedAt = proposal.ProposedAt,
            Message = "Your change proposal has been submitted for review by an administrator"
        };
    }
}
