using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;

namespace RAG.AddressBook.Features.SearchContacts;

public class SearchContactsService
{
    private readonly AddressBookDbContext _context;

    public SearchContactsService(AddressBookDbContext context)
    {
        _context = context;
    }

    public async Task<SearchContactsResponse> SearchAsync(
        SearchContactsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return new SearchContactsResponse { Results = new(), TotalCount = 0 };
        }

        var term = request.SearchTerm.ToLower();
        var results = await _context.Contacts
            .Where(c => c.IsActive && (
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term) ||
                c.Email!.ToLower().Contains(term) ||
                c.Department!.ToLower().Contains(term) ||
                c.Position!.ToLower().Contains(term) ||
                c.Location!.ToLower().Contains(term)
            ))
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Select(c => new ContactSearchResultDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                DisplayName = c.DisplayName,
                Department = c.Department,
                Position = c.Position,
                Email = c.Email,
                MobilePhone = c.MobilePhone
            })
            .ToListAsync(cancellationToken);

        return new SearchContactsResponse
        {
            Results = results,
            TotalCount = results.Count
        };
    }
}
