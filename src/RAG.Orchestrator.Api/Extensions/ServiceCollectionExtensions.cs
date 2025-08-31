using Microsoft.OpenApi.Models;

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
}
