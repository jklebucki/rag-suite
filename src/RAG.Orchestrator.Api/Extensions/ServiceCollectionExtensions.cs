using RAG.Orchestrator.Api.Features.Analytics;
using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Features.Plugins;
using RAG.Orchestrator.Api.Features.Search;

namespace RAG.Orchestrator.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register HTTP clients
        services.AddHttpClient<ILlmService, LlmService>();
        services.AddHttpClient<ISearchService, SearchService>();
        
        // Register all feature services
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IPluginService, PluginService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<ILlmService, LlmService>();

        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "RAG Orchestrator API",
                Version = "v1",
                Description = "A comprehensive API for managing RAG (Retrieval-Augmented Generation) operations including search, chat, plugins, and analytics."
            });
        });

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:5173")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }
}
