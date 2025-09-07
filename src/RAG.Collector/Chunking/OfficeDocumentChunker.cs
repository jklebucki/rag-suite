using RAG.Collector.Models;
using Microsoft.Extensions.Logging;

namespace RAG.Collector.Chunking;

/// <summary>
/// Text chunker optimized for Microsoft Office documents (.docx, .xlsx, .pptx)
/// Handles structured content with paragraph and section awareness
/// </summary>
public class OfficeDocumentChunker : ITextChunker
{
    private readonly ILogger<OfficeDocumentChunker> _logger;

    public OfficeDocumentChunker(ILogger<OfficeDocumentChunker>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<OfficeDocumentChunker>.Instance;
    }

    /// <summary>
    /// Supported content types for Office documents
    /// </summary>
    public IEnumerable<string> SupportedContentTypes => new[]
    {
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",       // .xlsx
        "application/vnd.openxmlformats-officedocument.presentationml.presentation" // .pptx
    };

    /// <summary>
    /// Check if this chunker can handle the specified content type
    /// </summary>
    /// <param name="contentType">Content type to check</param>
    /// <returns>True if this chunker can handle the content type</returns>
    public bool CanChunk(string contentType)
    {
        return SupportedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Chunks Office document content with awareness of document structure
    /// </summary>
    /// <param name="content">Text content to chunk</param>
    /// <param name="metadata">Metadata for the content</param>
    /// <param name="maxChunkSize">Maximum chunk size in characters</param>
    /// <param name="overlap">Overlap between chunks in characters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of text chunks</returns>
    public async Task<IList<TextChunk>> ChunkAsync(
        string content,
        Dictionary<string, object> metadata,
        int maxChunkSize = 1200,
        int overlap = 200,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("Content is null or empty, returning empty chunk list");
            return new List<TextChunk>();
        }

        var chunks = new List<TextChunk>();

        try
        {
            // Normalize line endings and clean up content
            var normalizedContent = NormalizeContent(content);

            // Split into logical sections first (paragraphs, sections, etc.)
            var sections = SplitIntoSections(normalizedContent);

            var chunkIndex = 0;
            var previousChunkEnd = "";

            foreach (var section in sections)
            {
                if (string.IsNullOrWhiteSpace(section))
                    continue;

                // If section fits in one chunk, create it directly
                if (section.Length <= maxChunkSize)
                {
                    var chunk = CreateChunk(section, metadata, chunkIndex++, previousChunkEnd, overlap);
                    chunks.Add(chunk);
                    previousChunkEnd = GetChunkEnd(section, overlap);
                }
                else
                {
                    // Split large sections into smaller chunks
                    var sectionChunks = await ChunkLargeSection(
                        section,
                        metadata,
                        maxChunkSize,
                        overlap,
                        chunkIndex,
                        previousChunkEnd,
                        cancellationToken);

                    chunks.AddRange(sectionChunks);
                    chunkIndex += sectionChunks.Count;

                    if (sectionChunks.Any())
                    {
                        var lastChunk = sectionChunks.Last();
                        previousChunkEnd = GetChunkEnd(lastChunk.Content, overlap);
                    }
                }
            }

            // Update total chunks count in metadata
            foreach (var chunk in chunks)
            {
                chunk.Position.TotalChunks = chunks.Count;
            }

            _logger.LogDebug("Successfully chunked Office document content into {ChunkCount} chunks", chunks.Count);
            return chunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error chunking Office document content");
            throw;
        }
    }

    /// <summary>
    /// Normalize content by cleaning up whitespace and formatting
    /// </summary>
    private string NormalizeContent(string content)
    {
        // Replace multiple whitespace characters with single spaces
        content = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ");

        // Normalize line endings
        content = content.Replace("\r\n", "\n").Replace("\r", "\n");

        // Clean up excessive newlines but preserve paragraph structure
        content = System.Text.RegularExpressions.Regex.Replace(content, @"\n\s*\n\s*\n+", "\n\n");

        return content.Trim();
    }

    /// <summary>
    /// Split content into logical sections (paragraphs, bullet points, etc.)
    /// </summary>
    private IEnumerable<string> SplitIntoSections(string content)
    {
        // Split by double newlines (paragraph breaks)
        var paragraphs = content.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

        var sections = new List<string>();

        foreach (var paragraph in paragraphs)
        {
            var trimmed = paragraph.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                // Further split very long paragraphs by sentence boundaries
                if (trimmed.Length > 800)
                {
                    var sentences = SplitIntoSentences(trimmed);
                    sections.AddRange(sentences);
                }
                else
                {
                    sections.Add(trimmed);
                }
            }
        }

        return sections;
    }

    /// <summary>
    /// Split text into sentences for better chunk boundaries
    /// </summary>
    private IEnumerable<string> SplitIntoSentences(string text)
    {
        // Simple sentence splitting - can be enhanced with more sophisticated NLP
        var sentences = System.Text.RegularExpressions.Regex.Split(text, @"(?<=[.!?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var sections = new List<string>();
        var currentSection = "";

        foreach (var sentence in sentences)
        {
            if (string.IsNullOrEmpty(currentSection))
            {
                currentSection = sentence;
            }
            else if (currentSection.Length + sentence.Length + 1 <= 600) // Conservative sentence grouping
            {
                currentSection += " " + sentence;
            }
            else
            {
                sections.Add(currentSection);
                currentSection = sentence;
            }
        }

        if (!string.IsNullOrEmpty(currentSection))
        {
            sections.Add(currentSection);
        }

        return sections;
    }

    /// <summary>
    /// Chunk a large section that exceeds maximum chunk size
    /// </summary>
    private Task<IList<TextChunk>> ChunkLargeSection(
        string section,
        Dictionary<string, object> metadata,
        int maxChunkSize,
        int overlap,
        int startIndex,
        string previousChunkEnd,
        CancellationToken cancellationToken)
    {
        var chunks = new List<TextChunk>();
        var currentPosition = 0;
        var chunkIndex = startIndex;
        var currentPreviousEnd = previousChunkEnd;

        while (currentPosition < section.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var remainingLength = section.Length - currentPosition;
            var chunkSize = Math.Min(maxChunkSize, remainingLength);

            // Try to find a good break point (sentence end, word boundary)
            if (chunkSize < remainingLength)
            {
                var breakPoint = FindGoodBreakPoint(section, currentPosition, chunkSize);
                chunkSize = breakPoint - currentPosition;
            }

            // Include overlap from previous chunk
            var startPos = Math.Max(0, currentPosition - (string.IsNullOrEmpty(currentPreviousEnd) ? 0 : Math.Min(overlap, currentPosition)));
            var actualChunkSize = chunkSize + (currentPosition - startPos);

            var chunkContent = section.Substring(startPos, actualChunkSize);

            var chunk = CreateChunk(chunkContent, metadata, chunkIndex++, currentPreviousEnd, overlap);
            chunks.Add(chunk);

            currentPreviousEnd = GetChunkEnd(chunkContent, overlap);
            currentPosition += chunkSize;
        }

        return Task.FromResult<IList<TextChunk>>(chunks);
    }

    /// <summary>
    /// Find a good break point for chunking (prefer sentence or word boundaries)
    /// </summary>
    private int FindGoodBreakPoint(string text, int startPosition, int idealLength)
    {
        var endPosition = startPosition + idealLength;

        if (endPosition >= text.Length)
            return text.Length;

        // Look for sentence ending within last 20% of chunk
        var searchStart = startPosition + (int)(idealLength * 0.8);
        for (var i = endPosition; i >= searchStart; i--)
        {
            if (i < text.Length && (text[i] == '.' || text[i] == '!' || text[i] == '?'))
            {
                // Move past the punctuation and any following whitespace
                var nextPos = i + 1;
                while (nextPos < text.Length && char.IsWhiteSpace(text[nextPos]))
                    nextPos++;
                return nextPos;
            }
        }

        // Look for word boundary
        for (var i = endPosition; i >= searchStart; i--)
        {
            if (i < text.Length && char.IsWhiteSpace(text[i]))
            {
                return i + 1;
            }
        }

        // Fallback to ideal length
        return endPosition;
    }

    /// <summary>
    /// Create a text chunk with proper metadata
    /// </summary>
    private TextChunk CreateChunk(
        string content,
        Dictionary<string, object> metadata,
        int chunkIndex,
        string previousChunkEnd,
        int overlap)
    {
        // Add overlap from previous chunk if available
        var finalContent = content;
        if (!string.IsNullOrEmpty(previousChunkEnd) && !content.StartsWith(previousChunkEnd))
        {
            finalContent = previousChunkEnd + " " + content;
        }

        var chunk = new TextChunk
        {
            Id = Guid.NewGuid().ToString(),
            Content = finalContent,
            Position = new ChunkPosition
            {
                ChunkIndex = chunkIndex,
                StartIndex = 0, // Will be set by calling service if needed
                EndIndex = finalContent.Length,
                TotalChunks = 1 // Will be updated when all chunks are created
            },
            Metadata = new Dictionary<string, object>(metadata)
            {
                ["chunk_type"] = "office_document",
                ["chunk_method"] = "section_aware",
                ["overlap_size"] = overlap,
                ["created_at"] = DateTime.UtcNow
            }
        };

        return chunk;
    }

    /// <summary>
    /// Get the end portion of a chunk for overlap with next chunk
    /// </summary>
    private string GetChunkEnd(string content, int overlapSize)
    {
        if (content.Length <= overlapSize)
            return content;

        var endPortion = content.Substring(content.Length - overlapSize);

        // Try to start at a word boundary
        var spaceIndex = endPortion.IndexOf(' ');
        if (spaceIndex > 0 && spaceIndex < overlapSize / 2)
        {
            endPortion = endPortion.Substring(spaceIndex + 1);
        }

        return endPortion;
    }
}
