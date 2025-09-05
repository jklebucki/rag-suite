using RAG.Orchestrator.Api.Features.Search;
using System.Text;
using System.Text.RegularExpressions;

namespace RAG.Orchestrator.Api.Features.Reconstruction;

/// <summary>
/// Service responsible for reconstructing documents from chunks while removing overlaps
/// </summary>
public class DocumentReconstructionService : IDocumentReconstructionService
{
    private readonly ILogger<DocumentReconstructionService> _logger;
    
    // Regex for detecting sentence boundaries
    private static readonly Regex SentenceBoundaryRegex = new(@"[.!?]+\s+", RegexOptions.Compiled);
    
    // Regex for detecting word boundaries
    private static readonly Regex WordBoundaryRegex = new(@"\s+", RegexOptions.Compiled);

    public DocumentReconstructionService(ILogger<DocumentReconstructionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Reconstructs a complete document from chunks, removing overlaps between adjacent chunks
    /// </summary>
    public string ReconstructDocument(IEnumerable<ChunkInfo> chunks, bool removeOverlap = true)
    {
        var sortedChunks = chunks.OrderBy(c => c.ChunkIndex).ToList();
        
        if (!sortedChunks.Any())
        {
            _logger.LogWarning("No chunks provided for reconstruction");
            return string.Empty;
        }

        if (sortedChunks.Count == 1)
        {
            // Single chunk - no overlap to remove
            return sortedChunks[0].Content?.Trim() ?? string.Empty;
        }

        if (!removeOverlap)
        {
            // Simple concatenation without overlap removal
            return string.Join("\n\n", sortedChunks.Select(c => c.Content?.Trim() ?? string.Empty));
        }

        var result = new StringBuilder();
        string? previousContent = null;

        for (int i = 0; i < sortedChunks.Count; i++)
        {
            var currentChunk = sortedChunks[i];
            var currentContent = currentChunk.Content?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(currentContent))
            {
                _logger.LogDebug("Skipping empty chunk {ChunkIndex} in document reconstruction", currentChunk.ChunkIndex);
                continue;
            }

            if (i == 0)
            {
                // First chunk - add as is
                result.Append(currentContent);
                previousContent = currentContent;
            }
            else
            {
                // Remove overlap with previous chunk
                var contentWithoutOverlap = RemoveOverlap(previousContent!, currentContent, 200);
                
                if (!string.IsNullOrEmpty(contentWithoutOverlap))
                {
                    // Add separator between chunks if the content doesn't start with punctuation
                    if (!string.IsNullOrEmpty(result.ToString()) && 
                        !contentWithoutOverlap.StartsWith('.') && 
                        !contentWithoutOverlap.StartsWith(',') && 
                        !contentWithoutOverlap.StartsWith(';') &&
                        !contentWithoutOverlap.StartsWith('!') &&
                        !contentWithoutOverlap.StartsWith('?'))
                    {
                        // Check if we need to add space or new line
                        var lastChar = result.Length > 0 ? result[result.Length - 1] : ' ';
                        if (!char.IsWhiteSpace(lastChar))
                        {
                            result.Append(' ');
                        }
                    }
                    
                    result.Append(contentWithoutOverlap);
                    previousContent = currentContent; // Keep original for next overlap detection
                }
            }
        }

        var reconstructed = result.ToString().Trim();
        
        _logger.LogDebug("Reconstructed document from {ChunkCount} chunks. Original total length: {OriginalLength}, " +
                        "Reconstructed length: {ReconstructedLength}, Overlap removed: {OverlapRemoved}", 
                        sortedChunks.Count, 
                        sortedChunks.Sum(c => c.Content?.Length ?? 0), 
                        reconstructed.Length,
                        removeOverlap);

        return reconstructed;
    }

    /// <summary>
    /// Removes overlap between two adjacent text segments using multiple strategies
    /// </summary>
    public string RemoveOverlap(string previousText, string currentText, int maxOverlapLength = 200)
    {
        if (string.IsNullOrEmpty(previousText) || string.IsNullOrEmpty(currentText))
        {
            return currentText ?? string.Empty;
        }

        // Limit the search area to improve performance
        var searchLength = Math.Min(maxOverlapLength, Math.Min(previousText.Length, currentText.Length));
        
        if (searchLength < 10)
        {
            // Too short to have meaningful overlap
            return currentText;
        }

        // Strategy 1: Find exact substring match (most reliable)
        var exactOverlap = FindExactOverlap(previousText, currentText, searchLength);
        if (exactOverlap > 0)
        {
            _logger.LogDebug("Found exact overlap of {OverlapLength} characters", exactOverlap);
            return currentText.Substring(exactOverlap);
        }

        // Strategy 2: Find overlap based on sentence boundaries
        var sentenceOverlap = FindSentenceBasedOverlap(previousText, currentText, searchLength);
        if (sentenceOverlap > 0)
        {
            _logger.LogDebug("Found sentence-based overlap of {OverlapLength} characters", sentenceOverlap);
            return currentText.Substring(sentenceOverlap);
        }

        // Strategy 3: Find overlap based on word boundaries
        var wordOverlap = FindWordBasedOverlap(previousText, currentText, searchLength);
        if (wordOverlap > 0)
        {
            _logger.LogDebug("Found word-based overlap of {OverlapLength} characters", wordOverlap);
            return currentText.Substring(wordOverlap);
        }

        // Strategy 4: Fuzzy matching for similar but not identical overlaps
        var fuzzyOverlap = FindFuzzyOverlap(previousText, currentText, searchLength);
        if (fuzzyOverlap > 0)
        {
            _logger.LogDebug("Found fuzzy overlap of {OverlapLength} characters", fuzzyOverlap);
            return currentText.Substring(fuzzyOverlap);
        }

        // No overlap detected - return original current text
        _logger.LogDebug("No overlap detected between text segments");
        return currentText;
    }

    /// <summary>
    /// Find exact substring overlap between the end of previous text and beginning of current text
    /// </summary>
    private int FindExactOverlap(string previousText, string currentText, int maxLength)
    {
        // Check for overlap starting from the longest possible match
        for (int overlapLength = Math.Min(maxLength, Math.Min(previousText.Length, currentText.Length)); 
             overlapLength >= 10; // Minimum meaningful overlap
             overlapLength--)
        {
            var previousSuffix = previousText.Substring(previousText.Length - overlapLength);
            var currentPrefix = currentText.Substring(0, overlapLength);
            
            if (string.Equals(previousSuffix, currentPrefix, StringComparison.Ordinal))
            {
                return overlapLength;
            }
        }
        
        return 0;
    }

    /// <summary>
    /// Find overlap based on sentence boundaries to maintain semantic integrity
    /// </summary>
    private int FindSentenceBasedOverlap(string previousText, string currentText, int maxLength)
    {
        // Find sentences in the overlap region of previous text
        var searchStart = Math.Max(0, previousText.Length - maxLength);
        var searchText = previousText.Substring(searchStart);
        
        var sentences = SentenceBoundaryRegex.Split(searchText)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (!sentences.Any())
            return 0;

        // Check if any complete sentences from the end of previous text appear at the start of current text
        for (int i = sentences.Count - 1; i >= 0; i--)
        {
            var sentence = sentences[i].Trim();
            if (sentence.Length < 5) continue; // Skip very short fragments
            
            if (currentText.StartsWith(sentence, StringComparison.OrdinalIgnoreCase))
            {
                return sentence.Length;
            }
            
            // Also check if sentence appears after some whitespace
            var sentencePattern = @"\s*" + Regex.Escape(sentence);
            var match = Regex.Match(currentText, sentencePattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Index < 50) // Found near the beginning
            {
                return match.Index + match.Length;
            }
        }

        return 0;
    }

    /// <summary>
    /// Find overlap based on word boundaries
    /// </summary>
    private int FindWordBasedOverlap(string previousText, string currentText, int maxLength)
    {
        // Extract words from the end of previous text
        var searchStart = Math.Max(0, previousText.Length - maxLength);
        var searchText = previousText.Substring(searchStart);
        
        var words = WordBoundaryRegex.Split(searchText)
            .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length > 2)
            .ToList();

        if (!words.Any())
            return 0;

        // Look for sequences of words that match at the beginning of current text
        for (int wordCount = Math.Min(words.Count, 10); wordCount >= 3; wordCount--)
        {
            var wordSequence = string.Join(" ", words.TakeLast(wordCount));
            
            if (currentText.StartsWith(wordSequence, StringComparison.OrdinalIgnoreCase))
            {
                return wordSequence.Length;
            }
            
            // Also check with some tolerance for whitespace differences
            var normalizedSequence = Regex.Replace(wordSequence, @"\s+", @"\s+");
            var pattern = @"^\s*" + normalizedSequence;
            var match = Regex.Match(currentText, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Length;
            }
        }

        return 0;
    }

    /// <summary>
    /// Find fuzzy overlap for cases where text might have minor differences
    /// </summary>
    private int FindFuzzyOverlap(string previousText, string currentText, int maxLength)
    {
        var searchLength = Math.Min(maxLength / 2, 100); // Limit fuzzy search to avoid performance issues
        
        for (int overlapLength = searchLength; overlapLength >= 20; overlapLength -= 5)
        {
            if (overlapLength > previousText.Length || overlapLength > currentText.Length)
                continue;
                
            var previousSuffix = previousText.Substring(previousText.Length - overlapLength);
            var currentPrefix = currentText.Substring(0, overlapLength);
            
            // Calculate similarity (simple character-based)
            var similarity = CalculateSimilarity(previousSuffix, currentPrefix);
            
            // If similarity is high enough (>80%), consider it an overlap
            if (similarity > 0.8)
            {
                return overlapLength;
            }
        }

        return 0;
    }

    /// <summary>
    /// Calculate text similarity using a simple character-based approach
    /// </summary>
    private double CalculateSimilarity(string text1, string text2)
    {
        if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            return 0.0;

        var maxLength = Math.Max(text1.Length, text2.Length);
        var minLength = Math.Min(text1.Length, text2.Length);
        
        int matches = 0;
        for (int i = 0; i < minLength; i++)
        {
            if (char.ToLowerInvariant(text1[i]) == char.ToLowerInvariant(text2[i]))
            {
                matches++;
            }
        }

        return (double)matches / maxLength;
    }
}
