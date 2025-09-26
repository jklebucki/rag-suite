using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RAG.CyberPanel.Data;
using RAG.CyberPanel.Services;
using Microsoft.Extensions.Logging;

using System;
using System.Threading.Tasks;

namespace RAG.CyberPanel.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCyberPanel(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SecurityDatabase")
            ?? "Host=localhost;Database=rag-suite;Username=postgres;Password=postgres";

        services.AddDbContext<CyberPanelDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        // Register services and handlers here (scaffold)
        services.AddScoped<ICyberPanelService, CyberPanelService>();

        return services;
    }

    public static async Task EnsureCyberPanelDatabaseCreatedAsync(this IServiceProvider services)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CyberPanelDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<CyberPanelDbContext>>();

            logger.LogInformation("Attempting to ensure CyberPanel database is created and migrated...");

            await db.Database.MigrateAsync();

            logger.LogInformation("CyberPanel database migration successful");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"CyberPanel database initialization failed: {ex.Message}", ex);
        }
    }
}
