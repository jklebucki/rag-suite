using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;

namespace RAG.AddressBook.Features.DeleteContactsWithoutPhoto;

public class DeleteContactsWithoutPhotoHandler
{
    private readonly AddressBookDbContext _context;

    public DeleteContactsWithoutPhotoHandler(AddressBookDbContext context)
    {
        _context = context;
    }

    public async Task<int> HandleAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Contacts
            .Where(c => c.PhotoUrl == null || (c.PhotoUrl != null && c.PhotoUrl.Trim() == string.Empty))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
