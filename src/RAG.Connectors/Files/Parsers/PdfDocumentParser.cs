using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;
using RAG.Connectors.Files.Interfaces;
using RAG.Connectors.Files.Models;
using System.Text;

namespace RAG.Connectors.Files.Parsers;

public class PdfDocumentParser : IDocumentParser
{
    private readonly ILogger<PdfDocumentParser> _logger;

    public string[] SupportedExtensions => new[] { ".pdf" };

    public PdfDocumentParser(ILogger<PdfDocumentParser> logger)
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
            var content = await ExtractTextFromPdfAsync(filePath);

            return new DocumentContent
            {
                Id = Guid.NewGuid().ToString(),
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                Content = content,
                FileType = "PDF",
                CreatedAt = fileInfo.CreationTimeUtc,
                ModifiedAt = fileInfo.LastWriteTimeUtc,
                FileSize = fileInfo.Length,
                Metadata = new Dictionary<string, object>
                {
                    ["parser"] = "PdfDocumentParser",
                    ["encoding"] = "UTF-8"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse PDF document: {FilePath}", filePath);
            throw;
        }
    }

    private async Task<string> ExtractTextFromPdfAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            using var pdfReader = new PdfReader(filePath);
            using var pdfDocument = new PdfDocument(pdfReader);

            var strategy = new SimpleTextExtractionStrategy();
            var text = new StringBuilder();

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                text.AppendLine(pageText);
            }

            return text.ToString();
        });
    }
}
