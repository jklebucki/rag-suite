using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RAG.CyberPanel.Data;
using RAG.CyberPanel.Services;

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
}
