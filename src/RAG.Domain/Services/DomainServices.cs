using RAG.Domain.Entities;
using RAG.Domain.ValueObjects;

namespace RAG.Domain.Services;

/// <summary>
/// Domain service for chat-related business logic
/// </summary>
public interface IChatDomainService
{
    /// <summary>
    /// Validates if a message can be added to the session
    /// </summary>
    bool CanAddMessage(ChatSession session, string content, string role);
    
    /// <summary>
    /// Calculates session activity score
    /// </summary>
    double CalculateSessionActivityScore(ChatSession session);
    
    /// <summary>
    /// Determines if session should be archived
    /// </summary>
    bool ShouldArchiveSession(ChatSession session, TimeSpan inactivityThreshold);
}

public class ChatDomainService : IChatDomainService
{
    private const int MaxMessagesPerSession = 100;
    private const int MaxMessageLength = 2000;

    public bool CanAddMessage(ChatSession session, string content, string role)
    {
        if (session == null)
            return false;

        if (string.IsNullOrWhiteSpace(content))
            return false;

        if (content.Length > MaxMessageLength)
            return false;

        if (session.Messages.Count >= MaxMessagesPerSession)
            return false;

        var validRoles = new[] { "user", "assistant", "system" };
        if (!validRoles.Contains(role))
            return false;

        return true;
    }

    public double CalculateSessionActivityScore(ChatSession session)
    {
        if (!session.Messages.Any())
            return 0.0;

        var messageCount = session.Messages.Count;
        var daysSinceCreated = (DateTime.UtcNow - session.CreatedAt).TotalDays;
        var daysSinceLastUpdate = (DateTime.UtcNow - session.UpdatedAt).TotalDays;

        // Activity score based on message frequency and recency
        var frequencyScore = Math.Min(1.0, messageCount / 10.0);
        var recencyScore = Math.Max(0.0, 1.0 - (daysSinceLastUpdate / 7.0));
        var longevityScore = Math.Min(1.0, daysSinceCreated / 30.0);

        return (frequencyScore * 0.4 + recencyScore * 0.4 + longevityScore * 0.2);
    }

    public bool ShouldArchiveSession(ChatSession session, TimeSpan inactivityThreshold)
    {
        var inactivityPeriod = DateTime.UtcNow - session.UpdatedAt;
        return inactivityPeriod > inactivityThreshold && session.Messages.Count > 0;
    }
}

/// <summary>
/// Domain service for search-related business logic
/// </summary>
public interface ISearchDomainService
{
    /// <summary>
    /// Validates search query according to business rules
    /// </summary>
    bool IsValidSearchQuery(SearchQuery query);
    
    /// <summary>
    /// Calculates relevance threshold based on query complexity
    /// </summary>
    double CalculateOptimalRelevanceThreshold(SearchQuery query);
    
    /// <summary>
    /// Determines if search results are sufficient for RAG
    /// </summary>
    bool AreSufficientForRAG(IEnumerable<SearchResultDto> results, SearchQuery query);
}

public class SearchDomainService : ISearchDomainService
{
    public bool IsValidSearchQuery(SearchQuery query)
    {
        if (query.Text.Length < 3)
            return false;

        if (query.Text.Length > 500)
            return false;

        // Check for SQL injection patterns
        var suspiciousPatterns = new[] { "DROP", "DELETE", "UPDATE", "INSERT", "EXEC", "--", "/*" };
        var upperQuery = query.Text.ToUpperInvariant();
        
        if (suspiciousPatterns.Any(pattern => upperQuery.Contains(pattern)))
            return false;

        return true;
    }

    public double CalculateOptimalRelevanceThreshold(SearchQuery query)
    {
        // Simple heuristic: longer, more specific queries can have higher thresholds
        var queryComplexity = query.Text.Split(' ').Length;
        
        return queryComplexity switch
        {
            <= 2 => 0.3,  // Short queries - lower threshold
            <= 5 => 0.5,  // Medium queries
            <= 10 => 0.7, // Long queries
            _ => 0.8      // Very long queries - high threshold
        };
    }

    public bool AreSufficientForRAG(IEnumerable<SearchResultDto> results, SearchQuery query)
    {
        var resultsList = results.ToList();
        
        if (!resultsList.Any())
            return false;

        // At least one result should meet the relevance threshold
        var highQualityResults = resultsList.Count(r => r.Score >= query.MinRelevanceThreshold);
        
        // For complex queries, we need more results
        var queryComplexity = query.Text.Split(' ').Length;
        var requiredResults = Math.Min(3, Math.Max(1, queryComplexity / 3));
        
        return highQualityResults >= requiredResults;
    }
}

// Temporary DTO - will be moved to proper layer
public record SearchResultDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public double Score { get; init; }
    public string Category { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
