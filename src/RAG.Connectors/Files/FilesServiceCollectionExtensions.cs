using Microsoft.Extensions.DependencyInjection;
using RAG.Connectors.Files.Interfaces;
using RAG.Connectors.Files.Parsers;
using RAG.Connectors.Files.Services;

namespace RAG.Connectors.Files;

public static class FilesServiceCollectionExtensions
{
    public static IServiceCollection AddFilesConnector(this IServiceCollection services)
    {
        // Register document parsers
        services.AddTransient<IDocumentParser, PdfDocumentParser>();
        services.AddTransient<IDocumentParser, WordDocumentParser>();
        services.AddTransient<IDocumentParser, ExcelDocumentParser>();
        services.AddTransient<IDocumentParser, TextDocumentParser>();
        
        // Register document service
        services.AddTransient<IDocumentService, DocumentService>();
        
        return services;
    }
}
