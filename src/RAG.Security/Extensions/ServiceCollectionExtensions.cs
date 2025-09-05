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
        // Register DefaultAdmin configuration
        var defaultAdminConfig = new DefaultAdminConfiguration();
        configuration.GetSection("DefaultAdmin").Bind(defaultAdminConfig);
        services.AddSingleton(defaultAdminConfig);

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
            // Password settings
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false; // For simplicity
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
            
            logger.LogInformation("Attempting to ensure PostgreSQL database is created...");
            
            // Ensure database is created - this will create the database if it doesn't exist
            // and won't throw exception if it already exists
            await context.Database.EnsureCreatedAsync();
            
            logger.LogInformation("PostgreSQL database creation successful");

            // Ensure admin user exists
            await EnsureAdminUserAsync(scope.ServiceProvider);
        }
        catch (Exception ex)
        {
            // General database initialization error
            throw new InvalidOperationException($"PostgreSQL database initialization failed: {ex.Message}", ex);
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
        }
    }
}
