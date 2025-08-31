using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only

namespace RAG.Orchestrator.Api.Extensions;

public static class SemanticKernelExtensions
{
    public static IServiceCollection AddSemanticKernel(this IServiceCollection services)
    {
        // Register a factory that can access configuration
        services.AddSingleton<Func<IServiceProvider, Kernel>>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            
            // Pobierz ustawienia z appsettings.json
            var llmUrl = configuration["Services:LlmService:Url"] ?? "http://localhost:11434";
            var model = configuration["Services:LlmService:Model"] ?? "llama3.1:8b";
            
            // Tworzenie kernela z konfiguracją z appsettings
            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddOllamaChatCompletion(model, new Uri(llmUrl));
            
            return kernelBuilder.Build();
        });
        
        // Register Kernel using the factory
        services.AddScoped<Kernel>(serviceProvider =>
        {
            var kernelFactory = serviceProvider.GetRequiredService<Func<IServiceProvider, Kernel>>();
            return kernelFactory(serviceProvider);
        });

        return services;
    }
}
