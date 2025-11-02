using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;

namespace RAG.AddressBook.Features.ListContacts;

public class ListContactsService
{
    private readonly AddressBookDbContext _context;

    public ListContactsService(AddressBookDbContext context)
    {
        _context = context;
    }

    public async Task<ListContactsResponse> ListAsync(
        ListContactsRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Contacts.AsQueryable();

        // Apply filters
        if (!request.IncludeInactive)
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
                Email = c.Email,
                MobilePhone = c.MobilePhone,
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
