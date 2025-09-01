using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        // Add SQLite DbContext
        var connectionString = configuration.GetConnectionString("SecurityDatabase") 
            ?? "Data Source=rag-security.db";
        
        services.AddDbContext<SecurityDbContext>(options =>
            options.UseSqlite(connectionString));

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
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Ensure admin user exists
        await EnsureAdminUserAsync(scope.ServiceProvider);
    }

    private static async Task EnsureAdminUserAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<User>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        var adminEmail = configuration["DefaultAdmin:Email"] ?? "admin@rag-suite.com";
        var adminPassword = configuration["DefaultAdmin:Password"] ?? "Admin123!";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                Email = adminEmail,
                UserName = "admin",
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Models.UserRoles.Admin);
            }
        }
    }
}
