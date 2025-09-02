using System.Text;
using System.Text.RegularExpressions;
using RAG.Collector.Models;

namespace RAG.Collector.Chunking;

/// <summary>
/// Basic text chunker that splits text by sentences and paragraphs
/// </summary>
public class SentenceAwareChunker : ITextChunker
{
    private static readonly string[] SentenceEndMarkers = { ".", "!", "?" };
    private static readonly string[] ParagraphSeparators = { "\n\n", "\r\n\r\n" };
    
    // Regex for sentence boundaries (handles abbreviations)
    private static readonly Regex SentenceBoundaryRegex = new(
        @"(?<=[.!?])\s+(?=[A-Z])",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public IEnumerable<string> SupportedContentTypes => new[]
    {
        "text/plain",
        "text/markdown",
        "application/json",
        "text/csv",
        "text/xml",
        "text/yaml"
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
        
        // First, try to split by paragraphs
        var paragraphs = SplitByParagraphs(content);
        
        var currentChunk = new StringBuilder();
        var currentStartIndex = 0;
        var chunkIndex = 0;

        foreach (var paragraph in paragraphs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // If paragraph alone exceeds chunk size, split it by sentences
            if (paragraph.Length > chunkSize)
            {
                // First, save current chunk if it has content
                if (currentChunk.Length > 0)
                {
                    chunks.Add(CreateChunk(
                        currentChunk.ToString().Trim(),
                        currentStartIndex,
                        currentStartIndex + currentChunk.Length,
                        chunkIndex++,
                        metadata));
                    
                    currentChunk.Clear();
                    currentStartIndex += currentChunk.Length;
                }

                // Split large paragraph by sentences
                var (sentenceChunks, newStartIndex, newChunkIndex) = SplitBySentencesInternal(
                    paragraph, chunkSize, overlap, currentStartIndex, chunkIndex, metadata);
                chunks.AddRange(sentenceChunks);
                currentStartIndex = newStartIndex;
                chunkIndex = newChunkIndex;
            }
            else
            {
                // Check if adding this paragraph would exceed chunk size
                if (currentChunk.Length + paragraph.Length + 2 > chunkSize && currentChunk.Length > 0)
                {
                    // Save current chunk and start new one with overlap
                    var chunkContent = currentChunk.ToString().Trim();
                    chunks.Add(CreateChunk(
                        chunkContent,
                        currentStartIndex,
                        currentStartIndex + chunkContent.Length,
                        chunkIndex++,
                        metadata));

                    // Create overlap
                    var overlapContent = CreateOverlap(chunkContent, overlap);
                    currentChunk.Clear();
                    currentChunk.Append(overlapContent);
                    if (overlapContent.Length > 0)
                        currentChunk.Append("\n\n");
                    
                    currentStartIndex += chunkContent.Length - overlapContent.Length;
                }

                // Add paragraph to current chunk
                if (currentChunk.Length > 0)
                    currentChunk.Append("\n\n");
                currentChunk.Append(paragraph);
            }
        }

        // Add final chunk if it has content
        if (currentChunk.Length > 0)
        {
            var finalContent = currentChunk.ToString().Trim();
            chunks.Add(CreateChunk(
                finalContent,
                currentStartIndex,
                currentStartIndex + finalContent.Length,
                chunkIndex,
                metadata));
        }

        // Update total chunks count
        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].Position.TotalChunks = chunks.Count;
        }

        return Task.FromResult<IList<TextChunk>>(chunks);
    }

    private List<string> SplitByParagraphs(string content)
    {
        var paragraphs = new List<string>();
        
        foreach (var separator in ParagraphSeparators)
        {
            if (content.Contains(separator))
            {
                return content.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(p => p.Trim())
                             .Where(p => !string.IsNullOrEmpty(p))
                             .ToList();
            }
        }

        // Fallback to single line breaks
        return content.Split('\n')
                     .Select(line => line.Trim())
                     .Where(line => !string.IsNullOrEmpty(line))
                     .ToList();
    }

    private (List<TextChunk> chunks, int newStartIndex, int newChunkIndex) SplitBySentencesInternal(
        string text,
        int chunkSize,
        int overlap,
        int startIndex,
        int chunkIndex,
        Dictionary<string, object> metadata)
    {
        var chunks = new List<TextChunk>();
        var sentences = SplitBySentences(text);
        
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
                metadata));
        }

        var finalStartIndex = chunkStartIndex + currentChunk.Length;
        return (chunks, finalStartIndex, chunkIndex);
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
            sentences = text.Split(SentenceEndMarkers, StringSplitOptions.RemoveEmptyEntries)
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
        var lastSentenceEnd = -1;

        foreach (var marker in SentenceEndMarkers)
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
                ChunkIndex = chunkIndex
            },
            Metadata = new Dictionary<string, object>(metadata),
            ContentHash = ComputeContentHash(content)
        };

        // Add chunk-specific metadata
        chunk.Metadata["chunk_size"] = content.Length;
        chunk.Metadata["chunk_index"] = chunkIndex;
        chunk.Metadata["estimated_tokens"] = chunk.EstimatedTokens;

        return chunk;
    }

    private static string ComputeContentHash(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hashBytes)[..12]; // First 12 characters for brevity
    }
}
