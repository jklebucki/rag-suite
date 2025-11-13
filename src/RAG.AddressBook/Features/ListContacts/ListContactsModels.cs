namespace RAG.AddressBook.Features.ListContacts;

public record ListContactsRequest
{
    public bool IncludeInactive { get; init; } = false;
    public string? Department { get; init; }
    public string? Location { get; init; }
}

public record ListContactsResponse
{
    public List<ContactListItemDto> Contacts { get; init; } = new();
    public int TotalCount { get; init; }
}

public record ContactListItemDto
{
    public Guid Id { get; init; }
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
    public bool IsActive { get; init; }
}
