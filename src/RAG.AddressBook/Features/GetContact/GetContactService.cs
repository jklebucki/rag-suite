using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;

namespace RAG.AddressBook.Features.GetContact;

public class GetContactService
{
    private readonly AddressBookDbContext _context;

    public GetContactService(AddressBookDbContext context)
    {
        _context = context;
    }

    public async Task<GetContactResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contact = await _context.Contacts
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contact == null)
            return null;

        return new GetContactResponse
        {
            Id = contact.Id,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            DisplayName = contact.DisplayName,
            Department = contact.Department,
            Position = contact.Position,
            Location = contact.Location,
            Company = contact.Company,
            WorkPhone = contact.WorkPhone,
            MobilePhone = contact.MobilePhone,
            Email = contact.Email,
            Notes = contact.Notes,
            IsActive = contact.IsActive,
            PhotoUrl = contact.PhotoUrl,
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt,
            Tags = contact.Tags.Select(t => new ContactTagDto
            {
                Id = t.Id,
                TagName = t.TagName,
                Color = t.Color
            }).ToList()
        };
    }
}
