using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only

namespace RAG.Orchestrator.Api.Extensions;

public static class SemanticKernelExtensions
{
    public static IServiceCollection AddSemanticKernel(this IServiceCollection services)
    {
        // Konfiguracja Semantic Kernel z Ollama
        services.AddKernel()
            .AddOllamaChatCompletion(
                modelId: "llama3.2",  // Model name in Ollama
                endpoint: new Uri("http://localhost:11434") // Default Ollama endpoint
            );

        return services;
    }
}
