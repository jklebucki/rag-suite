namespace RAG.AddressBook.Features.ImportContacts;

public record ImportContactsRequest
{
    public string CsvContent { get; init; } = string.Empty;
    public bool SkipDuplicates { get; init; } = true;
}

public record ImportContactsResponse
{
    public int TotalRows { get; init; }
    public int SuccessCount { get; init; }
    public int SkippedCount { get; init; }
    public int ErrorCount { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<ImportedContactDto> ImportedContacts { get; init; } = new();
}

public record ImportedContactDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Department { get; init; }
}
