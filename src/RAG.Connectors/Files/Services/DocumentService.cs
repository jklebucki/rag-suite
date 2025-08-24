using Microsoft.Extensions.Logging;
using RAG.Connectors.Files.Interfaces;
using RAG.Connectors.Files.Models;

namespace RAG.Connectors.Files.Services;

public interface IDocumentService
{
    Task<DocumentContent?> ProcessDocumentAsync(string filePath);
    Task<IEnumerable<DocumentContent>> ProcessDirectoryAsync(string directoryPath, bool recursive = true);
    bool IsDocumentSupported(string filePath);
    string[] GetSupportedExtensions();
}

public class DocumentService : IDocumentService
{
    private readonly IEnumerable<IDocumentParser> _parsers;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(IEnumerable<IDocumentParser> parsers, ILogger<DocumentService> logger)
    {
        _parsers = parsers;
        _logger = logger;
    }

    public async Task<DocumentContent?> ProcessDocumentAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            return null;
        }

        var parser = _parsers.FirstOrDefault(p => p.CanParse(filePath));
        if (parser == null)
        {
            _logger.LogWarning("No parser found for file: {FilePath}", filePath);
            return null;
        }

        try
        {
            _logger.LogInformation("Processing document: {FilePath} with {Parser}", filePath, parser.GetType().Name);
            var document = await parser.ParseAsync(filePath);
            _logger.LogInformation("Successfully processed document: {FileName}, Content length: {Length}", 
                document.FileName, document.Content.Length);
            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<IEnumerable<DocumentContent>> ProcessDirectoryAsync(string directoryPath, bool recursive = true)
    {
        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning("Directory not found: {DirectoryPath}", directoryPath);
            return Enumerable.Empty<DocumentContent>();
        }

        var documents = new List<DocumentContent>();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var supportedExtensions = GetSupportedExtensions();
        var files = Directory.GetFiles(directoryPath, "*.*", searchOption)
            .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));

        foreach (var filePath in files)
        {
            var document = await ProcessDocumentAsync(filePath);
            if (document != null)
            {
                documents.Add(document);
            }
        }

        _logger.LogInformation("Processed {Count} documents from directory: {DirectoryPath}", documents.Count, directoryPath);
        return documents;
    }

    public bool IsDocumentSupported(string filePath)
    {
        return _parsers.Any(p => p.CanParse(filePath));
    }

    public string[] GetSupportedExtensions()
    {
        return _parsers.SelectMany(p => p.SupportedExtensions).Distinct().ToArray();
    }
}
