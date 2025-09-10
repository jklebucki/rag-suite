using Microsoft.SemanticKernel;

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

            // Get settings from appsettings.json
            var llmUrl = configuration["Services:LlmService:Url"] ?? "http://localhost:11434";
            var model = configuration["Services:LlmService:Model"] ?? "llama3.1:8b";
            // Use Chat section timeout for actual chat operations (longer timeout)
            var chatTimeoutMinutes = configuration.GetValue<int>("Chat:RequestTimeoutMinutes", 15);

            // Create HttpClient with chat-specific timeout for Ollama
            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri(llmUrl),
                Timeout = TimeSpan.FromMinutes(chatTimeoutMinutes)
            };

            // Creating kernel with configuration from appsettings
            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddOllamaChatCompletion(model, httpClient: httpClient);

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
