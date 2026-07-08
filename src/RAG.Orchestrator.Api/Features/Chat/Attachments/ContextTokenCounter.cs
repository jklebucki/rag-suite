using Microsoft.ML.Tokenizers;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace RAG.Orchestrator.Api.Features.Chat.Attachments;

public class ContextTokenCounter : IContextTokenCounter
{
    private readonly ConcurrentDictionary<string, Lazy<Tokenizer?>> _tokenizers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ContextTokenCounter> _logger;

    public ContextTokenCounter(ILogger<ContextTokenCounter> logger)
    {
        _logger = logger;
    }

    public int CountTokens(string text, string? model = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        try
        {
            if (TryGetTokenizerModel(model, out var tokenizerModel))
            {
                var tokenizer = _tokenizers
                    .GetOrAdd(tokenizerModel, key => new Lazy<Tokenizer?>(() => CreateTokenizer(key)))
                    .Value;

                if (tokenizer != null)
                {
                    return tokenizer.CountTokens(text);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Tokenizer failed for model {Model}; using conservative character-based fallback.", model);
        }

        return CountFallbackTokens(text);
    }

    private static Tokenizer? CreateTokenizer(string model)
    {
        try
        {
            return TiktokenTokenizer.CreateForModel(model);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryGetTokenizerModel(string? model, out string tokenizerModel)
    {
        tokenizerModel = string.Empty;
        if (string.IsNullOrWhiteSpace(model))
        {
            return false;
        }

        var normalized = model.Trim();
        if (normalized.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("o1", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("o3", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("o4", StringComparison.OrdinalIgnoreCase))
        {
            tokenizerModel = normalized;
            return true;
        }

        return false;
    }

    private static int CountFallbackTokens(string text)
    {
        var charEstimate = Math.Ceiling(text.Length / 3.0);
        var wordCount = Regex.Matches(text, @"[\p{L}\p{N}_]+|[^\s]").Count;
        var wordEstimate = Math.Ceiling(wordCount * 1.35);
        return Math.Max(1, (int)Math.Max(charEstimate, wordEstimate));
    }
}
