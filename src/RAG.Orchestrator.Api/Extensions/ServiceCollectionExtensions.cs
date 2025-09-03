using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Features.Search;
using RAG.Orchestrator.Api.Features.Health;
using RAG.Orchestrator.Api.Features.Plugins;
using RAG.Orchestrator.Api.Features.Analytics;
using Elasticsearch.Net;

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
                Description = "RAG API with Semantic Kernel integration and multilingual support",
                Contact = new OpenApiContact
                {
                    Name = "RAG Suite Team"
                }
            });
            
            // Resolve type conflicts by using full names
            options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
            
            // Add JWT Bearer Authorization
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
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
                policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
            });
        });

        return services;
    }

    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        // Add HttpClient factory
        services.AddHttpClient();
        
        // Configure Elasticsearch Options
        services.Configure<ElasticsearchOptions>(options =>
        {
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            configuration.GetSection(ElasticsearchOptions.SectionName).Bind(options);
        });
        
        // Configure Elasticsearch Low Level Client
        services.AddSingleton<IElasticLowLevelClient>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var url = configuration["Services:Elasticsearch:Url"] ?? "http://localhost:9200";
            var username = configuration["Services:Elasticsearch:Username"] ?? "elastic";
            var password = configuration["Services:Elasticsearch:Password"] ?? "elastic";
            var timeoutMinutes = configuration.GetValue<int>("Services:Elasticsearch:TimeoutMinutes", 10);
            
            var settings = new ConnectionConfiguration(new Uri(url))
                .BasicAuthentication(username, password)
                .RequestTimeout(TimeSpan.FromMinutes(timeoutMinutes));
                
            return new ElasticLowLevelClient(settings);
        });
        
        // Add HttpClient for LLM service (used by HealthAggregator) with SHORT timeout for health checks
        services.AddHttpClient<ILlmService, LlmService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30); // Short timeout for health operations
        });
        
        // Add HttpClient for SearchService with reasonable timeout
        services.AddHttpClient<ISearchService, SearchService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2); // Reasonable timeout for search operations
        });
        
        // Register feature services
        // ChatService now uses Kernel instead of ILlmService
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IUserChatService, UserChatService>();
        services.AddScoped<IIndexManagementService, IndexManagementService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IHealthAggregator, HealthAggregator>();
        services.AddScoped<IPluginService, PluginService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        
        return services;
    }

    public static IServiceCollection AddChatDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        // Use the same connection string as SecurityDatabase for simplicity
        var connectionString = configuration.GetConnectionString("SecurityDatabase") 
            ?? "Host=localhost;Database=rag-suite;Username=postgres;Password=postgres";
        
        services.AddDbContext<ChatDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            
            // Enable sensitive data logging in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
            }
        });

        return services;
    }
}
