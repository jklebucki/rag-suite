namespace RAG.AddressBook.Features.GetContact;

public record GetContactResponse
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
    public bool IsActive { get; init; }
    public string? PhotoUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public List<ContactTagDto> Tags { get; init; } = new();
}

public record ContactTagDto
{
    public Guid Id { get; init; }
    public string TagName { get; init; } = string.Empty;
    public string? Color { get; init; }
}
