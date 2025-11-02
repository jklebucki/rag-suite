using RAG.AddressBook.Domain;

namespace RAG.AddressBook.Services;

/// <summary>
/// Core service interface for address book operations
/// </summary>
public interface IAddressBookService
{
    Task<Contact?> GetContactByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Contact>> GetAllContactsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<Contact> CreateContactAsync(Contact contact, CancellationToken cancellationToken = default);
    Task<Contact?> UpdateContactAsync(Guid id, Contact contact, CancellationToken cancellationToken = default);
    Task<bool> DeleteContactAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Contact>> SearchContactsAsync(string searchTerm, CancellationToken cancellationToken = default);
}
