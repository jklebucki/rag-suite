namespace RAG.Collector.ContentExtractors;

/// <summary>
/// Utility methods for text processing
/// </summary>
public static class TextUtilities
{
    /// <summary>
    /// Estimates word count in text content
    /// </summary>
    /// <param name="content">Text content to analyze</param>
    /// <returns>Estimated word count</returns>
    public static int EstimateWordCount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        return content.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}

