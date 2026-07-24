using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RAG.DocumentProcessing.Abstractions;

namespace RAG.DocumentProcessing.Providers.Docling;

public static class DoclingServiceCollectionExtensions
{
    public static IServiceCollection AddDoclingDocumentProcessingProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DoclingOptions>()
            .Bind(configuration.GetSection(DoclingOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _), "Endpoint must be an absolute URI.")
            .ValidateOnStart();

        services.AddHttpClient("docling", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<DoclingOptions>>().Value;
            client.BaseAddress = new Uri(options.Endpoint, UriKind.Absolute);
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        services.AddSingleton<IDocumentProcessingProvider>(provider => new DoclingDocumentProcessingProvider(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("docling"),
            provider.GetRequiredService<IOptions<DoclingOptions>>(),
            provider.GetRequiredService<ILogger<DoclingDocumentProcessingProvider>>()));
        return services;
    }
}
