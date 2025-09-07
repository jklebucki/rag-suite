using Microsoft.Extensions.Logging;

namespace RAG.Collector.ContentExtractors;

/// <summary>
/// Main service for content extraction that delegates to appropriate extractors
/// </summary>
public class ContentExtractionService
{
    private readonly ILogger<ContentExtractionService> _logger;
    private readonly IEnumerable<IContentExtractor> _extractors;
    private readonly Dictionary<string, IContentExtractor> _extractorMap;

    public ContentExtractionService(ILogger<ContentExtractionService> logger, IEnumerable<IContentExtractor> extractors)
    {
        _logger = logger;
        _extractors = extractors;

        // Build a map of file extensions to extractors for quick lookup
        _extractorMap = new Dictionary<string, IContentExtractor>(StringComparer.OrdinalIgnoreCase);

        foreach (var extractor in _extractors)
        {
            foreach (var extension in extractor.SupportedExtensions)
            {
                _extractorMap[extension] = extractor;
            }
        }

        _logger.LogInformation("Content extraction service initialized with {ExtractorCount} extractors supporting {ExtensionCount} file types",
            _extractors.Count(), _extractorMap.Count);
    }

    /// <summary>
    /// Gets all supported file extensions
    /// </summary>
    public IEnumerable<string> SupportedExtensions => _extractorMap.Keys;

    /// <summary>
    /// Checks if content extraction is supported for the given file extension
    /// </summary>
    /// <param name="extension">File extension (including the dot)</param>
    /// <returns>True if extraction is supported</returns>
    public bool IsSupported(string extension)
    {
        return _extractorMap.ContainsKey(extension);
    }

    /// <summary>
    /// Extracts content from the specified file
    /// </summary>
    /// <param name="filePath">Path to the file to extract content from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extraction result containing content and metadata</returns>
    public async Task<ContentExtractionResult> ExtractContentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return ContentExtractionResult.Failure("File path cannot be null or empty");
        }

        if (!File.Exists(filePath))
        {
            return ContentExtractionResult.Failure($"File not found: {filePath}");
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (!_extractorMap.TryGetValue(extension, out var extractor))
        {
            return ContentExtractionResult.Failure($"No extractor available for file type: {extension}");
        }

        try
        {
            _logger.LogDebug("Extracting content from {FilePath} using {ExtractorType}", filePath, extractor.GetType().Name);

            var result = await extractor.ExtractAsync(filePath, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogDebug("Successfully extracted {CharCount} characters from {FilePath}",
                    result.Content.Length, filePath);
            }
            else
            {
                _logger.LogWarning("Content extraction failed for {FilePath}: {ErrorMessage}",
                    filePath, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during content extraction from {FilePath}", filePath);
            return ContentExtractionResult.Failure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the appropriate extractor for the given file extension
    /// </summary>
    /// <param name="extension">File extension (including the dot)</param>
    /// <returns>Content extractor or null if not supported</returns>
    public IContentExtractor? GetExtractor(string extension)
    {
        _extractorMap.TryGetValue(extension, out var extractor);
        return extractor;
    }

    /// <summary>
    /// Gets information about all registered extractors
    /// </summary>
    /// <returns>Dictionary mapping extractor types to their supported extensions</returns>
    public Dictionary<string, IEnumerable<string>> GetExtractorInfo()
    {
        return _extractors.ToDictionary(
            extractor => extractor.GetType().Name,
            extractor => extractor.SupportedExtensions
        );
    }
}
