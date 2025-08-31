namespace RAG.Domain.ValueObjects;

/// <summary>
/// Value object representing search query with validation
/// </summary>
public record SearchQuery
{
    public string Text { get; }
    public int MaxResults { get; }
    public double MinRelevanceThreshold { get; }
    public SearchFilters? Filters { get; }

    public SearchQuery(string text, int maxResults = 10, double minRelevanceThreshold = 0.0, SearchFilters? filters = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Search text cannot be empty", nameof(text));
        
        if (maxResults <= 0 || maxResults > 100)
            throw new ArgumentException("Max results must be between 1 and 100", nameof(maxResults));

        if (minRelevanceThreshold < 0.0 || minRelevanceThreshold > 1.0)
            throw new ArgumentException("Relevance threshold must be between 0.0 and 1.0", nameof(minRelevanceThreshold));

        Text = text.Trim();
        MaxResults = maxResults;
        MinRelevanceThreshold = minRelevanceThreshold;
        Filters = filters;
    }
}

public record SearchFilters
{
    public string? Category { get; init; }
    public string? FileType { get; init; }
    public DateTime? CreatedAfter { get; init; }
    public DateTime? CreatedBefore { get; init; }
    public List<string> Tags { get; init; } = new();
}

/// <summary>
/// Value object for embedding vectors with validation
/// </summary>
public record EmbeddingVector
{
    public float[] Values { get; }
    public int Dimension => Values.Length;

    public EmbeddingVector(float[] values)
    {
        if (values == null || values.Length == 0)
            throw new ArgumentException("Embedding values cannot be null or empty");

        if (values.Any(v => float.IsNaN(v) || float.IsInfinity(v)))
            throw new ArgumentException("Embedding values cannot contain NaN or infinity");

        Values = (float[])values.Clone();
    }

    public double CosineSimilarity(EmbeddingVector other)
    {
        if (other.Dimension != Dimension)
            throw new ArgumentException("Vector dimensions must match");

        var dotProduct = 0.0;
        var normA = 0.0;
        var normB = 0.0;

        for (int i = 0; i < Dimension; i++)
        {
            dotProduct += Values[i] * other.Values[i];
            normA += Values[i] * Values[i];
            normB += other.Values[i] * other.Values[i];
        }

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
