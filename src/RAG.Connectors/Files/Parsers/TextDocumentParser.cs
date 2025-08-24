using Microsoft.Extensions.Logging;
using RAG.Connectors.Files.Interfaces;
using RAG.Connectors.Files.Models;

namespace RAG.Connectors.Files.Parsers;

public class TextDocumentParser : IDocumentParser
{
    private readonly ILogger<TextDocumentParser> _logger;

    public string[] SupportedExtensions => new[] { ".txt", ".md", ".csv" };

    public TextDocumentParser(ILogger<TextDocumentParser> logger)
    {
        _logger = logger;
    }

    public bool CanParse(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public async Task<DocumentContent> ParseAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var content = await File.ReadAllTextAsync(filePath);
            
            return new DocumentContent
            {
                Id = Guid.NewGuid().ToString(),
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                Content = content,
                FileType = GetFileType(filePath),
                CreatedAt = fileInfo.CreationTimeUtc,
                ModifiedAt = fileInfo.LastWriteTimeUtc,
                FileSize = fileInfo.Length,
                Metadata = new Dictionary<string, object>
                {
                    ["parser"] = "TextDocumentParser",
                    ["encoding"] = "UTF-8"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse text document: {FilePath}", filePath);
            throw;
        }
    }

    private static string GetFileType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".txt" => "Text Document",
            ".md" => "Markdown Document",
            ".csv" => "CSV Document",
            _ => "Text Document"
        };
    }
}
