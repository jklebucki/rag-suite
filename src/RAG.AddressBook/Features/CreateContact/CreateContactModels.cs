namespace RAG.AddressBook.Features.CreateContact;

public record CreateContactRequest
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
    public string? PhotoUrl { get; init; }
    public List<string>? Tags { get; init; }
}

public record CreateContactResponse
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public DateTime CreatedAt { get; init; }
}
