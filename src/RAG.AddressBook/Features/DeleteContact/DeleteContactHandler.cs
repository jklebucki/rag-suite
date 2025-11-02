using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;

namespace RAG.AddressBook.Features.DeleteContact;

public class DeleteContactHandler
{
    private readonly AddressBookDbContext _context;

    public DeleteContactHandler(AddressBookDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contact == null)
            return false;

        _context.Contacts.Remove(contact);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
