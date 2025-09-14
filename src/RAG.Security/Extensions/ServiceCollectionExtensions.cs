using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RAG.Security.Data;
using RAG.Security.Models;
using RAG.Security.Services;
using System.Text;

namespace RAG.Security.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRAGSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration options
        var defaultAdminConfig = new DefaultAdminConfiguration();
        configuration.GetSection("DefaultAdmin").Bind(defaultAdminConfig);
        services.AddSingleton(defaultAdminConfig);

        var passwordComplexityConfig = new PasswordComplexityOptions();
        configuration.GetSection(PasswordComplexityOptions.SectionName).Bind(passwordComplexityConfig);
        services.Configure<PasswordComplexityOptions>(configuration.GetSection(PasswordComplexityOptions.SectionName));

        // Add PostgreSQL DbContext
        var connectionString = configuration.GetConnectionString("SecurityDatabase")
            ?? "Host=localhost;Database=rag-suite;Username=postgres;Password=postgres";

        services.AddDbContext<SecurityDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        // Add Identity
        services.AddIdentity<User, Role>(options =>
        {
            // Password settings from configuration
            options.Password.RequiredLength = passwordComplexityConfig.RequiredLength;
            options.Password.RequireDigit = passwordComplexityConfig.RequireDigit;
            options.Password.RequireLowercase = passwordComplexityConfig.RequireLowercase;
            options.Password.RequireUppercase = passwordComplexityConfig.RequireUppercase;
            options.Password.RequireNonAlphanumeric = passwordComplexityConfig.RequireNonAlphanumeric;
            options.Password.RequiredUniqueChars = passwordComplexityConfig.RequiredUniqueChars;

            // Lockout settings from configuration
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(passwordComplexityConfig.Security.DefaultLockoutTimeSpanMinutes);
            options.Lockout.MaxFailedAccessAttempts = passwordComplexityConfig.Security.MaxFailedAccessAttempts;
            options.Lockout.AllowedForNewUsers = passwordComplexityConfig.Security.AllowedForNewUsers;

            // User settings from configuration
            options.User.RequireUniqueEmail = passwordComplexityConfig.Security.RequireUniqueEmail;
            options.SignIn.RequireConfirmedEmail = passwordComplexityConfig.Security.RequireEmailConfirmation;
        })
        .AddEntityFrameworkStores<SecurityDbContext>()
        .AddDefaultTokenProviders();

        // Add JWT Authentication
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? "your-super-secret-key-that-is-at-least-256-bits-long-for-security";
        var issuer = jwtSettings["Issuer"] ?? "RAG.Suite";
        var audience = jwtSettings["Audience"] ?? "RAG.Suite.Client";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        // Add Authorization
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireUser", policy => policy.RequireRole(Models.UserRoles.User, Models.UserRoles.PowerUser, Models.UserRoles.Admin))
            .AddPolicy("RequirePowerUser", policy => policy.RequireRole(Models.UserRoles.PowerUser, Models.UserRoles.Admin))
            .AddPolicy("RequireAdmin", policy => policy.RequireRole(Models.UserRoles.Admin));

        // Add custom services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IRegistrationConfigurationService, RegistrationConfigurationService>();
        services.AddHttpContextAccessor();

        // Add controllers
        services.AddControllers()
            .AddApplicationPart(typeof(Controllers.AuthController).Assembly);

        return services;
    }

    public static async Task EnsureSecurityDatabaseCreatedAsync(this IServiceProvider services)
    {
        try
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecurityDbContext>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

            logger.LogInformation("Attempting to ensure PostgreSQL database is created...");

            // Run migrations to ensure all tables are created and up-to-date
            await context.Database.MigrateAsync();

            logger.LogInformation("PostgreSQL database migration successful");

            // Ensure default roles exist
            await EnsureDefaultRolesAsync(roleManager, logger);

            // Ensure admin user exists
            await EnsureAdminUserAsync(scope.ServiceProvider);
        }
        catch (Exception ex)
        {
            // General database initialization error
            throw new InvalidOperationException($"PostgreSQL database initialization failed: {ex.Message}", ex);
        }
    }

    private static async Task EnsureDefaultRolesAsync(RoleManager<Role> roleManager, ILogger logger)
    {
        logger.LogInformation("Ensuring default roles exist...");

        foreach (var roleName in Models.UserRoles.AllRoles)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                logger.LogInformation("Creating role: {RoleName}", roleName);

                var role = new Role
                {
                    Name = roleName,
                    Description = Models.UserRoles.RoleDescriptions[roleName],
                    CreatedAt = DateTime.UtcNow
                };

                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    logger.LogInformation("Role '{RoleName}' created successfully", roleName);
                }
                else
                {
                    logger.LogError("Failed to create role '{RoleName}': {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("Role '{RoleName}' already exists", roleName);
            }
        }
    }

    private static async Task EnsureAdminUserAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<User>>();
        var logger = services.GetRequiredService<ILogger<SecurityDbContext>>();
        var adminConfig = services.GetRequiredService<DefaultAdminConfiguration>();

        // Get admin credentials from configuration
        var adminEmail = adminConfig.Email;
        var adminPassword = adminConfig.Password;

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            logger.LogInformation("Creating default admin user: {AdminEmail}", adminEmail);

            adminUser = new User
            {
                Email = adminEmail,
                UserName = "ADMIN",
                FirstName = "Admin",
                LastName = "Admin",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Models.UserRoles.Admin);
                logger.LogInformation("Default admin user created successfully with Admin role");
            }
            else
            {
                logger.LogError("Failed to create default admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogInformation("Default admin user already exists: {AdminEmail}", adminEmail);

            // Ensure admin has Admin role
            var isInAdminRole = await userManager.IsInRoleAsync(adminUser, Models.UserRoles.Admin);
            if (!isInAdminRole)
            {
                logger.LogInformation("Adding Admin role to existing admin user: {AdminEmail}", adminEmail);
                await userManager.AddToRoleAsync(adminUser, Models.UserRoles.Admin);
            }
        }
    }
}
