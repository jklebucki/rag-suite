using Microsoft.Extensions.Logging;
using RAG.Connectors.Files.Interfaces;
using RAG.Connectors.Files.Models;
using RAG.Shared;

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
        // Normalize the path for cross-platform compatibility
        var normalizedPath = PathHelper.NormalizePath(filePath);
        
        if (!File.Exists(normalizedPath))
        {
            _logger.LogWarning("File not found: {FilePath}", normalizedPath);
            return null;
        }

        var parser = _parsers.FirstOrDefault(p => p.CanParse(normalizedPath));
        if (parser == null)
        {
            _logger.LogWarning("No parser found for file: {FilePath}", normalizedPath);
            return null;
        }

        try
        {
            _logger.LogInformation("Processing document: {FilePath} with {Parser} on {OS}", 
                normalizedPath, parser.GetType().Name, PathHelper.GetOSName());
            var document = await parser.ParseAsync(normalizedPath);
            _logger.LogInformation("Successfully processed document: {FileName}, Content length: {Length}", 
                document.FileName, document.Content.Length);
            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document: {FilePath}", normalizedPath);
            return null;
        }
    }

    public async Task<IEnumerable<DocumentContent>> ProcessDirectoryAsync(string directoryPath, bool recursive = true)
    {
        // Normalize the path for cross-platform compatibility
        var normalizedPath = PathHelper.NormalizePath(directoryPath);
        
        if (!Directory.Exists(normalizedPath))
        {
            _logger.LogWarning("Directory not found: {DirectoryPath}", normalizedPath);
            return Enumerable.Empty<DocumentContent>();
        }

        var documents = new List<DocumentContent>();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var supportedExtensions = GetSupportedExtensions();
        var files = Directory.GetFiles(normalizedPath, "*.*", searchOption)
            .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));

        foreach (var filePath in files)
        {
            var document = await ProcessDocumentAsync(filePath);
            if (document != null)
            {
                documents.Add(document);
            }
        }

        _logger.LogInformation("Processed {Count} documents from directory: {DirectoryPath} on {OS}", 
            documents.Count, normalizedPath, PathHelper.GetOSName());
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
