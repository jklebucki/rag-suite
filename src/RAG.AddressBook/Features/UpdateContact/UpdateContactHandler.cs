using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.AddressBook.Features.GetContact;
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

    public async Task<UpdateContactResponse?> HandleAsync(
        Guid id,
        UpdateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _userContext.GetCurrentUserId() ?? "system";

        var contact = await _context.Contacts
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contact == null)
            return null;

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

        // Update tags
        if (request.Tags != null)
        {
            // Get existing tag names before modification
            var existingTagNames = contact.Tags.Select(t => t.TagName).ToList();
            
            // Remove tags that are not in the new list
            var tagsToRemove = contact.Tags
                .Where(t => !request.Tags.Contains(t.TagName))
                .ToList();
            foreach (var tag in tagsToRemove)
            {
                contact.Tags.Remove(tag);
                _context.ContactTags.Remove(tag);
            }

            // Add new tags that don't exist yet
            var tagsToAdd = request.Tags
                .Where(tagName => !existingTagNames.Contains(tagName))
                .ToList();
            foreach (var tagName in tagsToAdd)
            {
                contact.Tags.Add(new ContactTag
                {
                    TagName = tagName,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else
        {
            // If Tags is null, remove all tags
            _context.ContactTags.RemoveRange(contact.Tags);
            contact.Tags.Clear();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateContactResponse
        {
            Id = contact.Id,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            Email = contact.Email,
            UpdatedAt = contact.UpdatedAt ?? DateTime.UtcNow,
            Tags = contact.Tags.Select(t => new ContactTagDto
            {
                Id = t.Id,
                TagName = t.TagName,
                Color = t.Color
            }).ToList()
        };
    }
}
