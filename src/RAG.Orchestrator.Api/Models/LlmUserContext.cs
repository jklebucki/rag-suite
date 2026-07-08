namespace RAG.Orchestrator.Api.Models;

public record LlmUserContext
{
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string[] Roles { get; init; } = Array.Empty<string>();

    public string? DisplayName
    {
        get
        {
            var fullName = string.Join(" ", new[] { FirstName, LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;

            if (!string.IsNullOrWhiteSpace(UserName))
                return UserName;

            return Email;
        }
    }
}
