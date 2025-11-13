using RAG.AddressBook.Features.GetContact;

namespace RAG.AddressBook.Features.UpdateContact;

public record UpdateContactRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Department { get; init; }
    public string? Position { get; init; }
    public string? Location { get; init; }
    public string? Company { get; init; }
    public string? WorkPhone { get; init; }
    public string? MobilePhone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; } = true;
    public string? PhotoUrl { get; init; }
    public List<string>? Tags { get; init; }
}

public record UpdateContactResponse
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<ContactTagDto> Tags { get; init; } = new();
}
