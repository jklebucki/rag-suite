using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;
using RAG.AddressBook.Services;

namespace RAG.AddressBook.Features.ListProposals;

public class ListProposalsService
{
    private readonly AddressBookDbContext _context;
    private readonly IAddressBookAuthorizationService _authService;

    public ListProposalsService(
        AddressBookDbContext context,
        IAddressBookAuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    public async Task<ListProposalsResponse> ListAsync(
        ListProposalsRequest request,
        CancellationToken cancellationToken = default)
    {
        // Only Admin/PowerUser can list all proposals, regular users can only see their own
        var canViewAll = _authService.IsAdminOrPowerUser();
        var currentUserId = _authService.GetCurrentUserId();

        var query = _context.ContactChangeProposals
            .Include(p => p.Contact)
            .AsQueryable();

        // Filter by user if not admin
        if (!canViewAll)
        {
            query = query.Where(p => p.ProposedByUserId == currentUserId);
        }

        // Apply filters
        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        if (request.ProposalType.HasValue)
        {
            query = query.Where(p => p.ProposalType == request.ProposalType.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ProposedByUserId) && canViewAll)
        {
            query = query.Where(p => p.ProposedByUserId == request.ProposedByUserId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var proposals = await query
            .OrderByDescending(p => p.ProposedAt)
            .Select(p => new ProposalListItemDto
            {
                Id = p.Id,
                ContactId = p.ContactId,
                ContactName = p.Contact != null
                    ? $"{p.Contact.FirstName} {p.Contact.LastName}"
                    : null,
                ProposalType = p.ProposalType,
                Status = p.Status,
                ProposedByUserId = p.ProposedByUserId,
                ProposedByUserName = p.ProposedByUserName,
                ProposedAt = p.ProposedAt,
                ReviewedByUserName = p.ReviewedByUserName,
                ReviewedAt = p.ReviewedAt,
                Reason = p.Reason
            })
            .ToListAsync(cancellationToken);

        return new ListProposalsResponse
        {
            Proposals = proposals,
            TotalCount = totalCount
        };
    }
}
