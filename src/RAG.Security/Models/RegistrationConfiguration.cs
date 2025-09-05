namespace RAG.Security.Models;

public class RegistrationConfiguration
{
    public PasswordRequirements PasswordRequirements { get; set; } = new();
    public UserFieldRequirements UserFieldRequirements { get; set; } = new();
    public SecuritySettings SecuritySettings { get; set; } = new();
}

public class PasswordRequirements
{
    public int RequiredLength { get; set; } = 6;
    public bool RequireDigit { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = false;
    public string ValidationMessage { get; set; } = string.Empty;
    public List<string> ValidationRules { get; set; } = new();
}

public class UserFieldRequirements
{
    public FieldRequirement Email { get; set; } = new() { Required = true, MaxLength = 256 };
    public FieldRequirement UserName { get; set; } = new() { Required = true, MinLength = 3, MaxLength = 50 };
    public FieldRequirement FirstName { get; set; } = new() { Required = true, MaxLength = 100 };
    public FieldRequirement LastName { get; set; } = new() { Required = true, MaxLength = 100 };
    public FieldRequirement Password { get; set; } = new() { Required = true, MinLength = 6 };
    public FieldRequirement ConfirmPassword { get; set; } = new() { Required = true };
}

public class FieldRequirement
{
    public bool Required { get; set; } = true;
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
}

public class SecuritySettings
{
    public bool RequireEmailConfirmation { get; set; } = false;
    public bool RequireUniqueEmail { get; set; } = true;
    public bool RequireTermsAcceptance { get; set; } = true;
    public LockoutSettings Lockout { get; set; } = new();
}

public class LockoutSettings
{
    public bool AllowedForNewUsers { get; set; } = true;
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int DefaultLockoutTimeSpanMinutes { get; set; } = 15;
}
