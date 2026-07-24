using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RAG.DocumentProcessing.Abstractions;

namespace RAG.DocumentProcessing.Client;

public static class DocumentProcessingClientServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentProcessingClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DocumentProcessingClientOptions>()
            .Bind(configuration.GetSection(DocumentProcessingClientOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _), "Endpoint must be an absolute URI.")
            .ValidateOnStart();

        services.AddHttpClient<IDocumentProcessingClient, DocumentProcessingClient>();
        return services;
    }
}
