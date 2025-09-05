namespace RAG.Security.Models;

public class PasswordComplexityOptions
{
    public const string SectionName = "PasswordComplexity";
    
    public int RequiredLength { get; set; } = 6;
    public bool RequireDigit { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = false;
    public int RequiredUniqueChars { get; set; } = 1;
    
    // Additional settings for registration validation
    public UserFieldOptions UserFields { get; set; } = new();
    public SecurityOptions Security { get; set; } = new();
}

public class UserFieldOptions
{
    public int UserNameMinLength { get; set; } = 3;
    public int UserNameMaxLength { get; set; } = 50;
    public int FirstNameMaxLength { get; set; } = 100;
    public int LastNameMaxLength { get; set; } = 100;
    public int EmailMaxLength { get; set; } = 256;
}

public class SecurityOptions
{
    public bool RequireEmailConfirmation { get; set; } = false;
    public bool RequireUniqueEmail { get; set; } = true;
    public bool RequireTermsAcceptance { get; set; } = true;
    
    // Lockout settings
    public bool AllowedForNewUsers { get; set; } = true;
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int DefaultLockoutTimeSpanMinutes { get; set; } = 15;
}
