using Microsoft.Extensions.Logging;

namespace RAG.Collector.ContentExtractors;

/// <summary>
/// Content extractor for plain text files (.txt, .md, .csv, etc.)
/// </summary>
public class PlainTextExtractor : IContentExtractor
{
    private readonly ILogger<PlainTextExtractor> _logger;

    private static readonly string[] _supportedExtensions = { ".txt", ".md", ".csv", ".log", ".json", ".xml", ".yaml", ".yml" };

    public PlainTextExtractor(ILogger<PlainTextExtractor> logger)
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
            _logger.LogDebug("Extracting content from plain text file: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return ContentExtractionResult.Failure($"File not found: {filePath}");
            }

            // Read file content with automatic encoding detection
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);

            // Basic metadata
            var metadata = new Dictionary<string, string>();
            var fileInfo = new FileInfo(filePath);
            metadata["FileName"] = fileInfo.Name;
            metadata["FileSize"] = fileInfo.Length.ToString();
            metadata["LastModified"] = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss UTC");

            // Add content-specific metadata
            var lines = content.Split('\n');
            metadata["LineCount"] = lines.Length.ToString();
            metadata["CharacterCount"] = content.Length.ToString();
            metadata["WordCount"] = EstimateWordCount(content).ToString();

            _logger.LogDebug("Extracted {CharCount} characters from {FilePath}", content.Length, filePath);

            return ContentExtractionResult.Success(content, metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract content from plain text file: {FilePath}", filePath);
            return ContentExtractionResult.Failure($"Error reading file: {ex.Message}");
        }
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
