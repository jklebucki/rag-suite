namespace RAG.AddressBook.Features.SearchContacts;

public record SearchContactsRequest
{
    public string SearchTerm { get; init; } = string.Empty;
}

public record SearchContactsResponse
{
    public List<ContactSearchResultDto> Results { get; init; } = new();
    public int TotalCount { get; init; }
}

public record ContactSearchResultDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Department { get; init; }
    public string? Position { get; init; }
    public string? Email { get; init; }
    public string? MobilePhone { get; init; }
}
