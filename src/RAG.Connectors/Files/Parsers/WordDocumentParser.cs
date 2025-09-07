using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using RAG.Connectors.Files.Interfaces;
using RAG.Connectors.Files.Models;
using System.Text;

namespace RAG.Connectors.Files.Parsers;

public class WordDocumentParser : IDocumentParser
{
    private readonly ILogger<WordDocumentParser> _logger;

    public string[] SupportedExtensions => new[] { ".docx", ".doc" };

    public WordDocumentParser(ILogger<WordDocumentParser> logger)
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
            var content = await ExtractTextFromWordAsync(filePath);

            return new DocumentContent
            {
                Id = Guid.NewGuid().ToString(),
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                Content = content,
                FileType = "Word Document",
                CreatedAt = fileInfo.CreationTimeUtc,
                ModifiedAt = fileInfo.LastWriteTimeUtc,
                FileSize = fileInfo.Length,
                Metadata = new Dictionary<string, object>
                {
                    ["parser"] = "WordDocumentParser",
                    ["encoding"] = "UTF-8"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Word document: {FilePath}", filePath);
            throw;
        }
    }

    private async Task<string> ExtractTextFromWordAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            var text = new StringBuilder();

            using var wordDocument = WordprocessingDocument.Open(filePath, false);
            var body = wordDocument.MainDocumentPart?.Document.Body;

            if (body != null)
            {
                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    text.AppendLine(paragraph.InnerText);
                }

                // Extract text from tables
                foreach (var table in body.Elements<Table>())
                {
                    foreach (var row in table.Elements<TableRow>())
                    {
                        var rowText = new List<string>();
                        foreach (var cell in row.Elements<TableCell>())
                        {
                            rowText.Add(cell.InnerText.Trim());
                        }
                        text.AppendLine(string.Join(" | ", rowText));
                    }
                }
            }

            return text.ToString();
        });
    }
}
