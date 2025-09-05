using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RAG.Security.Models;

namespace RAG.Security.Services;

public interface IRegistrationConfigurationService
{
    RegistrationConfiguration GetConfiguration();
}

public class RegistrationConfigurationService : IRegistrationConfigurationService
{
    private readonly IdentityOptions _identityOptions;

    public RegistrationConfigurationService(IOptions<IdentityOptions> identityOptions)
    {
        _identityOptions = identityOptions.Value;
    }

    public RegistrationConfiguration GetConfiguration()
    {
        var passwordOptions = _identityOptions.Password;
        var userOptions = _identityOptions.User;
        var lockoutOptions = _identityOptions.Lockout;
        var signInOptions = _identityOptions.SignIn;

        return new RegistrationConfiguration
        {
            PasswordRequirements = new PasswordRequirements
            {
                RequiredLength = passwordOptions.RequiredLength,
                RequireDigit = passwordOptions.RequireDigit,
                RequireLowercase = passwordOptions.RequireLowercase,
                RequireUppercase = passwordOptions.RequireUppercase,
                RequireNonAlphanumeric = passwordOptions.RequireNonAlphanumeric,
                ValidationMessage = BuildPasswordValidationMessage(passwordOptions)
            },
            UserFieldRequirements = new UserFieldRequirements
            {
                Email = new FieldRequirement 
                { 
                    Required = true, 
                    MaxLength = 256,
                    Pattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
                    ValidationMessage = "Valid email address is required"
                },
                UserName = new FieldRequirement 
                { 
                    Required = true, 
                    MinLength = 3, 
                    MaxLength = 50,
                    ValidationMessage = "Username must be between 3 and 50 characters"
                },
                FirstName = new FieldRequirement 
                { 
                    Required = true, 
                    MaxLength = 100,
                    ValidationMessage = "First name is required and cannot exceed 100 characters"
                },
                LastName = new FieldRequirement 
                { 
                    Required = true, 
                    MaxLength = 100,
                    ValidationMessage = "Last name is required and cannot exceed 100 characters"
                }
            },
            SecuritySettings = new SecuritySettings
            {
                RequireEmailConfirmation = signInOptions.RequireConfirmedEmail,
                RequireUniqueEmail = userOptions.RequireUniqueEmail,
                RequireTermsAcceptance = true, // This is application-specific
                Lockout = new LockoutSettings
                {
                    AllowedForNewUsers = lockoutOptions.AllowedForNewUsers,
                    MaxFailedAccessAttempts = lockoutOptions.MaxFailedAccessAttempts,
                    DefaultLockoutTimeSpanMinutes = (int)lockoutOptions.DefaultLockoutTimeSpan.TotalMinutes
                }
            }
        };
    }

    private static string BuildPasswordValidationMessage(PasswordOptions options)
    {
        var requirements = new List<string>();

        requirements.Add($"at least {options.RequiredLength} characters");

        if (options.RequireUppercase)
            requirements.Add("uppercase letter");

        if (options.RequireLowercase)
            requirements.Add("lowercase letter");

        if (options.RequireDigit)
            requirements.Add("digit");

        if (options.RequireNonAlphanumeric)
            requirements.Add("special character");

        return requirements.Count switch
        {
            0 => "No specific requirements",
            1 => $"Password must contain {requirements[0]}",
            2 => $"Password must contain {requirements[0]} and {requirements[1]}",
            _ => $"Password must contain {string.Join(", ", requirements.Take(requirements.Count - 1))} and {requirements.Last()}"
        };
    }
}
