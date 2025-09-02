using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace RAG.Collector.ContentExtractors;

/// <summary>
/// Content extractor for PDF files using PdfPig library
/// </summary>
public class PdfExtractor : IContentExtractor
{
    private readonly ILogger<PdfExtractor> _logger;

    private static readonly string[] _supportedExtensions = { ".pdf" };

    public PdfExtractor(ILogger<PdfExtractor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IEnumerable<string> SupportedExtensions => _supportedExtensions;

    /// <inheritdoc />
    public bool CanExtract(string extension)
    {
        return _supportedExtensions.Contains(extension.ToLowerInvariant());
    }

    /// <inheritdoc />
    public async Task<ContentExtractionResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Extracting content from PDF file: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return ContentExtractionResult.Failure($"File not found: {filePath}");
            }

            return await Task.Run(() => ExtractPdfContent(filePath), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract content from PDF file: {FilePath}", filePath);
            return ContentExtractionResult.Failure($"Error reading PDF: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts content from PDF using PdfPig
    /// </summary>
    private ContentExtractionResult ExtractPdfContent(string filePath)
    {
        using var document = PdfDocument.Open(filePath);
        
        var contentBuilder = new List<string>();
        var metadata = new Dictionary<string, string>();

        // Extract document metadata
        if (document.Information != null)
        {
            var info = document.Information;
            if (!string.IsNullOrEmpty(info.Title)) metadata["Title"] = info.Title;
            if (!string.IsNullOrEmpty(info.Author)) metadata["Author"] = info.Author;
            if (!string.IsNullOrEmpty(info.Subject)) metadata["Subject"] = info.Subject;
            if (!string.IsNullOrEmpty(info.Creator)) metadata["Creator"] = info.Creator;
            if (!string.IsNullOrEmpty(info.Producer)) metadata["Producer"] = info.Producer;
            if (!string.IsNullOrEmpty(info.CreationDate)) metadata["CreationDate"] = info.CreationDate;
            if (!string.IsNullOrEmpty(info.ModifiedDate)) metadata["ModifiedDate"] = info.ModifiedDate;
        }

        // Add file metadata
        var fileInfo = new FileInfo(filePath);
        metadata["FileName"] = fileInfo.Name;
        metadata["FileSize"] = fileInfo.Length.ToString();
        metadata["LastModified"] = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss UTC");
        metadata["PageCount"] = document.NumberOfPages.ToString();

        // Extract text from each page
        for (int pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++)
        {
            try
            {
                var page = document.GetPage(pageNumber);
                var pageText = page.Text;

                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    // Add page marker for better chunking later
                    contentBuilder.Add($"\n--- Page {pageNumber} ---\n");
                    contentBuilder.Add(pageText.Trim());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to extract text from page {PageNumber} in {FilePath}: {Error}", 
                    pageNumber, filePath, ex.Message);
                contentBuilder.Add($"\n--- Page {pageNumber} (extraction failed) ---\n");
            }
        }

        var fullContent = string.Join("\n", contentBuilder);
        
        // Add content statistics
        metadata["CharacterCount"] = fullContent.Length.ToString();
        metadata["WordCount"] = EstimateWordCount(fullContent).ToString();

        _logger.LogDebug("Extracted {CharCount} characters from {PageCount} pages in {FilePath}", 
            fullContent.Length, document.NumberOfPages, filePath);

        return ContentExtractionResult.Success(fullContent, metadata, document.NumberOfPages);
    }

    /// <summary>
    /// Estimates word count in text content
    /// </summary>
    private static int EstimateWordCount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        return content.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
