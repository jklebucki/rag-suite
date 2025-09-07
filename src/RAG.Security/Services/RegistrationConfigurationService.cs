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
    private readonly PasswordComplexityOptions _passwordComplexityOptions;

    public RegistrationConfigurationService(
        IOptions<IdentityOptions> identityOptions,
        IOptions<PasswordComplexityOptions> passwordComplexityOptions)
    {
        _identityOptions = identityOptions.Value;
        _passwordComplexityOptions = passwordComplexityOptions.Value;
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
                ValidationMessage = BuildPasswordValidationMessage(passwordOptions),
                ValidationRules = BuildPasswordValidationRules(passwordOptions)
            },
            UserFieldRequirements = new UserFieldRequirements
            {
                Email = new FieldRequirement
                {
                    Required = true,
                    MaxLength = _passwordComplexityOptions.UserFields.EmailMaxLength,
                    Pattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
                    ValidationMessage = "auth.validation.email_invalid"
                },
                UserName = new FieldRequirement
                {
                    Required = true,
                    MinLength = _passwordComplexityOptions.UserFields.UserNameMinLength,
                    MaxLength = _passwordComplexityOptions.UserFields.UserNameMaxLength,
                    ValidationMessage = "auth.validation.username_min_length"
                },
                FirstName = new FieldRequirement
                {
                    Required = true,
                    MaxLength = _passwordComplexityOptions.UserFields.FirstNameMaxLength,
                    ValidationMessage = "auth.validation.first_name_required"
                },
                LastName = new FieldRequirement
                {
                    Required = true,
                    MaxLength = _passwordComplexityOptions.UserFields.LastNameMaxLength,
                    ValidationMessage = "auth.validation.last_name_required"
                },
                Password = new FieldRequirement
                {
                    Required = true,
                    MinLength = passwordOptions.RequiredLength,
                    ValidationMessage = "auth.validation.password_required"
                },
                ConfirmPassword = new FieldRequirement
                {
                    Required = true,
                    ValidationMessage = "auth.validation.password_mismatch"
                }
            },
            SecuritySettings = new SecuritySettings
            {
                RequireEmailConfirmation = _passwordComplexityOptions.Security.RequireEmailConfirmation,
                RequireUniqueEmail = _passwordComplexityOptions.Security.RequireUniqueEmail,
                RequireTermsAcceptance = _passwordComplexityOptions.Security.RequireTermsAcceptance,
                Lockout = new LockoutSettings
                {
                    AllowedForNewUsers = _passwordComplexityOptions.Security.AllowedForNewUsers,
                    MaxFailedAccessAttempts = _passwordComplexityOptions.Security.MaxFailedAccessAttempts,
                    DefaultLockoutTimeSpanMinutes = _passwordComplexityOptions.Security.DefaultLockoutTimeSpanMinutes
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

    private static List<string> BuildPasswordValidationRules(PasswordOptions options)
    {
        var rules = new List<string>();

        rules.Add($"auth.validation.password_min_length#{options.RequiredLength}");

        if (options.RequireUppercase)
            rules.Add("auth.validation.password_require_uppercase");

        if (options.RequireLowercase)
            rules.Add("auth.validation.password_require_lowercase");

        if (options.RequireDigit)
            rules.Add("auth.validation.password_require_digit");

        if (options.RequireNonAlphanumeric)
            rules.Add("auth.validation.password_require_special");

        return rules;
    }
}
