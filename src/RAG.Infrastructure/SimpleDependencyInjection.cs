using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using RAG.Infrastructure.SemanticKernel;
using RAG.Infrastructure.Persistence.Mock;
using RAG.Application.Services;
using RAG.Infrastructure.Oracle;

namespace RAG.Infrastructure;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Semantic Kernel configuration
        services.AddScoped<ISemanticKernelService, SemanticKernelService>();
        
        // Repository (Mock for now)
        services.AddScoped<IChatSessionRepository, MockChatSessionRepository>();
        
        // Application Services
        services.AddScoped<IChatService, ChatService>();
        
        // Oracle service (placeholder)
        services.AddScoped<IOracleService, OracleService>();
        
        // Semantic Kernel setup
        services.AddKernel();
        
        return services;
    }
}
