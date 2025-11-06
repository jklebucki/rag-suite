using RAG.Collector.Config;
using RAG.Collector.Models;
using System.Text;
using System.Text.RegularExpressions;
using static RAG.Collector.Config.Constants;

namespace RAG.Collector.Chunking;

/// <summary>
/// PDF-aware chunker that preserves page boundaries and structure
/// </summary>
public class PdfAwareChunker : ITextChunker
{
    private static readonly Regex PageMarkerRegex = new(
        @"\[Page \d+\]",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex SentenceBoundaryRegex = new(
        @"(?<=[.!?])\s+(?=[A-Z])",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public IEnumerable<string> SupportedContentTypes => new[]
    {
        "application/pdf"
    };

    public bool CanChunk(string contentType) => SupportedContentTypes.Contains(contentType);

    public Task<IList<TextChunk>> ChunkAsync(
        string content,
        Dictionary<string, object> metadata,
        int chunkSize = 1200,
        int overlap = 200,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult<IList<TextChunk>>(new List<TextChunk>());

        var chunks = new List<TextChunk>();
        var pages = ExtractPages(content);

        var chunkIndex = 0;
        var globalStartIndex = 0;

        foreach (var page in pages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pageChunks = ChunkPage(
                page.Content,
                page.PageNumber,
                chunkSize,
                overlap,
                ref globalStartIndex,
                ref chunkIndex,
                metadata);

            chunks.AddRange(pageChunks);
        }

        // Update total chunks count
        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].Position.TotalChunks = chunks.Count;
        }

        return Task.FromResult<IList<TextChunk>>(chunks);
    }

    private List<PageContent> ExtractPages(string content)
    {
        var pages = new List<PageContent>();
        var pageMatches = PageMarkerRegex.Matches(content);

        if (pageMatches.Count == 0)
        {
            // No page markers, treat as single page
            pages.Add(new PageContent { PageNumber = 1, Content = content });
            return pages;
        }

        for (int i = 0; i < pageMatches.Count; i++)
        {
            var pageMatch = pageMatches[i];
            var pageNumberText = pageMatch.Value;
            var pageNumber = ExtractPageNumber(pageNumberText);

            var startIndex = pageMatch.Index + pageMatch.Length;
            var endIndex = i < pageMatches.Count - 1
                ? pageMatches[i + 1].Index
                : content.Length;

            var pageContent = content.Substring(startIndex, endIndex - startIndex).Trim();

            if (!string.IsNullOrWhiteSpace(pageContent))
            {
                pages.Add(new PageContent
                {
                    PageNumber = pageNumber,
                    Content = pageContent
                });
            }
        }

        return pages;
    }

    private int ExtractPageNumber(string pageMarker)
    {
        var match = Regex.Match(pageMarker, @"\d+");
        return match.Success ? int.Parse(match.Value) : 1;
    }

    private List<TextChunk> ChunkPage(
        string pageContent,
        int pageNumber,
        int chunkSize,
        int overlap,
        ref int globalStartIndex,
        ref int chunkIndex,
        Dictionary<string, object> metadata)
    {
        var chunks = new List<TextChunk>();

        if (pageContent.Length <= chunkSize)
        {
            // Page fits in single chunk
            var chunk = CreateChunk(
                pageContent,
                globalStartIndex,
                globalStartIndex + pageContent.Length,
                chunkIndex++,
                pageNumber,
                metadata);

            chunks.Add(chunk);
            globalStartIndex += pageContent.Length;
        }
        else
        {
            // Split page into multiple chunks
            var pageChunks = SplitPageContent(
                pageContent,
                pageNumber,
                chunkSize,
                overlap,
                globalStartIndex,
                ref chunkIndex,
                metadata);

            chunks.AddRange(pageChunks);
            globalStartIndex += pageContent.Length;
        }

        return chunks;
    }

    private List<TextChunk> SplitPageContent(
        string content,
        int pageNumber,
        int chunkSize,
        int overlap,
        int startIndex,
        ref int chunkIndex,
        Dictionary<string, object> metadata)
    {
        var chunks = new List<TextChunk>();
        var sentences = SplitBySentences(content);

        var currentChunk = new StringBuilder();
        var chunkStartIndex = startIndex;

        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length + 1 > chunkSize && currentChunk.Length > 0)
            {
                // Save current chunk
                var chunkContent = currentChunk.ToString().Trim();
                chunks.Add(CreateChunk(
                    chunkContent,
                    chunkStartIndex,
                    chunkStartIndex + chunkContent.Length,
                    chunkIndex++,
                    pageNumber,
                    metadata));

                // Create overlap
                var overlapContent = CreateOverlap(chunkContent, overlap);
                currentChunk.Clear();
                currentChunk.Append(overlapContent);
                if (overlapContent.Length > 0)
                    currentChunk.Append(" ");

                chunkStartIndex += chunkContent.Length - overlapContent.Length;
            }

            if (currentChunk.Length > 0)
                currentChunk.Append(" ");
            currentChunk.Append(sentence);
        }

        // Add final chunk if it has content
        if (currentChunk.Length > 0)
        {
            var finalContent = currentChunk.ToString().Trim();
            chunks.Add(CreateChunk(
                finalContent,
                chunkStartIndex,
                chunkStartIndex + finalContent.Length,
                chunkIndex++,
                pageNumber,
                metadata));
        }

        return chunks;
    }

    private List<string> SplitBySentences(string text)
    {
        var sentences = SentenceBoundaryRegex.Split(text)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        // If regex didn't work well, fallback to simple splitting
        if (sentences.Count <= 1)
        {
            var sentenceEndMarkers = new[] { ".", "!", "?" };
            sentences = text.Split(sentenceEndMarkers, StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => s.Trim())
                           .Where(s => !string.IsNullOrEmpty(s))
                           .ToList();
        }

        return sentences;
    }

    private string CreateOverlap(string content, int overlapSize)
    {
        if (content.Length <= overlapSize)
            return content;

        // Try to find a good break point (sentence end)
        var candidateText = content.Substring(content.Length - overlapSize);
        var sentenceEndMarkers = new[] { ".", "!", "?" };
        var lastSentenceEnd = -1;

        foreach (var marker in sentenceEndMarkers)
        {
            var pos = candidateText.LastIndexOf(marker);
            if (pos > lastSentenceEnd)
                lastSentenceEnd = pos;
        }

        if (lastSentenceEnd > 0)
        {
            return candidateText.Substring(lastSentenceEnd + 1).Trim();
        }

        // Fallback to word boundary
        var words = candidateText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1)
        {
            return string.Join(" ", words.Skip(1));
        }

        return candidateText;
    }

    private TextChunk CreateChunk(
        string content,
        int startIndex,
        int endIndex,
        int chunkIndex,
        int pageNumber,
        Dictionary<string, object> metadata)
    {
        var chunk = new TextChunk
        {
            Id = Guid.NewGuid().ToString(),
            Content = content,
            Position = new ChunkPosition
            {
                StartIndex = startIndex,
                EndIndex = endIndex,
                ChunkIndex = chunkIndex,
                Page = pageNumber
            },
            Metadata = new Dictionary<string, object>(metadata),
            ContentHash = ComputeContentHash(content)
        };

        // Add chunk-specific metadata
        chunk.Metadata["chunk_size"] = content.Length;
        chunk.Metadata["chunk_index"] = chunkIndex;
        chunk.Metadata["page_number"] = pageNumber;
        chunk.Metadata["estimated_tokens"] = chunk.EstimatedTokens;

        return chunk;
    }

    private static string ComputeContentHash(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hashBytes)[..ContentHashDisplayLength];
    }

    private class PageContent
    {
        public int PageNumber { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
