using System.Text.RegularExpressions;

namespace RAG.Orchestrator.Api.Features.Search;

/// <summary>
/// Processes and optimizes search queries for better results
/// </summary>
public interface IQueryProcessor
{
    QueryProcessingResult ProcessQuery(string query);
}

public class QueryProcessingResult
{
    public string ProcessedQuery { get; set; } = "";
    public List<string> KeyTerms { get; set; } = new();
    public QueryType Type { get; set; }
    public double KeywordWeight { get; set; } = 0.5;
    public double SemanticWeight { get; set; } = 0.5;
}

public enum QueryType
{
    Conversational,
    Keywords,
    Question,
    Mixed
}

public class QueryProcessor : IQueryProcessor
{
    private readonly ILogger<QueryProcessor> _logger;

    // Polish stop words and common greetings
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "aby", "ale", "am", "ani", "być", "bez", "bardzo", "bo", "by", "czy", "dla", "do", "gdy", "go", "i", "ich", "ile", "im", "ja", "jak", "jako", "je", "jego", "jej", "jego", "już", "lub", "ma", "może", "na", "nad", "nie", "o", "od", "po", "pod", "się", "są", "ta", "tak", "te", "to", "tu", "w", "we", "z", "za", "że",
        "cześć", "dzień", "dobry", "witaj", "witam", "hej", "hello", "hi", "dzięki", "dziękuję", "proszę", "pomocy", "pomóż", "powiedz", "pokarz", "pokaż"
    };

    private static readonly HashSet<string> QuestionWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "jak", "co", "gdzie", "kiedy", "dlaczego", "czy", "kto", "ile", "jakie", "jaki", "która", "które"
    };

    public QueryProcessor(ILogger<QueryProcessor> logger)
    {
        _logger = logger;
    }

    public QueryProcessingResult ProcessQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new QueryProcessingResult
            {
                ProcessedQuery = query,
                Type = QueryType.Keywords
            };
        }

        var result = new QueryProcessingResult();

        // Normalize query
        var normalizedQuery = NormalizeQuery(query);

        // Determine query type
        result.Type = DetermineQueryType(normalizedQuery);

        // Extract key terms
        result.KeyTerms = ExtractKeyTerms(normalizedQuery);

        // Create processed query
        result.ProcessedQuery = CreateProcessedQuery(normalizedQuery, result.KeyTerms, result.Type);

        // Set weights based on query type
        SetWeights(result);

        _logger.LogDebug("Query processed: Original='{Original}', Type={Type}, KeyTerms={KeyTerms}, Processed='{Processed}'",
            query, result.Type, string.Join(", ", result.KeyTerms), result.ProcessedQuery);

        return result;
    }

    private string NormalizeQuery(string query)
    {
        // Remove extra whitespace and normalize
        return Regex.Replace(query.Trim(), @"\s+", " ");
    }

    private QueryType DetermineQueryType(string query)
    {
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Check for conversational elements
        var hasGreeting = words.Any(w => StopWords.Contains(w) &&
            new[] { "cześć", "dzień", "dobry", "witaj", "witam", "hej", "hello", "hi" }.Contains(w.ToLower()));

        var hasQuestionWord = words.Any(w => QuestionWords.Contains(w));
        var hasQuestionMark = query.Contains('?');

        // Count meaningful words (non-stop words)
        var meaningfulWords = words.Where(w => !StopWords.Contains(w)).ToList();

        if (hasGreeting || (words.Length > meaningfulWords.Count * 2))
            return QueryType.Conversational;

        if (hasQuestionWord || hasQuestionMark)
            return QueryType.Question;

        if (meaningfulWords.Count <= 3 && words.All(w => !StopWords.Contains(w) || QuestionWords.Contains(w)))
            return QueryType.Keywords;

        return QueryType.Mixed;
    }

    private List<string> ExtractKeyTerms(string query)
    {
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Extract meaningful terms (non-stop words)
        var keyTerms = words
            .Where(w => !StopWords.Contains(w))
            .Where(w => w.Length > 2) // Skip very short words
            .Select(w => w.ToLower())
            .Distinct()
            .ToList();

        // Look for compound terms (phrases in quotes or important combinations)
        var phrases = ExtractPhrases(query);
        keyTerms.AddRange(phrases);

        return keyTerms;
    }

    private List<string> ExtractPhrases(string query)
    {
        var phrases = new List<string>();

        // Extract quoted phrases
        var quotedMatches = Regex.Matches(query, @"""([^""]*)""");
        foreach (Match match in quotedMatches)
        {
            if (match.Groups[1].Value.Length > 0)
                phrases.Add(match.Groups[1].Value.ToLower());
        }

        // Look for domain-specific compound terms
        var compoundTerms = new[]
        {
            @"raport\s+należności", @"sprawozdanie\s+finansowe", @"proces\s+logistyczny",
            @"zamówienie\s+zakupu", @"faktura\s+vat", @"dokument\s+księgowy"
        };

        foreach (var pattern in compoundTerms)
        {
            var matches = Regex.Matches(query.ToLower(), pattern);
            foreach (Match match in matches)
            {
                phrases.Add(match.Value);
            }
        }

        return phrases;
    }

    private string CreateProcessedQuery(string originalQuery, List<string> keyTerms, QueryType queryType)
    {
        switch (queryType)
        {
            case QueryType.Keywords:
                return originalQuery; // Use as-is for keyword queries

            case QueryType.Conversational:
                // For conversational queries, prioritize key terms
                return keyTerms.Any() ? string.Join(" ", keyTerms) : originalQuery;

            case QueryType.Question:
                // For questions, combine key terms but keep some context
                var questionWords = originalQuery.Split(' ')
                    .Where(w => QuestionWords.Contains(w))
                    .ToList();
                var combined = questionWords.Concat(keyTerms).Distinct();
                return string.Join(" ", combined);

            case QueryType.Mixed:
            default:
                // Balanced approach
                return keyTerms.Any() ?
                    $"{string.Join(" ", keyTerms)} {originalQuery}" :
                    originalQuery;
        }
    }

    private void SetWeights(QueryProcessingResult result)
    {
        switch (result.Type)
        {
            case QueryType.Keywords:
                result.KeywordWeight = 0.8;
                result.SemanticWeight = 0.2;
                break;

            case QueryType.Conversational:
                result.KeywordWeight = 0.3;
                result.SemanticWeight = 0.7;
                break;

            case QueryType.Question:
                result.KeywordWeight = 0.6;
                result.SemanticWeight = 0.4;
                break;

            case QueryType.Mixed:
            default:
                result.KeywordWeight = 0.5;
                result.SemanticWeight = 0.5;
                break;
        }
    }
}
