using System.ComponentModel.DataAnnotations;

namespace RAG.Security.DTOs;

public record LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; init; } = string.Empty;

    public bool RememberMe { get; init; } = false;
}

public record RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string UserName { get; init; } = string.Empty;
}

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

public record ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; init; } = string.Empty;

    [Required]
    [Compare("NewPassword")]
    public string ConfirmNewPassword { get; init; } = string.Empty;
}

public record RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}

public record TokenValidationResponse
{
    public bool IsValid { get; init; }
    public string? UserId { get; init; }
    public string[] Roles { get; init; } = Array.Empty<string>();
    public string? ErrorMessage { get; init; }
}

public record ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string UiUrl { get; init; } = string.Empty;
}

public record ResetPasswordRequest
{
    [Required]
    public string Token { get; init; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; init; } = string.Empty;

    [Required]
    [Compare("NewPassword")]
    public string ConfirmNewPassword { get; init; } = string.Empty;
}
