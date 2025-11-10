using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RAG.Forum.Data;

namespace RAG.Forum.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddForum(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SecurityDatabase")
            ?? "Host=localhost;Database=rag-suite;Username=postgres;Password=postgres";

        services.AddDbContext<ForumDbContext>(options =>
        {
            options.UseNpgsql(connectionString, builder =>
            {
                builder.MigrationsHistoryTable("__ForumMigrationsHistory", "forum");
            });

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
            }
        });

        return services;
    }

    public static async Task EnsureForumDatabaseCreatedAsync(this IServiceProvider services)
    {
        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ForumDbContext>>();

            logger.LogInformation("Attempting to ensure Forum database is created and migrated...");

            await dbContext.Database.MigrateAsync();

            logger.LogInformation("Forum database migration successful");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Forum database initialization failed: {ex.Message}", ex);
        }
    }
}

