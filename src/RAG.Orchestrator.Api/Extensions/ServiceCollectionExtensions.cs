using Microsoft.OpenApi.Models;
using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Features.Search;
using RAG.Orchestrator.Api.Features.Health;
using RAG.Orchestrator.Api.Features.Plugins;
using RAG.Orchestrator.Api.Features.Analytics;

namespace RAG.Orchestrator.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "RAG Orchestrator API",
                Version = "v2.0",
                Description = "RAG API with Semantic Kernel integration",
                Contact = new OpenApiContact
                {
                    Name = "RAG Suite Team"
                }
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
                policy.WithOrigins(
                    "http://localhost:5173", // Vite dev server
                    "http://localhost:3000", // React dev server
                    "http://localhost:8080"  // Alternative dev server
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        // Add HttpClient factory
        services.AddHttpClient();
        
        // Add HttpClient for LLM service (used by HealthAggregator)
        services.AddHttpClient<ILlmService, LlmService>();
        
        // Add HttpClient for SearchService
        services.AddHttpClient<ISearchService, SearchService>();
        
        // Register feature services
        // ChatService now uses Kernel instead of ILlmService
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IHealthAggregator, HealthAggregator>();
        services.AddScoped<IPluginService, PluginService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        
        return services;
    }
}
