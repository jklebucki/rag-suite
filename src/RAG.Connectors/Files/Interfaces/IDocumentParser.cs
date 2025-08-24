using RAG.Connectors.Files.Models;

namespace RAG.Connectors.Files.Interfaces;

public interface IDocumentParser
{
    bool CanParse(string filePath);
    Task<DocumentContent> ParseAsync(string filePath);
    string[] SupportedExtensions { get; }
}
