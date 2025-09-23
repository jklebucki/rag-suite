using System.ComponentModel.DataAnnotations;

namespace RAG.Security.DTOs;

public record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserInfo User { get; init; } = new();
}

public record UserInfo
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string[] Roles { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}

public record TokenValidationResponse
{
    public bool IsValid { get; init; }
    public string? UserId { get; init; }
    public string[] Roles { get; init; } = Array.Empty<string>();
    public string? ErrorMessage { get; init; }
}
