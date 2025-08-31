namespace RAG.Orchestrator.Api.Models;

/// <summary>
/// Enhanced chat request supporting multiple languages
/// </summary>
public class MultilingualChatRequest
{
    /// <summary>
    /// User's message/query
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Session identifier for conversation context
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// Language code for the user's message (detected if not provided)
    /// </summary>
    public string? Language { get; set; }
    
    /// <summary>
    /// Preferred response language (defaults to message language if not specified)
    /// </summary>
    public string? ResponseLanguage { get; set; }
    
    /// <summary>
    /// Whether to enable automatic translation of responses
    /// </summary>
    public bool EnableTranslation { get; set; } = true;
    
    /// <summary>
    /// Additional context or metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Enhanced chat response with language information
/// </summary>
public class MultilingualChatResponse
{
    /// <summary>
    /// Generated response text
    /// </summary>
    public string Response { get; set; } = string.Empty;
    
    /// <summary>
    /// Session identifier
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// Detected language of the original message
    /// </summary>
    public string DetectedLanguage { get; set; } = "en";
    
    /// <summary>
    /// Language of the response
    /// </summary>
    public string ResponseLanguage { get; set; } = "en";
    
    /// <summary>
    /// Whether the response was translated
    /// </summary>
    public bool WasTranslated { get; set; }
    
    /// <summary>
    /// Translation confidence if applicable
    /// </summary>
    public double? TranslationConfidence { get; set; }
    
    /// <summary>
    /// Sources used to generate the response
    /// </summary>
    public List<string>? Sources { get; set; }
    
    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }
    
    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Enhanced search request with language support
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// Search query
    /// </summary>
    public string Query { get; set; } = string.Empty;
    
    /// <summary>
    /// Language of the query (detected if not provided)
    /// </summary>
    public string? Language { get; set; }
    
    /// <summary>
    /// Preferred language for search results
    /// </summary>
    public string? ResultLanguage { get; set; }
    
    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int MaxResults { get; set; } = 10;
    
    /// <summary>
    /// Whether to enable cross-language search
    /// </summary>
    public bool EnableCrossLanguageSearch { get; set; } = true;
    
    /// <summary>
    /// Additional search filters
    /// </summary>
    public Dictionary<string, object>? Filters { get; set; }
}

/// <summary>
/// Enhanced search response with language metadata
/// </summary>
public class SearchResponse
{
    /// <summary>
    /// Search results
    /// </summary>
    public List<SearchResult> Results { get; set; } = new();
    
    /// <summary>
    /// Detected language of the query
    /// </summary>
    public string DetectedLanguage { get; set; } = "en";
    
    /// <summary>
    /// Language of the results
    /// </summary>
    public string ResultLanguage { get; set; } = "en";
    
    /// <summary>
    /// Total number of results found
    /// </summary>
    public int TotalResults { get; set; }
    
    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }
    
    /// <summary>
    /// Whether cross-language search was performed
    /// </summary>
    public bool UsedCrossLanguageSearch { get; set; }
}

/// <summary>
/// Individual search result with language information
/// </summary>
public class SearchResult
{
    /// <summary>
    /// Document title
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Document content or excerpt
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Relevance score
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// Source document identifier
    /// </summary>
    public string? SourceId { get; set; }
    
    /// <summary>
    /// Original language of the document
    /// </summary>
    public string? OriginalLanguage { get; set; }
    
    /// <summary>
    /// Whether the content was translated
    /// </summary>
    public bool WasTranslated { get; set; }
    
    /// <summary>
    /// Translation confidence if applicable
    /// </summary>
    public double? TranslationConfidence { get; set; }
    
    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
