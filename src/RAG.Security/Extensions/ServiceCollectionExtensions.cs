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
        // Add SQLite DbContext with fallback
        var connectionString = configuration.GetConnectionString("SecurityDatabase") 
            ?? "Data Source=rag-security.db";
        
        services.AddDbContext<SecurityDbContext>(options =>
        {
            try
            {
                options.UseSqlite(connectionString);
            }
            catch (Exception ex)
            {
                // Fallback to in-memory database if SQLite fails
                Console.WriteLine($"SQLite initialization failed: {ex.Message}. Using in-memory database.");
                options.UseInMemoryDatabase("SecurityDatabase");
            }
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
            
            logger.LogInformation("Attempting to ensure database is created...");
            
            // Ensure database is created - this will create the database if it doesn't exist
            // and won't throw exception if it already exists
            await context.Database.EnsureCreatedAsync();
            
            logger.LogInformation("Database creation successful");

            // Ensure admin user exists
            await EnsureAdminUserAsync(scope.ServiceProvider);
        }
        catch (TypeInitializationException ex) when (ex.Message.Contains("Microsoft.Data.Sqlite.SqliteConnection"))
        {
            // Handle SQLite initialization issues
            throw new InvalidOperationException(
                "SQLite initialization failed. This might be due to missing native libraries on the server. " +
                "Please ensure SQLite native libraries are installed or consider using an alternative database provider.", ex);
        }
        catch (System.IO.FileNotFoundException ex) when (ex.Message.Contains("Microsoft.Data.Sqlite"))
        {
            // Handle missing SQLite assembly
            throw new InvalidOperationException(
                "Microsoft.Data.Sqlite assembly not found. Please ensure all SQLite dependencies are properly deployed.", ex);
        }
        catch (System.DllNotFoundException ex) when (ex.Message.Contains("e_sqlite3"))
        {
            // Handle missing native SQLite library
            throw new InvalidOperationException(
                "Native SQLite library (e_sqlite3) not found. Please install sqlite3 on the server or use self-contained deployment.", ex);
        }
        catch (Exception ex)
        {
            // General database initialization error
            throw new InvalidOperationException($"Database initialization failed: {ex.Message}", ex);
        }
    }

    private static async Task EnsureAdminUserAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<User>>();
        var logger = services.GetRequiredService<ILogger<SecurityDbContext>>();

        // Fixed admin credentials as requested
        var adminEmail = "admin@citronex.pl";
        var adminPassword = "Citro123";

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
