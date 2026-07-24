using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;
using RAG.AddressBook.Services;

namespace RAG.AddressBook.Features.ListContacts;

public class ListContactsService
{
    private readonly AddressBookDbContext _context;
    private readonly IAddressBookAuthorizationService _authService;

    public ListContactsService(AddressBookDbContext context, IAddressBookAuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    public async Task<ListContactsResponse> ListAsync(
        ListContactsRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Contacts.AsQueryable();

        // Inactive contacts are visible only to Admin/PowerUser, regardless of the requested flag
        var includeInactive = request.IncludeInactive && _authService.IsAdminOrPowerUser();
        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (!string.IsNullOrEmpty(request.Department))
        {
            query = query.Where(c => c.Department == request.Department);
        }

        if (!string.IsNullOrEmpty(request.Location))
        {
            query = query.Where(c => c.Location == request.Location);
        }

        var contacts = await query
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Select(c => new ContactListItemDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                DisplayName = c.DisplayName,
                Department = c.Department,
                Position = c.Position,
                Location = c.Location,
                Company = c.Company,
                WorkPhone = c.WorkPhone,
                MobilePhone = c.MobilePhone,
                Email = c.Email,
                Notes = c.Notes,
                PhotoUrl = c.PhotoUrl,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);

        return new ListContactsResponse
        {
            Contacts = contacts,
            TotalCount = contacts.Count
        };
    }
}
