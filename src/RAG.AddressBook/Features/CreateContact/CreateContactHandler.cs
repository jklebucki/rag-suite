using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.Security.Services;

namespace RAG.AddressBook.Features.CreateContact;

public class CreateContactHandler
{
    private readonly AddressBookDbContext _context;
    private readonly IUserContextService _userContext;

    public CreateContactHandler(AddressBookDbContext context, IUserContextService userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    public async Task<CreateContactResponse> HandleAsync(
        CreateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _userContext.GetCurrentUserId() ?? "system";

        var contact = new Contact
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            DisplayName = request.DisplayName,
            Department = request.Department,
            Position = request.Position,
            Location = request.Location,
            Company = request.Company,
            WorkPhone = request.WorkPhone,
            MobilePhone = request.MobilePhone,
            Email = request.Email,
            Notes = request.Notes,
            PhotoUrl = request.PhotoUrl,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        // Add tags if provided
        if (request.Tags != null && request.Tags.Any())
        {
            foreach (var tagName in request.Tags)
            {
                contact.Tags.Add(new ContactTag
                {
                    TagName = tagName,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateContactResponse
        {
            Id = contact.Id,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            Email = contact.Email,
            CreatedAt = contact.CreatedAt
        };
    }
}
