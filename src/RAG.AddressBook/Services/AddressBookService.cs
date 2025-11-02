using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;

namespace RAG.AddressBook.Services;

/// <summary>
/// Core service implementation for address book operations
/// </summary>
public class AddressBookService : IAddressBookService
{
    private readonly AddressBookDbContext _context;

    public AddressBookService(AddressBookDbContext context)
    {
        _context = context;
    }

    public async Task<Contact?> GetContactByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Contacts
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<Contact>> GetAllContactsAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Contacts.Include(c => c.Tags).AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Contact> CreateContactAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        contact.CreatedAt = DateTime.UtcNow;
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync(cancellationToken);
        return contact;
    }

    public async Task<Contact?> UpdateContactAsync(Guid id, Contact contact, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Contacts
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (existing == null)
            return null;

        // Update properties
        existing.FirstName = contact.FirstName;
        existing.LastName = contact.LastName;
        existing.DisplayName = contact.DisplayName;
        existing.Department = contact.Department;
        existing.Position = contact.Position;
        existing.Location = contact.Location;
        existing.Company = contact.Company;
        existing.WorkPhone = contact.WorkPhone;
        existing.MobilePhone = contact.MobilePhone;
        existing.Email = contact.Email;
        existing.Notes = contact.Notes;
        existing.IsActive = contact.IsActive;
        existing.PhotoUrl = contact.PhotoUrl;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedByUserId = contact.UpdatedByUserId;

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteContactAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contact = await _context.Contacts.FindAsync(new object[] { id }, cancellationToken);
        if (contact == null)
            return false;

        _context.Contacts.Remove(contact);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<Contact>> SearchContactsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var term = searchTerm.ToLower();
        return await _context.Contacts
            .Include(c => c.Tags)
            .Where(c => c.IsActive && (
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term) ||
                c.Email!.ToLower().Contains(term) ||
                c.Department!.ToLower().Contains(term) ||
                c.Position!.ToLower().Contains(term)
            ))
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync(cancellationToken);
    }
}
