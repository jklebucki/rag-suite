using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;
using RAG.Security.Services;

namespace RAG.AddressBook.Features.UpdateContact;

public class UpdateContactHandler
{
    private readonly AddressBookDbContext _context;
    private readonly IUserContextService _userContext;

    public UpdateContactHandler(AddressBookDbContext context, IUserContextService userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    public async Task<bool> HandleAsync(
        Guid id,
        UpdateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _userContext.GetCurrentUserId() ?? "system";

        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contact == null)
            return false;

        // Update properties
        contact.FirstName = request.FirstName;
        contact.LastName = request.LastName;
        contact.DisplayName = request.DisplayName;
        contact.Department = request.Department;
        contact.Position = request.Position;
        contact.Location = request.Location;
        contact.Company = request.Company;
        contact.WorkPhone = request.WorkPhone;
        contact.MobilePhone = request.MobilePhone;
        contact.Email = request.Email;
        contact.Notes = request.Notes;
        contact.IsActive = request.IsActive;
        contact.PhotoUrl = request.PhotoUrl;
        contact.UpdatedAt = DateTime.UtcNow;
        contact.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
