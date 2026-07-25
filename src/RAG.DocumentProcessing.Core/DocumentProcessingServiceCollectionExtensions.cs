using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RAG.DocumentProcessing.Core;

public static class DocumentProcessingServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentProcessingCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DocumentProcessingOptions>()
            .Bind(configuration.GetSection(DocumentProcessingOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => options.MaxQueuedJobs >= options.MaxConcurrentJobs, "MaxQueuedJobs must be at least MaxConcurrentJobs.")
            .ValidateOnStart();

        services.AddSingleton<DocumentProcessingJobService>();
        services.AddSingleton<IDocumentProcessingJobService>(provider => provider.GetRequiredService<DocumentProcessingJobService>());
        services.AddHostedService<DocumentProcessingWorker>();
        return services;
    }
}
